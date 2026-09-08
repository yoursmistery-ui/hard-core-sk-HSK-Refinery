// 特性拓展modHSK —— 引擎主体(v5): 低频 GameComponent + 双路径授予
// v5 (2026-09-05) 用户定案: "条件和文案要自己写, 还要多加一点特性生成的逻辑"。
//   · 路径A 行为沉淀: HSKBehaviorHook 行为计数(EndCurrentJob/Kill/精神崩溃), 手写规则表驱动;
//   · 路径B 技能提炼: 本组件每 checkIntervalDays 掷一次骰, 从环境内全量 TraitDef
//     (commonality 加权 + 最强技能耦合, HSK_* 白名单)中抽取;
//   · 两条路径共用同一组节奏门(开局门/入队门/个人冷却/容量/年度上限)与信件体系(HSKLetterLite)。
//   · 性能(AGENTS.md §9): 组件只在日界做事; 行为钩子只对玩家可控殖民者计数; 达标才掷骰。
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    // 每个小人的沉淀记录(沿用旧类名, 旧存档数据可部分读回)
    public class HSKPawnEntry : IExposable
    {
        public string pawnId;
        public int firstSeenTick = -1;
        public int lastGrantTick = int.MinValue;
        public int nextEligibleTick = int.MinValue;  // 个人冷却到期时刻
        public int dynamicCount;                     // 本引擎授予的特性数(容量判定)
        public List<string> grantedTraits = new List<string>();
        public int lastIngestTick = int.MinValue;    // 最近一次消遣类摄入(留作后续退场逻辑)
        public Dictionary<string, int> counters = new Dictionary<string, int>();   // 行为维度 -> 计数
        public Dictionary<string, int> lastVar = new Dictionary<string, int>();    // 文案槽位 -> 上次变体号

        // 行为计数缓冲: EndCurrentJob 前缀写入, 后置消费
        public string pendingJobDef = null;
        public string pendingJoyKind = null;
        public string pendingTargetDef = null;
        public int pendingHour = 0;
        public float pendingRoomTemp = 9999f;
        public bool pendingMoodHigh = false;
        public bool pendingSuccess = false;

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref firstSeenTick, "firstSeenTick", -1);
            Scribe_Values.Look(ref lastGrantTick, "lastGrantTick", int.MinValue);
            Scribe_Values.Look(ref nextEligibleTick, "nextEligibleTickV2", int.MinValue);
            Scribe_Values.Look(ref dynamicCount, "dynamicCount", 0);
            Scribe_Values.Look(ref lastIngestTick, "lastIngestTickV5", int.MinValue);
            Scribe_Collections.Look(ref grantedTraits, "grantedTraits", LookMode.Value);
            Scribe_Collections.Look(ref counters, "counters", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref lastVar, "lastVarV3", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (grantedTraits == null) grantedTraits = new List<string>();
                if (counters == null) counters = new Dictionary<string, int>();
                if (lastVar == null) lastVar = new Dictionary<string, int>();
            }
        }

        public int GetCount(string key)
        {
            int v; return counters.TryGetValue(key, out v) ? v : 0;
        }
        public void AddCount(string key, int n)
        {
            int v = GetCount(key) + n;
            if (v <= 0) counters.Remove(key); else counters[key] = v;
        }
    }

    public class HSKTraitLedger : GameComponent
    {
        public Dictionary<string, HSKPawnEntry> entries = new Dictionary<string, HSKPawnEntry>();
        public int nextCheckTick = int.MinValue;
        public int quotaYearIndex = -1;
        public int grantsThisYear = 0;

        public const int TICKS_PER_DAY = 60000;
        public const int TICKS_PER_YEAR = 60000 * 365;

        // 本环境 HSK 重编译版 Assembly-CSharp 的 Game.FillComponents() 按 (Game) 签名反射创建,
        // 必须提供 public 构造(v2 实测结论)。
        public HSKTraitLedger(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref entries, "entries", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref nextCheckTick, "nextCheckTickV4", int.MinValue);
            Scribe_Values.Look(ref quotaYearIndex, "quotaYearIndexV2", -1);
            Scribe_Values.Look(ref grantsThisYear, "grantsThisYearV2", 0);
            if (Scribe.mode == LoadSaveMode.LoadingVars && entries == null)
                entries = new Dictionary<string, HSKPawnEntry>();
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null) return;
            int tg = Find.TickManager.TicksGame;
            if (tg % TICKS_PER_DAY != 0) return;   // 只在日界做事

            CleanupDead();

            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null || !s.enableGrowth) return;

            // 路径B 技能提炼: 低频掷骰(路径A 由行为钩子直接驱动)
            if (s.enableSkillPath)
            {
                if (nextCheckTick == int.MinValue)
                    nextCheckTick = tg + s.checkIntervalDays * TICKS_PER_DAY;
                if (tg >= nextCheckTick)
                {
                    nextCheckTick = tg + s.checkIntervalDays * TICKS_PER_DAY;
                    TrySkillCheck(tg);
                }
            }
        }

        // ---- 路径B: 技能提炼 ----
        private void TrySkillCheck(int tg)
        {
            HSKTraitSetting s = HSKTraitMod.settings;
            if (tg < (long)s.colonyStartGateDays * TICKS_PER_DAY) return;
            RolloverYearIfNeeded(tg);
            if (s.colonyYearCap <= 0 || grantsThisYear >= s.colonyYearCap) return;

            List<Pawn> cands = EligiblePawns(tg, s);
            if (cands.Count == 0) return;
            // 用户定案(2026-09-05): 技能提炼每年总概率 5%(按检查间隔折算成单次概率), 且最强技能必须满级
            float chance = s.skillYearlyChance * ((float)s.checkIntervalDays / 365f);
            if (!Rand.Chance(chance)) return;

            Pawn pawn = cands[Rand.Range(0, cands.Count)];
            string bestSkill = BestSkill(pawn);
            TraitDef pick = PickTrait(pawn, bestSkill);
            if (pick == null) return;

            HSKGrowth.GrantWithReason(pawn, pick.defName, 0, "技能提炼", false, 0);
        }

        private List<Pawn> EligiblePawns(int tg, HSKTraitSetting s)
        {
            List<Pawn> list = new List<Pawn>();
            List<Pawn> all = PawnsFinder.AllMaps_FreeColonists;
            if (all == null) return list;
            for (int i = 0; i < all.Count; i++)
            {
                Pawn p = all[i];
                if (p == null || p.Dead || p.Destroyed) continue;
                if (p.story == null || p.story.traits == null) continue;
                HSKPawnEntry e = EntryFor(p);
                if (!HSKGrowth.CanAcquireNow(p, e, false)) continue;
                if (BestSkillLevel(p) < 20) continue;   // 满级门槛: 最强技能必须练到 20
                list.Add(p);
            }
            return list;
        }

        // ---- 特性池: 环境内全量 TraitDef, commonality 加权 + 技能耦合 ----
        private TraitDef PickTrait(Pawn pawn, string bestSkill)
        {
            string bestDomain = HSKData.DomainOf(bestSkill);

            List<TraitDef> pool = new List<TraitDef>();
            List<float> weights = new List<float>();
            List<TraitDef> defs = DefDatabase<TraitDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                TraitDef d = defs[i];
                if (d == null || d.defName == null) continue;

                bool hsk = d.defName.StartsWith("HSK_");
                float w;
                if (hsk)
                {
                    w = 1f;  // 白名单: commonality=0 的引擎专属特性仍可沉淀
                }
                else
                {
                    float c;
                    try { c = d.GetGenderSpecificCommonality(pawn.gender); }
                    catch { c = d.GetGenderSpecificCommonality(Gender.Male); }
                    if (c <= 0f) continue;   // 生成期不可出现的特性不进池
                    w = c;
                    if (w > 1.5f) w = 1.5f;
                    if (w < 0.25f) w = 0.25f;
                }

                if (d.requiredWorkTags != WorkTags.None && pawn.WorkTagIsDisabled(d.requiredWorkTags)) continue;
                if (pawn.story.traits.HasTrait(d)) continue;
                if (HSKGrowth.ConflictsWithAny(pawn, d)) continue;

                // 技能耦合: 与最强技能同域 ×1.5, degreeData 直接加成该技能 ×2
                string domain = HSKData.DomainHint(d.defName) ?? HSKData.DomainOf(DomainSkillOf(d));
                if (domain != null && domain == bestDomain) w *= 1.5f;
                if (domain != null && HasSkillGain(d, bestSkill)) w *= 2f;

                pool.Add(d);
                weights.Add(w);
            }
            if (pool.Count == 0) return null;

            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += weights[i];
            float roll = Rand.Value * total;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f) return pool[i];
            }
            return pool[pool.Count - 1];
        }

        private static string DomainSkillOf(TraitDef d)
        {
            if (d.degreeDatas == null) return null;
            for (int i = 0; i < d.degreeDatas.Count; i++)
            {
                TraitDegreeData dd = d.degreeDatas[i];
                if (dd == null || dd.skillGains == null) continue;
                for (int j = 0; j < dd.skillGains.Count; j++)
                {
                    SkillGain sg = dd.skillGains[j];
                    if (sg != null && sg.skill != null && sg.amount > 0) return sg.skill.defName;
                }
            }
            return null;
        }

        private static bool HasSkillGain(TraitDef d, string skillName)
        {
            if (skillName == null || d.degreeDatas == null) return false;
            for (int i = 0; i < d.degreeDatas.Count; i++)
            {
                TraitDegreeData dd = d.degreeDatas[i];
                if (dd == null || dd.skillGains == null) continue;
                for (int j = 0; j < dd.skillGains.Count; j++)
                {
                    SkillGain sg = dd.skillGains[j];
                    if (sg != null && sg.skill != null && sg.skill.defName == skillName && sg.amount > 0) return true;
                }
            }
            return false;
        }

        private static string BestSkill(Pawn pawn)
        {
            if (pawn == null || pawn.skills == null || pawn.skills.skills == null) return "Intellectual";
            string best = "Intellectual"; int bestLevel = -1;
            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                SkillRecord sr = pawn.skills.skills[i];
                if (sr == null || sr.def == null) continue;
                if (sr.Level > bestLevel) { bestLevel = sr.Level; best = sr.def.defName; }
            }
            return best;
        }

        private static int BestSkillLevel(Pawn pawn)
        {
            if (pawn == null || pawn.skills == null || pawn.skills.skills == null) return -1;
            int bestLevel = -1;
            for (int i = 0; i < pawn.skills.skills.Count; i++)
            {
                SkillRecord sr = pawn.skills.skills[i];
                if (sr == null || sr.def == null) continue;
                if (sr.Level > bestLevel) bestLevel = sr.Level;
            }
            return bestLevel;
        }

        private void RolloverYearIfNeeded(int tg)
        {
            int year = tg / TICKS_PER_YEAR;
            if (year != quotaYearIndex)
            {
                quotaYearIndex = year;
                grantsThisYear = 0;
            }
        }

        // 供 HSKGrowth.CanAcquireNow 使用
        public void RolloverPublic(int tg) { RolloverYearIfNeeded(tg); }

        public HSKPawnEntry EntryFor(Pawn p)
        {
            if (p == null) return null;
            string id = p.GetUniqueLoadID();
            HSKPawnEntry e;
            if (entries.TryGetValue(id, out e)) { if (e.pawnId == null) e.pawnId = id; return e; }
            e = new HSKPawnEntry { pawnId = id, firstSeenTick = Find.TickManager != null ? Find.TickManager.TicksGame : -1 };
            entries[id] = e;
            return e;
        }

        // 只查不改
        public HSKPawnEntry PeekEntry(Pawn p)
        {
            if (p == null || entries == null) return null;
            HSKPawnEntry e;
            entries.TryGetValue(p.GetUniqueLoadID(), out e);
            return e;
        }

        // 日界低频清理
        private void CleanupDead()
        {
            if (entries == null || entries.Count == 0) return;
            List<string> dead = null;
            foreach (KeyValuePair<string, HSKPawnEntry> kv in entries)
            {
                if (FindPawn(kv.Key) != null) continue;
                if (dead == null) dead = new List<string>();
                dead.Add(kv.Key);
            }
            if (dead != null) for (int i = 0; i < dead.Count; i++) entries.Remove(dead[i]);
        }

        private static Pawn FindPawn(string loadId)
        {
            Game g = Current.Game;
            if (g == null || g.Maps == null) return null;
            for (int m = 0; m < g.Maps.Count; m++)
            {
                Map map = g.Maps[m];
                if (map == null || map.mapPawns == null) continue;
                List<Pawn> all = map.mapPawns.AllPawns;
                for (int i = 0; i < all.Count; i++)
                {
                    Pawn p = all[i];
                    if (p != null && p.GetUniqueLoadID() == loadId) return p;
                }
            }
            return null;
        }
    }

    public static class HSKLedger
    {
        public static HSKTraitLedger Game
        {
            get { return (Current.Game != null) ? Current.Game.GetComponent<HSKTraitLedger>() : null; }
        }
    }

    // ---- 授予工具(节奏门/冲突检查/degree兜底/信件/想法) ----
    public static class HSKGrowth
    {
        // ---- 节奏门(路径A/B 共用) ----
        // 开局门 / 入队门 / 个人冷却 / 容量 / 年度上限
        public static bool CanAcquireNow(Pawn pawn, HSKPawnEntry e, bool negative)
        {
            if (e == null || pawn == null) return false;
            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null || Find.TickManager == null) return false;
            int tg = Find.TickManager.TicksGame;
            if (tg < (long)s.colonyStartGateDays * TICKS_DAY) return false;               // 开局门
            if (e.firstSeenTick < 0) return false;
            if (tg - (long)e.firstSeenTick < (long)s.pawnJoinGateDays * TICKS_DAY) return false; // 入队门
            if (e.nextEligibleTick != int.MinValue && tg < e.nextEligibleTick) return false;     // 个人冷却
            if (e.dynamicCount >= s.maxDynamicTraits) return false;                              // 容量
            HSKTraitLedger led = HSKLedger.Game;
            if (led == null) return false;
            led.RolloverPublic(tg);
            if (s.colonyYearCap <= 0 || led.grantsThisYear >= s.colonyYearCap) return false;     // 年度上限
            return true;
        }

        internal const int TICKS_DAY = 60000;

        // ---- 授予入口(两条路径统一走这里) ----
        // defName: 特性; slug: 成因维度(信件); negative: 负向维度; count: 达标计数(信件引述)
        public static bool GrantWithReason(Pawn pawn, string defName, int degree, string slug, bool negative, int count)
        {
            try
            {
                if (pawn == null || pawn.story == null || pawn.story.traits == null) return false;
                HSKTraitLedger led = HSKLedger.Game;
                if (led == null) return false;
                HSKPawnEntry e = led.EntryFor(pawn);
                if (!CanAcquireNow(pawn, e, negative)) return false;

                TraitDef td = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
                if (td == null) { Log.Warning("[HSKTraitExt] 找不到特性定义: " + defName); return false; }
                if (pawn.story.traits.HasTrait(td)) return false;
                if (ConflictsWithAny(pawn, td)) return false;

                degree = (td.degreeDatas == null || td.degreeDatas.Count == 0)
                    ? 0 : ResolveDegree(td, degree == 0 ? PawnGenerator.RandomTraitDegree(td) : degree);
                pawn.story.traits.GainTrait(new Trait(td, degree));

                HSKTraitSetting s = HSKTraitMod.settings;
                int tg = Find.TickManager.TicksGame;
                int cooldown = s.pawnCooldownDays + Rand.Range(0, s.pawnCooldownJitterDays);
                e.lastGrantTick = tg;
                e.nextEligibleTick = tg + (int)((long)cooldown * TICKS_DAY);
                e.dynamicCount++;
                if (!e.grantedTraits.Contains(defName)) e.grantedTraits.Add(defName);
                led.grantsThisYear++;

                HSKLetterLite.Send(pawn, td, slug, negative, count);
                GiveThought(pawn, "HSK_TraitShift");
                return true;
            }
            catch (Exception ex) { Log.Error("[HSKTraitExt] Grant 失败 " + defName + ": " + ex); return false; }
        }

        // degree 兜底: 1.6 光谱特质无 degree 0 时修正到最小非负档
        public static int ResolveDegree(TraitDef td, int degree)
        {
            if (td == null || td.degreeDatas == null || td.degreeDatas.Count == 0) return degree;
            for (int i = 0; i < td.degreeDatas.Count; i++)
                if (td.degreeDatas[i] != null && td.degreeDatas[i].degree == degree) return degree;
            int fallback = td.degreeDatas[0] != null ? td.degreeDatas[0].degree : degree;
            int minNonNeg = int.MaxValue; bool found = false;
            for (int i = 0; i < td.degreeDatas.Count; i++)
            {
                TraitDegreeData d = td.degreeDatas[i];
                if (d == null) continue;
                if (d.degree >= 0 && d.degree < minNonNeg) { minNonNeg = d.degree; found = true; }
            }
            return found ? minNonNeg : fallback;
        }

        public static bool ConflictsWithAny(Pawn pawn, TraitDef cand)
        {
            if (pawn == null || pawn.story == null || pawn.story.traits == null || cand == null) return false;
            List<Trait> all = pawn.story.traits.allTraits;
            for (int i = 0; i < all.Count; i++)
            {
                Trait held = all[i];
                if (held == null || held.def == null) continue;
                if (ReferenceEquals(held.def, cand)) return true;
                try
                {
                    if (held.def.ConflictsWith(cand) || cand.ConflictsWith(held.def)) return true;
                }
                catch { /* 个别 mod 特性无冲突表, 忽略 */ }
            }
            return false;
        }

        // Trait Rarity Colors 会把稀有度色串写进 degreeData.label, 取染色并加灰框突出
        public static string Highlight(Pawn pawn, TraitDef td)
        {
            string inner = null;
            try
            {
                int degree = 0;
                if (pawn != null && pawn.story != null && pawn.story.traits != null)
                {
                    List<Trait> all = pawn.story.traits.allTraits;
                    for (int i = 0; i < all.Count; i++)
                        if (all[i] != null && all[i].def == td) { degree = all[i].Degree; break; }
                }
                if (td != null && td.degreeDatas != null)
                {
                    for (int i = 0; i < td.degreeDatas.Count; i++)
                    {
                        TraitDegreeData dd = td.degreeDatas[i];
                        if (dd != null && dd.degree == degree && !dd.label.NullOrEmpty()) { inner = dd.label; break; }
                    }
                }
            }
            catch { }
            if (inner.NullOrEmpty() && td != null)
            {
                string l = (string)td.LabelCap;
                inner = string.IsNullOrEmpty(l) ? td.defName : l;
            }
            return "<color=#a8a8a8>【" + inner + "】</color>";
        }

        public static void GiveThought(Pawn pawn, string thoughtDefName)
        {
            try
            {
                ThoughtDef th = DefDatabase<ThoughtDef>.GetNamedSilentFail(thoughtDefName);
                if (th == null || pawn == null || pawn.needs == null || pawn.needs.mood == null) return;
                pawn.needs.mood.thoughts.memories.TryGainMemory(th, null);
            }
            catch (Exception ex) { Log.Error("[HSKTraitExt] 想法注入失败 " + thoughtDefName + ": " + ex); }
        }
    }
}

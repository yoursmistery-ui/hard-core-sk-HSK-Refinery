// 特性拓展modHSK —— 路径A 行为沉淀: 行为计数挂号 (v5 重写)
// 拦截: Pawn_JobTracker.EndCurrentJob(前缀取结工上下文) + Pawn.Kill(击杀计数)
//       + MentalStateHandler.TryStartMentalState(精神崩溃计数)
//
// 热路径纪律(AGENTS.md §9):
//   · 只统计玩家可控殖民者(Countable 早短路), 其余零开销返回;
//   · 规则按 TriggerType 预分桶, 每次收工只遍历同类规则; 复用 HashSet 缓冲;
//   · 达标才掷骰, 门控未开窗不消费计数(资格保留到窗口打开)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HSKTraitExt
{
    public static class HSKBehaviorHook
    {
        private static Dictionary<string, List<HSKData.LifestyleRule>> buckets;
        private static readonly HashSet<string> typeBuffer = new HashSet<string>();

        private static Dictionary<string, List<HSKData.LifestyleRule>> Buckets
        {
            get
            {
                if (buckets == null)
                {
                    buckets = new Dictionary<string, List<HSKData.LifestyleRule>>();
                    foreach (HSKData.LifestyleRule r in HSKData.LifestyleRules)
                    {
                        List<HSKData.LifestyleRule> l;
                        if (!buckets.TryGetValue(r.TriggerType, out l)) { l = new List<HSKData.LifestyleRule>(); buckets[r.TriggerType] = l; }
                        l.Add(r);
                    }
                }
                return buckets;
            }
        }

        public static void Apply(Harmony harmony)
        {
            MethodInfo end = FindEndCurrentJob();
            if (end != null)
            {
                harmony.Patch(end,
                    prefix: new HarmonyMethod(typeof(HSKBehaviorHook).GetMethod("JobEndPrefix", BindingFlags.Static | BindingFlags.NonPublic)),
                    postfix: new HarmonyMethod(typeof(HSKBehaviorHook).GetMethod("JobEndPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
            }
            MethodInfo kill = typeof(Pawn).GetMethod("Kill", new Type[] { typeof(DamageInfo?), typeof(Hediff) });
            if (kill != null)
                harmony.Patch(kill, postfix: new HarmonyMethod(typeof(HSKBehaviorHook).GetMethod("KillPostfix", BindingFlags.Static | BindingFlags.NonPublic)));

            MethodInfo breakStart = FindTryStartMentalState();
            if (breakStart != null)
                harmony.Patch(breakStart, postfix: new HarmonyMethod(typeof(HSKBehaviorHook).GetMethod("MentalBreakPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
        }

        private static MethodInfo FindEndCurrentJob()
        {
            string[] names = { "Verse.Pawn_JobTracker", "Verse.AI.Pawn_JobTracker" };
            foreach (string n in names)
            {
                Type t = AccessTools.TypeByName(n);
                if (t == null) continue;
                MethodInfo m = t.GetMethod("EndCurrentJob", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (m != null) return m;
            }
            return null;
        }

        // TryStartMentalState 多重载 → 显式按"返回 bool 且首参 MentalStateDef"挑主重载
        private static MethodInfo FindTryStartMentalState()
        {
            Type t = AccessTools.TypeByName("RimWorld.MentalStateHandler");
            if (t == null) t = AccessTools.TypeByName("Verse.MentalStateHandler");
            if (t == null) return null;
            MethodInfo best = null;
            foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name != "TryStartMentalState") continue;
                if (m.ReturnType != typeof(bool)) continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length == 0 || ps[0].ParameterType != typeof(MentalStateDef)) continue;
                if (best == null || ps.Length > best.GetParameters().Length) best = m;
            }
            return best;
        }

        // 热路径闸门
        private static bool Countable(Pawn pawn)
        {
            if (pawn == null || !pawn.IsColonistPlayerControlled) return false;
            if (pawn.story == null || pawn.story.traits == null) return false;
            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null || !s.enableGrowth || !s.enableBehavior) return false;
            return true;
        }

        // 前缀: 读当前即将结束的任务上下文
        private static void JobEndPrefix(object __instance)
        {
            try
            {
                if (__instance == null) return;
                Pawn pawn = GetFieldValue<Pawn>(__instance, "pawn");
                if (!Countable(pawn)) return;
                Job job = GetFieldValue<Job>(__instance, "curJob");
                if (job == null) return;
                HSKTraitLedger led = HSKLedger.Game;
                if (led == null) return;
                HSKPawnEntry e = led.EntryFor(pawn);
                if (e == null) return;
                JobDef jd = job.def;
                e.pendingJobDef = (jd != null) ? jd.defName : "";
                e.pendingJoyKind = (jd != null && jd.joyKind != null) ? jd.joyKind.defName : "";
                Thing t = job.GetTarget(TargetIndex.A).Thing;
                e.pendingTargetDef = (t != null && t.def != null) ? t.def.defName : "";
                e.pendingHour = GenLocalDate.HourInteger(pawn);
                e.pendingRoomTemp = pawn.AmbientTemperature;
                e.pendingMoodHigh = (pawn.needs != null && pawn.needs.mood != null && pawn.needs.mood.CurLevel > 0.85f);
                e.pendingSuccess = true;
            }
            catch { }
        }

        private static void JobEndPostfix(object __instance)
        {
            try
            {
                if (__instance == null) return;
                Pawn pawn = GetFieldValue<Pawn>(__instance, "pawn");
                if (!Countable(pawn)) return;
                HSKTraitLedger led = HSKLedger.Game;
                if (led == null) return;
                HSKPawnEntry e = led.EntryFor(pawn);
                if (e == null || !e.pendingSuccess) return;

                Dispatch(pawn, e);

                // 成瘾类"真实摄入"时刻(供禁毒/退场等未来逻辑用; v5 仅记录)
                string jd = e.pendingJobDef ?? "";
                if ((jd == "Ingest" || jd == "Eat") && !String.IsNullOrEmpty(DrugKey(e.pendingTargetDef ?? "")))
                    e.lastIngestTick = Find.TickManager.TicksGame;

                e.pendingSuccess = false;
                e.pendingJobDef = e.pendingJoyKind = e.pendingTargetDef = null;
                e.pendingMoodHigh = false;
            }
            catch { }
        }

        private static void KillPostfix(object __instance, DamageInfo? dinfo)
        {
            try
            {
                Pawn victim = __instance as Pawn;
                if (victim == null || victim.def == null || victim.def.race == null || !victim.def.race.Humanlike) return;
                if (!dinfo.HasValue) return;
                Pawn killer = dinfo.Value.Instigator as Pawn;
                if (!Countable(killer)) return;
                HSKTraitLedger led = HSKLedger.Game;
                if (led == null) return;
                HSKPawnEntry e = led.EntryFor(killer);
                if (e == null) return;
                IncrementAndMaybeGrant(killer, e, "嗜血", "Kill");
            }
            catch { }
        }

        private static void MentalBreakPostfix(object __instance, bool __result)
        {
            try
            {
                if (!__result) return;
                Pawn pawn = GetFieldValue<Pawn>(__instance, "pawn");
                if (!Countable(pawn)) return;
                HSKTraitLedger led = HSKLedger.Game;
                if (led == null) return;
                HSKPawnEntry e = led.EntryFor(pawn);
                if (e == null) return;
                IncrementAndMaybeGrant(pawn, e, "压力崩溃", "MentalBreak");
            }
            catch { }
        }

        // ---- 分发: 按触发类型取桶, 命中的规则各自计数 ----
        private static void Dispatch(Pawn pawn, HSKPawnEntry e)
        {
            string jd = e.pendingJobDef ?? "";
            string tg = e.pendingTargetDef ?? "";
            ActiveTypes(e, jd, tg);
            foreach (string tt in typeBuffer)
            {
                List<HSKData.LifestyleRule> rules;
                if (!Buckets.TryGetValue(tt, out rules)) continue;
                foreach (HSKData.LifestyleRule r in rules)
                {
                    if (!Match(e, r, tt, jd, tg)) continue;
                    IncrementAndMaybeGrant(pawn, e, r.Key, tt);
                }
            }
        }

        // 复用缓冲, 避免每次收工 new HashSet(调用方消费后由下一轮 Clear)
        private static void ActiveTypes(HSKPawnEntry e, string jd, string tg)
        {
            typeBuffer.Clear();
            typeBuffer.Add("Job");
            typeBuffer.Add("WorkType");
            if (e.pendingJoyKind != null && e.pendingJoyKind.Length > 0) typeBuffer.Add("JoyKind");
            if (jd.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0 || jd == "Harvest") typeBuffer.Add("Plant");
            if (jd == "Eat" || jd == "Ingest") typeBuffer.Add("Eat");
            if (jd == "ButcherCorpse") typeBuffer.Add("AbuseAnimal");
            int h = e.pendingHour;
            if (h < 6 || h >= 22) typeBuffer.Add("Night");
            else if (h >= 6 && h < 9) typeBuffer.Add("Dawn");
            if (e.pendingRoomTemp > HotTemp) typeBuffer.Add("TempHot");
            else if (e.pendingRoomTemp < ColdTemp) typeBuffer.Add("TempCold");
            if (e.pendingMoodHigh) typeBuffer.Add("MoodHigh");
        }

        private const float HotTemp = 35f;   // ℃, 高于此算"耐热作业"
        private const float ColdTemp = 5f;   // ℃, 低于此算"耐寒作业"

        private static bool Match(HSKPawnEntry e, HSKData.LifestyleRule r, string tt, string jd, string tg)
        {
            switch (tt)
            {
                case "Plant":
                    return jd.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0 || jd == "Harvest";
                case "Job":
                    return jd != "" && String.Equals(jd, r.TriggerTarget, StringComparison.OrdinalIgnoreCase);
                case "WorkType":
                    if (r.TriggerTarget == "Construction") return IsConstructionJob(jd);
                    if (r.TriggerTarget == "Art") return IsArtJob(jd);
                    return false;
                case "JoyKind":
                    return e.pendingJoyKind != null && e.pendingJoyKind != "" && String.Equals(e.pendingJoyKind, r.TriggerTarget, StringComparison.OrdinalIgnoreCase);
                case "AbuseAnimal":
                    // 屠宰"可作伴侣的物种"(petness>0)算虐待动物; 普通家畜/猎物不计。
                    return IsCompanionSpecies(tg);
                case "Drug":
                    if (jd != "Ingest" && jd != "Eat") return false;
                    return DrugKey(tg) == r.TriggerTarget;
                case "Night": case "Dawn": case "TempHot": case "TempCold": case "MoodHigh":
                    return true; // 计数已在 ActiveTypes 里按条件筛过
                default:
                    return false;
            }
        }

        private static bool IsCompanionSpecies(string tdef)
        {
            if (String.IsNullOrEmpty(tdef)) return false;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(tdef);
            if (def == null || def.race == null || !def.race.Animal) return false;
            return def.race.petness > 0f;
        }

        private static bool IsArtJob(string jd)
        {
            if (String.IsNullOrEmpty(jd)) return false;
            switch (jd)
            {
                case "BuildArtifact": case "Sculpt": case "CreateArt": return true;
                default: return false;
            }
        }

        private static bool IsConstructionJob(string jd)
        {
            if (String.IsNullOrEmpty(jd)) return false;
            switch (jd)
            {
                case "FinishFrame": case "Repair": case "FixBrokenDownBuilding":
                case "FillIn": case "DeconstructForBlueprint": case "ConstructRoof": return true;
                default: return false;
            }
        }

        private static string DrugKey(string tdef)
        {
            // 只认"消遣类成瘾药物"(Social/Hard), 医用(Medical)与非毒品一律不计。
            if (String.IsNullOrEmpty(tdef)) return null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(tdef);
            if (def == null || def.ingestible == null) return null;
            DrugCategory dc = def.ingestible.drugCategory;
            if (dc == DrugCategory.None || dc == DrugCategory.Medical) return null;
            string l = tdef.ToLowerInvariant();
            if (l.Contains("smokeleaf")) return "Smokeleaf";
            if (l.Contains("beer") || l.Contains("wort") || l.Contains("wine") || l.Contains("alcohol")) return "Alcohol";
            return "Other";
        }

        // ---- 达标后按概率授予 ----
        // 达标(计数≥阈值)时消费本次资格并掷一次骰: 通过→授予; 失败→资格作废需重新积累。
        // 门控未开窗时不消费计数。
        private static void IncrementAndMaybeGrant(Pawn pawn, HSKPawnEntry e, string key, string triggerType)
        {
            e.AddCount(key, 1);
            List<HSKData.LifestyleRule> rules;
            if (!Buckets.TryGetValue(triggerType, out rules)) return;
            HSKTraitSetting s = HSKTraitMod.settings;
            float tscale = (s != null && s.thresholdScale > 0f) ? s.thresholdScale : 1f;

            foreach (HSKData.LifestyleRule r in rules)
            {
                if (r.Key != key) continue;
                int cnt = e.GetCount(key);
                int posTh = (int)(r.PosThreshold * tscale);
                int negTh = (int)(r.NegThreshold * tscale);
                float chance = r.Chance;
                if (s != null) chance *= s.grantChanceScale;

                if (posTh > 0 && r.PosTrait != "" && cnt >= posTh)
                {
                    if (!HSKGrowth.CanAcquireNow(pawn, e, false)) return;   // 未开窗: 不消费计数
                    e.AddCount(key, -posTh);                                // 开窗即消费本次资格
                    if (chance < 1f && !Rand.Chance(chance)) return;
                    HSKGrowth.GrantWithReason(pawn, r.PosTrait, r.PosDegree, r.Key, false, cnt);
                }
                else if (negTh > 0 && r.NegTrait != "" && cnt >= negTh)
                {
                    if (!HSKGrowth.CanAcquireNow(pawn, e, true)) return;
                    e.AddCount(key, -negTh);
                    if (chance < 1f && !Rand.Chance(chance)) return;
                    HSKGrowth.GrantWithReason(pawn, r.NegTrait, r.NegDegree, r.Key, true, cnt);
                }
            }
        }

        private static T GetFieldValue<T>(object inst, string field)
        {
            FieldInfo f = inst.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) return default(T);
            object v = f.GetValue(inst);
            return (v is T) ? (T)v : default(T);
        }
    }
}

// 特性拓展modHSK —— 动态特性授予/失效引擎 + 特性冲突检查 (v2)
//
// v2 (2026-08-27) 门控链重写。用户要求: "开局满 2 年才判定 / 阈值数值全面抬高(负面特性尤甚) /
// 一个殖民地一年最多 1~2 人能获得特性 / 获得后 1~1.5 年随机冷却", 且这是附属玩法, 要低频省。
//
// 判定链 CanAcquireNow(全部满足才放行):
//   ① 开关: 对应机制在设置里启用(由调用方保证)
//   ② 开局门: 本局已进行 > colonyStartGateDays(默认 730 天 = 2 年)
//   ③ 入队门: 该小人入队满 pawnJoinGateDays(默认 365 天), 防止刚招到就凭空沉淀性格
//   ④ 个人冷却: 上次授予后满 grantCooldownDays + rand(0~grantCooldownJitterDays)(默认 365+0~183 = 1~1.5 年)
//   ⑤ 容量:   该小人动态特性数 < maxDynamicTraits
//   ⑥ 年度配额: 本年度获得特性的殖民者人数 < 按人口算的配额(1 人/年起, 每多 10 人 +1)
// 信件文案不在本文件拼字符串, 统一交给 HSKLetter(数据源 表/规则-获取文案.csv)。
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    public static class HSKTraits
    {
        // ---- 冲突检查: 候选与小人已有特性是否冲突 ----
        public static bool ConflictsWithAny(Pawn pawn, TraitDef cand)
        {
            if (pawn == null || pawn.story == null || pawn.story.traits == null || cand == null) return false;
            foreach (Trait held in pawn.story.traits.allTraits)
            {
                if (held == null || held.def == null) continue;
                if (Object.ReferenceEquals(held.def, cand)) return true;
                try
                {
                    if (held.def.ConflictsWith(cand) || cand.ConflictsWith(held.def)) return true;
                }
                catch { /* 个别 mod 特质无冲突表,忽略 */ }
            }
            return false;
        }

        // ---- 开局满 N 年? (殖民地级总门) ----
        public static bool ColonyGateOpen()
        {
            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null || Find.TickManager == null) return false;
            return Find.TickManager.TicksGame >= s.colonyStartGateDays * HSKTraitLedger.TICKS_PER_DAY;
        }

        // ---- 节流/容量/配额是否允许授予 ----
        // negative = 目标为"负面特性", 额外走更严的个人冷却(再乘 negCooldownScale)
        public static bool CanAcquireNow(Pawn pawn, HSKPawnEntry e, bool negative)
        {
            if (e == null) return false;
            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null || Find.TickManager == null) return false;
            if (!ColonyGateOpen()) return false;                       // ② 开局满 2 年

            int tg = Find.TickManager.TicksGame;

            // ③ 入队满 N 年
            if (e.firstSeenTick < 0) return false;
            if (tg - e.firstSeenTick < s.pawnJoinGateDays * HSKTraitLedger.TICKS_PER_DAY) return false;

            // ④ 个人冷却(含负面特性加长)
            if (e.nextEligibleTick != int.MinValue && tg < e.nextEligibleTick) return false;
            if (negative && e.lastGrantTick != int.MinValue)
            {
                int need = (int)(s.grantCooldownDays * s.negCooldownScale);
                if (tg - e.lastGrantTick < need * HSKTraitLedger.TICKS_PER_DAY) return false;
            }

            // ⑤ 容量
            if (e.dynamicCount >= s.maxDynamicTraits) return false;

            // ⑥ 殖民地年度配额
            HSKTraitLedger led = HSKLedger.Game;
            if (led == null || !led.ColonyQuotaLeft()) return false;

            return true;
        }

        public static bool CanRevokeNow(HSKPawnEntry e)
        {
            HSKTraitSetting s = HSKTraitMod.settings;
            if (e == null || e.dynamicCount <= 0 || s == null) return false;
            if (!ColonyGateOpen() || Find.TickManager == null) return false;
            if (e.lastRevokeTick == int.MinValue)
            {
                // 从未漂移过: 至少要等开局门 + 一个完整失效间隔, 避免刚满 2 年就被削
                return Find.TickManager.TicksGame >=
                    (s.colonyStartGateDays + s.revokeIntervalDays) * HSKTraitLedger.TICKS_PER_DAY;
            }
            int tg = Find.TickManager.TicksGame;
            if (tg - e.lastRevokeTick < s.revokeIntervalDays * HSKTraitLedger.TICKS_PER_DAY) return false;
            return true;
        }

        // ---- 授予一条动态特性 ----
        // dimensionKey: 机制二的行为维度(决定成因文案); frameIndex: 机制一的叙事者外壳(0~3), -1=非机制一
        public static bool Grant(Pawn pawn, string defName, int degree, string reason, bool negative,
                                 string dimensionKey, int frameIndex, int count = 0)
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
                if (ConflictsWithAny(pawn, td)) return false;             // 冲突则拒绝(不清除已有)

                degree = ResolveDegree(td, degree);                       // 1.6 光谱特质无 degree 0,兜底修正
                pawn.story.traits.GainTrait(new Trait(td, degree));

                HSKTraitSetting s = HSKTraitMod.settings;
                int tg = Find.TickManager.TicksGame;
                int cooldown = s.grantCooldownDays + Rand.Range(0, s.grantCooldownJitterDays);
                if (negative) cooldown = (int)(cooldown * s.negCooldownScale);
                e.lastGrantTick = tg;
                e.nextEligibleTick = tg + cooldown * HSKTraitLedger.TICKS_PER_DAY;
                e.dynamicCount++;
                if (negative) e.negativeGrants++;
                if (!e.grantedTraits.Contains(defName)) e.grantedTraits.Add(defName);
                led.NoteColonyGrant();

                HSKLetter.SendGrant(pawn, td, dimensionKey, frameIndex, negative, count);
                GiveThought(pawn, "HSK_TraitShift");
                return true;
            }
            catch (Exception ex) { Log.Error("[HSKTraitExt] Grant 失败 " + defName + ": " + ex); return false; }
        }

        // ---- 兜底: 目标 degree 不存在时修正 ----
        // RimWorld 1.6 把所有光谱特质(如 DrugDesire/ShootingAccuracy/Medic/Trader/Diplomat 等)
        // 的"中性基础档"从 degree 0 平移到 degree 1(0 度已不存在)。
        // 规则表/背景池仍按旧版写 0 时,DataAtDegree(0) 会报
        // "found no data at degree 0, returning first defined" 并错误落到第一个 degree。
        private static int ResolveDegree(TraitDef td, int degree)
        {
            if (td == null || td.degreeDatas == null || td.degreeDatas.Count == 0) return degree;
            for (int i = 0; i < td.degreeDatas.Count; i++)
                if (td.degreeDatas[i] != null && td.degreeDatas[i].degree == degree) return degree;

            int fallback = td.degreeDatas[0] != null ? td.degreeDatas[0].degree : degree;
            int minNonNeg = int.MaxValue;
            bool found = false;
            for (int i = 0; i < td.degreeDatas.Count; i++)
            {
                TraitDegreeData d = td.degreeDatas[i];
                if (d == null) continue;
                if (d.degree >= 0 && d.degree < minNonNeg) { minNonNeg = d.degree; found = true; }
            }
            int use = found ? minNonNeg : fallback;
            Log.Warning("[HSKTraitExt] degree 修正: " + td.defName + " 无 degree " + degree + ", 使用 " + use);
            return use;
        }

        // ---- 失效一条动态特性(移除最早授予的那条) ----
        public static bool RevokeOne(Pawn pawn)
        {
            try
            {
                if (pawn == null || pawn.story == null || pawn.story.traits == null) return false;
                HSKTraitLedger led = HSKLedger.Game;
                if (led == null) return false;
                HSKPawnEntry e = led.PeekEntry(pawn);
                if (!CanRevokeNow(e)) return false;

                string removeDef = (e.grantedTraits != null && e.grantedTraits.Count > 0) ? e.grantedTraits[0] : null;
                Trait target = null;
                if (removeDef != null)
                    foreach (Trait t in pawn.story.traits.allTraits)
                        if (t != null && t.def != null && t.def.defName == removeDef) { target = t; break; }
                if (target == null) return false; // 只动本 mod 授予过的特性, 绝不碰生成期自带的

                pawn.story.traits.allTraits.Remove(target);
                e.grantedTraits.Remove(removeDef);
                e.lastRevokeTick = Find.TickManager.TicksGame;
                if (e.dynamicCount > 0) e.dynamicCount--;
                if (target.def != null && IsNegativeNamed(target.def.defName) && e.negativeGrants > 0) e.negativeGrants--;

                HSKLetter.SendRevoke(pawn, target.def, "drift", 0);
                GiveThought(pawn, "HSK_TraitDrift");
                return true;
            }
            catch (Exception ex) { Log.Error("[HSKTraitExt] Revoke 失败: " + ex); return false; }
        }

        // ---- 禁毒 X 天: 移除成瘾类动态特性 ----
        // 由账本每日检查调用。自 lastDrugUseTick 起满 drugAbstinenceDays(设置, 默认 730 = 2 年)
        // 天无摄入任何消遣类毒品, 则把本mod授予的成瘾特性(VTE_Lush/VTE_Stoner/DrugDesire)移除,
        // 同时清空毒品行为计数(饮酒/吸烟/药物滥用)重新积累。
        public static bool TryRevokeDrugTraitsForAbstinence(Pawn pawn, HSKPawnEntry e)
        {
            try
            {
                if (pawn == null || e == null || pawn.story == null || pawn.story.traits == null) return false;
                if (e.lastDrugUseTick == int.MinValue) return false;
                int days = (HSKTraitMod.settings != null) ? HSKTraitMod.settings.drugAbstinenceDays : 730;
                long elapsedDays = (long)((Find.TickManager.TicksGame - e.lastDrugUseTick) / (float)HSKTraitLedger.TICKS_PER_DAY);
                if (elapsedDays < days) return false;

                bool any = false;
                for (int i = pawn.story.traits.allTraits.Count - 1; i >= 0; i--)
                {
                    Trait t = pawn.story.traits.allTraits[i];
                    if (t == null || t.def == null) continue;
                    if (!IsDrugTrait(t.def.defName)) continue;
                    if (e.grantedTraits == null || !e.grantedTraits.Contains(t.def.defName)) continue; // 只动本mod授予的
                    pawn.story.traits.allTraits.RemoveAt(i);
                    e.grantedTraits.Remove(t.def.defName);
                    if (e.dynamicCount > 0) e.dynamicCount--;
                    if (e.negativeGrants > 0) e.negativeGrants--;
                    HSKLetter.SendRevoke(pawn, t.def, "abstain", (int)elapsedDays);
                    GiveThought(pawn, "HSK_TraitShift");
                    any = true;
                }

                // 无论有没有特性可移除, 都清空毒品计数并重置计时(避免每天重复检查)
                string[] drugKeys = { "饮酒", "吸烟", "药物滥用" };
                foreach (string k in drugKeys) e.AddCount(k, -e.GetCount(k));
                e.lastDrugUseTick = int.MinValue;
                return any;
            }
            catch (Exception ex) { Log.Error("[HSKTraitExt] 禁毒移除失败: " + ex); return false; }
        }

        private static bool IsDrugTrait(string defName)
        {
            switch (defName)
            {
                case "VTE_Lush": case "VTE_Stoner": case "DrugDesire": return true;
                default: return false;
            }
        }

        // 仅用于 negativeGrants 统计, 判定不依赖它
        private static bool IsNegativeNamed(string defName)
        {
            switch (defName)
            {
                case "VTE_Lush": case "VTE_Stoner": case "DrugDesire": case "VTE_WorldWeary":
                case "VTE_AnimalHater": case "VTE_Kleptomaniac": case "VTE_MadSurgeon":
                case "VTE_Coward": case "VTE_Clumsy": case "VTE_Slob": case "VTE_Dunce":
                case "Wimp": case "Claustrophobic": case "HSK_Glutton": return true;
                default: return false;
            }
        }

        // 获得/失去特性后给小人一条短期想法, 让"性格变了"这件事本人也有感觉(代入感)
        private static void GiveThought(Pawn pawn, string thoughtDefName)
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

    // 跨mod 语义冲突双向注入。
    // 冲突表中"补充语义"对涉及 Core/VTE/核SK 的 TraitDef, 本mod无法改它们自己的 XML,
    // 于是在启动时把双方 defName 双向加进对方 conflictingTraits。
    // 这样: (1) 动态授予走 ConflictsWithAny 会拦截; (2) 新创建人物走 PawnGenerator 原生互斥检查也会拦截。
    [StaticConstructorOnStartup]
    public static class HSKConflictPump
    {
        static HSKConflictPump()
        {
            try
            {
                if (HSKData.ConflictPairs == null) return;
                int injected = 0;
                foreach (var pair in HSKData.ConflictPairs)
                {
                    if (pair == null || pair.Length < 2) continue;
                    if (Bind(pair[0], pair[1])) injected++;
                }
                Log.Message("[HSKTraitExt] 已注入语义冲突对 " + injected + " / " + HSKData.ConflictPairs.Length);
            }
            catch (Exception e)
            {
                Log.Error("[HSKTraitExt] 冲突注入失败: " + e);
            }
        }

        private static bool Bind(string a, string b)
        {
            TraitDef da = def(a); TraitDef db = def(b);
            if (da == null || db == null) return false;
            AddConflict(da, b);
            AddConflict(db, a);
            return true;
        }

        private static TraitDef def(string name)
        {
            try { return DefDatabase<TraitDef>.GetNamedSilentFail(name); }
            catch { return null; }
        }

        private static void AddConflict(TraitDef dst, string other)
        {
            if (dst.conflictingTraits == null) dst.conflictingTraits = new List<TraitDef>();
            for (int i = 0; i < dst.conflictingTraits.Count; i++)
                if (dst.conflictingTraits[i] != null && dst.conflictingTraits[i].defName == other) return;
            TraitDef od = def(other);
            if (od != null) dst.conflictingTraits.Add(od);
        }
    }
}

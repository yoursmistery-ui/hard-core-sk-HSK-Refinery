// 特性拓展modHSK —— 机制一: 背景强关联 授予事件 Worker (v2)
//
// v2 变更(2026-08-27):
//   · 失效/性格漂移不再走叙事者事件, 改由 HSKTraitLedger 的每日低频检查驱动
//     (节奏 = revokeIntervalDays + 0~183 天随机, 见 HSKLedger.MaybeDriftCheck), 少一个事件源更好控。
//   · 授予候选人筛选统一走 HSKTraits.CanAcquireNow(pawn, entry, negative:false),
//     即开局 2 年门 / 入队门 / 个人 1~1.5 年冷却 / 容量 / 殖民地年度配额 五重门一起生效。
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    public static class HSKBackground
    {
        // 技能域 -> 人格域
        public static string DomainOf(string skillDefName)
        {
            switch (skillDefName)
            {
                case "Shooting": case "Melee": return "Combat";
                case "Social": case "Animals": return "Social";
                case "Medical": case "Cooking": case "Intellectual": case "Crafting": return "Work";
                case "Construction": case "Growing": case "Artistic": return "Life";
                case "Mining": return "Work";
                default: return "Life";
            }
        }
    }

    public class IncidentWorker_HSKTraitGrant : IncidentWorker
    {
        // 叙事者外壳序号(0~3), 对应 HSKTrait.Frame.{n}.Title/Body 四套文案。
        // 各外壳事件共用同一个殖民地年度配额, 只是"由头"不同, 不会因此提高授予总量。
        protected virtual int FrameIndex { get { return 0; } }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null || !s.enableMechanismOne) return false;
            if (!HSKTraits.ColonyGateOpen()) return false;      // 开局满 2 年
            HSKTraitLedger led = HSKLedger.Game;
            if (led == null || !led.ColonyQuotaLeft()) return false; // 年度配额已用完就别弹事件
            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            List<Pawn> cands = EligibleGrantCandidates();
            if (cands.Count == 0) return false;
            // 概率门: 事件窗口本身由 minRefireDays 拉开, 授予还要过 backgroundGrantChance,
            // 双重下调后背景沉淀约为每 4~5 年一次。
            if (HSKTraitMod.settings != null && !Rand.Chance(HSKTraitMod.settings.backgroundGrantChance)) return false;
            Pawn pawn = cands[Rand.Range(0, cands.Count)];
            return GrantFromBackground(pawn, FrameIndex);
        }

        protected static List<Pawn> EligibleGrantCandidates()
        {
            List<Pawn> list = new List<Pawn>();
            HSKTraitLedger led = HSKLedger.Game;
            if (led == null) return list;
            foreach (Pawn p in PawnsFinder.AllMaps_FreeColonists)
            {
                if (p == null || p.story == null || p.story.traits == null || p.Dead) continue;
                if (!HSKTraits.CanAcquireNow(p, led.EntryFor(p), false)) continue;
                list.Add(p);
            }
            return list;
        }

        protected bool GrantFromBackground(Pawn pawn, int frame)
        {
            try
            {
                string skill = BestSkill(pawn);
                string domain = HSKBackground.DomainOf(skill);

                List<HSKData.DomainRow> rows = new List<HSKData.DomainRow>();
                foreach (var r in HSKData.DomainPool)
                    if (r.Domain == domain) rows.Add(r);
                if (rows.Count == 0) return false;

                // 权重抽取(与最钦佩技能域强耦合 ×2)
                float total = 0f;
                float[] w = new float[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                {
                    w[i] = rows[i].Weight * ((rows[i].Coupling == skill) ? 2f : 1f);
                    total += w[i];
                }
                float roll = Rand.Value * total;
                float acc = 0f;
                HSKData.DomainRow pick = rows[0];
                for (int i = 0; i < rows.Count; i++) { acc += w[i]; if (roll <= acc) { pick = rows[i]; break; } }

                return HSKTraits.Grant(pawn, pick.TraitDef, 0, "背景强关联·" + skill, false, null, frame);
            }
            catch (Exception ex) { Log.Error("[HSKTraitExt] 背景授予失败: " + ex); return false; }
        }

        private static string BestSkill(Pawn pawn)
        {
            if (pawn == null || pawn.skills == null || pawn.skills.skills == null) return "Intellectual";
            string best = "Intellectual"; int bestLevel = -1;
            foreach (SkillRecord sr in pawn.skills.skills)
            {
                if (sr == null || sr.def == null) continue;
                if (sr.Level > bestLevel) { bestLevel = sr.Level; best = sr.def.defName; }
            }
            return best;
        }
    }

    // ---- 机制一的另外三套叙事者外壳(2026-08-27 v2): 同一个授予引擎, 不同的"由头"与文案 ----
    // 四套事件共用殖民地年度配额(HSKTraitLedger.grantsThisYear), 因此事件变多只代表风味变多,
    // 授予总量不会上升。

    // 「没有被浪费的天分」—— 最擅长的事长成了性格
    public class IncidentWorker_HSKTraitTalentRipening : IncidentWorker_HSKTraitGrant
    {
        protected override int FrameIndex { get { return 1; } }
    }

    // 「别人眼中的样子」—— 同侪的评价反过来塑造了本人
    public class IncidentWorker_HSKTraitSeenByOthers : IncidentWorker_HSKTraitGrant
    {
        protected override int FrameIndex { get { return 2; } }
    }

    // 「换季的时候清点」—— 季节更替时的人数清点
    public class IncidentWorker_HSKTraitSeasonCounting : IncidentWorker_HSKTraitGrant
    {
        protected override int FrameIndex { get { return 3; } }
    }
}

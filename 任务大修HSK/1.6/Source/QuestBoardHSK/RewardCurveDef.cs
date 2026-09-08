using RimWorld;
using Verse;

namespace QuestBoardHSK
{
    /// <summary>
    /// 奖励曲线表(Def 驱动，可 XML 调，不改代码)。缺省时 BountyRules 回退内置常量。
    /// tier 倍率按目标科技档；prepay* 为发单预付手续费。
    /// </summary>
    public class RewardCurveDef : Def
    {
        public float tier1Mult = 1f;
        public float tier2Mult = 1.3f;
        public float tier3Mult = 1.7f;
        public float tier4Mult = 2.2f;

        public int prepayNeolithic = 500;
        public int prepayMedieval = 1000;
        public int prepayIndustrial = 2000;
        public int prepaySpacer = 3500;
        public int prepayUltra = 5000;
        public int prepayArchotech = 5000;

        public float aboveTechCostFactor = 2f;
    }

    /// <summary>取当前曲线（多份取第一；无则 null → BountyRules 用内置默认）。</summary>
    [StaticConstructorOnStartup]
    internal static class RewardCurves
    {
        private static RewardCurveDef cached;

        public static RewardCurveDef Current
        {
            get
            {
                if (cached == null)
                {
                    System.Collections.Generic.List<RewardCurveDef> all = DefDatabase<RewardCurveDef>.AllDefsListForReading;
                    if (all != null && all.Count > 0)
                        cached = all[0];
                }
                return cached;
            }
        }
    }
}

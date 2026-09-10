using RimWorld;
using Verse;

namespace QuestBoardHSK
{
    /// <summary>
    /// 暗杀委托好感策略（并入自边缘外政HSK，原 AssassinGoodwill）：成功→加好感、失败→不扣。
    /// 好感写一律主线程 + 原版 TryAffectGoodwillWith。
    /// </summary>
    internal static class AssassinGoodwill
    {
        // 成功结算好感增量（可平衡；对"接单/委托方"派系 +）。
        public static int SuccessDelta = 8;

        /// <param name="patron">被委托/雇佣的暗杀方派系（隐藏刺客公会）。</param>
        /// <param name="success">任务是否成功。</param>
        public static void ApplyOutcome(Faction patron, bool success)
        {
            if (patron == null || patron == Faction.OfPlayer)
                return;
            if (success)
                patron.TryAffectGoodwillWith(Faction.OfPlayer, SuccessDelta, true, false, null, null);
            // 失败：按规则不扣好感。
        }
    }
}

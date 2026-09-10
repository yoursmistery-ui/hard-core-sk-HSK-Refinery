using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 信鸽柱:委托/通缉的唯一收发入口(强绑定,通讯台/公告牌右键接收已移除)。
    /// 待收>0 且柱上有饲料时右键出现「收取信鸽来件」;缺饲料显示灰置选项提示补料。
    /// </summary>
    public class Building_RK_BirdPost : Building
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(selPawn))
                yield return o;
            if (selPawn == null || !selPawn.RaceProps.Humanlike)
                yield break;
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio == null || radio.pending.Count == 0)
                yield break;
            CompRefuelable fuel = GetComp<CompRefuelable>();
            if (fuel != null && !fuel.HasFuel)
            {
                yield return new FloatMenuOption("RK_Bounty.BirdPostNoFuel".Translate(), null);
                yield break;
            }
            int n = radio.pending.Count;
            yield return new FloatMenuOption(
                "RK_Bounty.ReceiveRadio".Translate(n),
                delegate
                {
                    Job job = JobMaker.MakeJob(QB_JobDefOf.RK_Bounty_ReceiveRadio, this);
                    selPawn.jobs.TryTakeOrderedJob(job);
                },
                MenuOptionPriority.High);
        }
    }
}

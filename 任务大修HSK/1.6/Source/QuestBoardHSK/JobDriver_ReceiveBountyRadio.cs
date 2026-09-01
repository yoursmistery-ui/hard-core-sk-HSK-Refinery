using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 殖民者接收悬赏通讯:目标可以是通讯台(需通电)或悬赏公告牌(随时可用)。
    /// 走到交互格 → 操作约 8 秒 → 从待接收队列弹出一条通缉转入榜单。
    /// </summary>
    public class JobDriver_ReceiveBountyRadio : JobDriver
    {
        private const int WorkTicks = 500;

        private Building TargetBld => (Building)job.targetA.Thing;

        private bool UsableNow()
        {
            if (TargetBld is Building_CommsConsole console)
                return console.CanUseCommsNow;
            return !TargetBld.Destroyed; // 公告牌等非电力目标,未摧毁即可
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(delegate { return !UsableNow(); });
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);

            Toil receive = ToilMaker.MakeToil("ReceiveBountyRadio");
            int done = 0;
            receive.initAction = delegate
            {
                done = 0;
                receive.actor.rotationTracker.FaceCell(job.targetA.Cell);
            };
            receive.tickAction = delegate
            {
                done++;
                receive.actor.skills?.Learn(SkillDefOf.Intellectual, 0.03f);
            };
            receive.defaultCompleteMode = ToilCompleteMode.Delay;
            receive.defaultDuration = WorkTicks;
            receive.WithProgressBar(TargetIndex.A, delegate
            {
                return (float)done / WorkTicks;
            });
            receive.FailOn(() => !UsableNow());
            receive.finishActions.Add(delegate
            {
                Warrant w = BountyRadioManager.Get()?.TryCollectOne();
                if (w != null)
                {
                    RadioNotifier.Send("RK_Bounty.RadioReceivedTitle",
                        "RK_Bounty.RadioReceivedText".Translate(QuestBoardAdapters.WarrantLabel(w), w.issuer != null ? w.issuer.Name : "—"));
                }
            });
            yield return receive;
        }
    }

    /// <summary>与 Dialog_BountyBoard 共用的 Warrant 适配入口(避免 driver 依赖 UI 类)。</summary>
    public static class QuestBoardAdapters
    {
        public static string WarrantLabel(Warrant w)
        {
            switch (w)
            {
                case Warrant_Pawn wp:
                    Pawn p = wp.Pawn;
                    return p != null ? p.LabelShortCap : "…";
                case Warrant_TameAnimal wt:
                    return wt.AnimalRace != null ? wt.AnimalRace.label : "…";
                case Warrant_Artifact wa:
                    return wa.thing != null ? wa.thing.Label : "…";
                default:
                    return w.GetUniqueLoadID();
            }
        }
    }
}

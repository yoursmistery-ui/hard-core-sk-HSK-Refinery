using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    public class CompProperties_BountyBoard : CompProperties
    {
        public CompProperties_BountyBoard()
        {
            compClass = typeof(CompBountyBoard);
        }
    }

    /// <summary>
    /// 悬赏公告牌 comp:挂在鼠族公告牌(RKFC_RL_Board)上,
    /// 提供"查看悬赏榜"gizmo 打开 Dialog_BountyBoard。无任何 tick 逻辑。
    /// </summary>
    public class CompBountyBoard : ThingComp
    {
        private static Texture2D boardIcon;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (WarrantsManager.Instance == null)
                yield break;
            if (parent.Faction != Faction.OfPlayer)
                yield break;

            if (boardIcon == null)
                boardIcon = ContentFinder<Texture2D>.Get("UI/Warrants/TargetWarrants", false);

            BountyRadioManager radio = BountyRadioManager.Get();
            bool hasPending = radio != null && radio.pending.Count > 0;

            yield return new Command_Action
            {
                defaultLabel = hasPending
                    ? "RK_Bounty.ViewBoardPending".Translate(radio.pending.Count)
                    : "RK_Bounty.ViewBoard".Translate(),
                defaultDesc = "RK_Bounty.ViewBoardDesc".Translate(),
                icon = boardIcon,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_BountyBoard());
                }
            };
        }
    }
}

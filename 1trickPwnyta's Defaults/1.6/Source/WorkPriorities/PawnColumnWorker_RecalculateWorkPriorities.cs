using Defaults.Defs;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.WorkPriorities
{
    public class PawnColumnWorker_RecalculateWorkPriorities : PawnColumnWorker
    {
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (DefaultSettingsCategoryDefOf.WorkPriorities.Enabled && pawn.workSettings != null)
            {
                if (!Settings.GetValue<bool>(Settings.HIDE_LOADDEFAULT))
                {
                    rect = rect.MiddlePartPixels(18f, 24f);
                    if (Widgets.ButtonImage(rect, TexButton.HotReloadDefs))
                    {
                        WorkPriorityUtility.SetWorkPrioritiesToDefault(pawn);
                        SoundDefOf.Click.PlayOneShot(null);
                    }
                    TooltipHandler.TipRegionByKey(rect, "Defaults_RecalculateWorkPriorities");
                }
            }
        }

        public override int GetMinWidth(PawnTable table)
        {
            return DefaultSettingsCategoryDefOf.WorkPriorities.Enabled
                ? Mathf.Max(base.GetMinWidth(table), !Settings.GetValue<bool>(Settings.HIDE_LOADDEFAULT) ? 18 : 0)
                : 0;
        }

        public override int GetMaxWidth(PawnTable table)
        {
            return DefaultSettingsCategoryDefOf.WorkPriorities.Enabled ? Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table)) : 0;
        }
    }
}

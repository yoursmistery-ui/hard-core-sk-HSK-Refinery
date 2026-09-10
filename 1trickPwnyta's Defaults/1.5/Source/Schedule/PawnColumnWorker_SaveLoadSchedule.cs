using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.Schedule
{
    public class PawnColumnWorker_SaveLoadSchedule : PawnColumnWorker
    {
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
        {
            if (pawn.timetable != null && !pawn.IsMutant)
            {
                rect = new Rect(rect.x, rect.y, 36f, 30f);
                MouseoverSounds.DoRegion(rect);
                Rect rect2 = new Rect(rect.x, rect.y + (rect.height / 2f - 12f), 18f, 24f);
                if (Widgets.ButtonImage(rect2, TexButton.Save))
                {
                    string name = "Defaults_ScheduleName".Translate(DefaultsSettings.DefaultSchedules.Count + 1);
                    DefaultsSettings.DefaultSchedules.Add(new Schedule(name, pawn));
                    LongEventHandler.ExecuteWhenFinished(DefaultsMod.Settings.Write);
                    Messages.Message("Defaults_ScheduleSavedAs".Translate(name), MessageTypeDefOf.PositiveEvent, false);
                }
                TooltipHandler.TipRegionByKey(rect2, "Defaults_SaveNewDefaultSchedule");
                Rect rect3 = rect2;
                rect3.x = rect2.xMax;
                if (Widgets.ButtonImage(rect3, TexButton.HotReloadDefs))
                {
                    Find.WindowStack.Add(new FloatMenu(DefaultsSettings.DefaultSchedules.Select(s => new FloatMenuOption(s.name, delegate () {
                        s.ApplyToPawnTimetable(pawn.timetable);
                    })).ToList()));
                }
                TooltipHandler.TipRegionByKey(rect3, "Defaults_ReplaceWithDefaultSchedule");
            }
        }

        public override int GetMinWidth(PawnTable table)
        {
            return Mathf.Max(base.GetMinWidth(table), 36);
        }

        public override int GetMaxWidth(PawnTable table)
        {
            return Mathf.Min(base.GetMaxWidth(table), this.GetMinWidth(table));
        }
    }
}

using Defaults.Defs;
using RimWorld;
using System.Collections.Generic;
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
            if (DefaultSettingsCategoryDefOf.Schedule.Enabled && pawn.timetable != null && !pawn.IsMutant)
            {
                rect = new Rect(rect.x, rect.y, GetMinWidth(table), 30f);
                MouseoverSounds.DoRegion(rect);
                List<Schedule> schedules = Settings.Get<List<Schedule>>(Settings.SCHEDULES);
                float x = 0f;

                if (!Settings.GetValue<bool>(Settings.HIDE_SETASDEFAULT))
                {
                    Rect rect2 = new Rect(rect.x + x, rect.y + (rect.height / 2f - 12f), 18f, 24f);
                    if (Widgets.ButtonImage(rect2, TexButton.Save))
                    {
                        string name = "Defaults_ScheduleName".Translate(schedules.Count + 1);
                        schedules.Add(new Schedule(name, pawn));
                        DefaultsMod.SaveSettings();
                        Messages.Message("Defaults_ScheduleSavedAs".Translate(name), MessageTypeDefOf.PositiveEvent, false);
                    }
                    TooltipHandler.TipRegionByKey(rect2, "Defaults_SaveNewDefaultSchedule");
                    x += rect2.width;
                }
                if (!Settings.GetValue<bool>(Settings.HIDE_LOADDEFAULT))
                {
                    Rect rect3 = new Rect(rect.x + x, rect.y + (rect.height / 2f - 12f), 18f, 24f);
                    if (Widgets.ButtonImage(rect3, TexButton.HotReloadDefs))
                    {
                        Find.WindowStack.Add(new FloatMenu(schedules.Select(s => new FloatMenuOption(s.name, () =>
                        {
                            s.ApplyToPawnTimetable(pawn.timetable);
                        })).ToList()));
                    }
                    TooltipHandler.TipRegionByKey(rect3, "Defaults_ReplaceWithDefaultSchedule");
                }
            }
        }

        public override int GetMinWidth(PawnTable table)
        {
            return DefaultSettingsCategoryDefOf.Schedule.Enabled
                ? Mathf.Max(base.GetMinWidth(table), (!Settings.GetValue<bool>(Settings.HIDE_SETASDEFAULT) ? 18 : 0) + (!Settings.GetValue<bool>(Settings.HIDE_LOADDEFAULT) ? 18 : 0))
                : 0;
        }

        public override int GetMaxWidth(PawnTable table)
        {
            return DefaultSettingsCategoryDefOf.Schedule.Enabled ? Mathf.Min(base.GetMaxWidth(table), GetMinWidth(table)) : 0;
        }
    }
}

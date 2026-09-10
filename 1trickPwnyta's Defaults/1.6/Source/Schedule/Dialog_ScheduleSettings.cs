using Defaults.Defs;
using Defaults.UI;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.Schedule
{
    public class Dialog_ScheduleSettings : Dialog_SettingsCategory
    {
        private static Vector2 scrollPos;

        public Dialog_ScheduleSettings(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(860f, 640f);

        protected override IList ReorderableItems => Settings.Get<List<Schedule>>(Settings.SCHEDULES);

        public override void DoSettings(Rect rect)
        {
            List<Schedule> defaultSchedules = Settings.Get<List<Schedule>>(Settings.SCHEDULES);

            float selectorHeight = 65f;
            TimeAssignmentSelector.DrawTimeAssignmentSelectorGrid(new Rect(rect.x, rect.y, 191f, selectorHeight));

            float buttonWidth = 160f;
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, 30f), "Defaults_AddAlternateSchedule".Translate()))
            {
                defaultSchedules.Add(new Schedule("Defaults_ScheduleName".Translate(defaultSchedules.Count + 1)));
                SoundDefOf.Click.PlayOneShotOnCamera(null);
            }

            float labelWidth = 160f;
            float copyButtonWidth = 18f;
            float rowHeight = 30f;
            float x = rect.x + rowHeight + labelWidth + copyButtonWidth;
            float cellWidth = 540 / 24f;
            using (new TextBlock(GameFont.Tiny, TextAnchor.LowerCenter))
            {
                for (int i = 0; i < 24; i++)
                {
                    Widgets.Label(new Rect(x, rect.y + rowHeight, cellWidth, rowHeight), i.ToString());
                    x += cellWidth;
                }
                x += 8f;
                Rect useLabelRect = new Rect(x, rect.y + rowHeight, 24f, rowHeight);
                Widgets.Label(useLabelRect, "Defaults_UseSchedule".Translate());
                TooltipHandler.TipRegionByKey(useLabelRect, "Defaults_UseScheduleTip");
            }

            Rect outRect = new Rect(rect.x, rect.y + rowHeight * 2, rect.width, rect.height - rowHeight * 2);
            reorderableRect = outRect;

            Widgets.BeginScrollView(outRect, ref scrollPos, new Rect(0f, 0f, rect.width - 16f, rowHeight * defaultSchedules.Count));

            float y = 0f;
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                for (int i = 0; i < defaultSchedules.Count; i++)
                {
                    Schedule schedule = defaultSchedules[i];
                    Rect scheduleRect = new Rect(rect.x, y, rect.width, rowHeight);
                    x = scheduleRect.x;

                    Rect dragRect = new Rect(x, scheduleRect.y, scheduleRect.height, scheduleRect.height).ContractedBy(2f);
                    x += scheduleRect.height;

                    schedule.name = Widgets.TextField(new Rect(x, scheduleRect.y, labelWidth, scheduleRect.height), schedule.name);
                    x += labelWidth;

                    CopyPasteUI.DoCopyPasteButtons(new Rect(x, scheduleRect.y, copyButtonWidth * 2, scheduleRect.height), () =>
                    {
                        defaultSchedules.Add(new Schedule("Defaults_ScheduleName".Translate(defaultSchedules.Count + 1), schedule));
                    }, null);
                    x += copyButtonWidth;

                    for (int j = 0; j < 24; j++)
                    {
                        DoTimeAssignment(new Rect(x, scheduleRect.y, cellWidth, scheduleRect.height), schedule, j);
                        x += cellWidth;
                    }

                    x += 8f;
                    Widgets.Checkbox(new Vector2(x, scheduleRect.y + (scheduleRect.height - 24f) / 2), ref schedule.use);
                    x += 24f;

                    if (defaultSchedules.Count > 1)
                    {
                        if (Widgets.ButtonImage(new Rect(x, scheduleRect.y + (scheduleRect.height - 24f) / 2, 24f, 24f), TexButton.Delete, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                        {
                            defaultSchedules.Remove(schedule);
                            i--;
                            SoundDefOf.Click.PlayOneShotOnCamera(null);
                        }
                    }

                    UIUtility.DoDraggable(ReorderableGroup, scheduleRect, dragRect, dragRect);

                    y += rowHeight;
                }
            }

            Widgets.EndScrollView();
        }

        private static void DoTimeAssignment(Rect rect, Schedule schedule, int hour)
        {
            rect = rect.ContractedBy(1f);
            bool mouseButton = Input.GetMouseButton(0);
            TimeAssignmentDef assignment = schedule[hour];
            GUI.DrawTexture(rect, assignment.ColorTexture);
            if (!mouseButton)
            {
                MouseoverSounds.DoRegion(rect);
            }
            if (Mouse.IsOver(rect) && !ReorderableWidget.Dragging)
            {
                Widgets.DrawBox(rect, 2, null);
                if (mouseButton && assignment != TimeAssignmentSelector.selectedAssignment && TimeAssignmentSelector.selectedAssignment != null)
                {
                    SoundDefOf.Designate_DragStandard_Changed_NoCam.PlayOneShotOnCamera(null);
                    schedule[hour] = TimeAssignmentSelector.selectedAssignment;
                }
            }
        }
    }
}

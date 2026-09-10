using Defaults.Defs;
using Defaults.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.WorkPriorities
{
    public class Dialog_WorkPriorities : Dialog_SettingsCategory
    {
        private float y;
        private Vector2 scrollPosition;
        private int? dragValue;

        public Dialog_WorkPriorities(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(500f, 750f);

        public override void DoSettings(Rect rect)
        {
            Rect viewRect = new Rect(0f, 0f, rect.width - 20f, y);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            y = 0f;

            bool advancedMode = Settings.Get<bool>(Settings.WORK_PRIORITIES_ADVANCED_MODE);
            Widgets.CheckboxLabeled(new Rect(viewRect.x, y, viewRect.width, 30f), "Defaults_WorkPrioritiesAdvancedMode".Translate(), ref advancedMode, placeCheckboxNearText: true);
            Settings.Set(Settings.WORK_PRIORITIES_ADVANCED_MODE, advancedMode);
            y += 30f;

            if (advancedMode)
            {
                DoAdvancedMode(viewRect, ref y);
            }
            else
            {
                DoBasicMode(viewRect, ref y);
            }

            Widgets.EndScrollView();
        }

        private void DoBasicMode(Rect rect, ref float y)
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, 60f), "Defaults_BasicWorkPrioritiesDesc".Translate());
            y += 60f;

            Listing_StandardHighlight listing = new Listing_StandardHighlight() { maxOneColumn = true };
            listing.Begin(new Rect(rect.x, y, rect.width, rect.height));

            Dictionary<WorkTypeDef, int> basicDefaultWorkPriorities = Settings.Get<Dictionary<WorkTypeDef, int>>(Settings.WORK_PRIORITIES_BASIC);
            foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(d => basicDefaultWorkPriorities.ContainsKey(d)).OrderByDescending(d => d.naturalPriority))
            {
                Rect rowRect = listing.GetRect(30f);
                using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(rowRect.LeftPart(0.7f), def.labelShort.CapitalizeFirst());
                Rect buttonRect = rowRect.RightPart(0.3f);
                int priority = basicDefaultWorkPriorities[def];
                Widgets.DraggableResult result = UIUtility.DoImageTextButtonDraggable(buttonRect, WorkPriorityValue.GetIcon(priority), WorkPriorityValue.GetLabel(priority).CapitalizeFirst());
                if (result == Widgets.DraggableResult.Pressed)
                {
                    Find.WindowStack.Add(new FloatMenu(WorkPriorityValue.GetOptions(i => basicDefaultWorkPriorities[def] = i)));
                }
                else if (result == Widgets.DraggableResult.Dragged)
                {
                    dragValue = priority;
                }
                if (dragValue.HasValue && Mouse.IsOver(buttonRect) && priority != dragValue.Value)
                {
                    basicDefaultWorkPriorities[def] = dragValue.Value;
                    SoundDefOf.Click.PlayOneShot(null);
                }
            }

            listing.End();
            y += listing.CurHeight;

            if (!Input.GetMouseButton(0))
            {
                dragValue = null;
            }
        }

        private void DoAdvancedMode(Rect rect, ref float y)
        {
            Listing_Standard listing = new Listing_Standard() { maxOneColumn = true };
            listing.Begin(new Rect(rect.x, y, rect.width, rect.height));

            listing.Label("Defaults_AdvancedWorkPrioritiesDesc".Translate());
            listing.GapLine();

            List<Rule> advancedGlobalWorkPriorityLogic = Settings.Get<List<Rule>>(Settings.WORK_PRIORITIES_GLOBAL_LOGIC);
            listing.Label("Defaults_WorkPriorityRulesGlobalDesc".Translate());
            Rect globalButtonRect = listing.GetRect(30f).LeftPart(0.3f);
            if (DoRuleButton(globalButtonRect, "Defaults_WorkPriorityRulesGlobal".Translate(), advancedGlobalWorkPriorityLogic.Count))
            {
                Find.WindowStack.Add(new Dialog_Rules("Defaults_WorkPriorityRulesGlobal".Translate(), advancedGlobalWorkPriorityLogic));
            }
            listing.Gap();

            Dictionary<WorkTypeDef, List<Rule>> advancedWorkPriorityLogic = Settings.Get<Dictionary<WorkTypeDef, List<Rule>>>(Settings.WORK_PRIORITIES_LOGIC);
            listing.Label("Defaults_WorkPriorityRulesSpecificDesc".Translate());
            Rect rowRect = listing.GetRect(30f);
            foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading.OrderByDescending(d => d.naturalPriority))
            {
                if (rowRect.width < 100f)
                {
                    listing.Gap(rect.width * 0.03f);
                    rowRect = listing.GetRect(30f);
                    if (rowRect.width < 100f)
                    {
                        throw new Exception("Need at least 100f width to do rule button.");
                    }
                }
                Rect buttonRect = rowRect;
                buttonRect.width = rect.width * 0.3f;
                if (DoRuleButton(buttonRect, def.labelShort.CapitalizeFirst(), advancedWorkPriorityLogic[def].Count))
                {
                    Find.WindowStack.Add(new Dialog_Rules("Defaults_WorkPriorityRulesSpecific".Translate(def.labelShort.CapitalizeFirst()), advancedWorkPriorityLogic[def]));
                }
                rowRect.xMin = buttonRect.xMax + rect.width * 0.03f;
            }

            listing.End();
            y += listing.CurHeight;
        }

        private bool DoRuleButton(Rect rect, string text, int count)
        {
            Widgets.DrawRectFast(rect, Widgets.MenuSectionBGFillColor);
            Widgets.DrawHighlightIfMouseover(rect);
            return Widgets.ButtonText(rect, count > 0 ? $"{text} ({count})" : text, false, overrideTextAnchor: TextAnchor.MiddleCenter);
        }
    }
}

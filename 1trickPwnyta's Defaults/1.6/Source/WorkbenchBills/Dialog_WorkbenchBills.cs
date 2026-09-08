using Defaults.Defs;
using Defaults.UI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.WorkbenchBills
{
    [StaticConstructorOnStartup]
    public class Dialog_WorkbenchBills : Dialog_SettingsCategory
    {
        private const float padding = 6f;
        private const float workbenchGroupHeight = 60f;
        private static readonly Color workbenchGroupColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly List<HashSet<ThingDef>> workbenchGroups;

        static Dialog_WorkbenchBills()
        {
            workbenchGroups = new List<HashSet<ThingDef>>();
            foreach (ThingDef workbench in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => typeof(Building_WorkTable).IsAssignableFrom(d.thingClass)))
            {
                HashSet<ThingDef> workbenchGroup = workbenchGroups.FirstOrDefault(g => g.Any(d => new HashSet<RecipeDef>(d.AllRecipes).SetEquals(new HashSet<RecipeDef>(workbench.AllRecipes))));
                if (workbenchGroup != null)
                {
                    workbenchGroup.Add(workbench);
                }
                else
                {
                    workbenchGroups.Add(new HashSet<ThingDef>() { workbench });
                }
            }
        }

        private Vector2 scrollPosition;
        private float y = 0f;

        public Dialog_WorkbenchBills(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(860f, 600f);

        protected override TaggedString ResetOptionWarning => "Defaults_ConfirmResetWorkbenchBills".Translate();

        protected override bool DoSearchWidget => true;

        public override void DoSettings(Rect rect)
        {
            Rect viewRect = new Rect(0f, 0f, rect.width - 20f, y);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            float x = 0f;
            y = 0f;
            float workbenchGroupWidth = (viewRect.width - padding * 2) / 3;
            foreach (HashSet<ThingDef> workbenchGroup in workbenchGroups.Where(g => g.Any(d => CommonSearchWidget.filter.Matches(d.label) || d.AllRecipes.Any(r => CommonSearchWidget.filter.Matches(r.label)))))
            {
                if (x + workbenchGroupWidth > viewRect.width)
                {
                    x = 0f;
                    y += workbenchGroupHeight + padding;
                }
                Rect workbenchRect = new Rect(viewRect.x + x, viewRect.y + y, workbenchGroupWidth, workbenchGroupHeight);
                DoWorkbenchGroup(workbenchRect, workbenchGroup);
                x += workbenchGroupWidth + padding;
            }
            y += workbenchGroupHeight;
            Widgets.EndScrollView();
        }

        private void DoWorkbenchGroup(Rect rect, HashSet<ThingDef> workbenchGroup)
        {
            Widgets.DrawRectFast(rect, workbenchGroupColor);
            Widgets.DrawHighlightIfMouseover(rect);

            float iconSize = rect.height - padding * 2;
            Rect iconRect = new Rect(rect.x + padding, rect.y + padding, iconSize, iconSize);
            ThingDef iconDef = BillUtility.GetWorkbenchGroupIconDef(workbenchGroup);
            Widgets.DefIcon(iconRect, iconDef, GenStuff.DefaultStuffFor(iconDef));

            Rect labelRect = new Rect(rect.x + padding + iconSize + padding, rect.y + padding, rect.width - padding - iconSize - padding - padding - 50f - padding, rect.height - padding * 2);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, string.Join("\n", workbenchGroup.Select(w => w.LabelCap)));
            Text.Anchor = default;

            Rect quantityRect = new Rect(labelRect.xMax + padding, labelRect.y, 50f, labelRect.height);
            Text.Anchor = TextAnchor.LowerRight;
            Widgets.Label(quantityRect, WorkbenchBillStore.Get(workbenchGroup).bills.Count.ToString());
            Text.Anchor = default;

            if (Widgets.ButtonInvisible(rect))
            {
                Find.WindowStack.Add(new Dialog_BillMaker(workbenchGroup));
            }
        }
    }
}

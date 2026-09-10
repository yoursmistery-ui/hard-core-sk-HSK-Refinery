using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.WorkbenchBills
{
    [StaticConstructorOnStartup]
    public class Dialog_WorkbenchBills : Window
    {
        private const float padding = 6f;
        private const float workbenchGroupHeight = 60f;
        private static readonly Color workbenchGroupColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly List<HashSet<ThingDef>> workbenchGroups;
        private static readonly QuickSearchWidget search = new QuickSearchWidget();

        static Dialog_WorkbenchBills()
        {
            workbenchGroups = new List<HashSet<ThingDef>>();
            foreach (ThingDef workbench in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => typeof(Building_WorkTable).IsAssignableFrom(d.thingClass)))
            {
                HashSet<ThingDef> workbenchGroup = workbenchGroups.FirstOrDefault(g => g.Any(d => new HashSet<RecipeDef>(d.AllRecipes).SetEquals(new HashSet<RecipeDef>(workbench.AllRecipes))));
                if (workbenchGroup != null )
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

        public Dialog_WorkbenchBills()
        {
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            optionalTitle = "Defaults_WorkbenchBills".Translate();
        }

        public override Vector2 InitialSize => new Vector2(860f, 600f);

        public override void PreOpen()
        {
            base.PreOpen();
            search.Reset();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect globalRect = new Rect(inRect.xMax - 200f, inRect.y, 200f, 29f);
            if (Widgets.ButtonText(globalRect, "Defaults_GlobalBillSettings".Translate()))
            {
                Find.WindowStack.Add(new Dialog_GlobalBillSettings());
            }

            Rect outRect = new Rect(inRect.x, globalRect.yMax + padding, inRect.width, inRect.height - Window.CloseButSize.y - padding - Window.QuickSearchSize.y - padding - globalRect.height - padding);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, y);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float x = 0f;
            y = 0f;
            float workbenchGroupWidth = (viewRect.width - padding * 2) / 3;
            foreach (HashSet<ThingDef> workbenchGroup in workbenchGroups.Where(g => g.Any(d => search.filter.Matches(d.label) || d.AllRecipes.Any(r => search.filter.Matches(r.label)))))
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

            Rect searchRect = new Rect(inRect.x, inRect.yMax - Window.CloseButSize.y - padding - Window.QuickSearchSize.y, Window.QuickSearchSize.x, Window.QuickSearchSize.y);
            search.OnGUI(searchRect);
        }

        private void DoWorkbenchGroup(Rect rect, HashSet<ThingDef> workbenchGroup)
        {
            Widgets.DrawRectFast(rect, workbenchGroupColor);
            Widgets.DrawHighlightIfMouseover(rect);

            float iconSize = rect.height - padding * 2;
            Rect iconRect = new Rect(rect.x + padding, rect.y + padding, iconSize, iconSize);
            Widgets.DefIcon(iconRect, workbenchGroup.First());

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

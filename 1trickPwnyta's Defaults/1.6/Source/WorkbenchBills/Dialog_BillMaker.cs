using Defaults.UI;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.WorkbenchBills
{
    public class Dialog_BillMaker : Dialog_Common
    {
        private const float padding = 4f;
        private static readonly Color billColor = new Color(0.15f, 0.15f, 0.15f);

        private readonly HashSet<ThingDef> workbenchGroup;
        private Vector2 scrollPosition;
        private float height = 0f;

        public Dialog_BillMaker(HashSet<ThingDef> workbenchGroup)
        {
            this.workbenchGroup = workbenchGroup;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(400f, 500f);

        protected override IList ReorderableItems => WorkbenchBillStore.Get(workbenchGroup).bills;

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            float y = inRect.y;
            Rect titleRect = new Rect(inRect.x, y, inRect.width - 75f - padding, 60f);
            Rect titleIconRect = new Rect(titleRect.x, titleRect.y, titleRect.height, titleRect.height);
            ThingDef iconDef = BillUtility.GetWorkbenchGroupIconDef(workbenchGroup);
            Widgets.DefIcon(titleIconRect, iconDef, GenStuff.DefaultStuffFor(iconDef));
            Rect titleLabelRect = new Rect(titleIconRect.xMax + padding, titleRect.y, titleRect.width - titleIconRect.width - padding, titleRect.height);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(titleLabelRect, string.Join("\n", workbenchGroup.Select(w => w.LabelCap)));
            Rect buttonRect = new Rect(titleRect.xMax + padding, titleRect.y + 30f, 75f, 30f);
            if (Widgets.ButtonText(buttonRect, "AddBill".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(workbenchGroup.First().AllRecipes.Select(r => new FloatMenuOption(r.LabelCap, () =>
                {
                    WorkbenchBillStore.Get(workbenchGroup).bills.Add(new BillTemplate(r));
                }, r.UIIconThing, r.UIIcon, null, true, MenuOptionPriority.Default, null, null, 29f, rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, r), null, true, -r.displayPriority)).ToList()));
            }
            y += titleRect.height + padding;

            List<BillTemplate> bills = WorkbenchBillStore.Get(workbenchGroup).bills;
            bool limit15 = Settings.GetValue<bool>(Settings.LIMIT_BILLS_TO_15);

            bool billsLimited = bills.Count(b => b.use) > 15 && limit15;
            Rect warningRect = new Rect(inRect.x, y, inRect.width, 70f);
            if (billsLimited)
            {
                Widgets.DrawRectFast(warningRect, Widgets.MenuSectionBGFillColor);
                using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft)) Widgets.Label(warningRect.LeftPartPixels(inRect.width - 150f).ContractedBy(3f), "Defaults_BillsLimitedto15".Translate().Colorize(Color.yellow));
                if (Widgets.ButtonText(warningRect.RightPartPixels(150f).MiddlePartPixels(150f, 30f).ContractedBy(3f), "Defaults_RemoveLimit".Translate()))
                {
                    Settings.SetValue(Settings.LIMIT_BILLS_TO_15, false);
                }
                y += warningRect.height + padding;
            }

            Rect outRect = new Rect(inRect.x, y, inRect.width, inRect.height - y - CloseButSize.y - padding);
            reorderableRect = outRect;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, height);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            height = 0f;

            int numBills = 0;
            foreach (BillTemplate bill in bills.ListFullCopy())
            {
                Rect billRect = new Rect(viewRect.x, viewRect.y + height, viewRect.width, 68f);
                DoBill(billRect, bill, bills);
                if (!bill.use || (limit15 && numBills >= 15))
                {
                    Widgets.DrawRectFast(billRect, Color.black.WithAlpha(0.25f));
                }
                height += billRect.height + padding;
                if (bill.use)
                {
                    numBills++;
                }
            }
            Widgets.EndScrollView();
        }

        private void DoBill(Rect rect, BillTemplate bill, List<BillTemplate> allBills)
        {
            Widgets.DrawRectFast(rect, billColor);

            Rect labelRect = new Rect(rect.x + 8f, rect.y, rect.width - 8f - 24f - 4f - 24f - 8f, 25f);
            Widgets.Label(labelRect, bill.recipe.LabelCap);

            Rect nameRect = new Rect(labelRect.x, labelRect.yMax - 4f, labelRect.width, 18f);
            if (!bill.name.EqualsIgnoreCase(bill.recipe.LabelCap))
            {
                using (new TextBlock(GameFont.Tiny)) Widgets.Label(nameRect, bill.name);
            }

            Rect deleteRect = new Rect(rect.xMax - 24f, rect.y, 24f, 24f);
            if (Widgets.ButtonImage(deleteRect, TexButton.Delete, Color.white, GenUI.SubtleMouseoverColor))
            {
                allBills.Remove(bill);
                SoundDefOf.Click.PlayOneShot(null);
            }
            TooltipHandler.TipRegionByKey(deleteRect, "DeleteBillTip");

            Rect copyRect = new Rect(deleteRect);
            copyRect.x -= copyRect.width + 4f;
            if (Widgets.ButtonImageFitted(copyRect, TexButton.Copy))
            {
                WorkbenchBillStore.Get(workbenchGroup).bills.Add(bill.Clone());
                SoundDefOf.Tick_High.PlayOneShot(null);
            }

            Rect repeatInfoRect = new Rect(rect.x + 28f, rect.y + 47f, 100f, 30f);
            string repeatInfo = "";
            if (bill.repeatMode == BillRepeatModeDefOf.Forever)
            {
                repeatInfo = "Forever".Translate();
            }
            else if (bill.repeatMode == BillRepeatModeDefOf.RepeatCount)
            {
                repeatInfo = bill.repeatCount + "x";
            }
            else if (bill.repeatMode == BillRepeatModeDefOf.TargetCount)
            {
                repeatInfo = "/" + bill.targetCount;
            }
            GUI.color = new Color(1f, 1f, 1f, 0.65f);
            Widgets.Label(repeatInfoRect, repeatInfo);
            GUI.color = Color.white;

            WidgetRow row = new WidgetRow(rect.xMax, rect.y + 44f, UIDirection.LeftThenUp);
            if (row.ButtonText("Details".Translate() + "..."))
            {
                Find.WindowStack.Add(new Dialog_Bill(bill));
            }
            if (row.ButtonText(bill.repeatMode.LabelCap.Resolve().PadRight(20)))
            {
                bill.DoBillRepeatModeMenu();
            }

            void countAction(int multiplier)
            {
                if (bill.repeatMode == BillRepeatModeDefOf.Forever)
                {
                    bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
                    bill.repeatCount = 1;
                }
                else if (bill.repeatMode == BillRepeatModeDefOf.TargetCount)
                {
                    int amount = bill.recipe.targetCountAdjustment * GenUI.CurrentAdjustmentMultiplier() * multiplier;
                    bill.targetCount = Mathf.Max(0, bill.targetCount + amount);
                    bill.unpauseWhenYouHave += Mathf.Max(0, bill.unpauseWhenYouHave + amount);
                }
                else if (bill.repeatMode == BillRepeatModeDefOf.RepeatCount)
                {
                    bill.repeatCount = Mathf.Max(0, bill.repeatCount + GenUI.CurrentAdjustmentMultiplier() * multiplier);
                }
                SoundDefOf.DragSlider.PlayOneShot(null);
            }
            if (row.ButtonIcon(TexButton.Plus))
            {
                countAction(1);
            }
            if (row.ButtonIcon(TexButton.Minus))
            {
                countAction(-1);
            }

            UIUtility.DoDraggable(ReorderableGroup, rect, tipRect: labelRect.Union(nameRect));
        }
    }
}

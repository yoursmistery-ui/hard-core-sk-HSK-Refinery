using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.WorkbenchBills
{
    public class Dialog_BillMaker : Window
    {
        private const float padding = 4f;
        private static readonly Color billColor = new Color(0.15f, 0.15f, 0.15f);

        private readonly HashSet<ThingDef> workbenchGroup;
        private Vector2 scrollPosition;
        private float y = 0f;

        public Dialog_BillMaker(HashSet<ThingDef> workbenchGroup)
        {
            this.workbenchGroup = workbenchGroup;
            doCloseX = true;
            doCloseButton = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(400f, 500f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width - 75f - padding, 60f);
            Rect titleIconRect = new Rect(titleRect.x, titleRect.y, titleRect.height, titleRect.height);
            Widgets.DefIcon(titleIconRect, workbenchGroup.First());
            Rect titleLabelRect = new Rect(titleIconRect.xMax + padding, titleRect.y, titleRect.width - titleIconRect.width - padding, titleRect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(titleLabelRect, string.Join("\n", workbenchGroup.Select(w => w.LabelCap)));
            Text.Anchor = default;

            Rect buttonRect = new Rect(titleRect.xMax + padding, inRect.y + 30f, 75f, 30f);
            if (Widgets.ButtonText(buttonRect, "AddBill".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(workbenchGroup.First().AllRecipes.Select(r => new FloatMenuOption(r.LabelCap, () =>
                {
                    WorkbenchBillStore.Get(workbenchGroup).bills.Add(new BillTemplate(r));
                }, r.UIIconThing, r.UIIcon, null, true, MenuOptionPriority.Default, null, null, 29f, (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, r), null, true, -r.displayPriority)).ToList()));
            }

            Rect outRect = new Rect(inRect.x, titleRect.yMax + padding, inRect.width, inRect.height - titleRect.height - padding - Window.CloseButSize.y - padding);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, y);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            y = 0f;
            List<BillTemplate> bills = WorkbenchBillStore.Get(workbenchGroup).bills;
            foreach (BillTemplate bill in bills.ListFullCopy())
            {
                Rect billRect = new Rect(viewRect.x, viewRect.y + y, viewRect.width, 68f);
                DoBill(billRect, bill, bills);
                y += billRect.height + padding;
            }
            Widgets.EndScrollView();
        }

        private void DoBill(Rect rect, BillTemplate bill, List<BillTemplate> allBills)
        {
            Widgets.DrawRectFast(rect, billColor);

            int index = allBills.IndexOf(bill);
            if (index > 0)
            {
                Rect upRect = new Rect(rect.x, rect.y, 24f, 24f);
                if (Widgets.ButtonImage(upRect, TexButton.ReorderUp))
                {
                    allBills.Remove(bill);
                    allBills.Insert(index - 1, bill);
                    SoundDefOf.Tick_High.PlayOneShot(null);
                }
                TooltipHandler.TipRegionByKey(upRect, "ReorderBillUpTip");
            }
            if (index < allBills.Count - 1)
            {
                Rect downRect = new Rect(rect.x, rect.y + 24f, 24f, 24f);
                if (Widgets.ButtonImage(downRect, TexButton.ReorderDown))
                {
                    allBills.Remove(bill);
                    allBills.Insert(index + 1, bill);
                    SoundDefOf.Tick_Low.PlayOneShot(null);
                }
                TooltipHandler.TipRegionByKey(downRect, "ReorderBillDownTip");
            }

            Rect labelRect = new Rect(rect.x + 28f, rect.y, rect.width - 48f - 20f, 25f);
            Widgets.Label(labelRect, bill.recipe.LabelCap);

            if (!bill.name.EqualsIgnoreCase(bill.recipe.LabelCap))
            {
                Rect nameRect = new Rect(labelRect.x, labelRect.yMax - 4f, labelRect.width, 18f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(nameRect, bill.name);
                Text.Font = GameFont.Small;
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
            Action<int> countAction = (multiplier) =>
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
            };
            if (row.ButtonIcon(TexButton.Plus))
            {
                countAction(1);
            }
            if (row.ButtonIcon(TexButton.Minus))
            {
                countAction(-1);
            }
        }
    }
}

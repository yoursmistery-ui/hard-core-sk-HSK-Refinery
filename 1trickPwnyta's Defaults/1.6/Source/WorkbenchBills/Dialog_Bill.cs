using Defaults.Compatibility;
using Defaults.UI;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class Dialog_Bill : Window
    {
        private const float RepeatModeSubdialogHeight = 354f;
        private const float StoreModeSubdialogHeight = 30f;
        private const float WorkerSelectionSubdialogHeight = 96f;
        private const float IngredientRadiusSubdialogHeight = 50f;

        private readonly BillTemplate bill;
        private readonly ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();
        private string repeatCountEditBuffer;
        private string targetCountEditBuffer;
        private string unpauseCountEditBuffer;

        public override Vector2 InitialSize => new Vector2(800f, 664f);

        public Dialog_Bill(BillTemplate bill)
        {
            this.bill = bill;
            forcePause = true;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            thingFilterState.quickSearch.Reset();
        }

        protected override void LateWindowOnGUI(Rect inRect)
        {
            Rect rect = new Rect(inRect.x, inRect.y, 34f, 34f);
            Widgets.DefIcon(rect, bill.recipe);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "Would make it ugly af")]
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect labelRect = new Rect(inRect.x + 40f, inRect.y, 400f, 34f);
            Widgets.Label(labelRect, bill.recipe.LabelCap);
            float num = (int)((inRect.width - 34f) / 3f);
            Rect columnRect = new Rect(0f, 80f, num, inRect.height - 80f);

            Rect renameRect = new Rect(labelRect.x + Text.CalcSize(bill.recipe.LabelCap).x + 16f, labelRect.y, labelRect.height, labelRect.height);
            if (Widgets.ButtonImage(renameRect, TexButton.Rename))
            {
                Find.WindowStack.Add(new Dialog_RenameBillTemplate(bill));
            }

            if (!bill.name.EqualsIgnoreCase(bill.recipe.LabelCap))
            {
                Text.Font = GameFont.Tiny;
                Rect nameRect = new Rect(labelRect.x, labelRect.yMax, labelRect.width, 18f);
                Widgets.Label(nameRect, bill.name);
            }

            Text.Font = GameFont.Small;
            Listing_Standard listing = new Listing_Standard();
            Rect listingRect = new Rect(columnRect.xMax + 17f, 50f, num, inRect.height - 50f - CloseButSize.y);
            listing.Begin(listingRect);

            Listing_Standard repeatSection = listing.BeginSection(RepeatModeSubdialogHeight, 4f, 4f);
            if (repeatSection.ButtonText(bill.repeatMode.LabelCap))
            {
                bill.DoBillRepeatModeMenu();
            }
            repeatSection.Gap(12f);
            if (bill.repeatMode == BillRepeatModeDefOf.RepeatCount)
            {
                repeatSection.Label("RepeatCount".Translate(bill.repeatCount));
                repeatSection.IntEntry(ref bill.repeatCount, ref repeatCountEditBuffer);
            }
            else if (bill.repeatMode == BillRepeatModeDefOf.TargetCount)
            {
                string targetString = "/ " + ((bill.targetCount < 999999) ? bill.targetCount.ToString() : "Infinite".Translate().ToLower().ToString());
                string productDescription = bill.recipe.WorkerCounter.ProductsDescription(null);
                if (!productDescription.NullOrEmpty())
                {
                    targetString += "\n" + "CountingProducts".Translate() + ": " + productDescription.CapitalizeFirst();
                }
                repeatSection.Label(targetString);
                int targetCount = bill.targetCount;
                repeatSection.IntEntry(ref bill.targetCount, ref targetCountEditBuffer, bill.recipe.targetCountAdjustment);
                bill.unpauseWhenYouHave = Mathf.Max(0, bill.unpauseWhenYouHave + (bill.targetCount - targetCount));
                ThingDef producedThingDef = bill.recipe.ProducedThingDef;
                if (producedThingDef != null)
                {
                    if (producedThingDef.IsWeapon || producedThingDef.IsApparel)
                    {
                        repeatSection.CheckboxLabeled("IncludeEquipped".Translate(), ref bill.includeEquipped);
                    }
                    if (producedThingDef.IsApparel && producedThingDef.apparel.careIfWornByCorpse)
                    {
                        repeatSection.CheckboxLabeled("IncludeTainted".Translate(), ref bill.includeTainted);
                    }
                    if (bill.recipe.products.Any(prod => prod.thingDef.useHitPoints))
                    {
                        Widgets.FloatRange(repeatSection.GetRect(32f, 1f), 997564327, ref bill.hpRange, 0f, 1f, "HitPoints", ToStringStyle.PercentZero);
                        bill.hpRange.min = Mathf.Round(bill.hpRange.min * 100f) / 100f;
                        bill.hpRange.max = Mathf.Round(bill.hpRange.max * 100f) / 100f;
                    }
                    if (producedThingDef.HasComp(typeof(CompQuality)))
                    {
                        Widgets.QualityRange(repeatSection.GetRect(32f, 1f), 1109890656, ref bill.qualityRange);
                    }
                    if (producedThingDef.MadeFromStuff)
                    {
                        repeatSection.CheckboxLabeled("LimitToAllowedStuff".Translate(), ref bill.limitToAllowedStuff);
                    }
                }
                repeatSection.CheckboxLabeled("PauseWhenSatisfied".Translate(), ref bill.pauseWhenSatisfied);
                if (bill.pauseWhenSatisfied)
                {
                    repeatSection.Label("UnpauseWhenYouHave".Translate() + ": " + bill.unpauseWhenYouHave.ToString("F0"));
                    repeatSection.IntEntry(ref bill.unpauseWhenYouHave, ref unpauseCountEditBuffer, bill.recipe.targetCountAdjustment);
                    if (bill.unpauseWhenYouHave >= bill.targetCount)
                    {
                        bill.unpauseWhenYouHave = bill.targetCount - 1;
                        unpauseCountEditBuffer = bill.unpauseWhenYouHave.ToStringCached();
                    }
                }
            }
            listing.EndSection(repeatSection);
            listing.Gap(12f);

            Listing_Standard storeSection = listing.BeginSection(StoreModeSubdialogHeight, 4f, 4f);
            if (storeSection.ButtonText(bill.storeMode.LabelCap))
            {
                Find.WindowStack.Add(new FloatMenu(DefDatabase<BillStoreModeDef>.AllDefsListForReading.Where(d => d != BillStoreModeDefOf.SpecificStockpile).Select(d => new FloatMenuOption(d.LabelCap, () =>
                {
                    bill.storeMode = d;
                })).ToList()));
            }
            listing.EndSection(storeSection);
            listing.Gap(12f);

            Listing_Standard workerSection = listing.BeginSection(WorkerSelectionSubdialogHeight, 4f, 4f);
            string buttonLabel;
            if (ModsConfig.IdeologyActive && bill.slavesOnly)
            {
                buttonLabel = "AnySlave".Translate();
            }
            else if (ModsConfig.BiotechActive && bill.recipe.mechanitorOnlyRecipe)
            {
                buttonLabel = "AnyMechanitor".Translate();
            }
            else if (ModsConfig.BiotechActive && bill.mechsOnly)
            {
                buttonLabel = "AnyMech".Translate();
            }
            else if (ModsConfig.BiotechActive && bill.nonMechsOnly)
            {
                buttonLabel = "AnyNonMech".Translate();
            }
            else
            {
                buttonLabel = "AnyWorker".Translate();
            }
            if (Widgets.ButtonText(workerSection.GetRect(30f, 1f), buttonLabel))
            {
                Find.WindowStack.Add(new FloatMenu(GeneratePawnRestrictionOptions().ToList()));
            }
            if (bill.recipe.workSkill != null && !bill.mechsOnly)
            {
                workerSection.Label("AllowedSkillRange".Translate(bill.recipe.workSkill.label) + ": " + (bill.allowedSkillRange.max == 20 ? "Unlimited".TranslateSimple() : ""));
                workerSection.IntRange(ref bill.allowedSkillRange, 0, 20);
            }
            listing.EndSection(workerSection);

            listing.End();

            Rect ingredientConfigRect = new Rect(listingRect.xMax + 17f, 50f, 0f, inRect.height - 50f - CloseButSize.y) { xMax = inRect.xMax };
            float y = ingredientConfigRect.y;
            DoIngredientConfigPane(ingredientConfigRect.x, ref y, ingredientConfigRect.width, ingredientConfigRect.height);

            Listing_Standard infoListing = new Listing_Standard();
            infoListing.Begin(columnRect);
            StringBuilder infoStringBuilder = new StringBuilder();
            if (bill.recipe.description != null)
            {
                infoStringBuilder.AppendLine(bill.recipe.description);
                infoStringBuilder.AppendLine();
            }
            infoStringBuilder.AppendLine("WorkAmount".Translate() + ": " + bill.recipe.WorkAmountTotal(null).ToStringWorkAmount());
            if (ModsConfig.BiotechActive && bill.recipe.products.Count == 1)
            {
                ThingDef thingDef = bill.recipe.products[0].thingDef;
                if (thingDef.IsApparel)
                {
                    infoStringBuilder.AppendLine("WearableBy".Translate() + ": " + thingDef.apparel.developmentalStageFilter.ToCommaList(false).CapitalizeFirst());
                }
            }
            if (bill.recipe.gestationCycles > 0)
            {
                infoStringBuilder.AppendLine("GestationCycles".Translate() + ": " + bill.recipe.gestationCycles);
                if (!bill.recipe.mechResurrection)
                {
                    float wastepacks = (int)bill.recipe.ProducedThingDef.GetStatValueAbstract(StatDefOf.WastepacksPerRecharge) * bill.recipe.ProducedThingDef.GetStatValueAbstract(StatDefOf.BandwidthCost);
                    infoStringBuilder.AppendLine(Find.ActiveLanguageWorker.Pluralize(ThingDefOf.Wastepack.LabelCap) + " " + "ThingsProduced".Translate() + ": " + wastepacks);
                }
            }
            infoStringBuilder.AppendLine("BillRequires".Translate() + ": ");
            foreach (IngredientCount ingredientCount in bill.recipe.ingredients.OrderBy(i => i.CountFor(bill.recipe)))
            {
                if (!ingredientCount.filter.Summary.NullOrEmpty())
                {
                    infoStringBuilder.AppendLine(" - " + bill.recipe.IngredientValueGetter.BillRequirementsDescription(bill.recipe, ingredientCount));
                }
            }
            infoStringBuilder.AppendLine();
            string extraDescription = bill.recipe.IngredientValueGetter.ExtraDescriptionLine(bill.recipe);
            if (extraDescription != null)
            {
                infoStringBuilder.AppendLine(extraDescription);
                infoStringBuilder.AppendLine();
            }
            if (!bill.recipe.skillRequirements.NullOrEmpty())
            {
                infoStringBuilder.AppendLine("MinimumSkills".Translate());
                infoStringBuilder.AppendLine(bill.recipe.MinSkillString);
            }
            string infoString = infoStringBuilder.ToString();
            if (Text.CalcHeight(infoString, columnRect.width) > columnRect.height)
            {
                Text.Font = GameFont.Tiny;
            }
            infoListing.Label(infoString);
            Text.Font = GameFont.Small;
            infoListing.End();

            if (bill.recipe.products.Count == 1)
            {
                ThingDef product = bill.recipe.products[0].thingDef;
                Widgets.InfoCardButton(columnRect.x, ingredientConfigRect.y, product, GenStuff.DefaultStuffFor(product));
            }

            Rect useRect = new Rect(columnRect.xMax - 60f, ingredientConfigRect.y, 60f, 24f);
            Widgets.CheckboxLabeled(useRect, "Defaults_UseBill".Translate(), ref bill.use);
            TooltipHandler.TipRegionByKey(useRect, "Defaults_UseBillTip");

            ModCompatibilityUtility_BetterWorkbench.DoBetterWorkbenchOptionsInterface(columnRect.BottomPartPixels(62f + CloseButSize.y).TopPartPixels(62f), bill);
        }

        protected virtual void DoIngredientConfigPane(float x, ref float y, float width, float height)
        {
            if (bill.recipe.ingredients.Any(i => !i.IsFixedIngredient))
            {
                Rect lockRect = new Rect(x + width - 32f, y - 32f, 32f, 32f);
                UIUtility.DoLockButton(lockRect, ref bill.locked);

                Rect thingFilterRect = new Rect(x, y, width, height - IngredientRadiusSubdialogHeight);
                ThingFilterUI.DoThingFilterConfigWindow(thingFilterRect, thingFilterState, bill.ingredientFilter, bill.recipe.fixedIngredientFilter, TreeOpenMasks.BillConfig, null, ((IEnumerable<SpecialThingFilterDef>)typeof(Dialog_BillConfig).Method("get_HiddenSpecialThingFilters").Invoke(null, new object[] { })).ConcatIfNotNull(bill.recipe.forceHiddenSpecialFilters), false, false, false, bill.recipe.GetPremultipliedSmallIngredients());
                y += thingFilterRect.height;
            }
            Rect ingredientRadiusRect = new Rect(x, y, width, IngredientRadiusSubdialogHeight);
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(ingredientRadiusRect);
            string ingredientRadiusLabel = "IngredientSearchRadius".Translate().Truncate(ingredientRadiusRect.width * 0.6f);
            string ingredientRadiusValue = bill.ingredientSearchRadius == 999f ? "Unlimited".TranslateSimple().Truncate(ingredientRadiusRect.width * 0.3f) : bill.ingredientSearchRadius.ToString("F0");
            listing_Standard.Label(ingredientRadiusLabel + ": " + ingredientRadiusValue);
            bill.ingredientSearchRadius = listing_Standard.Slider((bill.ingredientSearchRadius > 100f) ? 100f : bill.ingredientSearchRadius, 3f, 100f);
            if (bill.ingredientSearchRadius >= 100f)
            {
                bill.ingredientSearchRadius = 999f;
            }
            listing_Standard.End();
            y += IngredientRadiusSubdialogHeight;
        }

        private IEnumerable<FloatMenuOption> GeneratePawnRestrictionOptions()
        {
            if (ModsConfig.BiotechActive && bill.recipe.mechanitorOnlyRecipe)
            {
                yield return new FloatMenuOption("AnyMechanitor".Translate(), () =>
                {
                    bill.SetAnyPawnRestriction();
                });
            }
            else
            {
                yield return new FloatMenuOption("AnyWorker".Translate(), () =>
                {
                    bill.SetAnyPawnRestriction();
                });
                if (ModsConfig.IdeologyActive)
                {
                    yield return new FloatMenuOption("AnySlave".Translate(), () =>
                    {
                        bill.SetAnySlaveRestriction();
                    });
                }
                if (ModsConfig.BiotechActive && MechWorkUtility.AnyWorkMechCouldDo(bill.recipe))
                {
                    yield return new FloatMenuOption("AnyMech".Translate(), () =>
                    {
                        bill.SetAnyMechRestriction();
                    });
                    yield return new FloatMenuOption("AnyNonMech".Translate(), () =>
                    {
                        bill.SetAnyNonMechRestriction();
                    });
                }
            }
        }
    }
}

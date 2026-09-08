using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class Dialog_GlobalBillSettings : Window
    {
        public Dialog_GlobalBillSettings()
        {
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(250f, 450f);

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            string ingredientRadiusLabel = "IngredientSearchRadius".Translate().Truncate(inRect.width * 0.6f);
            string ingredientRadiusValue = DefaultsSettings.DefaultBillIngredientSearchRadius == 999f ? "Unlimited".TranslateSimple().Truncate(inRect.width * 0.3f) : DefaultsSettings.DefaultBillIngredientSearchRadius.ToString("F0");
            listing.Label(ingredientRadiusLabel + ": " + ingredientRadiusValue);
            DefaultsSettings.DefaultBillIngredientSearchRadius = listing.Slider((DefaultsSettings.DefaultBillIngredientSearchRadius > 100f) ? 100f : DefaultsSettings.DefaultBillIngredientSearchRadius, 3f, 100f);
            if (DefaultsSettings.DefaultBillIngredientSearchRadius >= 100f)
            {
                DefaultsSettings.DefaultBillIngredientSearchRadius = 999f;
            }

            listing.Label("AllowedSkillRange".Translate("") + ": " + (DefaultsSettings.DefaultBillAllowedSkillRange.max == 20 ? "Unlimited".TranslateSimple() : ""));
            listing.IntRange(ref DefaultsSettings.DefaultBillAllowedSkillRange, 0, 20);
            listing.Gap(12f);

            if (listing.ButtonText(DefaultsSettings.DefaultBillStoreMode.LabelCap))
            {
                Find.WindowStack.Add(new FloatMenu(DefDatabase<BillStoreModeDef>.AllDefsListForReading.Where(d => d != BillStoreModeDefOf.SpecificStockpile).Select(d => new FloatMenuOption(d.LabelCap, () =>
                {
                    DefaultsSettings.DefaultBillStoreMode = d;
                })).ToList()));
            }

            listing.End();
        }
    }
}

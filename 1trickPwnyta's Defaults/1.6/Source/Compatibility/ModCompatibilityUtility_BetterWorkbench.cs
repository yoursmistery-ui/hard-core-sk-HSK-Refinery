using Defaults.WorkbenchBills;
using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Compatibility
{
    public static class ModCompatibilityUtility_BetterWorkbench
    {
        private static readonly bool betterWorkbenchActive = AccessTools.TypeByName("ImprovedWorkbenches.BillConfig_DoWindowContents_Patch") != null;

        private static object GetBetterWorkbenchExtendedBillData(Bill_Production bill)
        {
            object main = AccessTools.TypeByName("ImprovedWorkbenches.Main").PropertyGetter("Instance").Invoke(null, new object[] { });
            object extendedBillDataStorage = main.GetType().Method("GetExtendedBillDataStorage").Invoke(main, new object[] { });
            object extendedBillData = extendedBillDataStorage.GetType().Method("GetOrCreateExtendedDataFor").Invoke(extendedBillDataStorage, new[] { bill });
            return extendedBillData;
        }

        public static void ApplyBetterWorkbenchOptions(BetterWorkbenchOptions options, Bill_Production bill)
        {
            if (betterWorkbenchActive)
            {
                object extendedBillData = GetBetterWorkbenchExtendedBillData(bill);
                if (options.CountAdditionalItems.Any())
                {
                    if (!(extendedBillData.GetType().Field("ProductAdditionalFilter").GetValue(extendedBillData) is ThingFilter filter))
                    {
                        filter = new ThingFilter();
                        extendedBillData.GetType().Field("ProductAdditionalFilter").SetValue(extendedBillData, filter);
                    }
                    filter.SetDisallowAll();
                    foreach (ThingDef def in options.CountAdditionalItems)
                    {
                        filter.SetAllow(def, true);
                    }
                }
                extendedBillData.GetType().Field("CountAway").SetValue(extendedBillData, options.CountWhenAway);
            }
        }

        public static void SetBetterWorkbenchOptions(BetterWorkbenchOptions options, Bill_Production bill)
        {
            if (betterWorkbenchActive)
            {
                object extendedBillData = GetBetterWorkbenchExtendedBillData(bill);
                ThingFilter filter = extendedBillData.GetType().Field("ProductAdditionalFilter").GetValue(extendedBillData) as ThingFilter ?? new ThingFilter();
                options.CountAdditionalItems = filter.AllowedThingDefs.ToHashSet();
                options.CountWhenAway = (bool)extendedBillData.GetType().Field("CountAway").GetValue(extendedBillData);
            }
        }

        public static void WriteBetterWorkbenchOptions(ref BetterWorkbenchOptions betterWorkbenchOptions)
        {
            if (betterWorkbenchActive)
            {
                Scribe_Deep.Look(ref betterWorkbenchOptions, "betterWorkbenchOptions");
                if (Scribe.mode == LoadSaveMode.PostLoadInit && betterWorkbenchOptions == null)
                {
                    betterWorkbenchOptions = new BetterWorkbenchOptions();
                }
            }
        }

        public static void DoBetterWorkbenchOptionsInterface(Rect rect, BillTemplate bill)
        {
            if (betterWorkbenchActive)
            {
                if (bill.recipe != null && bill.recipe.specialProducts == null && bill.recipe.products?.Count == 1 && bill.repeatMode == BillRepeatModeDefOf.TargetCount)
                {
                    Listing_Standard listing = new Listing_Standard();
                    listing.Begin(rect);
                    if (listing.ButtonText("IW.OutputFilterLabel".Translate()))
                    {
                        Find.WindowStack.Add(new Dialog_BetterWorkbenchAdditionalItems(bill.betterWorkbenchOptions));
                    }
                    listing.Gap(2f);
                    listing.CheckboxLabeled("IW.CountAwayLabel".Translate(), ref bill.betterWorkbenchOptions.CountWhenAway, "IW.CountAwayDesc".Translate());
                    listing.End();
                }
            }
        }
    }
}

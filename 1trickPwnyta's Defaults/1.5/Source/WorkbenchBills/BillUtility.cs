using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace Defaults.WorkbenchBills
{
    public static class BillUtility
    {
        private static readonly Type rr = AccessTools.TypeByName("PeteTimesSix.ResearchReinvented.Extensions.RecipeDefExtensions");

        public static bool IsSuspendedOrUnavailable(this Bill bill)
        {
            // Mod compatibility with Research Reinvented
            bool rrPrototypeAvailable = false;
            if (rr != null)
            {
                rrPrototypeAvailable = (bool)rr.Method("IsAvailableOnlyForPrototyping").Invoke(null, new object[] { bill.recipe, true });
            }

            return bill.suspended || !(bill.recipe.AvailableNow || rrPrototypeAvailable);
        }

        public static void DoBillRepeatModeMenu(this BillTemplate bill)
        {
            Find.WindowStack.Add(new FloatMenu(DefDatabase<BillRepeatModeDef>.AllDefsListForReading.Select(d => new FloatMenuOption(d.LabelCap, () =>
            {
                if (d != BillRepeatModeDefOf.TargetCount || bill.recipe.WorkerCounter.CanCountProducts(null))
                {
                    bill.repeatMode = d;
                }
                else
                {
                    Messages.Message("RecipeCannotHaveTargetCount".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            })).ToList()));
        }
    }
}

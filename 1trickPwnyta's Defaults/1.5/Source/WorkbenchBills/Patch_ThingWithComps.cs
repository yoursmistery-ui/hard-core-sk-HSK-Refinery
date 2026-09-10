using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace Defaults.WorkbenchBills
{
    [HarmonyPatch(typeof(ThingWithComps))]
    [HarmonyPatch(nameof(ThingWithComps.PostMake))]
    public static class Patch_ThingWithComps
    {
        public static void Postfix(ThingWithComps __instance)
        {
            if (__instance is Building_WorkTable table && DefaultsSettings.DefaultWorkbenchBills != null)
            {
                foreach (BillTemplate bill in DefaultsSettings.DefaultWorkbenchBills.Where(s => s.workbenchGroup.Contains(table.def)).SelectMany(s => s.bills))
                {
                    if (bill.use && table.billStack.Count < 15)
                    {
                        table.billStack.AddBill(bill.ToBill());
                    }
                }
            }
        }
    }
}

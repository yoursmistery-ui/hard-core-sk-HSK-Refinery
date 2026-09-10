using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Defaults.WorkbenchBills
{
    [HarmonyPatchCategory("WorkbenchBills")]
    public static class Patch_Thing
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(Thing).Method(nameof(Thing.SetFactionDirect));
            yield return typeof(Thing).Method(nameof(Thing.SetFaction));
        }

        public static void Prefix(Faction ___factionInt, ref Faction __state) => __state = ___factionInt;

        public static void Postfix(Thing __instance, Faction newFaction, Faction __state)
        {
            if (__state != newFaction && newFaction?.IsPlayer == true && __instance is Building_WorkTable table)
            {
                List<WorkbenchBillStore> workbenchBills = Settings.Get<List<WorkbenchBillStore>>(Settings.WORKBENCH_BILLS);
                if (workbenchBills != null)
                {
                    bool limit15 = Settings.GetValue<bool>(Settings.LIMIT_BILLS_TO_15);
                    foreach (BillTemplate bill in workbenchBills.Where(s => s.workbenchGroup.Contains(table.def)).SelectMany(s => s.bills))
                    {
                        if (bill.use && (table.billStack.Count < 15 || !limit15))
                        {
                            table.billStack.AddBill(bill.ToBill());
                        }
                    }
                }
            }
        }
    }
}

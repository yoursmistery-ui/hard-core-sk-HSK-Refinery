using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Defaults.WorkbenchBills
{
    [HarmonyPatchCategory("WorkbenchBills")]
    [HarmonyPatchMod("falconne.BWM")]
    public static class CompatibilityPatch_ImprovedWorkbenches
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.TypeByName("ImprovedWorkbenches.Main").Method("GetMaxBills");
        }

        public static void Postfix(ref int __result)
        {
            if (!Settings.GetValue<bool>(Settings.LIMIT_BILLS_TO_15))
            {
                __result = 125;
            }
        }
    }
}

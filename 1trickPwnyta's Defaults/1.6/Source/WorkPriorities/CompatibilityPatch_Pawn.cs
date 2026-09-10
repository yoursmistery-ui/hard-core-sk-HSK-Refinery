using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace Defaults.WorkPriorities
{
    [HarmonyPatchMod("cyanobot.toddlers")]
    [HarmonyPatchCategory("WorkPriorities")]
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.WorkTypeIsDisabled))]
    public static class CompatibilityPatch_Pawn
    {
        public static void Postfix(List<WorkTypeDef> ___cachedDisabledWorkTypes, WorkTypeDef w, ref bool __result)
        {
            if (__result)
            {
                __result = ___cachedDisabledWorkTypes.Contains(w);
            }
        }
    }
}

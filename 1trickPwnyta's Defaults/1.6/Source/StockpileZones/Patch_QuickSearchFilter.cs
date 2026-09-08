using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.StockpileZones
{
    [HarmonyPatchCategory("Storage")]
    [HarmonyPatch(typeof(QuickSearchFilter))]
    [HarmonyPatch(nameof(QuickSearchFilter.Matches))]
    [HarmonyPatch(new[] { typeof(ThingDef) })]
    public static class Patch_QuickSearchFilter_Matches
    {
        public static bool Prefix(ThingDef td, QuickSearchFilter __instance, ref bool __result)
        {
            if (Find.HiddenItemsManager == null)
            {
                __result = __instance.Matches(td.label);
                return false;
            }
            return true;
        }
    }
}

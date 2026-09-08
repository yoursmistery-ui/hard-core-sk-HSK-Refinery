using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.StockpileZones
{
    [HarmonyPatchCategory("Storage")]
    [HarmonyPatch(typeof(Find))]
    [HarmonyPatch(nameof(Find.HiddenItemsManager), MethodType.Getter)]
    public static class Patch_Find_get_HiddenItemsManager
    {
        public static bool Prefix(ref HiddenItemsManager __result)
        {
            if (Current.Game == null)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}

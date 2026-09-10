using HarmonyLib;
using RimWorld;

namespace Defaults.StockpileZones.Shelves
{
    [HarmonyPatch(typeof(Blueprint_Storage))]
    [HarmonyPatch(nameof(Blueprint_Storage.PostMake))]
    public static class Patch_Blueprint_Storage
    {
        public static void Postfix(Blueprint_Storage __instance)
        {
            if (__instance.BuildDef.IsShelf())
            {
                __instance.settings.Priority = DefaultsSettings.DefaultShelfSettings.Priority;
                __instance.settings.filter.CopyAllowancesFrom(DefaultsSettings.DefaultShelfSettings.filter);
            }
        }
    }
}

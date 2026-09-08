using HarmonyLib;
using RimWorld;

namespace Defaults.StockpileZones.Buildings
{
    [HarmonyPatchCategory("Storage")]
    [HarmonyPatch(typeof(Building_OutfitStand))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatchMod("Ludeon.RimWorld.Odyssey")]
    public static class Patch_Building_OutfitStand
    {
        public static void Postfix(ref bool ___allowRemovingItems)
        {
            ___allowRemovingItems = Settings.GetValue<bool>(Settings.ALLOW_REMOVE_OUTFIT);
        }
    }
}

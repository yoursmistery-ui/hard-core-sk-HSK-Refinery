using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Misc.TargetTemperature
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(ThingComp))]
    [HarmonyPatch(nameof(ThingComp.PostPostMake))]
    public static class Patch_ThingComp
    {
        public static void Postfix(ThingComp __instance)
        {
            if (__instance is CompTempControl comp)
            {
                if (comp.parent is Building_Heater || comp.parent is Building_Cooler)
                {
                    comp.TargetTemperature = PatchUtility_CompTempControl.GetDefaultTargetTemperature(comp);
                }
            }
        }
    }
}

using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.TargetTemperature
{
    [HarmonyPatch(typeof(ThingComp))]
    [HarmonyPatch(nameof(ThingComp.PostPostMake))]
    public static class Patch_ThingComp
    {
        public static void Postfix(ThingComp __instance)
        {
            if (__instance is CompTempControl comp && !(comp is Comp_AtmosphericHeater))
            {
                comp.TargetTemperature = PatchUtility_CompTempControl.GetDefaultTargetTemperature(comp);
            }
        }
    }
}

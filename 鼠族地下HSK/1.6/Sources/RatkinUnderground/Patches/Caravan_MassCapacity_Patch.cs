using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;

namespace RatkinUnderground;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Caravan), "MassCapacity", MethodType.Getter)]
public static class Caravan_MassCapacity_Patch
{
    public static void Postfix(Caravan __instance, ref float __result)
    {
        if (__instance is RKU_DrillingVehicleOnMap)
        {
            float originalValue = __result;
            __result = 1000f;
        }
    }
}



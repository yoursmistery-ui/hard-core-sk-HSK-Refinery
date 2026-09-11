using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RatkinUnderground;

[StaticConstructorOnStartup]

[HarmonyPatch(typeof(IncidentWorker_CaravanDemand), "TryExecuteWorker")]
public static class CaravanDemand_Patch
{
    public static bool Prefix(IncidentParms parms, ref bool __result)
    {
        // 检查目标是否是RKU_DrillingVehicleOnMap类型的商队
        if (parms.target is RKU_DrillingVehicleOnMap)
        {
            __result = false;
            return false; 
        }
        return true;
    }
}

using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Misc.PlantType
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(Zone_Growing))]
    [HarmonyPatch(nameof(Zone_Growing.PlantDefToGrow), MethodType.Getter)]
    public static class Patch_Zone_Growing_get_PlantDefToGrow
    {
        public static void Prefix(Zone_Growing __instance, ref ThingDef ___plantDefToGrow)
        {
            if (___plantDefToGrow == null)
            {
                ___plantDefToGrow = PollutionUtility.SettableEntirelyPolluted(__instance)
                    ? ThingDefOf.Plant_Toxipotato
                    : Settings.Get<ThingDef>(Settings.PLANT_TYPE);
            }
        }
    }
}

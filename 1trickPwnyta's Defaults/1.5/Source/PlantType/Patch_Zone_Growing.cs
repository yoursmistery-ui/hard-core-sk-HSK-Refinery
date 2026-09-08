using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Medicine
{
    [HarmonyPatch(typeof(Zone_Growing), "get_PlantDefToGrow")]
    public static class Patch_Zone_Growing_get_PlantDefToGrow
    {
        public static void Prefix(Zone_Growing __instance, ref ThingDef ___plantDefToGrow)
        {
            if (___plantDefToGrow == null)
            {
                if (PollutionUtility.SettableEntirelyPolluted(__instance))
                {
                    ___plantDefToGrow = ThingDefOf.Plant_Toxipotato;
                }
                else
                {
                    ___plantDefToGrow = DefDatabase<ThingDef>.GetNamed(DefaultsSettings.DefaultPlantType);
                }
            }
        }
    }
}

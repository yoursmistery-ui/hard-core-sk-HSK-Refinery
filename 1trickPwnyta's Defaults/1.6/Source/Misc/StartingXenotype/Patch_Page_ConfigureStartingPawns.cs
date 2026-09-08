using HarmonyLib;
using RimWorld;

namespace Defaults.Misc.StartingXenotype
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns))]
    [HarmonyPatch(nameof(Page_ConfigureStartingPawns.PostOpen))]
    [HarmonyPatchMod("Ludeon.RimWorld.Biotech")]
    public static class Patch_Page_ConfigureStartingPawns
    {
        public static void Postfix()
        {
            StartingXenotypeUtility.InitializeStartingPawns();
        }
    }
}

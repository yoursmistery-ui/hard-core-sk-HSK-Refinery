using HarmonyLib;
using Verse;

namespace Defaults.Misc.HostilityResponse
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(PawnGenerator))]
    [HarmonyPatch("GenerateNewPawnInternal")]
    public static class Patch_PawnGenerator_GenerateNewPawnInternal
    {
        public static void Postfix(Pawn __result)
        {
            if (__result != null)
            {
                HostilityResponseModeUtility.SetHostilityResponseMode(__result, __result.playerSettings);
            }
        }
    }
}

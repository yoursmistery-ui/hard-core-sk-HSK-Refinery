using HarmonyLib;
using Verse;

namespace Defaults.HostilityResponse
{
    [HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
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

using HarmonyLib;
using Verse;

namespace Defaults.Misc.HostilityResponse
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction
    {
        public static void Postfix(Pawn __instance)
        {
            HostilityResponseModeUtility.SetHostilityResponseMode(__instance, __instance.playerSettings);
        }
    }
}

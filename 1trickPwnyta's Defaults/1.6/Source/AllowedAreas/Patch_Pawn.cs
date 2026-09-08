using HarmonyLib;
using Verse;

namespace Defaults.AllowedAreas
{
    [HarmonyPatchCategory("AllowedAreas")]
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.SetFaction))]
    public static class Patch_Pawn
    {
        public static void Postfix(Pawn __instance)
        {
            AllowedAreaUtility.SetDefaultAllowedArea(__instance);
        }
    }
}

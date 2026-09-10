using HarmonyLib;
using Verse;

namespace Defaults.Policies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.SetFaction))]
    public static class Patch_Pawn
    {
        public static void Postfix(Pawn __instance)
        {
            PolicyUtility.SetAllDefaultPolicies(__instance);
        }
    }
}

using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.AllowedAreas
{
    [HarmonyPatchCategory("AllowedAreas")]
    [HarmonyPatch(typeof(MutantUtility))]
    [HarmonyPatch(nameof(MutantUtility.SetPawnAsMutantInstantly))]
    public static class Patch_MutantUtility
    {
        public static void Prefix(Pawn pawn, ref PawnType? __state)
        {
            __state = PawnTypeUtility.GetPawnType(pawn);
        }

        public static void Postfix(Pawn pawn, PawnType? __state)
        {
            AllowedAreaUtility.SetDefaultAllowedArea(pawn, __state);
        }
    }
}

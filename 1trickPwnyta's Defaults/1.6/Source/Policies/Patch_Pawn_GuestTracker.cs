using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Policies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(Pawn_GuestTracker))]
    [HarmonyPatch(nameof(Pawn_GuestTracker.SetGuestStatus))]
    public static class Patch_Pawn_GuestTracker
    {
        public static void Prefix(Pawn ___pawn, ref PawnType? __state)
        {
            __state = PawnTypeUtility.GetPawnType(___pawn);
        }

        public static void Postfix(Pawn ___pawn, PawnType? __state)
        {
            PolicyUtility.SetAllDefaultPolicies(___pawn, __state);
        }
    }
}

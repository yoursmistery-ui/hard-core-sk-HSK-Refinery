using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.AllowedAreas
{
    [HarmonyPatchCategory("AllowedAreas")]
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
            AllowedAreaUtility.SetDefaultAllowedArea(___pawn, __state);
        }
    }
}

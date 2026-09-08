using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Policies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(Pawn_AgeTracker))]
    [HarmonyPatch("BirthdayBiological")]
    public static class Patch_Pawn_AgeTracker
    {
        public static void Postfix(Pawn_AgeTracker __instance, Pawn ___pawn, int birthdayAge)
        {
            if (___pawn.RaceProps.Humanlike)
            {
                if (birthdayAge == __instance.AdultMinAge && PawnTypeUtility.GetPawnType(___pawn) == PawnType.AdultColonist)
                {
                    PolicyUtility.SetAllDefaultPolicies(___pawn, PawnType.ChildColonist);
                }
                if (birthdayAge == __instance.LifeStageMinAge(LifeStageDefOf.HumanlikeChild) && PawnTypeUtility.GetPawnType(___pawn) == PawnType.ChildColonist)
                {
                    PolicyUtility.SetAllDefaultPolicies(___pawn);
                }
            }
        }
    }
}

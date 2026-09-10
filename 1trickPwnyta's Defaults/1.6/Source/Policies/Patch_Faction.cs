using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Policies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(Faction))]
    [HarmonyPatch(nameof(Faction.Notify_MemberLeftExtraFaction))]
    public static class Patch_Faction
    {
        public static void Postfix(Pawn member)
        {
            PolicyUtility.SetAllDefaultPolicies(member, PawnType.Guest);
        }
    }
}

using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.AllowedAreas
{
    [HarmonyPatchCategory("AllowedAreas")]
    [HarmonyPatch(typeof(Faction))]
    [HarmonyPatch(nameof(Faction.Notify_MemberLeftExtraFaction))]
    public static class Patch_Faction
    {
        public static void Postfix(Pawn member)
        {
            AllowedAreaUtility.SetDefaultAllowedArea(member, PawnType.Guest);
        }
    }
}

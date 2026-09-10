using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.AllowedAreas
{
    [HarmonyPatchCategory("AllowedAreas")]
    [HarmonyPatch(typeof(PregnancyUtility))]
    [HarmonyPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static class Patch_PregnancyUtility
    {
        public static void Postfix(Thing birtherThing, Thing __result)
        {
            if (__result is Pawn baby && birtherThing is Pawn mother)
            {
                AllowedAreaUtility.SetDefaultAllowedArea(baby, mother: mother);
            }
        }
    }
}

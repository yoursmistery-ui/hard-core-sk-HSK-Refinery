using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.WorkPriorities
{
    [HarmonyPatchCategory("WorkPriorities")]
    [HarmonyPatch(typeof(Pawn_WorkSettings))]
    [HarmonyPatch(nameof(Pawn_WorkSettings.EnableAndInitialize))]
    public static class Patch_Pawn_WorkSettings_EnableAndInitialize
    {
        public static void Postfix(Pawn ___pawn)
        {
            WorkPriorityUtility.SetWorkPrioritiesToDefault(___pawn);
        }
    }
}

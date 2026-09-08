using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace Defaults.WorkPriorities
{
    [HarmonyPatchCategory("WorkPriorities")]
    [HarmonyPatch(typeof(Pawn_IdeoTracker))]
    [HarmonyPatch(nameof(Pawn_IdeoTracker.SetIdeo))]
    public static class Patch_Pawn_IdeoTracker
    {
        public static void Prefix(Pawn ___pawn, ref Ideo __state)
        {
            __state = ___pawn.Ideo;
        }

        public static void Postfix(Pawn ___pawn, Ideo ideo, Ideo __state)
        {
            foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading.Except(___pawn.GetDisabledWorkTypes()).Where(w => (ideo?.IsWorkTypeConsideredDangerous(w) ?? false) || (__state?.IsWorkTypeConsideredDangerous(w) ?? false)))
            {
                WorkPriorityUtility.SetWorkPrioritiesToDefault(___pawn, def);
            }
        }
    }
}

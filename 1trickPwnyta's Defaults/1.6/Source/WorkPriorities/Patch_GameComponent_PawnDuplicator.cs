using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.WorkPriorities
{
    [HarmonyPatchCategory("WorkPriorities")]
    [HarmonyPatch(typeof(GameComponent_PawnDuplicator))]
    [HarmonyPatch(nameof(GameComponent_PawnDuplicator.Duplicate))]
    public static class Patch_GameComponent_PawnDuplicator
    {
        public static void Postfix(Pawn __result)
        {
            WorkPriorityUtility.SetWorkPrioritiesToDefault(__result);
        }
    }
}

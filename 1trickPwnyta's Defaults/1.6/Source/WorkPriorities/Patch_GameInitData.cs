using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace Defaults.WorkPriorities
{
    [HarmonyPatchCategory("WorkPriorities")]
    [HarmonyPatch(typeof(GameInitData))]
    [HarmonyPatch(nameof(GameInitData.PrepForMapGen))]
    public static class Patch_GameInitData
    {
        public static void Postfix(List<Pawn> ___startingAndOptionalPawns)
        {
            foreach (Pawn pawn in ___startingAndOptionalPawns)
            {
                WorkPriorityUtility.SetWorkPrioritiesToDefault(pawn);
            }
        }
    }
}

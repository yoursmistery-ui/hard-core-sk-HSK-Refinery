using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorkPriorities
{
    [HarmonyPatchCategory("WorkPriorities")]
    [HarmonyPatch(typeof(ChoiceLetter_GrowthMoment))]
    [HarmonyPatch(nameof(ChoiceLetter_GrowthMoment.MakeChoices))]
    public static class Patch_ChoiceLetter_GrowthMoment
    {
        public static void Postfix(Pawn ___pawn, List<SkillDef> skills)
        {
            foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading.Except(___pawn.GetDisabledWorkTypes()).Where(d => d.relevantSkills.Intersect(skills).Any()))
            {
                WorkPriorityUtility.SetWorkPrioritiesToDefault(___pawn, def);
            }
        }
    }
}

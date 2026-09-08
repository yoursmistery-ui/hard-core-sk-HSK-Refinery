using Defaults.Compatibility;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorkPriorities
{
    [HarmonyPatchCategory("WorkPriorities")]
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.Notify_DisabledWorkTypesChanged))]
    public static class Patch_Pawn
    {
        public static void Prefix(Pawn __instance, List<WorkTypeDef> ___cachedDisabledWorkTypes, List<WorkTypeDef> ___cachedDisabledWorkTypesPermanent)
        {
            if (___cachedDisabledWorkTypes != null)
            {
                foreach (WorkTypeDef def in ___cachedDisabledWorkTypes.ListFullCopy().Except(__instance.GetDisabledWorkTypes()))
                {
                    WorkPriorityUtility.SetWorkPrioritiesToDefault(__instance, def);
                }
            }

            if (___cachedDisabledWorkTypesPermanent != null)
            {
                foreach (WorkTypeDef def in ___cachedDisabledWorkTypesPermanent.ListFullCopy().Except(__instance.GetDisabledWorkTypes(true)))
                {
                    WorkPriorityUtility.SetWorkPrioritiesToDefault(__instance, def);
                }
            }
        }

        public static void Postfix(Pawn __instance)
        {
            List<WorkTypeDef> disabledWorkTypes = __instance.GetDisabledWorkTypes();
            ModCompatibilityUtility_Toddlers.FixDisabledWorkTypes(__instance, ref disabledWorkTypes);

            // tmpEnabledWorkTypes includes all newly enabled work types for a new life stage
            if (typeof(Pawn_AgeTracker).Field("tmpEnabledWorkTypes").GetValue(null) is List<WorkTypeDef> ageWorkTypes)
            {
                // When first becoming a child, assume all work types are newly enabled
                if (ageWorkTypes.Any() && __instance.ageTracker.AgeBiologicalYears == (int)__instance.RaceProps.lifeStageAges.First(l => l.def == LifeStageDefOf.HumanlikeChild).minAge)
                {
                    ageWorkTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
                }

                foreach (WorkTypeDef def in ageWorkTypes.Where(w => !disabledWorkTypes.Contains(w)))
                {
                    WorkPriorityUtility.SetWorkPrioritiesToDefault(__instance, def);
                }
            }
        }
    }
}

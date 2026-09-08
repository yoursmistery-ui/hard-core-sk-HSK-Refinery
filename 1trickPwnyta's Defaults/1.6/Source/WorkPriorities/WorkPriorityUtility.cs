using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorkPriorities
{
    public static class WorkPriorityUtility
    {
        public static List<Rule> ruleClipboard = new List<Rule>();

        public static void SetWorkPrioritiesToDefault(Pawn pawn)
        {
            if (pawn.IsFreeColonist)
            {
                foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(d => !pawn.WorkTypeIsDisabled(d)))
                {
                    SetWorkPrioritiesToDefault(pawn, def);
                }
            }
        }

        public static void SetWorkPrioritiesToDefault(Pawn pawn, WorkTypeDef def)
        {
            if (pawn.IsFreeColonist)
            {
                if (Settings.Get<bool>(Settings.WORK_PRIORITIES_ADVANCED_MODE))
                {
                    List<Rule> advancedGlobalWorkPriorityLogic = Settings.Get<List<Rule>>(Settings.WORK_PRIORITIES_GLOBAL_LOGIC);
                    List<Rule> advancedSpecificWorkPriorityLogic = Settings.Get<Dictionary<WorkTypeDef, List<Rule>>>(Settings.WORK_PRIORITIES_LOGIC).TryGetValue(def);
                    int original = pawn.workSettings.GetPriority(def);
                    bool? applied = ApplyRules(advancedGlobalWorkPriorityLogic.ConcatIfNotNull(advancedSpecificWorkPriorityLogic), def, pawn);
                    if (applied.HasValue && !applied.Value)
                    {
                        pawn.workSettings.SetPriority(def, original);
                    }
                }
                else
                {
                    Dictionary<WorkTypeDef, int> basicDefaultWorkPriorities = Settings.Get<Dictionary<WorkTypeDef, int>>(Settings.WORK_PRIORITIES_BASIC);
                    if (basicDefaultWorkPriorities.ContainsKey(def))
                    {
                        int priority = basicDefaultWorkPriorities[def];
                        if (priority == WorkPriorityValue.DoNotDo)
                        {
                            pawn.workSettings.Disable(def);
                        }
                        else if (priority > 0)
                        {
                            pawn.workSettings.SetPriority(def, priority);
                        }
                    }
                }
            }
        }

        public static bool? ApplyRules(IEnumerable<Rule> rules, WorkTypeDef def, Pawn pawn)
        {
            bool? applied = null;
            foreach (Rule rule in rules)
            {
                bool? appliedRule = rule.Apply(def, pawn);
                if (appliedRule.HasValue)
                {
                    applied = appliedRule.Value;
                }
            }
            return applied;
        }
    }
}

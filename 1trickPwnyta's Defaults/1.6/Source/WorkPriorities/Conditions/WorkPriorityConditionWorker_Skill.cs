using Defaults.Defs;
using Defaults.UI;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class WorkPriorityConditionWorker_Skill : WorkPriorityConditionWorker<Condition_Skill>
    {
        public WorkPriorityConditionWorker_Skill(WorkPriorityConditionDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Condition_Skill condition)
        {
            DoAboveOrBelowDropdown(rect.LeftPartPixels(rect.height), condition.above, above => condition.above = above);
            UIUtility.IntEntry(rect.RightPartPixels(rect.width - rect.height), ref condition.level, ref condition.editBuffer);
        }
    }
}

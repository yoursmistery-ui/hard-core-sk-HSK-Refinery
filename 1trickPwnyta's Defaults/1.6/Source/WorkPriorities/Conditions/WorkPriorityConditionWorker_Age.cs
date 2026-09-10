using Defaults.Defs;
using Defaults.UI;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class WorkPriorityConditionWorker_Age : WorkPriorityConditionWorker<Condition_Age>
    {
        public WorkPriorityConditionWorker_Age(WorkPriorityConditionDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Condition_Age condition)
        {
            DoAboveOrBelowDropdown(rect.LeftPartPixels(rect.height), condition.above, above => condition.above = above);
            UIUtility.IntEntry(rect.RightPartPixels(rect.width - rect.height), ref condition.years, ref condition.editBuffer);
        }
    }
}

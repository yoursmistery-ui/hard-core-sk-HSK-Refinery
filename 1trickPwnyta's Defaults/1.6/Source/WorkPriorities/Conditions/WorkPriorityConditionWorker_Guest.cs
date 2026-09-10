using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;

namespace Defaults.WorkPriorities.Conditions
{
    public class WorkPriorityConditionWorker_Guest : WorkPriorityConditionWorker<Condition_Guest>
    {
        public WorkPriorityConditionWorker_Guest(WorkPriorityConditionDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Condition_Guest condition)
        {
        }
    }
}

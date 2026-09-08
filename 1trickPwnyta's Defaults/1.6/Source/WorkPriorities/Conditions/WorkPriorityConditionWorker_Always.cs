using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;

namespace Defaults.WorkPriorities.Conditions
{
    public class WorkPriorityConditionWorker_Always : WorkPriorityConditionWorker<Condition_Always>
    {
        public WorkPriorityConditionWorker_Always(WorkPriorityConditionDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Condition_Always condition)
        {
        }
    }
}

using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;

namespace Defaults.WorkPriorities.Conditions
{
    public class WorkPriorityConditionWorker_Slave : WorkPriorityConditionWorker<Condition_Slave>
    {
        public WorkPriorityConditionWorker_Slave(WorkPriorityConditionDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Condition_Slave condition)
        {
        }
    }
}

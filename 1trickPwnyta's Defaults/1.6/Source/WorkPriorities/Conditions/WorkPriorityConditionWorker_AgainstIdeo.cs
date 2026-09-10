using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;

namespace Defaults.WorkPriorities.Conditions
{
    public class WorkPriorityConditionWorker_AgainstIdeo : WorkPriorityConditionWorker<Condition_AgainstIdeo>
    {
        public WorkPriorityConditionWorker_AgainstIdeo(WorkPriorityConditionDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Condition_AgainstIdeo condition)
        {
        }
    }
}

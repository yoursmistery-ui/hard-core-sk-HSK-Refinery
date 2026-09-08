using Defaults.Workers;
using System;

namespace Defaults.Defs
{
    public class WorkPriorityConditionDef : DefWithIcon
    {
        private IWorkPriorityConditionWorker worker;

        public Type workerClass;

        public IWorkPriorityConditionWorker Worker
        {
            get
            {
                if (worker == null)
                {
                    worker = (IWorkPriorityConditionWorker)Activator.CreateInstance(workerClass, new[] { this });
                }
                return worker;
            }
        }
    }
}

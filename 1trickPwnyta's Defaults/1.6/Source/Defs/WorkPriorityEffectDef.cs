using Defaults.Workers;
using System;

namespace Defaults.Defs
{
    public class WorkPriorityEffectDef : DefWithIcon
    {
        private IWorkPriorityEffectWorker worker;

        public Type workerClass;

        public IWorkPriorityEffectWorker Worker
        {
            get
            {
                if (worker == null)
                {
                    worker = (IWorkPriorityEffectWorker)Activator.CreateInstance(workerClass, new[] { this });
                }
                return worker;
            }
        }
    }
}

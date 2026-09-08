using Defaults.Defs;
using Defaults.WorkPriorities.Conditions;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.Workers
{
    public interface IWorkPriorityConditionWorker
    {
        void DoUI(Rect rect, Condition condition);

        Condition MakeCondition();
    }

    public abstract class WorkPriorityConditionWorker<T> : IWorkPriorityConditionWorker where T : Condition
    {
        public readonly WorkPriorityConditionDef def;

        public WorkPriorityConditionWorker(WorkPriorityConditionDef def)
        {
            this.def = def;
        }

        protected abstract void DoUI(Rect rect, T condition);

        public void DoUI(Rect rect, Condition condition) => DoUI(rect, condition as T);

        public Condition MakeCondition() => Activator.CreateInstance(typeof(T), new[] { def }) as Condition;

        protected void DoAboveOrBelowDropdown(Rect rect, bool above, Action<bool> callback)
        {
            string label(bool b) => (b ? ">" : "<");

            if (Widgets.ButtonText(rect, label(above)))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>()
                {
                    new FloatMenuOption(label(true), () => callback(true)),
                    new FloatMenuOption(label(false), () => callback(false))
                }));
            }
        }
    }
}

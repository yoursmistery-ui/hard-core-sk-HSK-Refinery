using Defaults.Defs;
using Defaults.WorkPriorities.Effects;
using System;
using UnityEngine;

namespace Defaults.Workers
{
    public interface IWorkPriorityEffectWorker
    {
        void DoUI(Rect rect, Effect effect);

        Effect MakeEffect();
    }

    public abstract class WorkPriorityEffectWorker<T> : IWorkPriorityEffectWorker where T : Effect
    {
        public readonly WorkPriorityEffectDef def;

        public WorkPriorityEffectWorker(WorkPriorityEffectDef def)
        {
            this.def = def;
        }

        protected abstract void DoUI(Rect rect, T effect);

        public void DoUI(Rect rect, Effect effect) => DoUI(rect, effect as T);

        public Effect MakeEffect() => Activator.CreateInstance(typeof(T), new[] { def }) as Effect;
    }
}

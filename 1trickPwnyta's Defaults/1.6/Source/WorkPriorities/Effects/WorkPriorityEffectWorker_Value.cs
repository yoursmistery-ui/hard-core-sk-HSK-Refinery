using Defaults.Defs;
using Defaults.UI;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.WorkPriorities.Effects
{
    public class WorkPriorityEffectWorker_Value : WorkPriorityEffectWorker<Effect_Value>
    {
        public WorkPriorityEffectWorker_Value(WorkPriorityEffectDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Effect_Value effect)
        {
            if (UIUtility.DoImageTextButton(rect, WorkPriorityValue.GetIcon(effect.value), WorkPriorityValue.GetLabel(effect.value).CapitalizeFirst()))
            {
                Find.WindowStack.Add(new FloatMenu(WorkPriorityValue.GetOptions(x => effect.value = x)));
            }
        }
    }
}

using Defaults.Defs;
using Defaults.UI;
using Defaults.Workers;
using RimWorld;
using UnityEngine;
using Verse;

namespace Defaults.WorkPriorities.Effects
{
    [StaticConstructorOnStartup]
    public class WorkPriorityEffectWorker_Increase : WorkPriorityEffectWorker<Effect_Increase>
    {
        private static readonly Texture2D allowZeroTex = WidgetsWork.WorkBoxBGTex_Mid;

        public WorkPriorityEffectWorker_Increase(WorkPriorityEffectDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Effect_Increase effect)
        {
            UIUtility.IntEntry(rect.LeftPartPixels(rect.width - rect.height), ref effect.amount, ref effect.editBuffer, minimum: 1, maximum: WorkPriorityValue.Max);
            UIUtility.DoCheckButton(rect.RightPartPixels(rect.height), allowZeroTex, "Defaults_WorkPriorityAllowIncreaseFromZero".Translate(), ref effect.allowZero);
        }
    }
}

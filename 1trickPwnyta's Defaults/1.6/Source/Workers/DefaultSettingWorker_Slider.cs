using Defaults.Defs;
using UnityEngine;
using Verse;

namespace Defaults.Workers
{
    public abstract class DefaultSettingWorker_Slider<T> : DefaultSettingWorker<T?> where T : struct
    {
        protected DefaultSettingWorker_Slider(DefaultSettingDef def) : base(def)
        {
        }

        protected abstract T Min { get; }

        protected abstract T Max { get; }

        protected virtual float Increment => 1f;

        protected virtual string LeftLabel => string.Empty;

        protected virtual string MiddleLabel => string.Empty;

        protected virtual string RightLabel => string.Empty;

        protected virtual TaggedString Tip => null;

        protected abstract float GetNumber(T value);

        protected abstract T GetValue(float value);

        public override bool RenderLast => true;

        protected override void DoWidget(Rect rect)
        {
            rect = rect.RightHalf();

            float number = Widgets.HorizontalSlider(rect, GetNumber(setting.Value), GetNumber(Min), GetNumber(Max), true, MiddleLabel, LeftLabel, RightLabel, Increment);
            setting = GetValue(number);

            TaggedString tooltip = Tip;
            if (tooltip != null)
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }
    }
}

using Defaults.Defs;
using UnityEngine;
using Verse;

namespace Defaults.Workers
{
    public abstract class DefaultSettingWorker_IntRange : DefaultSettingWorker<IntRange?>
    {
        protected DefaultSettingWorker_IntRange(DefaultSettingDef def) : base(def)
        {
        }

        protected abstract int Min { get; }

        protected abstract int Max { get; }

        protected virtual int Increment => 1;

        protected virtual string Label => string.Empty;

        protected virtual TaggedString Tip => null;

        protected override void DoWidget(Rect rect)
        {
            rect = rect.RightHalf();

            IntRange range = setting.Value;
            Widgets.IntRange(rect, def.GetHashCode(), ref range, Min, Max, "Defaults_EmptyString");
            setting = range;

            string label = Label;
            if (!label.NullOrEmpty())
            {
                using (new TextBlock(TextAnchor.UpperCenter)) Widgets.Label(rect, label);
            }

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

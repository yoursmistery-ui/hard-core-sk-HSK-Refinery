using Defaults.Defs;
using UnityEngine;
using Verse;

namespace Defaults.Workers
{
    public abstract class DefaultSettingWorker_Options<T> : DefaultSettingWorker<T>, IQuickOption where T : IExposable
    {
        protected DefaultSettingWorker_Options(DefaultSettingDef def) : base(def)
        {
        }

        public FloatMenuOption QuickOption => new FloatMenuOption(def.LabelCap, Configure);

        protected virtual string Text => "Defaults_Configure".Translate();

        protected abstract void Configure();

        protected override void DoWidget(Rect rect)
        {
            string text = Text;
            if (Widgets.ButtonText(rect.RightPartPixels(Mathf.Max(Verse.Text.CalcSize(text).x + 8f, 100f)), text))
            {
                Configure();
            }
        }

        protected override void ExposeSetting()
        {
            Scribe_Deep.Look(ref setting, Key);
        }
    }
}

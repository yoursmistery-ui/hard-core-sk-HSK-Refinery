using Defaults.Defs;
using Defaults.UI;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.Misc.TargetTemperature
{
    public abstract class DefaultSettingWorker_TargetTemp : DefaultSettingWorker<float?>
    {
        protected override float? Default => 21f;

        protected DefaultSettingWorker_TargetTemp(DefaultSettingDef def) : base(def)
        {
        }

        protected override void DoWidget(Rect rect)
        {
            rect.x += rect.width / 2;
            rect.width /= 2;
            float target = setting.Value;
            UIUtility.DoTemperatureEntry(rect, ref target, 1, -50f, 50f);
            setting = target;
        }

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }
    }
}

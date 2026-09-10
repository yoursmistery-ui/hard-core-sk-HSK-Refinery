using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.WorldSettings
{
    public class DefaultSettingWorker_PlanetPollution : DefaultSettingWorker_Slider<float>
    {
        public DefaultSettingWorker_PlanetPollution(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.PLANET_POLLUTION;

        protected override float Min => 0f;

        protected override float Max => 1f;

        protected override float? Default => 0.05f;

        protected override float Increment => 0.05f;

        protected override string MiddleLabel => setting.Value.ToStringPercent();

        protected override float GetNumber(float value) => value;

        protected override float GetValue(float value) => value;
    }
}

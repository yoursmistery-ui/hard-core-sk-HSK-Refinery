using Defaults.Defs;
using Defaults.Workers;
using RimWorld.Planet;
using Verse;

namespace Defaults.WorldSettings
{
    public class DefaultSettingWorker_OverallTemperature : DefaultSettingWorker_Slider<OverallTemperature>
    {
        public DefaultSettingWorker_OverallTemperature(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.OVERALL_TEMPERATURE;

        protected override OverallTemperature Min => OverallTemperature.VeryCold;

        protected override OverallTemperature Max => OverallTemperature.VeryHot;

        protected override OverallTemperature? Default => OverallTemperature.Normal;

        protected override string LeftLabel => "PlanetTemperature_Low".Translate();

        protected override string MiddleLabel => "PlanetTemperature_Normal".Translate();

        protected override string RightLabel => "PlanetTemperature_High".Translate();

        protected override float GetNumber(OverallTemperature value) => (float)value;

        protected override OverallTemperature GetValue(float value) => (OverallTemperature)value;
    }
}

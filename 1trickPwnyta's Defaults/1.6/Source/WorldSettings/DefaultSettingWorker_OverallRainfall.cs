using Defaults.Defs;
using Defaults.Workers;
using RimWorld.Planet;
using Verse;

namespace Defaults.WorldSettings
{
    public class DefaultSettingWorker_OverallRainfall : DefaultSettingWorker_Slider<OverallRainfall>
    {
        public DefaultSettingWorker_OverallRainfall(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.OVERALL_RAINFALL;

        protected override OverallRainfall Min => OverallRainfall.AlmostNone;

        protected override OverallRainfall Max => OverallRainfall.VeryHigh;

        protected override OverallRainfall? Default => OverallRainfall.Normal;

        protected override string LeftLabel => "PlanetRainfall_Low".Translate();

        protected override string MiddleLabel => "PlanetRainfall_Normal".Translate();

        protected override string RightLabel => "PlanetRainfall_High".Translate();

        protected override float GetNumber(OverallRainfall value) => (float)value;

        protected override OverallRainfall GetValue(float value) => (OverallRainfall)value;
    }
}

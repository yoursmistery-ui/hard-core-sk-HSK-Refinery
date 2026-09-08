using Defaults.Defs;
using Defaults.Workers;
using RimWorld.Planet;
using Verse;

namespace Defaults.WorldSettings
{
    public class DefaultSettingWorker_OverallPopulation : DefaultSettingWorker_Slider<OverallPopulation>
    {
        public DefaultSettingWorker_OverallPopulation(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.OVERALL_POPULATION;

        protected override OverallPopulation Min => OverallPopulation.AlmostNone;

        protected override OverallPopulation Max => OverallPopulation.VeryHigh;

        protected override OverallPopulation? Default => OverallPopulation.Normal;

        protected override string LeftLabel => "PlanetPopulation_Low".Translate();

        protected override string MiddleLabel => "PlanetPopulation_Normal".Translate();

        protected override string RightLabel => "PlanetPopulation_High".Translate();

        protected override float GetNumber(OverallPopulation value) => (float)value;

        protected override OverallPopulation GetValue(float value) => (OverallPopulation)value;
    }
}

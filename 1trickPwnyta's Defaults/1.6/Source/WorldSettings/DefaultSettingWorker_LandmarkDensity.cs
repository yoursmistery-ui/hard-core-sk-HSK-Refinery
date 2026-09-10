using Defaults.Defs;
using Defaults.Workers;
using RimWorld.Planet;
using Verse;

namespace Defaults.WorldSettings
{
    public class DefaultSettingWorker_LandmarkDensity : DefaultSettingWorker_Slider<LandmarkDensity>
    {
        public DefaultSettingWorker_LandmarkDensity(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.LANDMARK_DENSITY;

        protected override LandmarkDensity Min => LandmarkDensity.Sparse;

        protected override LandmarkDensity Max => LandmarkDensity.Crowded;

        protected override LandmarkDensity? Default => LandmarkDensity.Normal;

        protected override string LeftLabel => "PlanetLandmarkDensity_Low".Translate();

        protected override string MiddleLabel => "PlanetLandmarkDensity_Normal".Translate();

        protected override string RightLabel => "PlanetLandmarkDensity_High".Translate();

        protected override float GetNumber(LandmarkDensity value) => (float)value;

        protected override LandmarkDensity GetValue(float value) => (LandmarkDensity)value;
    }
}

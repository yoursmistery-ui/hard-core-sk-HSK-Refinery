using Defaults.Defs;
using Defaults.Workers;
using System.Collections.Generic;
using Verse;

namespace Defaults.WorldSettings
{
    public class DefaultSettingWorker_PlanetCoverage : DefaultSettingWorker_Dropdown<float?>
    {
        public static readonly List<float?> PlanetCoverages = new List<float?>
        {
            0.3f,
            0.5f,
            1f
        };

        public DefaultSettingWorker_PlanetCoverage(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.PLANET_COVERAGE;

        protected override IEnumerable<float?> Options => PlanetCoverages;

        protected override float? Default => ModsConfig.OdysseyActive ? 0.5f : 0.3f;

        protected override TaggedString GetText(float? option) => option.Value.ToStringPercent();

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }
    }
}

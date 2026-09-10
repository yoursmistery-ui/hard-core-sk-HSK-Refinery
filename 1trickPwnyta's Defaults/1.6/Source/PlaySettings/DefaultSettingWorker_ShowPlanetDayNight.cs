using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_ShowPlanetDayNight : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_ShowPlanetDayNight(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.SHOW_PLANET_DAY_NIGHT;

        protected override bool? Default => true;

        protected override Texture2D Icon => TexButton.UsePlanetDayNightSystem;

        protected override TaggedString Tip => "UsePlanetDayNightSystemToggleButton".Translate();
    }
}

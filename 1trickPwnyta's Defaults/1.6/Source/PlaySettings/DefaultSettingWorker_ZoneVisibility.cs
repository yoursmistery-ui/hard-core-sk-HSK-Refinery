using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_ZoneVisibility : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_ZoneVisibility(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.ZONE_VISIBILITY;

        protected override bool? Default => true;

        protected override Texture2D Icon => TexButton.ShowZones;

        protected override TaggedString Tip => "ZoneVisibilityToggleButton".Translate();
    }
}

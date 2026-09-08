using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_ShowImportantExpandingIcons : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_ShowImportantExpandingIcons(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.SHOW_EXPANDING_ICONS_IMPORTANT;

        protected override bool? Default => true;

        protected override Texture2D Icon => TexButton.ShowImportantLocations;

        protected override TaggedString Tip => "ShowImportantExpandingIconsToggleButton".Translate();
    }
}

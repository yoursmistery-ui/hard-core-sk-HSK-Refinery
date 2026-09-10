using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_ShowExpandingLandmarks : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_ShowExpandingLandmarks(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.SHOW_EXPANDING_ICONS_LANDMARKS;

        protected override bool? Default => false;

        protected override Texture2D Icon => TexButton.ShowLandmarkIcons;

        protected override TaggedString Tip => "ShowExpandingLandmarksToggleButton".Translate();
    }
}

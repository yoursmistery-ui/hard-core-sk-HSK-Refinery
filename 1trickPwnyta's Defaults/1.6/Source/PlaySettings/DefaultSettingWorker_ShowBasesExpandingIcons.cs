using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_ShowBasesExpandingIcons : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_ShowBasesExpandingIcons(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.SHOW_EXPANDING_ICONS_BASES;

        protected override bool? Default => true;

        protected override Texture2D Icon => TexButton.ShowOtherFactionBases;

        protected override TaggedString Tip => "ShowBasesExpandingIconsToggleButton".Translate();
    }
}

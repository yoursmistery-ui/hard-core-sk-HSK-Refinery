using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_AutoHomeArea : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_AutoHomeArea(DefaultSettingDef def) : base(def)
        {
        }

        protected override bool? Default => true;

        public override string Key => Settings.AUTO_HOME_AREA;

        protected override Texture2D Icon => TexButton.AutoHomeArea;

        protected override TaggedString Tip => "AutoHomeAreaToggleButton".Translate();
    }
}

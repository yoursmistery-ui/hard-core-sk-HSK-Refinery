using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_ShowWorldFeatures : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_ShowWorldFeatures(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.SHOW_WORLD_FEATURES;

        protected override bool? Default => true;

        protected override Texture2D Icon => TexButton.ShowWorldFeatures;

        protected override TaggedString Tip => "ShowWorldFeaturesToggleButton".Translate();
    }
}

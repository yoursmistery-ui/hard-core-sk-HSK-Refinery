using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_AutoRebuild : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_AutoRebuild(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.AUTO_REBUILD;

        protected override bool? Default => false;

        protected override Texture2D Icon => TexButton.AutoRebuild;

        protected override TaggedString Tip => "AutoRebuildButton".Translate();
    }
}

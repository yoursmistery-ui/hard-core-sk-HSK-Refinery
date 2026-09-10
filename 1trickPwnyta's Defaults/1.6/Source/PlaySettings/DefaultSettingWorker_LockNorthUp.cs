using Defaults.Defs;
using Defaults.Workers;
using UnityEngine;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingWorker_LockNorthUp : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_LockNorthUp(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.LOCK_NORTH_UP;

        protected override bool? Default => true;

        protected override Texture2D Icon => TexButton.LockNorthUp;

        protected override TaggedString Tip => "LockNorthUpToggleButton".Translate();
    }
}

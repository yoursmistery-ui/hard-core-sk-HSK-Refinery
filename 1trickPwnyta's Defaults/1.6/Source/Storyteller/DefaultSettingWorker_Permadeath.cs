using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.Storyteller
{
    public class DefaultSettingWorker_Permadeath : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_Permadeath(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.PERMADEATH;

        protected override bool? Default => false;
    }
}

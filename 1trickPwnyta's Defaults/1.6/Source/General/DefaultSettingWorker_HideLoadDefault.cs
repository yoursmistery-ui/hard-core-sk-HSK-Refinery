using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.General
{
    public class DefaultSettingWorker_HideLoadDefault : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_HideLoadDefault(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.HIDE_LOADDEFAULT;

        protected override bool? Default => false;
    }
}

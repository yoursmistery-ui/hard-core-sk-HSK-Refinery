using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.General
{
    public class DefaultSettingWorker_HideSetAsDefault : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_HideSetAsDefault(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.HIDE_SETASDEFAULT;

        protected override bool? Default => false;
    }
}

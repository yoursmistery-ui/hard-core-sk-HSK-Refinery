using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.Misc.CaravanAutoSelect
{
    public class DefaultSettingWorker_CaravanAutoSelect : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_CaravanAutoSelect(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.CARAVAN_AUTO_SELECT;

        protected override bool? Default => true;
    }
}

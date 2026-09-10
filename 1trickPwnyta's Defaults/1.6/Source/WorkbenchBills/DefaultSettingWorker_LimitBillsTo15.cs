using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.WorkbenchBills
{
    public class DefaultSettingWorker_LimitBillsTo15 : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_LimitBillsTo15(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.LIMIT_BILLS_TO_15;

        protected override bool? Default => true;
    }
}

using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.General
{
    public class DefaultSettingWorker_ShowDisabledInSearch : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_ShowDisabledInSearch(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.SHOW_DISABLED_IN_SEARCH;

        protected override bool? Default => false;
    }
}

using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_MedCarryGuest : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_MedCarryGuest(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.GUESTS_CARRY_MEDICINE;

        protected override bool? Default => false;
    }
}

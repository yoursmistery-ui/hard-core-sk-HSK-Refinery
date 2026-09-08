using Defaults.Defs;
using RimWorld;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_DefaultCareForNeutralFaction : DefaultSettingWorker_DefaultCareFor
    {
        public DefaultSettingWorker_DefaultCareForNeutralFaction(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DEFAULT_CARE_NEUTRAL_FACTION;

        protected override MedicalCareCategory? Default => MedicalCareCategory.HerbalOrWorse;
    }
}

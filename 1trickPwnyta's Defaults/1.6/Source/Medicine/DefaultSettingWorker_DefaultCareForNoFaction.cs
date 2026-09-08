using Defaults.Defs;
using RimWorld;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_DefaultCareForNoFaction : DefaultSettingWorker_DefaultCareFor
    {
        public DefaultSettingWorker_DefaultCareForNoFaction(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DEFAULT_CARE_NO_FACTION;

        protected override MedicalCareCategory? Default => MedicalCareCategory.HerbalOrWorse;
    }
}

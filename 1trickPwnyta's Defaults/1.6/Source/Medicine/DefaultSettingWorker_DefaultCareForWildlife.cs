using Defaults.Defs;
using RimWorld;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_DefaultCareForWildlife : DefaultSettingWorker_DefaultCareFor
    {
        public DefaultSettingWorker_DefaultCareForWildlife(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DEFAULT_CARE_WILDLIFE;

        protected override MedicalCareCategory? Default => MedicalCareCategory.HerbalOrWorse;
    }
}

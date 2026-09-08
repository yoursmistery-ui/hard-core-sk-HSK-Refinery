using Defaults.Defs;
using RimWorld;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_DefaultCareForGhouls : DefaultSettingWorker_DefaultCareFor
    {
        public DefaultSettingWorker_DefaultCareForGhouls(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DEFAULT_CARE_GHOUL;

        protected override MedicalCareCategory? Default => MedicalCareCategory.NoMeds;
    }
}

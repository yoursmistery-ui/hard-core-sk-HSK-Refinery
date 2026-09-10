using Defaults.Defs;
using RimWorld;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_DefaultCareForTamedAnimal : DefaultSettingWorker_DefaultCareFor
    {
        public DefaultSettingWorker_DefaultCareForTamedAnimal(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DEFAULT_CARE_TAMED_ANIMAL;

        protected override MedicalCareCategory? Default => MedicalCareCategory.HerbalOrWorse;
    }
}

using Defaults.Defs;
using RimWorld;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_DefaultCareForEntities : DefaultSettingWorker_DefaultCareFor
    {
        public DefaultSettingWorker_DefaultCareForEntities(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DEFAULT_CARE_ENTITY;

        protected override MedicalCareCategory? Default => MedicalCareCategory.NoMeds;
    }
}

using Defaults.Defs;
using RimWorld;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_DefaultCareForColonist : DefaultSettingWorker_DefaultCareFor
    {
        public DefaultSettingWorker_DefaultCareForColonist(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DEFAULT_CARE_COLONIST;

        protected override MedicalCareCategory? Default => MedicalCareCategory.Best;
    }
}

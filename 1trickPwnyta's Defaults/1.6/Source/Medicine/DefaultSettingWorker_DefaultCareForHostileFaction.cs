using Defaults.Defs;
using RimWorld;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_DefaultCareForHostileFaction : DefaultSettingWorker_DefaultCareFor
    {
        public DefaultSettingWorker_DefaultCareForHostileFaction(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DEFAULT_CARE_HOSTILE_FACTION;

        protected override MedicalCareCategory? Default => MedicalCareCategory.HerbalOrWorse;
    }
}

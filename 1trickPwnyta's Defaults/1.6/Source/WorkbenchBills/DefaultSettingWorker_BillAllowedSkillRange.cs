using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class DefaultSettingWorker_BillAllowedSkillRange : DefaultSettingWorker_IntRange
    {
        public DefaultSettingWorker_BillAllowedSkillRange(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.BILL_ALLOWED_SKILL_RANGE;

        protected override int Min => 0;

        protected override int Max => 20;

        protected override IntRange? Default => new IntRange(0, 20);

        protected override string Label => setting.Value.min.ToStringCached() + " - " + (setting.Value.max >= Max ? "Unlimited".TranslateSimple() : setting.Value.max.ToStringCached());
    }
}

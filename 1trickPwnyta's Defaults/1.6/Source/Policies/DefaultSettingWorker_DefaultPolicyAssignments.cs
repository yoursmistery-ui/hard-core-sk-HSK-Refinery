using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.Policies
{
    public class DefaultSettingWorker_DefaultPolicyAssignments : DefaultSettingWorker_Options<DefaultPolicyAssignments>
    {
        public DefaultSettingWorker_DefaultPolicyAssignments(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.POLICY_ASSIGNMENTS;

        protected override DefaultPolicyAssignments Default => new DefaultPolicyAssignments();

        protected override void Configure()
        {
            Find.WindowStack.Add(new Dialog_DefaultPolicyAssignments());
        }

        public override void Notify_FirstSpawnAnywhere(Pawn pawn)
        {
            PolicyUtility.SetAllDefaultPolicies(pawn);
        }
    }
}

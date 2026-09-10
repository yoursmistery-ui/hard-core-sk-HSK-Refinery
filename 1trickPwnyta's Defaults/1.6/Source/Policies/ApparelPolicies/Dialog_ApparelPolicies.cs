using Defaults.UI;
using Defaults.Workers;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.Policies.ApparelPolicies
{
    public class Dialog_ApparelPolicies : Dialog_ManageApparelPolicies, IPolicyDialog
    {
        private static List<ApparelPolicy> Policies => Settings.Get<List<ApparelPolicy>>(Settings.POLICIES_APPAREL);

        public Dialog_ApparelPolicies() : base(Policies[0])
        {
            typeof(Dialog_ManageApparelPolicies).Field("thingFilterState").SetValue(this, new UIState_Ext());
            optionalTitle = TitleKey.Translate();
        }

        public string Topic => "Defaults_ApparelPolicies".Translate();

        public string Title => TitleKey.Translate();

        public void ResetPolicies()
        {
            DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Policies>().ResetApparelPolicies();
            SelectedPolicy = GetDefaultPolicy();
        }

        protected override ApparelPolicy CreateNewPolicy() => PolicyUtility.NewDefaultPolicy<ApparelPolicy>();

        protected override ApparelPolicy GetDefaultPolicy() => Policies[0];

        protected override List<ApparelPolicy> GetPolicies() => Policies;

        protected override void SetDefaultPolicy(ApparelPolicy policy)
        {
            int currentIndex = Policies.IndexOf(policy);
            Policies[currentIndex] = Policies[0];
            Policies[0] = policy;
        }

        protected override AcceptanceReport TryDeletePolicy(ApparelPolicy policy)
        {
            return policy == GetDefaultPolicy()
                ? (AcceptanceReport)"Defaults_CantDeleteDefaultPolicy".Translate()
                : (AcceptanceReport)Policies.Remove(policy);
        }
    }
}

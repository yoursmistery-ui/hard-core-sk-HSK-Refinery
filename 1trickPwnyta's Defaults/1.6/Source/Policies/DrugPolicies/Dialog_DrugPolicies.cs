using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.Policies.DrugPolicies
{
    public class Dialog_DrugPolicies : Dialog_ManageDrugPolicies, IPolicyDialog
    {
        private static List<DrugPolicy> Policies => Settings.Get<List<DrugPolicy>>(Settings.POLICIES_DRUG);

        public Dialog_DrugPolicies() : base(Policies[0])
        {
            optionalTitle = TitleKey.Translate();
        }

        public string Topic => "Defaults_FoodPolicies".Translate();

        public string Title => TitleKey.Translate();

        public void ResetPolicies()
        {
            DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Policies>().ResetDrugPolicies();
            SelectedPolicy = GetDefaultPolicy();
        }

        protected override DrugPolicy CreateNewPolicy() => PolicyUtility.NewDefaultPolicy<DrugPolicy>();

        protected override DrugPolicy GetDefaultPolicy() => Policies[0];

        protected override List<DrugPolicy> GetPolicies() => Policies;

        protected override void SetDefaultPolicy(DrugPolicy policy)
        {
            int currentIndex = Policies.IndexOf(policy);
            Policies[currentIndex] = Policies[0];
            Policies[0] = policy;
        }

        protected override AcceptanceReport TryDeletePolicy(DrugPolicy policy)
        {
            return policy == GetDefaultPolicy()
                ? (AcceptanceReport)"Defaults_CantDeleteDefaultPolicy".Translate()
                : (AcceptanceReport)Policies.Remove(policy);
        }
    }
}

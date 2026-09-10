using Defaults.UI;
using Defaults.Workers;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.Policies.FoodPolicies
{
    public class Dialog_FoodPolicies : Dialog_ManageFoodPolicies, IPolicyDialog
    {
        private static List<FoodPolicy> Policies => Settings.Get<List<FoodPolicy>>(Settings.POLICIES_FOOD);

        public Dialog_FoodPolicies() : base(Policies[0])
        {
            typeof(Dialog_ManageFoodPolicies).Field("thingFilterState").SetValue(this, new UIState_Ext());
            optionalTitle = TitleKey.Translate();
        }

        public string Topic => "Defaults_FoodPolicies".Translate();

        public string Title => TitleKey.Translate();

        public void ResetPolicies()
        {
            DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Policies>().ResetFoodPolicies();
            SelectedPolicy = GetDefaultPolicy();
        }

        protected override FoodPolicy CreateNewPolicy() => PolicyUtility.NewDefaultPolicy<FoodPolicy>();

        protected override FoodPolicy GetDefaultPolicy() => Policies[0];

        protected override List<FoodPolicy> GetPolicies() => Policies;

        protected override void SetDefaultPolicy(FoodPolicy policy)
        {
            int currentIndex = Policies.IndexOf(policy);
            Policies[currentIndex] = Policies[0];
            Policies[0] = policy;
        }

        protected override AcceptanceReport TryDeletePolicy(FoodPolicy policy)
        {
            return policy == GetDefaultPolicy()
                ? (AcceptanceReport)"Defaults_CantDeleteDefaultPolicy".Translate()
                : (AcceptanceReport)Policies.Remove(policy);
        }
    }
}

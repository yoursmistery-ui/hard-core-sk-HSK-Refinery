using Defaults.UI;
using Defaults.Workers;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.Policies.ReadingPolicies
{
    public class Dialog_ReadingPolicies : Dialog_ManageReadingPolicies, IPolicyDialog
    {
        private static List<ReadingPolicy> Policies => Settings.Get<List<ReadingPolicy>>(Settings.POLICIES_READING);

        public Dialog_ReadingPolicies() : base(Policies[0])
        {
            typeof(Dialog_ManageReadingPolicies).Field("thingFilterState").SetValue(this, new UIState_Ext());
            typeof(Dialog_ManageReadingPolicies).Field("effectFilterState").SetValue(this, new UIState_Ext());
            optionalTitle = TitleKey.Translate();
        }

        public string Topic => "Defaults_ReadingPolicies".Translate();

        public string Title => TitleKey.Translate();

        public void ResetPolicies()
        {
            DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Policies>().ResetReadingPolicies();
            SelectedPolicy = GetDefaultPolicy();
        }

        protected override ReadingPolicy CreateNewPolicy() => PolicyUtility.NewDefaultPolicy<ReadingPolicy>();

        protected override ReadingPolicy GetDefaultPolicy() => Policies[0];

        protected override List<ReadingPolicy> GetPolicies() => Policies;

        protected override void SetDefaultPolicy(ReadingPolicy policy)
        {
            int currentIndex = Policies.IndexOf(policy);
            Policies[currentIndex] = Policies[0];
            Policies[0] = policy;
        }

        protected override AcceptanceReport TryDeletePolicy(ReadingPolicy policy)
        {
            return policy == GetDefaultPolicy()
                ? (AcceptanceReport)"Defaults_CantDeleteDefaultPolicy".Translate()
                : (AcceptanceReport)Policies.Remove(policy);
        }
    }
}

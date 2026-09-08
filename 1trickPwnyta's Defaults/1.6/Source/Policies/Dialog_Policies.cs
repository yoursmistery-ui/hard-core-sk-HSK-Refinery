using Defaults.Defs;
using Defaults.Policies.ApparelPolicies;
using Defaults.Policies.DrugPolicies;
using Defaults.Policies.FoodPolicies;
using Defaults.Policies.ReadingPolicies;
using Defaults.UI;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.Policies
{
    public class Dialog_Policies : Dialog_SettingsCategory
    {
        private static IPolicyDialog currentWindow;

        private readonly List<PolicyTab> tabs = new List<PolicyTab>();

        public Dialog_Policies(DefaultSettingsCategoryDef category) : base(category)
        {
            tabs.AddRange(new[]
            {
                new PolicyTab(new Dialog_ApparelPolicies()),
                new PolicyTab(new Dialog_FoodPolicies()),
                new PolicyTab(new Dialog_DrugPolicies()),
                new PolicyTab(new Dialog_ReadingPolicies())
            });
            currentWindow = tabs[0].window;
        }

        public override Vector2 InitialSize => new Vector2(1320f, 700f);

        public override void DoSettings(Rect rect)
        {
            Rect tabsRect = new Rect(rect.x, rect.y + 32f, rect.width, 1f);
            TabDrawer.DrawTabs(tabsRect, tabs);
            Rect contentRect = new Rect(rect.x, tabsRect.yMax, currentWindow.InitialSize.x - 36f, rect.height - 32f - tabsRect.height + CloseButSize.y + 10f);
            currentWindow.DoWindowContents(contentRect);
        }

        protected override TaggedString ResetOptionWarning => "Defaults_ConfirmResetTheseSettings".Translate(currentWindow.Topic);

        protected override void OnResetOptionClicked()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(ResetOptionWarning, "Confirm".Translate(), currentWindow.ResetPolicies, "GoBack".Translate(), null, null, true, currentWindow.ResetPolicies));
        }

        private class PolicyTab : TabRecord
        {
            public IPolicyDialog window;

            public PolicyTab(IPolicyDialog window) : base(window.Title, () => currentWindow = window, () => currentWindow == window)
            {
                this.window = window;
            }
        }
    }
}

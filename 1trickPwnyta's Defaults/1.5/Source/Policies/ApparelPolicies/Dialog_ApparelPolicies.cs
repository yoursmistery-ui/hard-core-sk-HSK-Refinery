using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Policies.ApparelPolicies
{
    public class Dialog_ApparelPolicies : Dialog_ManagePolicies<ApparelPolicy>
    {
        private static readonly ThingFilter apparelGlobalFilter = new ThingFilter();

        private readonly ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();

        static Dialog_ApparelPolicies()
        {
            apparelGlobalFilter.SetAllow(ThingCategoryDefOf.Apparel, true);
        }

        public Dialog_ApparelPolicies(ApparelPolicy policy) : base(policy)
        {
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(700f, 700f);
            }
        }

        protected override string TitleKey => "ApparelPolicyTitle";

        protected override string TipKey => "ApparelPolicyTip";

        protected override ApparelPolicy CreateNewPolicy()
        {
            string name;
            int i = DefaultsSettings.DefaultApparelPolicies.Count + 1;
            do
            {
                name = "ApparelPolicy".Translate() + " " + i++;
            } while (DefaultsSettings.DefaultApparelPolicies.Any(p => p.label == name));
            ApparelPolicy policy = new ApparelPolicy(0, name);
            DefaultsSettings.DefaultApparelPolicies.Add(policy);
            return policy;
        }

        protected override void DoContentsRect(Rect rect)
        {
            ThingFilterUI.DoThingFilterConfigWindow(rect, thingFilterState, SelectedPolicy.filter, apparelGlobalFilter, 16, null, HiddenSpecialThingFilters());
        }

        protected override ApparelPolicy GetDefaultPolicy() => DefaultsSettings.DefaultApparelPolicies.First();

        protected override List<ApparelPolicy> GetPolicies() => DefaultsSettings.DefaultApparelPolicies;

        protected override AcceptanceReport TryDeletePolicy(ApparelPolicy policy)
        {
            if (policy == GetDefaultPolicy())
            {
                return "Defaults_CantDeleteDefaultPolicy".Translate();
            }
            return DefaultsSettings.DefaultApparelPolicies.Remove(policy);
        }

        private IEnumerable<SpecialThingFilterDef> HiddenSpecialThingFilters()
        {
            yield return SpecialThingFilterDefOf.AllowNonDeadmansApparel;
            if (ModsConfig.IdeologyActive)
            {
                yield return SpecialThingFilterDefOf.AllowVegetarian;
                yield return SpecialThingFilterDefOf.AllowCarnivore;
                yield return SpecialThingFilterDefOf.AllowCannibal;
                yield return SpecialThingFilterDefOf.AllowInsectMeat;
            }
        }
    }
}

using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Policies.FoodPolicies
{
    public class Dialog_FoodPolicies : Dialog_ManagePolicies<FoodPolicy>
    {
        private static readonly ThingFilter foodGlobalFilter = new ThingFilter();

        private readonly ThingFilterUI.UIState thingFilterState = new ThingFilterUI.UIState();

        static Dialog_FoodPolicies()
        {
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.GetStatValueAbstract(StatDefOf.Nutrition, null) > 0f))
            {
                foodGlobalFilter.SetAllow(thingDef, true);
            }
        }

        public Dialog_FoodPolicies(FoodPolicy policy) : base(policy)
        {
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(700f, 700f);
            }
        }

        protected override string TitleKey => "FoodPolicyTitle";

        protected override string TipKey => "FoodPolicyTip";

        protected override FoodPolicy CreateNewPolicy()
        {
            string name;
            int i = DefaultsSettings.DefaultFoodPolicies.Count + 1;
            do
            {
                name = "FoodPolicy".Translate() + " " + i++;
            } while (DefaultsSettings.DefaultFoodPolicies.Any(p => p.label == name));
            FoodPolicy policy = new FoodPolicy(0, name);
            DefaultsSettings.DefaultFoodPolicies.Add(policy);
            return policy;
        }

        protected override void DoContentsRect(Rect rect)
        {
            ThingFilterUI.DoThingFilterConfigWindow(rect, thingFilterState, SelectedPolicy.filter, foodGlobalFilter, 1, null, HiddenSpecialThingFilters(), true);
        }

        protected override FoodPolicy GetDefaultPolicy() => DefaultsSettings.DefaultFoodPolicies.First();

        protected override List<FoodPolicy> GetPolicies() => DefaultsSettings.DefaultFoodPolicies;

        protected override AcceptanceReport TryDeletePolicy(FoodPolicy policy)
        {
            if (policy == GetDefaultPolicy())
            {
                return "Defaults_CantDeleteDefaultPolicy".Translate();
            }
            return DefaultsSettings.DefaultFoodPolicies.Remove(policy);
        }

        private IEnumerable<SpecialThingFilterDef> HiddenSpecialThingFilters()
        {
            yield return SpecialThingFilterDefOf.AllowFresh;
        }
    }
}

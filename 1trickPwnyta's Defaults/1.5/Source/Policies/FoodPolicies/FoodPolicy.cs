using RimWorld;
using System.Linq;
using Verse;

namespace Defaults.Policies.FoodPolicies
{
    public class FoodPolicy : Policy
    {
        public ThingFilter filter = new ThingFilter();
        public bool locked = true;

        public FoodPolicy()
        {
            
        }

        public FoodPolicy(int id, string label) : base(id, label)
        {
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.GetStatValueAbstract(StatDefOf.Nutrition, null) > 0f))
            {
                filter.SetAllow(thingDef, true);
            }
            if (ModsConfig.IdeologyActive)
            {
                filter.SetAllow(SpecialThingFilterDefOf.AllowVegetarian, true);
                filter.SetAllow(SpecialThingFilterDefOf.AllowCarnivore, true);
                filter.SetAllow(SpecialThingFilterDefOf.AllowCannibal, true);
                filter.SetAllow(SpecialThingFilterDefOf.AllowInsectMeat, true);
            }
            if (ModsConfig.BiotechActive)
            {
                filter.SetAllow(ThingDefOf.HemogenPack, false);
            }
        }

        protected override string LoadKey => "FoodPolicies.FoodPolicy";

        public override void CopyFrom(Policy other)
        {
            filter.CopyAllowancesFrom(((FoodPolicy)other).filter);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            DefaultsSettings.ScribeThingFilter(filter);
            Scribe_Values.Look(ref locked, "locked", true);
        }
    }
}

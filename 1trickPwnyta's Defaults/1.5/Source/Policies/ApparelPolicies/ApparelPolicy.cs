using RimWorld;
using Verse;

namespace Defaults.Policies.ApparelPolicies
{
    public class ApparelPolicy : Policy
    {
        public ThingFilter filter = new ThingFilter();
        public bool locked = true;

        public ApparelPolicy()
        {
            
        }

        public ApparelPolicy(int id, string label) : base(id, label)
        {
            filter.SetAllow(ThingCategoryDefOf.Apparel, true);
        }

        protected override string LoadKey => "ApparelPolicies.ApparelPolicy";

        public override void CopyFrom(Policy other)
        {
            filter.CopyAllowancesFrom(((ApparelPolicy)other).filter);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            DefaultsSettings.ScribeThingFilter(filter);
            Scribe_Values.Look(ref locked, "locked", true);
        }
    }
}

using Verse;

namespace Defaults.Policies.ReadingPolicies
{
    public class ReadingPolicy : RimWorld.ReadingPolicy
    {
        public bool locked = true;

        public ReadingPolicy()
        {

        }

        public ReadingPolicy(int id, string label) : base(id, label)
        {

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref locked, "locked", true);
        }
    }
}

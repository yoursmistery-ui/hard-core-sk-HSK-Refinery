using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class Condition_Age : Condition
    {
        public int years = 12;
        public bool above = true;
        public string editBuffer;

        public Condition_Age()
        {
        }

        public Condition_Age(WorkPriorityConditionDef def) : base(def)
        {
        }

        public override bool Applies(WorkTypeDef def, Pawn pawn)
        {
            int age = pawn.ageTracker.AgeBiologicalYears;
            return above ? age > years : age < years;
        }

        public override Condition MakeCopy() => new Condition_Age(def)
        {
            years = years,
            above = above
        };

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref years, "years", 12);
            Scribe_Values.Look(ref above, "above", true);
        }
    }
}

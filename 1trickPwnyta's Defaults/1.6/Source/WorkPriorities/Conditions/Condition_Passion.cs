using Defaults.Defs;
using RimWorld;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class Condition_Passion : Condition
    {
        public Passion passion = Passion.Minor;

        public Condition_Passion()
        {
        }

        public Condition_Passion(WorkPriorityConditionDef def) : base(def)
        {
        }

        public override bool Applies(WorkTypeDef def, Pawn pawn)
        {
            return pawn.skills.MaxPassionOfRelevantSkillsFor(def) >= passion;
        }

        public override Condition MakeCopy() => new Condition_Passion(def)
        {
            passion = passion
        };

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref passion, "passion", Passion.Minor);
        }
    }
}

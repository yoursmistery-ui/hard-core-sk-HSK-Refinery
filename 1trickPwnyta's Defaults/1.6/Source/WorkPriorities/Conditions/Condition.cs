using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public abstract class Condition : IExposable
    {
        public WorkPriorityConditionDef def;

        protected Condition()
        {

        }

        public Condition(WorkPriorityConditionDef def)
        {
            this.def = def;
        }

        public abstract bool Applies(WorkTypeDef def, Pawn pawn);

        public abstract Condition MakeCopy();

        public virtual void ExposeData()
        {
            Scribe_Defs_Silent.Look(ref def, "def");
        }
    }
}

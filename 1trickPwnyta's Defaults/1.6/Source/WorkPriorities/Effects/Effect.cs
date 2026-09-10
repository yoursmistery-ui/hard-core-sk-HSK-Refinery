using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Effects
{
    public abstract class Effect : IExposable
    {
        public WorkPriorityEffectDef def;

        protected Effect()
        {

        }

        public Effect(WorkPriorityEffectDef def)
        {
            this.def = def;
        }

        public abstract bool? Apply(WorkTypeDef def, Pawn pawn);

        public abstract Effect MakeCopy();

        public virtual void ExposeData()
        {
            Scribe_Defs_Silent.Look(ref def, "def");
        }
    }
}

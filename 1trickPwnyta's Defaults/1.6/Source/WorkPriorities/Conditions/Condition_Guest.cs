using Defaults.Defs;
using RimWorld;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class Condition_Guest : Condition
    {
        public Condition_Guest()
        {
        }

        public Condition_Guest(WorkPriorityConditionDef def) : base(def)
        {
        }

        public override bool Applies(WorkTypeDef def, Pawn pawn)
        {
            return pawn.HasExtraMiniFaction() || pawn.HasExtraHomeFaction();
        }

        public override Condition MakeCopy() => new Condition_Guest(def);
    }
}

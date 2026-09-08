using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class Condition_Always : Condition
    {
        public Condition_Always()
        {

        }

        public Condition_Always(WorkPriorityConditionDef def) : base(def)
        {
        }

        public override bool Applies(WorkTypeDef def, Pawn pawn)
        {
            return true;
        }

        public override Condition MakeCopy() => new Condition_Always(def);
    }
}

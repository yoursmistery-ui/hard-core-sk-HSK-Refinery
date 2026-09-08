using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class Condition_Slave : Condition
    {
        public Condition_Slave()
        {
        }

        public Condition_Slave(WorkPriorityConditionDef def) : base(def)
        {
        }

        public override bool Applies(WorkTypeDef def, Pawn pawn)
        {
            return pawn.IsSlave;
        }

        public override Condition MakeCopy() => new Condition_Slave(def);
    }
}

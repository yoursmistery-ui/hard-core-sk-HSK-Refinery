using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class Condition_AgainstIdeo : Condition
    {
        public Condition_AgainstIdeo()
        {
        }

        public Condition_AgainstIdeo(WorkPriorityConditionDef def) : base(def)
        {
        }

        public override bool Applies(WorkTypeDef def, Pawn pawn)
        {
            return pawn.Ideo?.IsWorkTypeConsideredDangerous(def) ?? false;
        }

        public override Condition MakeCopy() => new Condition_AgainstIdeo(def);
    }
}

using Defaults.WorkPriorities.Conditions;
using Defaults.WorkPriorities.Effects;
using Verse;

namespace Defaults.WorkPriorities
{
    public class Rule : IExposable
    {
        public Condition condition;
        public Effect effect;

        public bool IsValid => condition?.def != null && effect?.def != null;

        public bool? Apply(WorkTypeDef def, Pawn pawn)
        {
            return condition.Applies(def, pawn) ? effect.Apply(def, pawn) : null;
        }

        public Rule MakeCopy() => new Rule
        {
            condition = condition.MakeCopy(),
            effect = effect.MakeCopy()
        };

        public void ExposeData()
        {
            Scribe_Deep.Look(ref condition, "condition");
            Scribe_Deep.Look(ref effect, "effect");
        }
    }
}

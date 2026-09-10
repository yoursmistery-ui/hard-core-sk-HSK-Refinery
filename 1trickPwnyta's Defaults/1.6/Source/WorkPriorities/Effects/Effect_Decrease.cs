using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Effects
{
    public class Effect_Decrease : Effect
    {
        public int amount = 1;
        public bool allowZero = false;
        public string editBuffer;

        public Effect_Decrease()
        {
        }

        public Effect_Decrease(WorkPriorityEffectDef def) : base(def)
        {
        }

        public override bool? Apply(WorkTypeDef def, Pawn pawn)
        {
            if (pawn.workSettings.WorkIsActive(def))
            {
                int value = pawn.workSettings.GetPriority(def);
                value += amount;
                if (value > WorkPriorityValue.Max)
                {
                    value = allowZero ? WorkPriorityValue.DoNotDo : WorkPriorityValue.Max;
                }
                pawn.workSettings.SetPriority(def, value);
            }
            return true;
        }

        public override Effect MakeCopy() => new Effect_Decrease(def)
        {
            amount = amount,
            allowZero = allowZero
        };

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref amount, "amount", 1);
            Scribe_Values.Look(ref allowZero, "allowZero", false);
        }
    }
}

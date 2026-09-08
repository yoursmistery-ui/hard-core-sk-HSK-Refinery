using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Effects
{
    public class Effect_Increase : Effect
    {
        public int amount = 1;
        public bool allowZero = false;
        public string editBuffer;

        public Effect_Increase()
        {
        }

        public Effect_Increase(WorkPriorityEffectDef def) : base(def)
        {
        }

        public override bool? Apply(WorkTypeDef def, Pawn pawn)
        {
            int value = pawn.workSettings.GetPriority(def);
            if (value > WorkPriorityValue.DoNotDo)
            {
                value -= amount;
            }
            else if (allowZero && value == WorkPriorityValue.DoNotDo)
            {
                value = WorkPriorityValue.Max + 1 - amount;
            }
            else
            {
                return true;
            }
            if (value < 1)
            {
                value = 1;
            }
            pawn.workSettings.SetPriority(def, value);
            return true;
        }

        public override Effect MakeCopy() => new Effect_Increase(def)
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

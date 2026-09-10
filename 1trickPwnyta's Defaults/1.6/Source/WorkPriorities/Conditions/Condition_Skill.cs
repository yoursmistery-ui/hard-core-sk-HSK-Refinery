using Defaults.Defs;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class Condition_Skill : Condition
    {
        public int level = 5;
        public bool above = true;
        public string editBuffer;

        public Condition_Skill()
        {
        }

        public Condition_Skill(WorkPriorityConditionDef def) : base(def)
        {
        }

        public override bool Applies(WorkTypeDef def, Pawn pawn)
        {
            float skill = pawn.skills.AverageOfRelevantSkillsFor(def);
            return above ? skill > level : skill < level;
        }

        public override Condition MakeCopy() => new Condition_Skill(def)
        {
            level = level,
            above = above
        };

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref level, "level", 5);
            Scribe_Values.Look(ref above, "above", true);
        }
    }
}

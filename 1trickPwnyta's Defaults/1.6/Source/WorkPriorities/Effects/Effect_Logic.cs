using Defaults.Defs;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorkPriorities.Effects
{
    public class Effect_Logic : Effect
    {
        public List<Rule> rules = new List<Rule>();

        public Effect_Logic()
        {
        }

        public Effect_Logic(WorkPriorityEffectDef def) : base(def)
        {
        }

        public override bool? Apply(WorkTypeDef def, Pawn pawn)
        {
            return WorkPriorityUtility.ApplyRules(rules, def, pawn);
        }

        public override Effect MakeCopy() => new Effect_Logic(def)
        {
            rules = rules.Select(r => r.MakeCopy()).ToList()
        };

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref rules, "rules", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && rules == null)
            {
                rules = new List<Rule>();
            }
        }
    }
}

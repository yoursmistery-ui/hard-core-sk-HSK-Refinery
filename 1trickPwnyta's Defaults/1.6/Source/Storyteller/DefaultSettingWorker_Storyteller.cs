using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Storyteller
{
    public class DefaultSettingWorker_Storyteller : DefaultSettingWorker_Dropdown<StorytellerDef>
    {
        public DefaultSettingWorker_Storyteller(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.STORYTELLER;

        protected override IEnumerable<StorytellerDef> Options => DefDatabase<StorytellerDef>.AllDefsListForReading.Where(s => s.listVisible).OrderBy(s => s.listOrder);

        protected override StorytellerDef Default => StorytellerDefOf.Cassandra;

        protected override TaggedString GetText(StorytellerDef option) => option.LabelCap;

        protected override TaggedString GetTip(StorytellerDef option) => option.description;

        protected override TaggedString GetMenuTip(StorytellerDef option) => option.description;

        protected override void ExposeSetting()
        {
            Scribe_Defs_Silent.Look(ref setting, Key);
        }
    }
}

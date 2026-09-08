using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.Storyteller
{
    public class DefaultSettingWorker_Difficulty : DefaultSettingWorker_Dropdown<DifficultyDef>
    {
        public DefaultSettingWorker_Difficulty(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.DIFFICULTY;

        protected override IEnumerable<DifficultyDef> Options => DefDatabase<DifficultyDef>.AllDefsListForReading;

        protected override DifficultyDef Default => DifficultyDefOf.Rough;

        protected override TaggedString GetText(DifficultyDef option) => option.LabelCap;

        protected override TaggedString GetTip(DifficultyDef option) => option.description;

        protected override TaggedString GetMenuTip(DifficultyDef option) => option.description;

        protected override void ExposeSetting()
        {
            Scribe_Defs_Silent.Look(ref setting, Key);
        }
    }
}

using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.WorldSettings
{
    public class DefaultSettingWorker_StartingSeason : DefaultSettingWorker_Dropdown<Season?>
    {
        public DefaultSettingWorker_StartingSeason(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.STARTING_SEASON;

        protected override IEnumerable<Season?> Options => new Season?[]
        {
            Season.Undefined,
            Season.Spring,
            Season.Summer,
            Season.Fall,
            Season.Winter
        };

        protected override Season? Default => Season.Undefined;

        protected override TaggedString GetText(Season? option) => option == Season.Undefined ? "MapStartSeasonDefault".Translate() : (TaggedString)option.Value.LabelCap();

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }
    }
}

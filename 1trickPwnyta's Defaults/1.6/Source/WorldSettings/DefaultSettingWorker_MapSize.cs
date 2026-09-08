using Defaults.Defs;
using Defaults.Workers;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorldSettings
{
    public class DefaultSettingWorker_MapSize : DefaultSettingWorker_Dropdown<int?>
    {
        public static readonly int?[] MapSizes = new int?[]
        {
            200,
            225,
            250,
            275,
            300,
            325
        };

        public static readonly int?[] TestMapSizes = new int?[]
        {
            350,
            400
        };

        public DefaultSettingWorker_MapSize(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.MAP_SIZE;

        protected override IEnumerable<int?> Options => MapSizes.Concat(Prefs.TestMapSizes ? TestMapSizes : new int?[] { });

        protected override int? Default => 250;

        protected override TaggedString GetText(int? option) => "MapSizeDesc".Translate(option.Value, option.Value * option.Value);

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }
    }
}

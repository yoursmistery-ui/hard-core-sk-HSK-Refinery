using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_MedCarryAmount : DefaultSettingWorker_Dropdown<int?>
    {
        public DefaultSettingWorker_MedCarryAmount(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.MEDICINE_AMOUNT_TO_CARRY;

        protected override int? Default => 0;

        protected override IEnumerable<int?> Options => Enumerable.Range(InventoryStockGroupDefOf.Medicine.min, InventoryStockGroupDefOf.Medicine.max + 1).Cast<int?>();

        protected override TaggedString GetText(int? option) => option.ToString();

        protected override float Width => 30f;

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }
    }
}

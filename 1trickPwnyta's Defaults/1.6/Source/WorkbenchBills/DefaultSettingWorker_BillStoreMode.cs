using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class DefaultSettingWorker_BillStoreMode : DefaultSettingWorker_Dropdown<BillStoreModeDef>
    {
        public DefaultSettingWorker_BillStoreMode(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.BILL_STORE_MODE;

        protected override IEnumerable<BillStoreModeDef> Options => DefDatabase<BillStoreModeDef>.AllDefsListForReading.Where(d => d != BillStoreModeDefOf.SpecificStockpile);

        protected override BillStoreModeDef Default => BillStoreModeDefOf.BestStockpile;

        protected override TaggedString GetText(BillStoreModeDef option) => option.LabelCap;

        protected override void ExposeSetting()
        {
            Scribe_Defs.Look(ref setting, Key);
        }
    }
}

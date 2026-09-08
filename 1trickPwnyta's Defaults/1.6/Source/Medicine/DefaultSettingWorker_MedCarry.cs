using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.Medicine
{
    public class DefaultSettingWorker_MedCarry : DefaultSettingWorker_Dropdown<ThingDef>
    {
        public DefaultSettingWorker_MedCarry(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.MEDICINE_TO_CARRY;

        protected override ThingDef Default => ThingDefOf.MedicineIndustrial;

        protected override IEnumerable<ThingDef> Options => InventoryStockGroupDefOf.Medicine.thingDefs;

        protected override Texture2D GetIcon(ThingDef option) => option.uiIcon;

        protected override TaggedString GetText(ThingDef option) => option.LabelCap;

        protected override TaggedString GetTip(ThingDef option) => "Defaults_MedicineToCarryTip".Translate() + "\n\n" + "Defaults_CurrentMedicineToCarry".Translate() + ": " + GetText(option);

        protected override void ExposeSetting()
        {
            Scribe_Defs_Silent.Look(ref setting, Key);
        }
    }
}

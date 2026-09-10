using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Misc.PlantType
{
    public class DefaultSettingWorker_PlantType : DefaultSettingWorker_Dropdown<ThingDef>
    {
        public DefaultSettingWorker_PlantType(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.PLANT_TYPE;

        protected override ThingDef Default => ThingDefOf.Plant_Potato;

        protected override IEnumerable<ThingDef> Options => DefDatabase<ThingDef>.AllDefs.Where(d =>
            d.category == ThingCategory.Plant
            && d.plant.sowTags.Contains("Ground")
            // RR_Agriculture included to support Research Reinvented: Stepping Stones
            && (d.plant.sowResearchPrerequisites == null || d.plant.sowResearchPrerequisites.Any(p => p.defName == "RR_Agriculture"))
            && !d.plant.RequiresPollution).OrderBy(d => -d.GetPlantListPriority());

        protected override Texture2D GetIcon(ThingDef option) => option.uiIcon;

        protected override TaggedString GetText(ThingDef option) => option.LabelCap + (option.plant.sowMinSkill > 0 ? string.Concat(new object[]
        {
            " (" + "MinSkill".Translate() + ": ",
            option.plant.sowMinSkill,
            ")"
        }) : string.Empty);

        protected override TaggedString GetTip(ThingDef option) => "Defaults_PlantTypeTip".Translate() + "\n\n" + "Defaults_CurrentPlantType".Translate() + ": " + GetText(option);

        protected override void ExposeSetting()
        {
            Scribe_Defs_Silent.Look(ref setting, Key);
        }
    }
}

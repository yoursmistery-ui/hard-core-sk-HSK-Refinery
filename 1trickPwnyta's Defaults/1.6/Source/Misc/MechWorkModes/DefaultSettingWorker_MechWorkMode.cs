using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.Misc.MechWorkModes
{
    public abstract class DefaultSettingWorker_MechWorkMode : DefaultSettingWorker_Dropdown<MechWorkModeDef>
    {
        protected DefaultSettingWorker_MechWorkMode(DefaultSettingDef def) : base(def)
        {
        }

        protected override IEnumerable<MechWorkModeDef> Options => DefDatabase<MechWorkModeDef>.AllDefsListForReading;

        protected override Texture2D GetIcon(MechWorkModeDef option) => option.uiIcon;

        protected override TaggedString GetText(MechWorkModeDef option) => option.LabelCap;

        protected override TaggedString GetTip(MechWorkModeDef option) => string.Concat(new string[]
        {
            ("CurrentMechWorkMode".Translate() + ": " + option.LabelCap).Colorize(ColoredText.TipSectionTitleColor),
            "\n",
            option.description,
        });

        protected override TaggedString GetMenuTip(MechWorkModeDef option) => option.description;

        protected override void ExposeSetting()
        {
            Scribe_Defs_Silent.Look(ref setting, Key);
        }

        protected override MechWorkModeDef Default => MechWorkModeDefOf.Work;
    }
}

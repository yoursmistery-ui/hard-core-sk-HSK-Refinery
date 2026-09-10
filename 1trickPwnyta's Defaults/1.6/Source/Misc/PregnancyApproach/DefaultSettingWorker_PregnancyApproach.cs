using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.Misc.PregnancyApproach
{
    public class DefaultSettingWorker_PregnancyApproach : DefaultSettingWorker_Dropdown<RimWorld.PregnancyApproach?>
    {
        public DefaultSettingWorker_PregnancyApproach(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.PREGNANCY_APPROACH;

        protected override RimWorld.PregnancyApproach? Default => RimWorld.PregnancyApproach.Normal;

        protected override IEnumerable<RimWorld.PregnancyApproach?> Options
        {
            get
            {
                foreach (RimWorld.PregnancyApproach value in Enum.GetValues(typeof(RimWorld.PregnancyApproach)))
                {
                    yield return value;
                }
            }
        }

        protected override Texture2D GetIcon(RimWorld.PregnancyApproach? option) => option.Value.GetIcon();

        protected override TaggedString GetText(RimWorld.PregnancyApproach? option) => option.Value.GetDescription();

        protected override TaggedString GetTip(RimWorld.PregnancyApproach? option) => string.Concat(new string[]
        {
            "PregnancyApproach".Translate().Colorize(ColoredText.TipSectionTitleColor),
            "\n",
            GetText(option),
            "\n\n",
            "ClickToChangePregnancyApproach".Translate().Colorize(ColoredText.SubtleGrayColor)
        });

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }
    }
}

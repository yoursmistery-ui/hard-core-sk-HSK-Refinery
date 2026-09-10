using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.PregnancyApproach
{
    public static class PregnancyApproachUtility
    {
        public static void DrawPregnancyApproachButton(Rect rect)
        {
            if (Widgets.ButtonImage(rect, DefaultsSettings.DefaultPregnancyApproach.GetIcon(), true, string.Concat(new string[]
                {
                    "PregnancyApproach".Translate().Colorize(ColoredText.TipSectionTitleColor),
                    "\n",
                    DefaultsSettings.DefaultPregnancyApproach.GetDescription(),
                    "\n\n",
                    "ClickToChangePregnancyApproach".Translate().Colorize(ColoredText.SubtleGrayColor)
                })))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (RimWorld.PregnancyApproach pregnancyApproach in Enum.GetValues(typeof(RimWorld.PregnancyApproach)))
                {
                    options.Add(new FloatMenuOption(pregnancyApproach.GetDescription(), delegate ()
                    {
                        DefaultsSettings.DefaultPregnancyApproach = pregnancyApproach;
                    }, pregnancyApproach.GetIcon(), Color.white));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }
    }
}

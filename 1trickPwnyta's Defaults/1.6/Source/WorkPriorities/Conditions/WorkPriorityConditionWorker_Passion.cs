using Defaults.Defs;
using Defaults.UI;
using Defaults.Workers;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.WorkPriorities.Conditions
{
    public class WorkPriorityConditionWorker_Passion : WorkPriorityConditionWorker<Condition_Passion>
    {
        public WorkPriorityConditionWorker_Passion(WorkPriorityConditionDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Condition_Passion condition)
        {
            if (UIUtility.DoImageTextButton(rect, GetIcon(condition.passion), condition.passion.GetLabel()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (Passion passion in Enum.GetValues(typeof(Passion)))
                {
                    if (passion > Passion.None)
                    {
                        options.Add(new FloatMenuOption(passion.GetLabel(), () =>
                        {
                            condition.passion = passion;
                        }, GetIcon(passion), Color.white));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static Texture2D GetIcon(Passion passion)
        {
            switch (passion)
            {
                case Passion.Minor: return SkillUI.PassionMinorIcon;
                case Passion.Major: return SkillUI.PassionMajorIcon;
                default: return null;
            }
        }
    }
}

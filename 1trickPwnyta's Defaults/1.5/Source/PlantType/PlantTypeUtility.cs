using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.PlantType
{
    public static class PlantTypeUtility
    {
        public static void DrawPlantButton(Rect rect)
        {
            ThingDef currentPlantTypeDef = DefDatabase<ThingDef>.GetNamed(DefaultsSettings.DefaultPlantType);
            Widgets.Dropdown(rect, null, new Color(0.84f, 0.84f, 0.84f), new Func<object, ThingDef>(DrawResponseButton_GetResponse), new Func<object, IEnumerable<Widgets.DropdownMenuElement<ThingDef>>>(DrawResponseButton_GenerateMenu), null, currentPlantTypeDef.uiIcon, null, null, null, true, new float?(4f));
            if (Mouse.IsOver(rect))
            {
                TooltipHandler.TipRegion(rect, "Defaults_PlantTypeTip".Translate() + "\n\n" + "Defaults_CurrentPlantType".Translate() + ": " + currentPlantTypeDef.LabelCap);
            }
        }

        private static ThingDef DrawResponseButton_GetResponse(object obj)
        {
            return DefDatabase<ThingDef>.GetNamed(DefaultsSettings.DefaultPlantType);
        }

        private static IEnumerable<Widgets.DropdownMenuElement<ThingDef>> DrawResponseButton_GenerateMenu(object obj)
        {
            IEnumerable<ThingDef> choices = DefDatabase<ThingDef>.AllDefs.Where(def => def.category == ThingCategory.Plant && def.plant.sowTags.Contains("Ground") && def.plant.sowResearchPrerequisites == null && !def.plant.RequiresPollution).OrderBy(def => -GetPlantListPriority(def));
            foreach (ThingDef choice in choices)
            {
                string text = choice.LabelCap;
                if (choice.plant.sowMinSkill > 0)
                {
                    text = string.Concat(new object[]
                    {
                        text,
                        " (" + "MinSkill".Translate() + ": ",
                        choice.plant.sowMinSkill,
                        ")"
                    });
                }
                yield return new Widgets.DropdownMenuElement<ThingDef>
                {
                    option = new FloatMenuOption(text, delegate ()
                    {
                        DefaultsSettings.DefaultPlantType = choice.defName;
                    }, choice.uiIcon, Color.white),
                    payload = choice
                };
            }
        }

        private static float GetPlantListPriority(ThingDef plantDef)
        {
            switch (plantDef.plant.purpose)
            {
                case PlantPurpose.Food:
                    return 4f;
                case PlantPurpose.Health:
                    return 3f;
                case PlantPurpose.Beauty:
                    return 2f;
                case PlantPurpose.Misc:
                    return 0f;
                default:
                    return 0f;
            }
        }
    }
}

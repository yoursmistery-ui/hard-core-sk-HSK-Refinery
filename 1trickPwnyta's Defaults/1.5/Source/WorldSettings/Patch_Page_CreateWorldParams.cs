using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.WorldSettings
{
    [HarmonyPatch(typeof(Page_CreateWorldParams))]
    [HarmonyPatch(nameof(Page_CreateWorldParams.Reset))]
    public static class Patch_Page_CreateWorldParams_Reset
    {
        public static void Postfix(ref float ___planetCoverage, ref OverallRainfall ___rainfall, ref OverallTemperature ___temperature, ref OverallPopulation ___population, ref float ___pollution)
        {
            ___planetCoverage = DefaultsSettings.DefaultPlanetCoverage;
            ___rainfall = DefaultsSettings.DefaultOverallRainfall;
            ___temperature = DefaultsSettings.DefaultOverallTemperature;
            ___population = DefaultsSettings.DefaultOverallPopulation;
            ___pollution = DefaultsSettings.DefaultPollution;
        }
    }

    [HarmonyPatch(typeof(Page_CreateWorldParams))]
    [HarmonyPatch("ResetFactionCounts")]
    public static class Patch_Page_CreateWorldParams_ResetFactionCounts
    {
        public static void Postfix(ref List<FactionDef> ___factions)
        {
            ___factions = DefaultsSettings.DefaultFactions.Select(f => DefDatabase<FactionDef>.GetNamedSilentFail(f)).Where(f => f != null && f.displayInFactionSelection).Concat(FactionsUtility.GetDefaultNonselectableFactions()).ToList();
        }
    }

    [HarmonyPatch(typeof(Page_CreateWorldParams))]
    [HarmonyPatch(nameof(Page_CreateWorldParams.DoWindowContents))]
    public static class Patch_Page_CreateWorldParams_DoWindowContents
    {
        public static void Postfix(Rect rect, float ___planetCoverage, OverallRainfall ___rainfall, OverallTemperature ___temperature, OverallPopulation ___population, float ___pollution, List<FactionDef> ___factions)
        {
            Rect buttonRect = new Rect(rect.x + rect.width - 150f - 16f, rect.y + 4f, 150f, 40f);
            if (Widgets.ButtonText(buttonRect, "Defaults_SetAsDefault".Translate()))
            {
                DefaultsSettings.DefaultPlanetCoverage = ___planetCoverage;
                DefaultsSettings.DefaultOverallRainfall = ___rainfall;
                DefaultsSettings.DefaultOverallTemperature = ___temperature;
                DefaultsSettings.DefaultOverallPopulation = ___population;
                DefaultsSettings.DefaultPollution = ___pollution;
                DefaultsSettings.DefaultFactions = ___factions.Where(f => f.displayInFactionSelection).Select(f => f.defName).ToList();
                LongEventHandler.ExecuteWhenFinished(DefaultsMod.Settings.Write);
                Messages.Message("Defaults_SetAsDefaultConfirmed".Translate(), MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}

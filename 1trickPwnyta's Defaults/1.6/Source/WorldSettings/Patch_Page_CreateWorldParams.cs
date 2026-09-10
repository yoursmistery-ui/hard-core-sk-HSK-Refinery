using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.WorldSettings
{
    [HarmonyPatchCategory("World")]
    [HarmonyPatch(typeof(Page_CreateWorldParams))]
    [HarmonyPatch(nameof(Page_CreateWorldParams.Reset))]
    public static class Patch_Page_CreateWorldParams_Reset
    {
        public static void Postfix(ref float ___planetCoverage, ref OverallRainfall ___rainfall, ref OverallTemperature ___temperature, ref OverallPopulation ___population, ref float ___pollution, ref LandmarkDensity ___landmarkDensity)
        {
            ___planetCoverage = Settings.GetValue<float>(Settings.PLANET_COVERAGE);
            ___rainfall = Settings.GetValue<OverallRainfall>(Settings.OVERALL_RAINFALL);
            ___temperature = Settings.GetValue<OverallTemperature>(Settings.OVERALL_TEMPERATURE);
            ___population = Settings.GetValue<OverallPopulation>(Settings.OVERALL_POPULATION);
            if (ModsConfig.BiotechActive)
            {
                ___pollution = Settings.GetValue<float>(Settings.PLANET_POLLUTION);
            }
            if (ModsConfig.OdysseyActive)
            {
                ___landmarkDensity = Settings.GetValue<LandmarkDensity>(Settings.LANDMARK_DENSITY);
            }
        }
    }

    [HarmonyPatchCategory("World")]
    [HarmonyPatch(typeof(Page_CreateWorldParams))]
    [HarmonyPatch("ResetFactionCounts")]
    public static class Patch_Page_CreateWorldParams_ResetFactionCounts
    {
        public static void Postfix(List<FactionDef> ___factions)
        {
            FactionsUtility.SetDefaultFactions(___factions);
        }
    }

    [HarmonyPatchCategory("World")]
    [HarmonyPatch(typeof(Page_CreateWorldParams))]
    [HarmonyPatch(nameof(Page_CreateWorldParams.DoWindowContents))]
    public static class Patch_Page_CreateWorldParams_DoWindowContents
    {
        public static void Postfix(Rect rect, float ___planetCoverage, OverallRainfall ___rainfall, OverallTemperature ___temperature, OverallPopulation ___population, float ___pollution, LandmarkDensity ___landmarkDensity, List<FactionDef> ___factions)
        {
            if (!Settings.GetValue<bool>(Settings.HIDE_SETASDEFAULT))
            {
                Rect buttonRect = new Rect(rect.x + rect.width - 150f - 16f, rect.y + 4f, 150f, 40f);
                if (Widgets.ButtonText(buttonRect, "Defaults_SetAsDefault".Translate()))
                {
                    Settings.SetValue(Settings.PLANET_COVERAGE, ___planetCoverage);
                    Settings.SetValue(Settings.OVERALL_RAINFALL, ___rainfall);
                    Settings.SetValue(Settings.OVERALL_TEMPERATURE, ___temperature);
                    Settings.SetValue(Settings.OVERALL_POPULATION, ___population);
                    if (ModsConfig.BiotechActive)
                    {
                        Settings.SetValue(Settings.PLANET_POLLUTION, ___pollution);
                    }
                    if (ModsConfig.OdysseyActive)
                    {
                        Settings.SetValue(Settings.LANDMARK_DENSITY, ___landmarkDensity);
                    }
                    Settings.SetValue(Settings.MAP_SIZE, Find.GameInitData.mapSize);
                    Settings.SetValue(Settings.STARTING_SEASON, Find.GameInitData.startingSeason);
                    Settings.Set(Settings.FACTIONS, ___factions.Where(f => f.displayInFactionSelection).ToList());
                    DefaultsMod.SaveSettings();
                    Messages.Message("Defaults_SetAsDefaultConfirmed".Translate(), MessageTypeDefOf.PositiveEvent, false);
                }
            }
        }
    }

    [HarmonyPatchCategory("World")]
    [HarmonyPatch(typeof(Page_CreateWorldParams))]
    [HarmonyPatch(nameof(Page_CreateWorldParams.PostOpen))]
    public static class Patch_Page_CreateWorldParams_PostOpen
    {
        public static void Postfix()
        {
            Find.GameInitData.mapSize = Settings.GetValue<int>(Settings.MAP_SIZE);
            Find.GameInitData.startingSeason = Settings.GetValue<Season>(Settings.STARTING_SEASON);
        }
    }
}

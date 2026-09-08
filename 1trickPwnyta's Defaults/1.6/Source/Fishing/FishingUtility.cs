using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Fishing
{
    public static class FishingUtility
    {
        public static string GetLabel(this FishRepeatMode mode)
        {
            switch (mode)
            {
                case FishRepeatMode.RepeatCount: return "FishRepeatMode_RepeatCount".Translate().CapitalizeFirst();
                case FishRepeatMode.TargetCount: return "FishRepeatMode_TargetCount".Translate().CapitalizeFirst();
                case FishRepeatMode.DoForever: return "FishRepeatMode_Forever".Translate().CapitalizeFirst();
                default: return "Unknown";
            }
        }

        public static void SetDefaultFishingZoneSettings(Zone_Fishing zone)
        {
            FishingZoneOptions options = Settings.Get<FishingZoneOptions>(Settings.FISHING_ZONE_OPTIONS);
            zone.repeatMode = options.DefaultFishRepeatMode;
            zone.repeatCount = options.DefaultFishRepeatCount;
            zone.targetCount = options.DefaultFishTargetCount;
            zone.pauseWhenSatisfied = options.DefaultFishPause;
            zone.unpauseAtCount = options.DefaultFishUnpauseCount;
            WaterBody waterBody = zone.Cells[0].GetWaterBody(zone.Map);
            zone.targetPopulationPct = Mathf.Max(options.DefaultFishTargetPopulation, 10f / waterBody.MaxPopulation);
            ITab_Fishing tab = zone.GetInspectTabs().First() as ITab_Fishing;
            typeof(ITab_Fishing).Field("repeatCountEditBuffer").SetValue(tab, zone.repeatCount.ToString());
            typeof(ITab_Fishing).Field("targetCountEditBuffer").SetValue(tab, zone.targetCount.ToString());
            typeof(ITab_Fishing).Field("unpauseAtCountEditBuffer").SetValue(tab, zone.unpauseAtCount.ToString());
        }
    }
}

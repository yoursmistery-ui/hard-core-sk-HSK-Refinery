using Defaults.UI;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace Defaults.Fishing
{
    [HarmonyPatchCategory("Fishing")]
    [HarmonyPatch(typeof(Zone_Fishing))]
    [HarmonyPatch(nameof(Zone_Fishing.GetGizmos))]
    [HarmonyPatchMod("Ludeon.RimWorld.Odyssey")]
    public static class Patch_Zone_Fishing
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Zone_Fishing __instance)
        {
            foreach (Gizmo gizmo in gizmos)
            {
                yield return gizmo;
            }

            if (!Settings.GetValue<bool>(Settings.HIDE_LOADDEFAULT))
            {
                yield return new Command_Action
                {
                    defaultLabel = "Defaults_ResetToDefault".Translate(),
                    defaultDesc = "Defaults_ResetFishingZoneDesc".Translate(),
                    icon = UIUtility.ResetCommandTex,
                    defaultIconColor = UIUtility.CommandColor,
                    action = () =>
                    {
                        FishingUtility.SetDefaultFishingZoneSettings(__instance);
                        SoundDefOf.Click.PlayOneShot(null);
                    }
                };
            }

            if (!Settings.GetValue<bool>(Settings.HIDE_SETASDEFAULT))
            {
                yield return new Command_Action
                {
                    defaultLabel = "Defaults_SetAsDefault".Translate(),
                    defaultDesc = "Defaults_SetDefaultFishingZoneDesc".Translate(),
                    icon = UIUtility.SaveCommandTex,
                    defaultIconColor = UIUtility.CommandColor,
                    action = () =>
                    {
                        FishingZoneOptions options = Settings.Get<FishingZoneOptions>(Settings.FISHING_ZONE_OPTIONS);
                        options.DefaultFishRepeatMode = __instance.repeatMode;
                        options.DefaultFishRepeatCount = __instance.repeatCount;
                        options.DefaultFishTargetCount = __instance.targetCount;
                        options.DefaultFishPause = __instance.pauseWhenSatisfied;
                        options.DefaultFishUnpauseCount = __instance.unpauseAtCount;
                        options.DefaultFishTargetPopulation = __instance.targetPopulationPct;
                        DefaultsMod.SaveSettings();
                        SoundDefOf.Click.PlayOneShot(null);
                        Messages.Message("Defaults_SetAsDefaultConfirmed".Translate(), MessageTypeDefOf.PositiveEvent, false);
                    }
                };
            }
        }
    }
}

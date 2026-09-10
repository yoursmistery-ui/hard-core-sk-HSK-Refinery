using HarmonyLib;
using Verse;

namespace Defaults.PlaySettings
{
    [HarmonyPatchCategory("PlaySettings")]
    [HarmonyPatch(typeof(RimWorld.PlaySettings))]
    [HarmonyPatch(MethodType.Constructor)]
    public static class Patch_PlaySettings_ctor
    {
        public static void Postfix(RimWorld.PlaySettings __instance)
        {
            __instance.autoRebuild = Settings.GetValue<bool>(Settings.AUTO_REBUILD);
            __instance.autoHomeArea = Settings.GetValue<bool>(Settings.AUTO_HOME_AREA);
            __instance.showZones = Settings.GetValue<bool>(Settings.ZONE_VISIBILITY);
            __instance.useWorkPriorities = Settings.GetValue<bool>(Settings.MANUAL_PRIORITIES);
            __instance.lockNorthUp = Settings.GetValue<bool>(Settings.LOCK_NORTH_UP);
            __instance.usePlanetDayNightSystem = Settings.GetValue<bool>(Settings.SHOW_PLANET_DAY_NIGHT);
            __instance.showImportantExpandingIcons = Settings.GetValue<bool>(Settings.SHOW_EXPANDING_ICONS_IMPORTANT);
            __instance.showBasesExpandingIcons = Settings.GetValue<bool>(Settings.SHOW_EXPANDING_ICONS_BASES);
            if (ModsConfig.OdysseyActive)
            {
                __instance.showExpandingLandmarks = Settings.GetValue<bool>(Settings.SHOW_EXPANDING_ICONS_LANDMARKS);
            }
            __instance.showWorldFeatures = Settings.GetValue<bool>(Settings.SHOW_WORLD_FEATURES);
        }
    }
}

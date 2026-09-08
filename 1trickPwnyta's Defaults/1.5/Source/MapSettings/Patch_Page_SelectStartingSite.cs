using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.MapSettings
{
    [HarmonyPatch(typeof(Page_SelectStartingSite))]
    [HarmonyPatch(nameof(Page_SelectStartingSite.PostOpen))]
    public static class Patch_Page_SelectStartingSite
    {
        public static void Postfix()
        {
            Find.GameInitData.mapSize = DefaultsSettings.DefaultMapSize;
            Find.GameInitData.startingSeason = DefaultsSettings.DefaultStartingSeason;
        }
    }
}

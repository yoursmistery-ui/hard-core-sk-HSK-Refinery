using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.StockpileZones
{
    [HarmonyPatch(typeof(Designator_ZoneAddStockpile))]
    [HarmonyPatch("MakeNewZone")]
    public static class Patch_Designator_ZoneAddStockpile
    {
        public static void Postfix(Designator_ZoneAddStockpile __instance, Zone __result)
        {
            if (__instance is Designator_ZoneAddStockpile_Resources)
            {
                if (DefaultsSettings.DefaultStockpileZone != null)
                {
                    (__result as Zone_Stockpile).settings.Priority = DefaultsSettings.DefaultStockpileZone.Priority;
                    (__result as Zone_Stockpile).settings.filter.CopyAllowancesFrom(DefaultsSettings.DefaultStockpileZone.filter);
                }
            }
            if (__instance is Designator_ZoneAddStockpile_Dumping)
            {
                if (DefaultsSettings.DefaultDumpingStockpileZone != null)
                {
                    (__result as Zone_Stockpile).settings.Priority = DefaultsSettings.DefaultDumpingStockpileZone.Priority;
                    (__result as Zone_Stockpile).settings.filter.CopyAllowancesFrom(DefaultsSettings.DefaultDumpingStockpileZone.filter);
                }
            }
        }
    }
}

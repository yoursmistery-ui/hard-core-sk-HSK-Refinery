using Defaults.Workers;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.StockpileZones
{
    [HarmonyPatchCategory("Storage")]
    [HarmonyPatch(typeof(Designator_ZoneAddStockpile))]
    [HarmonyPatch("MakeNewZone")]
    public static class Patch_Designator_ZoneAddStockpile
    {
        public static void Postfix(Designator_ZoneAddStockpile __instance, Zone __result)
        {
            DefaultSettingsCategoryWorker_Storage worker = DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Storage>();

            if (__instance is Designator_ZoneAddStockpile_Resources)
            {
                if (worker.DefaultStockpileZone != null)
                {
                    (__result as Zone_Stockpile).settings.Priority = worker.DefaultStockpileZone.priority;
                    (__result as Zone_Stockpile).settings.filter.CopyAllowancesFrom(worker.DefaultStockpileZone.filter);
                }
            }
            if (__instance is Designator_ZoneAddStockpile_Dumping)
            {
                if (worker.DefaultDumpingStockpileZone != null)
                {
                    (__result as Zone_Stockpile).settings.Priority = worker.DefaultDumpingStockpileZone.priority;
                    (__result as Zone_Stockpile).settings.filter.CopyAllowancesFrom(worker.DefaultDumpingStockpileZone.filter);
                }
            }
        }
    }
}

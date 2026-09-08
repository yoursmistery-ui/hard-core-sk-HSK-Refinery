using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.StockpileZones
{
    public class Designator_ZoneAddStockpile_Custom : Designator_ZoneAddStockpile
    {
        private readonly ZoneType zoneType;

        public Designator_ZoneAddStockpile_Custom(ZoneType type)
        {
            zoneType = type;
            defaultLabel = type.Name;
            defaultDesc = type.Desc;
            icon = type.Icon;
            soundSucceeded = type.Sound;
            hotKey = null;
        }

        protected override string NewZoneLabel => zoneType.Name;

        protected override Zone MakeNewZone()
        {
            Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, Find.CurrentMap.zoneManager);
            typeof(Zone).Field("baseLabel").SetValue(zone, zoneType.Name);
            zone.label = Find.CurrentMap.zoneManager.NewZoneName(zoneType.Name);
            zone.settings.Priority = zoneType.priority;
            zone.settings.filter.CopyAllowancesFrom(zoneType.filter);
            return zone;
        }
    }
}

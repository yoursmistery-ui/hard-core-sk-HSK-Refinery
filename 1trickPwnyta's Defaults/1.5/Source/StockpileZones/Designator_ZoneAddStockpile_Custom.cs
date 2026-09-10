using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.StockpileZones
{
    public class Designator_ZoneAddStockpile_Custom : Designator_ZoneAddStockpile
    {
        private ZoneType zoneType;

        public Designator_ZoneAddStockpile_Custom(ZoneType type)
        {
            zoneType = type;
            this.defaultLabel = type.Name;
            this.defaultDesc = type.Desc;
            this.icon = type.Icon;
            this.soundSucceeded = type.Sound;
            this.hotKey = null;
        }

        protected override string NewZoneLabel
        {
            get
            {
                return zoneType.Name;
            }
        }

        protected override Zone MakeNewZone()
        {
            Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, Find.CurrentMap.zoneManager);
            typeof(Zone).Field("baseLabel").SetValue(zone, zoneType.Name);
            zone.label = Find.CurrentMap.zoneManager.NewZoneName(zoneType.Name);
            zone.settings.Priority = zoneType.Priority;
            zone.settings.filter.CopyAllowancesFrom(zoneType.filter);
            return zone;
        }
    }
}

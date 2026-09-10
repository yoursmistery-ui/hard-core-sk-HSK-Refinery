using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.StockpileZones.Buildings
{
    public static class BuildingUtility
    {
        public static void SetDefaultBuildingStorageSettings(ThingDef def, IStoreSettingsParent parent)
        {
            ZoneType zone = Settings.Get<Dictionary<ThingDef, ZoneType_Building>>(Settings.BUILDING_STORAGE).TryGetValue(def);
            if (zone != null)
            {
                StorageSettings settings = parent.GetStoreSettings();
                if (settings != null)
                {
                    settings.Priority = zone.priority;
                    settings.filter.CopyAllowancesFrom(zone.filter);
                }
            }
        }

        public static bool IsValidSpecialThingFilter(this ThingFilter filter, SpecialThingFilterDef def)
        {
            foreach (ThingCategoryDef category in filter.DisplayRootCategory.catDef.ThisAndChildCategoryDefs.Where(c => c.DescendantThingDefs.Any(t => filter.Allows(t))))
            {
                if (category.DescendantSpecialThingFilterDefs.Contains(def))
                {
                    return true;
                }
                if (category.ParentsSpecialThingFilterDefs.Contains(def))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

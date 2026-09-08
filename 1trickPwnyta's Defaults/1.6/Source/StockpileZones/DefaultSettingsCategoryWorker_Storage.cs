using Defaults.Compatibility;
using Defaults.Defs;
using Defaults.StockpileZones.Buildings;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.StockpileZones
{
    [StaticConstructorOnStartup]
    public class DefaultSettingsCategoryWorker_Storage : DefaultSettingsCategoryWorker
    {
        private static readonly List<ThingDef> buildingsUnlockedByDefault = new List<ThingDef>()
        {
            ThingDefOf.Shelf,
            ThingDefOf.ShelfSmall,
            ThingDef.Named("Bookcase"),
            ThingDef.Named("BookcaseSmall"),
            ThingDefOf.Hopper,
            ThingDefOf.GrowthVat,
            ThingDefOf.BiosculpterPod,
            ThingDef.Named("Artillery_Mortar")
        };

        private List<ZoneType> defaultStockpileZones;
        private Dictionary<ThingDef, ZoneType_Building> defaultBuildingStorageSettings;

        public DefaultSettingsCategoryWorker_Storage(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public ZoneType DefaultStockpileZone => defaultStockpileZones.FirstOrDefault(z => z.DesignatorType == typeof(Designator_ZoneAddStockpile_Resources));

        public ZoneType DefaultDumpingStockpileZone => defaultStockpileZones.FirstOrDefault(z => z.DesignatorType == typeof(Designator_ZoneAddStockpile_Dumping));

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_StockpileZones(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.STOCKPILE_ZONES:
                    value = defaultStockpileZones;
                    return true;
                case Settings.BUILDING_STORAGE:
                    value = defaultBuildingStorageSettings;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.STOCKPILE_ZONES:
                    defaultStockpileZones = value as List<ZoneType>;
                    return true;
                case Settings.BUILDING_STORAGE:
                    defaultBuildingStorageSettings = value as Dictionary<ThingDef, ZoneType_Building>;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        public override void HandleNewDefs(IEnumerable<Def> defs)
        {
            foreach (ZoneType zone in defaultStockpileZones)
            {
                if (!zone.locked)
                {
                    foreach (ThingDef def in defs.OfType<ThingDef>())
                    {
                        switch (zone.preset)
                        {
                            case StorageSettingsPreset.DumpingStockpile:
                                if (ThingCategoryDefOf.Corpses.DescendantThingDefs.Union(ThingCategoryDefOf.Chunks.DescendantThingDefs).Contains(def) || (ModsConfig.BiotechActive && def == ThingDefOf.Wastepack))
                                {
                                    zone.filter.SetAllow(def, true);
                                }
                                break;
                            case StorageSettingsPreset.CorpseStockpile:
                                if (ThingCategoryDefOf.Corpses.DescendantThingDefs.Contains(def))
                                {
                                    zone.filter.SetAllow(def, true);
                                }
                                break;
                            case StorageSettingsPreset.DefaultStockpile:
                            default:
                                if (ThingCategoryDefOf.Foods.DescendantThingDefs.Union(ThingCategoryDefOf.Manufactured.DescendantThingDefs).Union(ThingCategoryDefOf.ResourcesRaw.DescendantThingDefs).Union(ThingCategoryDefOf.Items.DescendantThingDefs).Union(ThingCategoryDefOf.Buildings.DescendantThingDefs).Union(ThingCategoryDefOf.Weapons.DescendantThingDefs).Union(ThingCategoryDefOf.Apparel.DescendantThingDefs).Union(ThingCategoryDefOf.BodyParts.DescendantThingDefs).Contains(def) && (!ModsConfig.BiotechActive || def != ThingDefOf.Wastepack))
                                {
                                    zone.filter.SetAllow(def, true);
                                }
                                break;
                        }
                    }

                    foreach (SpecialThingFilterDef def in defs.OfType<SpecialThingFilterDef>())
                    {
                        if (!def.allowedByDefault)
                        {
                            zone.filter.SetAllow(def, false);
                        }
                    }
                }
            }

            foreach (ZoneType_Building zone in defaultBuildingStorageSettings.Values)
            {
                if (!zone.locked)
                {
                    foreach (ThingDef def in defs.OfType<ThingDef>())
                    {
                        if (zone.buildingDef.building.defaultStorageSettings.filter.Allows(def))
                        {
                            zone.filter.SetAllow(def, true);
                        }
                    }

                    foreach (SpecialThingFilterDef def in defs.OfType<SpecialThingFilterDef>())
                    {
                        if (!zone.buildingDef.building.defaultStorageSettings.filter.Allows(def))
                        {
                            zone.filter.SetAllow(def, false);
                        }
                    }
                }
            }
        }

        public void ResetStockpileZoneSettings(bool forced)
        {
            if (forced || defaultStockpileZones == null)
            {
                defaultStockpileZones = new List<ZoneType>();
            }
            if (!defaultStockpileZones.Any(z => z.DesignatorType == typeof(Designator_ZoneAddStockpile_Dumping)))
            {
                defaultStockpileZones.Insert(0, ZoneType.MakeBuiltInDumpingStockpileZone());
            }
            if (!defaultStockpileZones.Any(z => z.DesignatorType == typeof(Designator_ZoneAddStockpile_Resources)))
            {
                defaultStockpileZones.Insert(0, ZoneType.MakeBuiltInStockpileZone());
            }
        }

        public void ResetBuildingStorageSettings(bool forced)
        {
            if (forced || defaultBuildingStorageSettings == null)
            {
                defaultBuildingStorageSettings = new Dictionary<ThingDef, ZoneType_Building>();
            }
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.building?.defaultStorageSettings != null))
            {
                if (!defaultBuildingStorageSettings.ContainsKey(def))
                {
                    defaultBuildingStorageSettings[def] = new ZoneType_Building(def)
                    {
                        locked = !buildingsUnlockedByDefault.Contains(def)
                    };
                }
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            ResetStockpileZoneSettings(forced);
            ResetBuildingStorageSettings(forced);
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Collections.Look(ref defaultStockpileZones, Settings.STOCKPILE_ZONES, LookMode.Deep);
            Scribe_Collections_Silent.LookKeysDef(ref defaultBuildingStorageSettings, Settings.BUILDING_STORAGE, LookMode.Deep);
            BackwardCompatibilityUtility.MigrateDefaultShelfSettings(ref defaultBuildingStorageSettings);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && defaultBuildingStorageSettings != null)
            {
                defaultBuildingStorageSettings.RemoveAll(kv => kv.Value.buildingDef == null);
            }
        }
    }
}

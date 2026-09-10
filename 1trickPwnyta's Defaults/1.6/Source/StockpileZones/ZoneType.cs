using Defaults.Compatibility;
using Defaults.StockpileZones.Shelves;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.StockpileZones
{
    public class ZoneType : IExposable, IRenameable
    {
        private Type designatorType = typeof(Designator_ZoneAddStockpile_Custom);
        private string name;
        public StoragePriority priority = StoragePriority.Normal;
        public ThingFilter filter = new ThingFilter();
        public StorageSettingsPreset preset;
        public bool locked = true;

        private List<ThingDef> compatibility_allowed;
        private FloatRange compatibility_allowedHitPointsPercents;
        private QualityRange compatibility_allowedQualityLevels;
        private List<SpecialThingFilterDef> compatibility_disallowedSpecialFilters;

        public static ZoneType MakeBuiltInStockpileZone()
        {
            ZoneType zoneType = new ZoneType(StorageSettingsPreset.DefaultStockpile.PresetName(), StorageSettingsPreset.DefaultStockpile)
            {
                designatorType = typeof(Designator_ZoneAddStockpile_Resources),
                locked = false
            };
            return zoneType;
        }

        public static ZoneType MakeBuiltInDumpingStockpileZone()
        {
            ZoneType zoneType = new ZoneType(StorageSettingsPreset.DumpingStockpile.PresetName(), StorageSettingsPreset.DumpingStockpile)
            {
                designatorType = typeof(Designator_ZoneAddStockpile_Dumping),
                locked = false
            };
            return zoneType;
        }

        public static ZoneType MakeBuiltInShelfSettings()
        {
            ZoneType zoneType = new ZoneType_Shelf { priority = StoragePriority.Preferred };
            Array.ForEach(new[] { ThingCategoryDefOf.Foods, ThingCategoryDefOf.Manufactured, ThingCategoryDefOf.ResourcesRaw, ThingCategoryDefOf.Items, ThingCategoryDefOf.Weapons, ThingCategoryDefOf.Apparel, ThingCategoryDefOf.BodyParts }, d =>
            {
                zoneType.filter.SetAllow(d, true);
            });
            zoneType.locked = false;
            return zoneType;
        }

        public ZoneType()
        {

        }

        public ZoneType(string name, StorageSettingsPreset preset)
        {
            this.name = name;
            filter.SetFromPreset(preset);
            this.preset = preset;
        }

        public ZoneType(string name, ZoneType other)
        {
            this.name = name;
            priority = other.priority;
            filter.CopyAllowancesFrom(other.filter);
            preset = other.preset;
        }

        public Type DesignatorType => designatorType;

        public virtual string Name => RenamableLabel;

        public string RenamableLabel { get => name; set => name = value; }

        public string BaseLabel => RenamableLabel;

        public string InspectLabel => RenamableLabel;

        public string Desc
        {
            get
            {
                switch (preset)
                {
                    case StorageSettingsPreset.DefaultStockpile: return "DesignatorZoneCreateStorageResourcesDesc".Translate();
                    case StorageSettingsPreset.DumpingStockpile: return "DesignatorZoneCreateStorageDumpingDesc".Translate();
                    case StorageSettingsPreset.CorpseStockpile: return "Defaults_CorpseStockpileZoneDesc".Translate();
                    default: return null;
                }
            }
        }

        public virtual Texture2D Icon
        {
            get
            {
                switch (preset)
                {
                    case StorageSettingsPreset.DefaultStockpile: return Dialog_StockpileZones.DefaultStockpileIcon;
                    case StorageSettingsPreset.DumpingStockpile: return Dialog_StockpileZones.DumpingStockpileIcon;
                    case StorageSettingsPreset.CorpseStockpile: return Dialog_StockpileZones.CorpseStockpileIcon;
                    default: return null;
                }
            }
        }

        public virtual Color IconColor => Color.white;

        public SoundDef Sound
        {
            get
            {
                switch (preset)
                {
                    case StorageSettingsPreset.DefaultStockpile: return SoundDefOf.Designate_ZoneAdd_Stockpile;
                    case StorageSettingsPreset.DumpingStockpile: return SoundDefOf.Designate_ZoneAdd_Dumping;
                    case StorageSettingsPreset.CorpseStockpile: return SoundDefOf.Designate_ZoneAdd_Dumping;
                    default: return null;
                }
            }
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref designatorType, "designatorType");
            Scribe_Values.Look(ref name, "Name");
            Scribe_Values.Look(ref priority, "Priority");
            Scribe_Values.Look(ref preset, "Preset");
            Scribe_Deep.Look(ref filter, "filter");
            if (Scribe.mode == LoadSaveMode.LoadingVars && filter == null)
            {
                filter = new ThingFilter();
            }
            Scribe_Values.Look(ref locked, "locked", true);

            ThingFilter compatibilityFilter = BackwardCompatibilityUtility.LoadExposedThingFilter(ref compatibility_allowed, ref compatibility_allowedHitPointsPercents, ref compatibility_allowedQualityLevels, ref compatibility_disallowedSpecialFilters);
            if (compatibilityFilter != null)
            {
                filter = compatibilityFilter;
            }
        }
    }
}

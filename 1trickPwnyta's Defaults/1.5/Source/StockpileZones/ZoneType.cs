using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Defaults.StockpileZones
{
    public class ZoneType : IExposable, IRenameable
    {
        private Type designatorType = typeof(Designator_ZoneAddStockpile_Custom);
        public string Name;
        public StoragePriority Priority = StoragePriority.Normal;
        public ThingFilter filter = new ThingFilter();
        public StorageSettingsPreset Preset;
        public bool locked = true;

        public static ZoneType MakeBuiltInStockpileZone()
        {
            ZoneType zoneType = new ZoneType(StorageSettingsPreset.DefaultStockpile.PresetName(), StorageSettingsPreset.DefaultStockpile);
            zoneType.designatorType = typeof(Designator_ZoneAddStockpile_Resources);
            zoneType.locked = false;
            return zoneType;
        }

        public static ZoneType MakeBuiltInDumpingStockpileZone()
        {
            ZoneType zoneType = new ZoneType(StorageSettingsPreset.DumpingStockpile.PresetName(), StorageSettingsPreset.DumpingStockpile);
            zoneType.designatorType = typeof(Designator_ZoneAddStockpile_Dumping);
            zoneType.locked = false;
            return zoneType;
        }

        public static ZoneType MakeBuiltInShelfSettings()
        {
            ZoneType zoneType = new ZoneType();
            zoneType.Priority = StoragePriority.Preferred;
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
            Name = name;
            filter.SetFromPreset(preset);
            Preset = preset;
        }

        public ZoneType(string name, ZoneType other)
        {
            Name = name;
            Priority = other.Priority;
            filter.CopyAllowancesFrom(other.filter);
            Preset = other.Preset;
        }

        public Type DesignatorType
        {
            get => designatorType;
        }

        public string RenamableLabel { get => Name; set => Name = value; }

        public string BaseLabel { get => RenamableLabel; }

        public string InspectLabel { get => RenamableLabel; }

        public string Desc
        {
            get
            {
                switch (Preset)
                {
                    case StorageSettingsPreset.DefaultStockpile: return "DesignatorZoneCreateStorageResourcesDesc".Translate();
                    case StorageSettingsPreset.DumpingStockpile: return "DesignatorZoneCreateStorageDumpingDesc".Translate();
                    case StorageSettingsPreset.CorpseStockpile: return "Defaults_CorpseStockpileZoneDesc".Translate();
                    default: return null;
                }
            }
        }

        public Texture2D Icon
        {
            get
            {
                switch (Preset)
                {
                    case StorageSettingsPreset.DefaultStockpile: return Dialog_StockpileZones.DefaultStockpileIcon;
                    case StorageSettingsPreset.DumpingStockpile: return Dialog_StockpileZones.DumpingStockpileIcon;
                    case StorageSettingsPreset.CorpseStockpile: return Dialog_StockpileZones.CorpseStockpileIcon;
                    default: return null;
                }
            }
        }

        public SoundDef Sound
        {
            get
            {
                switch (Preset)
                {
                    case StorageSettingsPreset.DefaultStockpile: return SoundDefOf.Designate_ZoneAdd_Stockpile;
                    case StorageSettingsPreset.DumpingStockpile: return SoundDefOf.Designate_ZoneAdd_Dumping;
                    case StorageSettingsPreset.CorpseStockpile: return SoundDefOf.Designate_ZoneAdd_Dumping;
                    default: return null;
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref designatorType, "designatorType");
            Scribe_Values.Look(ref Name, "Name");
            Scribe_Values.Look(ref Priority, "Priority");
            Scribe_Values.Look(ref Preset, "Preset");
            DefaultsSettings.ScribeThingFilter(filter);
            Scribe_Values.Look(ref locked, "locked", true);
        }
    }
}

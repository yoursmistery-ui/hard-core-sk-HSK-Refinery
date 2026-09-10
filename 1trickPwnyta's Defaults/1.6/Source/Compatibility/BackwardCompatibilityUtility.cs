using Defaults.Compatibility;
using Defaults.Defs;
using Defaults.Medicine;
using Defaults.Policies;
using Defaults.StockpileZones.Buildings;
using Defaults.WorkbenchBills;
using Defaults.WorldSettings;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Compatibility
{
    public static class BackwardCompatibilityUtility
    {
        private abstract class Compatibility_Policy<T> : Policy where T : Policy
        {
            public List<ThingDef> allowed;
            public FloatRange allowedHitPointsPercents;
            public QualityRange allowedQualityLevels;
            public List<SpecialThingFilterDef> disallowedSpecialFilters;
            public bool? locked;
            public ThingFilter filter;

            protected abstract ThingFilter GetFilter(T policy);

            public void Migrate()
            {
                if (filter != null)
                {
                    T policy = PolicyUtility.NewDefaultPolicy<T>(label);
                    GetFilter(policy).CopyAllowancesFrom(filter);
                }
                if (locked != null)
                {
                    T policy = PolicyUtility.GetDefaultPolicies<T>().First(p => p.label == label);
                    policy.SetLocked(locked.Value);
                }
            }

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Values.Look(ref locked, "locked");
                ThingFilter compatibilityFilter = LoadExposedThingFilter(ref allowed, ref allowedHitPointsPercents, ref allowedQualityLevels, ref disallowedSpecialFilters);
                if (compatibilityFilter != null)
                {
                    filter = compatibilityFilter;
                }
            }

            public override void CopyFrom(Policy other)
            {
            }
        }

        private class Compatibility_ApparelPolicy : Compatibility_Policy<ApparelPolicy>
        {
            protected override string LoadKey => "Compatibility_ApparelPolicy";

            protected override ThingFilter GetFilter(ApparelPolicy policy) => policy.filter;
        }

        private class Compatibility_FoodPolicy : Compatibility_Policy<FoodPolicy>
        {
            protected override string LoadKey => "Compatibility_FoodPolicy";

            protected override ThingFilter GetFilter(FoodPolicy policy) => policy.filter;
        }

        private class Compatibility_ReadingPolicy : Compatibility_Policy<ReadingPolicy>
        {
            protected override string LoadKey => "Compatibility_ReadingPolicy";

            protected override ThingFilter GetFilter(ReadingPolicy policy) => null;
        }

        private static List<Compatibility_ApparelPolicy> compatibility_apparelPolicies;
        private static List<Compatibility_FoodPolicy> compatibility_foodPolicies;
        private static List<Compatibility_ReadingPolicy> compatibility_readingPolicies;
        private static ZoneType_Building compatibility_shelfSettings;

        public static ThingFilter LoadExposedThingFilter(ref List<ThingDef> allowed, ref FloatRange allowedHitPointsPercents, ref QualityRange allowedQualityLevels, ref List<SpecialThingFilterDef> disallowedSpecialFilters)
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                Scribe_Collections_Silent.Look(ref allowed, "Allowed");
                Scribe_Values.Look(ref allowedHitPointsPercents, "AllowedHitPointsPercents");
                Scribe_Values.Look(ref allowedQualityLevels, "AllowedQualityLevels");
                Scribe_Collections_Silent.Look(ref disallowedSpecialFilters, "DisallowedSpecialFilters");
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (allowed != null)
                {
                    ThingFilter filter = new ThingFilter();
                    foreach (ThingDef def in allowed)
                    {
                        if (def != null)
                        {
                            filter.SetAllow(def, true);
                        }
                    }
                    filter.AllowedHitPointsPercents = allowedHitPointsPercents;
                    filter.AllowedQualityLevels = allowedQualityLevels;
                    foreach (SpecialThingFilterDef def in disallowedSpecialFilters)
                    {
                        if (def != null)
                        {
                            filter.SetAllow(def, false);
                        }
                    }
                    return filter;
                }
            }

            return null;
        }

        public static void MigrateAnomalyPlaystyleSettings(Difficulty difficultyValues)
        {
            if (ModsConfig.AnomalyActive && Scribe.mode == LoadSaveMode.LoadingVars)
            {
                AnomalyPlaystyleDef compatibility_anomalyPlaystyleDef = default;
                Scribe_Defs_Silent.Look(ref compatibility_anomalyPlaystyleDef, "DefaultAnomalyPlaystyle");
                if (compatibility_anomalyPlaystyleDef != null)
                {
                    difficultyValues.AnomalyPlaystyleDef = compatibility_anomalyPlaystyleDef;
                }
            }
        }

        public static void MigratePlanetOptions()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                PlanetOptions options = null;
                Scribe_Deep.Look(ref options, Settings.PLANET);
                if (options != null)
                {
                    Settings.SetValue(Settings.PLANET_COVERAGE, options.DefaultPlanetCoverage);
                    Settings.SetValue(Settings.OVERALL_RAINFALL, options.DefaultOverallRainfall);
                    Settings.SetValue(Settings.OVERALL_TEMPERATURE, options.DefaultOverallTemperature);
                    Settings.SetValue(Settings.OVERALL_POPULATION, options.DefaultOverallPopulation);
                    if (ModsConfig.OdysseyActive)
                    {
                        Settings.SetValue(Settings.LANDMARK_DENSITY, options.DefaultLandmarkDensity);
                    }
                    if (ModsConfig.BiotechActive)
                    {
                        Settings.SetValue(Settings.PLANET_POLLUTION, options.DefaultPollution);
                    }
                }
            }
        }

        public static void MigrateMapOptions()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                MapOptions options = null;
                Scribe_Deep.Look(ref options, Settings.MAP);
                if (options != null)
                {
                    Settings.SetValue(Settings.MAP_SIZE, options.DefaultMapSize);
                    Settings.SetValue(Settings.STARTING_SEASON, options.DefaultStartingSeason);
                }
            }
        }

        public static void MigrateMedicineOptions()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                MedicineOptions options = null;
                Scribe_Deep.Look(ref options, Settings.MEDICINE);
                if (options != null)
                {
                    Settings.SetValue(Settings.DEFAULT_CARE_COLONIST, options.DefaultCareForColonist);
                    Settings.SetValue(Settings.DEFAULT_CARE_PRISONER, options.DefaultCareForPrisoner);
                    if (ModsConfig.IdeologyActive)
                    {
                        Settings.SetValue(Settings.DEFAULT_CARE_SLAVE, options.DefaultCareForSlave);
                    }
                    Settings.SetValue(Settings.DEFAULT_CARE_TAMED_ANIMAL, options.DefaultCareForTamedAnimal);
                    Settings.SetValue(Settings.DEFAULT_CARE_FRIENDLY_FACTION, options.DefaultCareForFriendlyFaction);
                    Settings.SetValue(Settings.DEFAULT_CARE_NEUTRAL_FACTION, options.DefaultCareForNeutralFaction);
                    Settings.SetValue(Settings.DEFAULT_CARE_HOSTILE_FACTION, options.DefaultCareForHostileFaction);
                    Settings.SetValue(Settings.DEFAULT_CARE_NO_FACTION, options.DefaultCareForNoFaction);
                    Settings.SetValue(Settings.DEFAULT_CARE_WILDLIFE, options.DefaultCareForWildlife);
                    if (ModsConfig.AnomalyActive)
                    {
                        Settings.SetValue(Settings.DEFAULT_CARE_ENTITY, options.DefaultCareForEntities);
                        Settings.SetValue(Settings.DEFAULT_CARE_GHOUL, options.DefaultCareForGhouls);
                    }
                }
            }
        }

        public static void MigrateGlobalBillOptions()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                GlobalBillOptions options = null;
                Scribe_Deep.Look(ref options, Settings.GLOBAL_BILL_OPTIONS);
                if (options != null)
                {
                    Settings.SetValue(Settings.BILL_INGREDIENT_SEARCH_RADIUS, options.DefaultBillIngredientSearchRadius);
                    Settings.SetValue(Settings.BILL_ALLOWED_SKILL_RANGE, options.DefaultBillAllowedSkillRange);
                    Settings.Set(Settings.BILL_STORE_MODE, options.DefaultBillStoreMode);
                    Settings.SetValue(Settings.LIMIT_BILLS_TO_15, options.LimitBillsTo15);
                }
            }
        }

        public static void MigrateApparelPolicies(List<ApparelPolicy> policies)
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                Scribe_Collections.Look(ref compatibility_apparelPolicies, "DefaultApparelPolicies", LookMode.Deep);
                if (compatibility_apparelPolicies != null)
                {
                    if (compatibility_apparelPolicies.Any(p => p.filter != null))
                    {
                        policies.Clear();
                    }
                    foreach (Compatibility_ApparelPolicy policy in compatibility_apparelPolicies)
                    {
                        policy.Migrate();
                    }
                }
            }
        }

        public static void MigrateFoodPolicies(List<FoodPolicy> policies)
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                Scribe_Collections.Look(ref compatibility_foodPolicies, "DefaultFoodPolicies", LookMode.Deep);
                if (compatibility_foodPolicies != null)
                {
                    if (compatibility_foodPolicies.Any(p => p.filter != null))
                    {
                        policies.Clear();
                    }
                    foreach (Compatibility_FoodPolicy policy in compatibility_foodPolicies)
                    {
                        policy.Migrate();
                    }
                }
            }
        }

        public static void MigrateReadingPolicies()
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                Scribe_Collections.Look(ref compatibility_readingPolicies, "DefaultReadingPolicies", LookMode.Deep);
                if (compatibility_readingPolicies != null)
                {
                    foreach (Compatibility_ReadingPolicy policy in compatibility_readingPolicies)
                    {
                        policy.Migrate();
                    }
                }
            }
        }

        public static void MigrateDefaultShelfSettings(ref Dictionary<ThingDef, ZoneType_Building> defaultBuildingStorageSettings)
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                Scribe_Deep.Look(ref compatibility_shelfSettings, "DefaultShelfSettings");
                if (compatibility_shelfSettings != null && Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    if (defaultBuildingStorageSettings == null)
                    {
                        defaultBuildingStorageSettings = new Dictionary<ThingDef, ZoneType_Building>();
                    }
                    defaultBuildingStorageSettings[ThingDefOf.Shelf] = new ZoneType_Building(ThingDefOf.Shelf, compatibility_shelfSettings);
                    defaultBuildingStorageSettings[ThingDefOf.ShelfSmall] = new ZoneType_Building(ThingDefOf.ShelfSmall, compatibility_shelfSettings);
                }
            }
        }

        public static void CleanFactions(List<FactionDef> factions)
        {
            factions.RemoveAll(f => f == null || !f.displayInFactionSelection);
        }
    }
}

namespace Defaults.StockpileZones.Shelves
{
    public class ZoneType_Shelf : ZoneType_Building
    {
        public List<ThingDef> allowed;
        public FloatRange allowedHitPointsPercents;
        public QualityRange allowedQualityLevels;
        public List<SpecialThingFilterDef> disallowedSpecialFilters;

        public override void ExposeData()
        {
            base.ExposeData();
            ThingFilter compatibilityFilter = BackwardCompatibilityUtility.LoadExposedThingFilter(ref allowed, ref allowedHitPointsPercents, ref allowedQualityLevels, ref disallowedSpecialFilters);
            if (compatibilityFilter != null)
            {
                filter = compatibilityFilter;
            }
        }
    }
}

namespace Defaults.WorldSettings
{
    public class PlanetOptions : IExposable
    {
        public float DefaultPlanetCoverage;
        public OverallRainfall DefaultOverallRainfall;
        public OverallTemperature DefaultOverallTemperature;
        public OverallPopulation DefaultOverallPopulation;
        public LandmarkDensity DefaultLandmarkDensity;
        public float DefaultPollution;

        public void ExposeData()
        {
            Scribe_Values.Look(ref DefaultPlanetCoverage, "DefaultPlanetCoverage", ModsConfig.OdysseyActive ? 0.5f : 0.3f);
            Scribe_Values.Look(ref DefaultOverallRainfall, "DefaultOverallRainfall", OverallRainfall.Normal);
            Scribe_Values.Look(ref DefaultOverallTemperature, "DefaultOverallTemperature", OverallTemperature.Normal);
            Scribe_Values.Look(ref DefaultOverallPopulation, "DefaultOverallPopulation", OverallPopulation.Normal);
            Scribe_Values.Look(ref DefaultLandmarkDensity, "DefaultLandmarkDensity", LandmarkDensity.Normal);
            Scribe_Values.Look(ref DefaultPollution, "DefaultPollution", 0.05f);
        }
    }

    public class MapOptions : IExposable
    {
        public int DefaultMapSize = 250;
        public Season DefaultStartingSeason = Season.Undefined;

        public void ExposeData()
        {
            Scribe_Values.Look(ref DefaultMapSize, "DefaultMapSize", 250);
            Scribe_Values.Look(ref DefaultStartingSeason, "DefaultStartingSeason", Season.Undefined);
        }
    }
}

namespace Defaults.Medicine
{
    public class MedicineOptions : IExposable
    {
        public MedicalCareCategory DefaultCareForColonist;
        public MedicalCareCategory DefaultCareForPrisoner;
        public MedicalCareCategory DefaultCareForSlave;
        public MedicalCareCategory DefaultCareForGhouls;
        public MedicalCareCategory DefaultCareForTamedAnimal;
        public MedicalCareCategory DefaultCareForFriendlyFaction;
        public MedicalCareCategory DefaultCareForNeutralFaction;
        public MedicalCareCategory DefaultCareForHostileFaction;
        public MedicalCareCategory DefaultCareForNoFaction;
        public MedicalCareCategory DefaultCareForWildlife;
        public MedicalCareCategory DefaultCareForEntities;

        public void ExposeData()
        {
            Scribe_Values.Look(ref DefaultCareForColonist, "DefaultCareForColonist", MedicalCareCategory.Best);
            Scribe_Values.Look(ref DefaultCareForPrisoner, "DefaultCareForPrisoner", MedicalCareCategory.HerbalOrWorse);
            Scribe_Values.Look(ref DefaultCareForSlave, "DefaultCareForSlave", MedicalCareCategory.HerbalOrWorse);
            Scribe_Values.Look(ref DefaultCareForGhouls, "DefaultCareForGhouls", MedicalCareCategory.NoMeds);
            Scribe_Values.Look(ref DefaultCareForTamedAnimal, "DefaultCareForTamedAnimal", MedicalCareCategory.HerbalOrWorse);
            Scribe_Values.Look(ref DefaultCareForFriendlyFaction, "DefaultCareForFriendlyFaction", MedicalCareCategory.HerbalOrWorse);
            Scribe_Values.Look(ref DefaultCareForNeutralFaction, "DefaultCareForNeutralFaction", MedicalCareCategory.HerbalOrWorse);
            Scribe_Values.Look(ref DefaultCareForHostileFaction, "DefaultCareForHostileFaction", MedicalCareCategory.HerbalOrWorse);
            Scribe_Values.Look(ref DefaultCareForNoFaction, "DefaultCareForNoFaction", MedicalCareCategory.HerbalOrWorse);
            Scribe_Values.Look(ref DefaultCareForWildlife, "DefaultCareForWildlife", MedicalCareCategory.HerbalOrWorse);
            Scribe_Values.Look(ref DefaultCareForEntities, "DefaultCareForEntities", MedicalCareCategory.NoMeds);
        }
    }
}

namespace Defaults.WorkbenchBills
{
    public class GlobalBillOptions : IExposable
    {
        public float DefaultBillIngredientSearchRadius;
        public IntRange DefaultBillAllowedSkillRange;
        public BillStoreModeDef DefaultBillStoreMode;
        public bool LimitBillsTo15;

        public void ExposeData()
        {
            Scribe_Values.Look(ref DefaultBillIngredientSearchRadius, "DefaultBillIngredientSearchRadius", 999f);
            Scribe_Values.Look(ref DefaultBillAllowedSkillRange, "DefaultBillAllowedSkillRange", new IntRange(0, 20));
            Scribe_Defs_Silent.Look(ref DefaultBillStoreMode, "DefaultBillStoreMode");
            Scribe_Values.Look(ref LimitBillsTo15, "LimitBillsTo15", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && DefaultBillStoreMode == null)
            {
                DefaultBillStoreMode = BillStoreModeDefOf.BestStockpile;
            }
        }
    }
}
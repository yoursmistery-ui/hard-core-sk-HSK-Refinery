using Defaults.Compatibility;
using Defaults.Defs;
using Defaults.Workers;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Policies
{
    public class DefaultSettingsCategoryWorker_Policies : DefaultSettingsCategoryWorker
    {
        private List<ApparelPolicy> defaultApparelPolicies;
        private List<FoodPolicy> defaultFoodPolicies;
        private List<DrugPolicy> defaultDrugPolicies;
        private List<ReadingPolicy> defaultReadingPolicies;
        private HashSet<Policy> unlockedPolicies;

        public DefaultSettingsCategoryWorker_Policies(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_Policies(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.POLICIES_APPAREL:
                    value = defaultApparelPolicies;
                    return true;
                case Settings.POLICIES_FOOD:
                    value = defaultFoodPolicies;
                    return true;
                case Settings.POLICIES_DRUG:
                    value = defaultDrugPolicies;
                    return true;
                case Settings.POLICIES_READING:
                    value = defaultReadingPolicies;
                    return true;
                case Settings.UNLOCKED_POLICIES:
                    value = unlockedPolicies;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.POLICIES_APPAREL:
                    defaultApparelPolicies = value as List<ApparelPolicy>;
                    return true;
                case Settings.POLICIES_FOOD:
                    defaultFoodPolicies = value as List<FoodPolicy>;
                    return true;
                case Settings.POLICIES_DRUG:
                    defaultDrugPolicies = value as List<DrugPolicy>;
                    return true;
                case Settings.POLICIES_READING:
                    defaultReadingPolicies = value as List<ReadingPolicy>;
                    return true;
                case Settings.UNLOCKED_POLICIES:
                    unlockedPolicies = value as HashSet<Policy>;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        public override void HandleNewDefs(IEnumerable<Def> defs)
        {
            foreach (ApparelPolicy policy in defaultApparelPolicies)
            {
                if (!policy.IsLocked())
                {
                    foreach (ThingDef def in defs.OfType<ThingDef>())
                    {
                        if (ThingCategoryDefOf.Apparel.DescendantThingDefs.Contains(def))
                        {
                            policy.filter.SetAllow(def, true);
                        }
                    }
                    foreach (SpecialThingFilterDef def in defs.OfType<SpecialThingFilterDef>())
                    {
                        if (!def.allowedByDefault)
                        {
                            policy.filter.SetAllow(def, false);
                        }
                    }
                }
            }

            foreach (FoodPolicy policy in defaultFoodPolicies)
            {
                if (!policy.IsLocked())
                {
                    foreach (ThingDef def in defs.OfType<ThingDef>())
                    {
                        if (def.GetStatValueAbstract(StatDefOf.Nutrition, null) > 0f)
                        {
                            policy.filter.SetAllow(def, true);
                        }
                        if (ModsConfig.BiotechActive && def == ThingDefOf.HemogenPack)
                        {
                            policy.filter.SetAllow(def, false);
                        }
                    }
                    foreach (SpecialThingFilterDef def in defs.OfType<SpecialThingFilterDef>())
                    {
                        if (!def.allowedByDefault)
                        {
                            policy.filter.SetAllow(def, false);
                        }
                    }
                }
            }

            foreach (ReadingPolicy policy in defaultReadingPolicies)
            {
                if (!policy.IsLocked())
                {
                    foreach (ThingDef def in defs.OfType<ThingDef>())
                    {
                        if (def.HasComp<CompBook>())
                        {
                            policy.defFilter.SetAllow(def, true);
                        }
                    }
                    foreach (SpecialThingFilterDef def in defs.OfType<SpecialThingFilterDef>())
                    {
                        if (!def.allowedByDefault && ThingCategoryDefOf.BookEffects.DescendantSpecialThingFilterDefs.Contains(def))
                        {
                            policy.effectFilter.SetAllow(def, false);
                        }
                    }
                }
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            if (forced || unlockedPolicies == null)
            {
                unlockedPolicies = new HashSet<Policy>();
            }
            if (forced || defaultApparelPolicies.NullOrEmpty())
            {
                ResetApparelPolicies();
            }
            if (forced || defaultFoodPolicies.NullOrEmpty())
            {
                ResetFoodPolicies();
            }
            if (forced || defaultDrugPolicies.NullOrEmpty())
            {
                ResetDrugPolicies();
            }
            if (forced || defaultReadingPolicies.NullOrEmpty())
            {
                ResetReadingPolicies();
            }
        }

        public void ResetApparelPolicies()
        {
            unlockedPolicies.RemoveWhere(p => p is ApparelPolicy);
            defaultApparelPolicies = VanillaPolicyStore.VanillaApparelPolicies.ListFullCopy();
            unlockedPolicies.Add(defaultApparelPolicies[0]);
        }

        public void ResetFoodPolicies()
        {
            unlockedPolicies.RemoveWhere(p => p is FoodPolicy);
            defaultFoodPolicies = VanillaPolicyStore.VanillaFoodPolicies.ListFullCopy();
            unlockedPolicies.Add(defaultFoodPolicies[0]);
        }

        public void ResetDrugPolicies()
        {
            defaultDrugPolicies = VanillaPolicyStore.VanillaDrugPolicies.ListFullCopy();
        }

        public void ResetReadingPolicies()
        {
            unlockedPolicies.RemoveWhere(p => p is ReadingPolicy);
            defaultReadingPolicies = VanillaPolicyStore.VanillaReadingPolicies.ListFullCopy();
            unlockedPolicies.Add(defaultReadingPolicies[0]);
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Collections.Look(ref defaultApparelPolicies, Settings.POLICIES_APPAREL, LookMode.Deep);
            Scribe_Collections.Look(ref defaultFoodPolicies, Settings.POLICIES_FOOD, LookMode.Deep);
            Scribe_Collections.Look(ref defaultDrugPolicies, Settings.POLICIES_DRUG, LookMode.Deep);
            ValidateDrugPolicies();
            Scribe_Collections.Look(ref defaultReadingPolicies, Settings.POLICIES_READING, LookMode.Deep);
            Scribe_Collections.Look(ref unlockedPolicies, Settings.UNLOCKED_POLICIES, LookMode.Reference);
            BackwardCompatibilityUtility.MigrateApparelPolicies(defaultApparelPolicies);
            BackwardCompatibilityUtility.MigrateFoodPolicies(defaultFoodPolicies);
            BackwardCompatibilityUtility.MigrateReadingPolicies();
        }

        // Make sure all drug policy entries are for actual drugs
        private void ValidateDrugPolicies()
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                List<DrugPolicyEntry> entriesToRemove = new List<DrugPolicyEntry>();
                foreach (DrugPolicy drugPolicy in defaultDrugPolicies)
                {
                    entriesToRemove.Clear();
                    for (int i = 0; i < drugPolicy.Count; i++)
                    {
                        DrugPolicyEntry entry = drugPolicy[i];
                        if (!entry.drug.IsDrug)
                        {
                            entriesToRemove.Add(entry);
                        }
                    }
                    foreach (DrugPolicyEntry entry in entriesToRemove)
                    {
                        (typeof(DrugPolicy).Field("entriesInt").GetValue(drugPolicy) as List<DrugPolicyEntry>).Remove(entry);
                    }
                }
            }
        }
    }
}

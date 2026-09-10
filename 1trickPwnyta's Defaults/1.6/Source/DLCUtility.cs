using Defaults.Policies;
using Defaults.UI;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults
{
    public static class DLCUtility
    {
        public static void HandleNewDLCs()
        {
            object data = typeof(ModsConfig).Field("data").GetValue(null);
            List<string> newKnownDLCs = data.GetType().Field("knownExpansions").GetValue(data) as List<string>;
            if (DefaultsSettings.KnownDLCs != null)
            {
                foreach (string id in newKnownDLCs.Except(DefaultsSettings.KnownDLCs))
                {
                    HandleNewDLC(id);
                }
            }
            DefaultsSettings.KnownDLCs = newKnownDLCs.ListFullCopy();
            DefaultsMod.SaveSettings(false);
        }

        private static void HandleNewDLC(string id)
        {
            Policy[] policies;
            switch (id)
            {
                case "ludeon.rimworld.ideology":
                    policies = new Policy[]
                    {
                        VanillaPolicyStore.GetVanillaApparelPolicy("OutfitSlave".Translate()),
                        VanillaPolicyStore.GetVanillaFoodPolicy("FoodRestrictionVegetarian".Translate()),
                        VanillaPolicyStore.GetVanillaFoodPolicy("FoodRestrictionCarnivore".Translate()),
                        VanillaPolicyStore.GetVanillaFoodPolicy("FoodRestrictionCannibal".Translate()),
                        VanillaPolicyStore.GetVanillaFoodPolicy("FoodRestrictionInsectMeat".Translate())
                    };
                    break;
                case "ludeon.rimworld.anomaly":
                    policies = new Policy[]
                    {
                        VanillaPolicyStore.GetVanillaReadingPolicy("TomePolicy".Translate())
                    };
                    break;
                case "ludeon.rimworld.odyssey":
                    policies = new Policy[]
                    {
                        VanillaPolicyStore.GetVanillaApparelPolicy("OutfitSpacefarer".Translate()),
                        VanillaPolicyStore.GetVanillaReadingPolicy("MapPolicy".Translate())
                    };
                    break;
                default:
                    return;
            }
            PromptToAddPolicies(ModLister.GetExpansionWithIdentifier(id), policies.Where(p => p != null));
        }

        private static void PromptToAddPolicies(ExpansionDef dlc, IEnumerable<Policy> newPolicies)
        {
            List<Policy> missingPolicies = newPolicies.Where(p =>
                (p is ApparelPolicy && !Settings.Get<List<ApparelPolicy>>(Settings.POLICIES_APPAREL).Any(a => a.label.EqualsIgnoreCase(p.label)))
                || (p is FoodPolicy && !Settings.Get<List<FoodPolicy>>(Settings.POLICIES_FOOD).Any(a => a.label.EqualsIgnoreCase(p.label)))
                || (p is DrugPolicy && !Settings.Get<List<DrugPolicy>>(Settings.POLICIES_DRUG).Any(a => a.label.EqualsIgnoreCase(p.label)))
                || (p is ReadingPolicy && !Settings.Get<List<ReadingPolicy>>(Settings.POLICIES_READING).Any(a => a.label.EqualsIgnoreCase(p.label)))).ToList();

            if (missingPolicies.Any())
            {
                Find.WindowStack.Add(new Dialog_SelectMany("Defaults_NewDLC".Translate(dlc.LabelCap), "Defaults_NewPoliciesDLC".Translate(dlc.LabelCap), new[]
                {
                    new Tuple<string, IEnumerable<TaggedString>>("Defaults_ApparelPolicies", missingPolicies.Where(p => p is ApparelPolicy).Select(p => (TaggedString)p.label)),
                    new Tuple<string, IEnumerable<TaggedString>>("Defaults_FoodPolicies", missingPolicies.Where(p => p is FoodPolicy).Select(p => (TaggedString)p.label)),
                    new Tuple<string, IEnumerable<TaggedString>>("Defaults_DrugPolicies", missingPolicies.Where(p => p is DrugPolicy).Select(p => (TaggedString)p.label)),
                    new Tuple<string, IEnumerable<TaggedString>>("Defaults_ReadingPolicies", missingPolicies.Where(p => p is ReadingPolicy).Select(p => (TaggedString)p.label))
                }, true, results =>
                {
                    foreach (Tuple<string, TaggedString> result in results)
                    {
                        switch (result.Item1)
                        {
                            case "Defaults_ApparelPolicies":
                                ApparelPolicy apparelPolicy = PolicyUtility.NewDefaultPolicy<ApparelPolicy>(result.Item2);
                                apparelPolicy.filter.CopyAllowancesFrom(VanillaPolicyStore.GetVanillaApparelPolicy(result.Item2).filter);
                                break;
                            case "Defaults_FoodPolicies":
                                FoodPolicy foodPolicy = PolicyUtility.NewDefaultPolicy<FoodPolicy>(result.Item2);
                                foodPolicy.filter.CopyAllowancesFrom(VanillaPolicyStore.GetVanillaFoodPolicy(result.Item2).filter);
                                break;
                            case "Defaults_DrugPolicies":
                                DrugPolicy drugPolicy = PolicyUtility.NewDefaultPolicy<DrugPolicy>(result.Item2);
                                drugPolicy.CopyFrom(VanillaPolicyStore.GetVanillaDrugPolicy(result.Item2));
                                break;
                            case "Defaults_ReadingPolicies":
                                ReadingPolicy readingPolicy = PolicyUtility.NewDefaultPolicy<ReadingPolicy>(result.Item2);
                                readingPolicy.CopyFrom(VanillaPolicyStore.GetVanillaReadingPolicy(result.Item2));
                                break;
                            default:
                                throw new Exception("Invalid translation key: " + result.Item1);
                        }
                    }
                    DefaultsMod.SaveSettings();
                }, forceInput: true));
            }
        }
    }
}

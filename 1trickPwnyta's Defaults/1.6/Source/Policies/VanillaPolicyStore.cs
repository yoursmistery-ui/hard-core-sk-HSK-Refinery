using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Policies
{
    [StaticConstructorOnStartup]
    public static class VanillaPolicyStore
    {
        public static readonly bool loaded;

        private static readonly OutfitDatabase outfits = new OutfitDatabase();
        private static readonly FoodRestrictionDatabase foodRestrictions = new FoodRestrictionDatabase();
        private static readonly DrugPolicyDatabase drugPolicies = new DrugPolicyDatabase();
        private static readonly ReadingPolicyDatabase readingPolicies = new ReadingPolicyDatabase();

        static VanillaPolicyStore()
        {
            loaded = true;
        }

        public static ApparelPolicy GetVanillaApparelPolicy(string name) => outfits.AllOutfits.FirstOrDefault(o => o.label.Equals(name));

        public static FoodPolicy GetVanillaFoodPolicy(string name) => foodRestrictions.AllFoodRestrictions.FirstOrDefault(f => f.label.Equals(name));

        public static DrugPolicy GetVanillaDrugPolicy(string name) => drugPolicies.AllPolicies.FirstOrDefault(p => p.label.Equals(name));

        public static ReadingPolicy GetVanillaReadingPolicy(string name) => readingPolicies.AllReadingPolicies.FirstOrDefault(r => r.label.Equals(name));

        public static List<ApparelPolicy> VanillaApparelPolicies => outfits.AllOutfits;

        public static List<FoodPolicy> VanillaFoodPolicies => foodRestrictions.AllFoodRestrictions;

        public static List<DrugPolicy> VanillaDrugPolicies => drugPolicies.AllPolicies;

        public static List<ReadingPolicy> VanillaReadingPolicies => readingPolicies.AllReadingPolicies;
    }
}

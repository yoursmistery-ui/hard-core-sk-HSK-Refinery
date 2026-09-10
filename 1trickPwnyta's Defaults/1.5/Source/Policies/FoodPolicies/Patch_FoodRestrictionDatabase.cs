using HarmonyLib;
using RimWorld;

namespace Defaults.Policies.FoodPolicies
{
    [HarmonyPatch(typeof(FoodRestrictionDatabase))]
    [HarmonyPatch("GenerateStartingFoodRestrictions")]
    public static class Patch_FoodRestrictionDatabase
    {
        public static bool Prefix(FoodRestrictionDatabase __instance)
        {
            foreach (FoodPolicy policy in DefaultsSettings.DefaultFoodPolicies)
            {
                RimWorld.FoodPolicy foodPolicy = __instance.MakeNewFoodRestriction();
                foodPolicy.label = policy.label;
                foodPolicy.filter.CopyAllowancesFrom(policy.filter);
            }

            return false;
        }
    }
}

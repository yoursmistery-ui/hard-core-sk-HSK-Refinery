using HarmonyLib;
using RimWorld;
using System.Collections.Generic;

namespace Defaults.Policies.FoodPolicies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(FoodRestrictionDatabase))]
    [HarmonyPatch("GenerateStartingFoodRestrictions")]
    public static class Patch_FoodRestrictionDatabase
    {
        public static bool Prefix(FoodRestrictionDatabase __instance)
        {
            if (VanillaPolicyStore.loaded)
            {
                foreach (FoodPolicy policy in Settings.Get<List<FoodPolicy>>(Settings.POLICIES_FOOD))
                {
                    FoodPolicy foodPolicy = __instance.MakeNewFoodRestriction();
                    foodPolicy.label = policy.label;
                    foodPolicy.CopyFrom(policy);
                }

                return false;
            }
            else
            {
                return true;
            }
        }
    }
}

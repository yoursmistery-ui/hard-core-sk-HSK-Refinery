using HarmonyLib;
using RimWorld;

namespace Defaults.Policies.DrugPolicies
{
    [HarmonyPatch(typeof(DrugPolicyDatabase))]
    [HarmonyPatch("GenerateStartingDrugPolicies")]
    public static class Patch_DrugPolicyDatabase
    {
        public static bool Prefix(DrugPolicyDatabase __instance)
        {
            foreach (DrugPolicy policy in DefaultsSettings.DefaultDrugPolicies)
            {
                DrugPolicy drugPolicy = __instance.MakeNewDrugPolicy();
                drugPolicy.label = policy.label;
                drugPolicy.CopyFrom(policy);
            }

            return false;
        }
    }
}

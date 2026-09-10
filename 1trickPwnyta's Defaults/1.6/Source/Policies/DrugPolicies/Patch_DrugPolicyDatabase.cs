using HarmonyLib;
using RimWorld;
using System.Collections.Generic;

namespace Defaults.Policies.DrugPolicies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(DrugPolicyDatabase))]
    [HarmonyPatch("GenerateStartingDrugPolicies")]
    public static class Patch_DrugPolicyDatabase
    {
        public static bool Prefix(DrugPolicyDatabase __instance)
        {
            if (VanillaPolicyStore.loaded)
            {
                foreach (DrugPolicy policy in Settings.Get<List<DrugPolicy>>(Settings.POLICIES_DRUG))
                {
                    DrugPolicy drugPolicy = __instance.MakeNewDrugPolicy();
                    drugPolicy.label = policy.label;
                    drugPolicy.CopyFrom(policy);
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

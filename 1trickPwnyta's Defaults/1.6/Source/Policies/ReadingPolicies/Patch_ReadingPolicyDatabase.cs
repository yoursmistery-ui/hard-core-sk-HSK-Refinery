using HarmonyLib;
using RimWorld;
using System.Collections.Generic;

namespace Defaults.Policies.ReadingPolicies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(ReadingPolicyDatabase))]
    [HarmonyPatch("GenerateStartingPolicies")]
    public static class Patch_ReadingPolicyDatabase
    {
        public static bool Prefix(ReadingPolicyDatabase __instance)
        {
            if (VanillaPolicyStore.loaded)
            {
                foreach (ReadingPolicy policy in Settings.Get<List<ReadingPolicy>>(Settings.POLICIES_READING))
                {
                    ReadingPolicy readingPolicy = __instance.MakeNewReadingPolicy();
                    readingPolicy.label = policy.label;
                    readingPolicy.CopyFrom(policy);
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

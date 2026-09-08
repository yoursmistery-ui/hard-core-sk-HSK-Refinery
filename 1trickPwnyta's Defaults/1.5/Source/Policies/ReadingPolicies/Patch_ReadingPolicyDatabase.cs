using HarmonyLib;
using RimWorld;

namespace Defaults.Policies.ReadingPolicies
{
    [HarmonyPatch(typeof(ReadingPolicyDatabase))]
    [HarmonyPatch("GenerateStartingPolicies")]
    public static class Patch_ReadingPolicyDatabase
    {
        public static bool Prefix(ReadingPolicyDatabase __instance)
        {
            foreach (ReadingPolicy policy in DefaultsSettings.DefaultReadingPolicies)
            {
                RimWorld.ReadingPolicy readingPolicy = __instance.MakeNewReadingPolicy();
                readingPolicy.label = policy.label;
                readingPolicy.CopyFrom(policy);
            }

            return false;
        }
    }
}

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;

namespace Defaults.Policies.ApparelPolicies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(OutfitDatabase))]
    [HarmonyPatch("GenerateStartingOutfits")]
    public static class Patch_OutfitDatabase
    {
        public static bool Prefix(OutfitDatabase __instance)
        {
            if (VanillaPolicyStore.loaded)
            {
                foreach (ApparelPolicy policy in Settings.Get<List<ApparelPolicy>>(Settings.POLICIES_APPAREL))
                {
                    ApparelPolicy apparelPolicy = __instance.MakeNewOutfit();
                    apparelPolicy.label = policy.label;
                    apparelPolicy.CopyFrom(policy);
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

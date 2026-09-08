using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;

namespace Defaults.Rewards
{
    [HarmonyPatchCategory("Rewards")]
    [HarmonyPatch(typeof(FactionGenerator))]
    [HarmonyPatch(nameof(FactionGenerator.NewGeneratedFaction))]
    [HarmonyPatch(new[] { typeof(PlanetLayer), typeof(FactionGeneratorParms) })]
    public static class Patch_FactionGenerator_NewGeneratedFaction
    {
        public static void Postfix(Faction __result)
        {
            Dictionary<FactionDef, RewardPreference> rewards = Settings.Get<Dictionary<FactionDef, RewardPreference>>(Settings.REWARDS);
            if (rewards.TryGetValue(__result.def, out RewardPreference preference))
            {
                __result.allowRoyalFavorRewards = preference.allowRoyalFavorRewards;
                __result.allowGoodwillRewards = preference.allowGoodwillRewards;
            }
        }
    }
}

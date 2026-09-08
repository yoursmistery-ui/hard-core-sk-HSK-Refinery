using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.BabyFeeding
{
    [HarmonyPatchCategory("BabyFeeding")]
    [HarmonyPatch(typeof(Pawn_FoodRestrictionTracker))]
    [HarmonyPatch("TrySetupAllowedBabyFoodTypes")]
    [HarmonyPatchMod("Ludeon.RimWorld.Biotech")]
    public static class Patch_Pawn_FoodRestrictionTracker
    {
        public static void Prefix(ref Dictionary<ThingDef, bool> ___allowedBabyFoodTypes)
        {
            if (___allowedBabyFoodTypes == null)
            {
                ___allowedBabyFoodTypes = new Dictionary<ThingDef, bool>();
                foreach (ThingDef def in ITab_Pawn_Feeding.BabyConsumableFoods)
                {
                    ___allowedBabyFoodTypes.Add(def, Settings.Get<BabyFeedingOptions>(Settings.BABY_FEEDING).AllowedConsumables.Contains(def));
                }
            }
        }
    }
}

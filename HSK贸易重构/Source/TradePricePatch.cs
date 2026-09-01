using HarmonyLib;
using RimWorld;
using Verse;

namespace TechGapTradePrice
{
    [HarmonyPatch(typeof(Tradeable), "GetPriceFor")]
    public static class Patch_Tradeable_GetPriceFor
    {
        [HarmonyPostfix]
        public static void Postfix(Tradeable __instance, TradeAction action, ref float __result)
        {
            float multiplier = TechGapTradePriceUtility.GetMultiplier(__instance, action);
            if (multiplier > 1f)
            {
                __result *= multiplier;
            }
        }
    }

    [HarmonyPatch(typeof(Tradeable), "GetPriceTooltip")]
    public static class Patch_Tradeable_GetPriceTooltip
    {
        [HarmonyPostfix]
        public static void Postfix(Tradeable __instance, TradeAction action, ref string __result)
        {
            float multiplier = TechGapTradePriceUtility.GetMultiplier(__instance, action);
            if (multiplier > 1f)
            {
                __result += "\n" + "TechGapTradePrice.TechGapSurcharge".Translate(TechGapTradePriceUtility.GetMultiplierLabel(multiplier));
            }
        }
    }
}

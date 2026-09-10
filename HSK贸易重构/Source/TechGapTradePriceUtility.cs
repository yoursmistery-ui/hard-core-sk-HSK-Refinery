using CombatExtended;
using RimWorld;
using Verse;

namespace TechGapTradePrice
{
    public static class TechGapTradePriceUtility
    {
        public const float MaxMultiplier = 1000f;

        public static bool IsEligible(ThingDef def)
        {
            if (def == null)
            {
                return false;
            }
            if (def.IsWeapon)
            {
                return true;
            }
            if (def.IsApparel)
            {
                return true;
            }
            return def is AmmoDef;
        }

        public static float GetMultiplier(Tradeable tradeable, TradeAction action)
        {
            if (action != TradeAction.PlayerBuys)
            {
                return 1f;
            }
            if (!TradeSession.Active || TradeSession.giftMode)
            {
                return 1f;
            }
            if (tradeable == null)
            {
                return 1f;
            }

            ThingDef def = tradeable.ThingDef;
            if (!IsEligible(def))
            {
                return 1f;
            }

            TechLevel playerTech = TechLevel.Undefined;
            if (Faction.OfPlayer != null && Faction.OfPlayer.def != null)
            {
                playerTech = Faction.OfPlayer.def.techLevel;
            }

            TechLevel itemTech = def.techLevel;
            if (playerTech == TechLevel.Undefined || itemTech == TechLevel.Undefined)
            {
                return 1f;
            }

            int n = (int)itemTech - (int)playerTech;
            if (n <= 0)
            {
                return 1f;
            }
            if (n >= 3)
            {
                return MaxMultiplier;
            }

            float multiplier = 1f;
            for (int i = 0; i < n; i++)
            {
                multiplier *= 10f;
            }
            return multiplier;
        }

        public static string GetMultiplierLabel(float multiplier)
        {
            if (multiplier >= MaxMultiplier)
            {
                return "1000";
            }
            return multiplier.ToString("F0");
        }
    }
}

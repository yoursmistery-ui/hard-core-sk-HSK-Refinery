using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TradeDropSpotFix
{
    [StaticConstructorOnStartup]
    public static class TradeDropSpotFixInit
    {
        static TradeDropSpotFixInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.tradedropspotfix");
                MethodInfo target = AccessTools.Method(typeof(DropCellFinder), "TradeDropSpot");
                if (target == null)
                {
                    Log.Warning("[TradeDropSpotFix] DropCellFinder.TradeDropSpot not found, patch skipped");
                    return;
                }
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(TradeDropSpotFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[TradeDropSpotFix] patched DropCellFinder.TradeDropSpot: trade/caravan drops prefer ship landing beacon");
            }
            catch (Exception e)
            {
                Log.Warning("[TradeDropSpotFix] patch failed: " + e);
            }
        }

        // Return the vanilla drop spot near a ship landing beacon (飞船着陆信标) when one exists and
        // is usable, so comms-called traders / drop-pod caravans dump goods there instead of at the
        // orbital trade beacon. Fall through to the original method otherwise.
        private static bool Prefix(Map map, ref IntVec3 __result)
        {
            try
            {
                ThingDef beaconDef = DefDatabase<ThingDef>.GetNamedSilentFail("ShipLandingBeacon");
                if (beaconDef == null)
                {
                    return true;
                }
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (building.def != beaconDef)
                    {
                        continue;
                    }
                    if (map.roofGrid.Roofed(building.Position))
                    {
                        continue;
                    }
                    if (!AnyAdjacentGoodDropSpot(building.Position, map, false, false))
                    {
                        continue;
                    }
                    IntVec3 result;
                    if (DropCellFinder.TryFindDropSpotNear(
                        building.Position, map, out result, false, false, true))
                    {
                        __result = result;
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[TradeDropSpotFix] prefix failed, falling back to vanilla: " + e.Message);
            }
            return true;
        }

        private static bool AnyAdjacentGoodDropSpot(IntVec3 c, Map map, bool allowFogged, bool canRoofPunch)
        {
            if (!DropCellFinder.IsGoodDropSpot(c + IntVec3.North, map, allowFogged, canRoofPunch) &&
                !DropCellFinder.IsGoodDropSpot(c + IntVec3.East, map, allowFogged, canRoofPunch) &&
                !DropCellFinder.IsGoodDropSpot(c + IntVec3.South, map, allowFogged, canRoofPunch))
            {
                return DropCellFinder.IsGoodDropSpot(c + IntVec3.West, map, allowFogged, canRoofPunch);
            }
            return true;
        }
    }
}
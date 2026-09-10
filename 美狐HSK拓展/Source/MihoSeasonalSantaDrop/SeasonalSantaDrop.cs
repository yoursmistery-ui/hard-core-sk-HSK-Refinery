using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MihoHSK.SeasonalSanta
{
    public sealed class SeasonalSantaDropComponent : GameComponent
    {
        private const float AnnualChance = 0.60f;
        private const int FirstDay = 10;
        private const int LastDay = 15;
        private const int CheckInterval = 2500;

        private bool fired;
        private int lastRollYear = -1;
        private int scheduledTick = -1;

        public SeasonalSantaDropComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref fired, "mihoSeasonalSantaDropFired", false);
            Scribe_Values.Look(ref lastRollYear, "mihoSeasonalSantaDropLastRollYear", -1);
            Scribe_Values.Look(ref scheduledTick, "mihoSeasonalSantaDropScheduledTick", -1);
        }

        public override void GameComponentTick()
        {
            if (fired || GenTicks.TicksGame % CheckInterval != 0)
            {
                return;
            }

            Map map = EligibleMapsInEventWindow().FirstOrDefault();

            if (scheduledTick >= 0)
            {
                if (GenTicks.TicksGame >= scheduledTick)
                {
                    if (map != null && TryFire(map))
                    {
                        fired = true;
                    }

                    scheduledTick = -1;
                }

                return;
            }

            if (map == null)
            {
                return;
            }

            int year = GenLocalDate.Year(map);
            if (lastRollYear == year)
            {
                return;
            }

            lastRollYear = year;
            if (!Rand.Chance(AnnualChance))
            {
                return;
            }

            int humanDay = GenLocalDate.DayOfQuadrum(map) + 1;
            int ticksLeftInWindow = ((LastDay - humanDay) * GenDate.TicksPerDay)
                + (GenDate.TicksPerDay - GenLocalDate.DayTick(map));
            if (ticksLeftInWindow <= CheckInterval)
            {
                fired = TryFire(map);
                return;
            }

            int maxDelay = System.Math.Max(CheckInterval, ticksLeftInWindow - CheckInterval);
            scheduledTick = GenTicks.TicksGame + Rand.RangeInclusive(CheckInterval, maxDelay);
        }

        private static IEnumerable<Map> EligibleMapsInEventWindow()
        {
            return Find.Maps.Where(map =>
                map.IsPlayerHome
                && map.mapPawns.FreeColonistsSpawnedCount > 0
                && IsEventWindow(map));
        }

        private static bool IsEventWindow(Map map)
        {
            int humanDay = GenLocalDate.DayOfQuadrum(map) + 1;
            int quadrumIndex = GenLocalDate.DayOfYear(map) / GenDate.DaysPerQuadrum;
            return quadrumIndex == (int)Quadrum.Decembary
                && humanDay >= FirstDay
                && humanDay <= LastDay;
        }

        private static bool TryFire(Map map)
        {
            IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail("Miho_SeasonalSantaDrop");
            if (incident == null)
            {
                Log.Error("[Miho HSK] Miho_SeasonalSantaDrop IncidentDef was not found.");
                return false;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            return incident.Worker.TryExecute(parms);
        }
    }

    public sealed class IncidentWorker_SeasonalSantaDrop : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms.target as Map;
            return map != null
                && map.IsPlayerHome
                && map.mapPawns.FreeColonistsSpawnedCount > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            PawnKindDef pawnKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Miho_Seasonal_SantaGift");
            if (pawnKind == null)
            {
                Log.Error("[Miho HSK] Miho_Seasonal_SantaGift PawnKindDef was not found.");
                return false;
            }

            Pawn pawn = PawnGenerator.GeneratePawn(pawnKind, Faction.OfPlayer);
            IntVec3 dropCell = DropCellFinder.RandomDropSpot(map, true);
            DropPodUtility.DropThingsNear(
                dropCell,
                map,
                new List<Thing> { pawn },
                openDelay: 110,
                canInstaDropDuringInit: false,
                leaveSlag: true,
                canRoofPunch: true,
                forbid: false,
                allowFogged: false,
                faction: Faction.OfPlayer);

            Find.LetterStack.ReceiveLetter(
                "MihoHSK_SeasonalSantaDrop_LetterLabel".Translate(),
                "MihoHSK_SeasonalSantaDrop_LetterText".Translate(),
                LetterDefOf.PositiveEvent,
                new TargetInfo(dropCell, map));

            return true;
        }
    }
}

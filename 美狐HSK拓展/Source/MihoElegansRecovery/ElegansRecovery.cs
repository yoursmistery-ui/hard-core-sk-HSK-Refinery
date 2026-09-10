using System;
using Verse;

namespace MihoElegansRecovery
{
    public sealed class HediffCompProperties_ElegansRecovery : HediffCompProperties
    {
        public HediffDef targetHediff;
        public int intervalTicks = 6000;
        public float severityGain = 0.1f;

        public HediffCompProperties_ElegansRecovery()
        {
            compClass = typeof(HediffComp_ElegansRecovery);
        }
    }

    public sealed class HediffComp_ElegansRecovery : HediffComp
    {
        private int ticksUntilRecovery;

        private HediffCompProperties_ElegansRecovery Props
        {
            get { return (HediffCompProperties_ElegansRecovery)props; }
        }

        public override void CompPostMake()
        {
            base.CompPostMake();
            ResetTimer();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksUntilRecovery, "ticksUntilElegansRecovery", Props.intervalTicks, false);
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);

            if (Props.targetHediff == null || Props.intervalTicks <= 0 || parent.pawn == null || parent.pawn.Dead)
            {
                return;
            }

            if (ticksUntilRecovery <= 0)
            {
                ResetTimer();
            }

            ticksUntilRecovery -= Math.Max(delta, 1);
            while (ticksUntilRecovery <= 0)
            {
                RecoverElegans(parent.pawn);
                ticksUntilRecovery += Props.intervalTicks;
            }
        }

        private void RecoverElegans(Pawn pawn)
        {
            Hediff target = pawn.health.hediffSet.GetFirstHediffOfDef(Props.targetHediff, false);
            if (target == null)
            {
                target = pawn.health.AddHediff(Props.targetHediff);
                target.Severity = Props.severityGain;
                return;
            }

            target.Severity += Props.severityGain;
        }

        private void ResetTimer()
        {
            ticksUntilRecovery = Math.Max(Props.intervalTicks, 1);
        }
    }
}

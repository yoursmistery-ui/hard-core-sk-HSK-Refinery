using RimWorld;
using Verse;

namespace Defaults.Fishing
{
    public class FishingZoneOptions : IExposable
    {
        public FishRepeatMode DefaultFishRepeatMode = FishRepeatMode.TargetCount;
        public int DefaultFishRepeatCount = 1;
        public int DefaultFishTargetCount = 100;
        public bool DefaultFishPause = false;
        public int DefaultFishUnpauseCount = 50;
        public float DefaultFishTargetPopulation = 0.5f;

        public void ExposeData()
        {
            Scribe_Values.Look(ref DefaultFishRepeatMode, "DefaultFishRepeatMode", FishRepeatMode.TargetCount);
            Scribe_Values.Look(ref DefaultFishRepeatCount, "DefaultFishRepeatCount", 1);
            Scribe_Values.Look(ref DefaultFishTargetCount, "DefaultFishTargetCount", 100);
            Scribe_Values.Look(ref DefaultFishPause, "DefaultFishPause", false);
            Scribe_Values.Look(ref DefaultFishUnpauseCount, "DefaultFishUnpauseCount", 50);
            Scribe_Values.Look(ref DefaultFishTargetPopulation, "DefaultFishTargetPopulation", 0.5f);
        }
    }
}

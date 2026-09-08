using Verse;

namespace Defaults.Rewards
{
    public class RewardPreference : IExposable
    {
        public bool allowRoyalFavorRewards = true;
        public bool allowGoodwillRewards = true;

        public void ExposeData()
        {
            Scribe_Values.Look(ref allowRoyalFavorRewards, "allowRoyalFavorRewards");
            Scribe_Values.Look(ref allowGoodwillRewards, "allowGoodwillRewards");
        }
    }
}

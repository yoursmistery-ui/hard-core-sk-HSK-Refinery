using RimWorld;
using Verse;

namespace Defaults.Policies
{
    public static class StatUtility
    {
        public static bool IsGameStartedInClassicMode()
        {
            return Find.World != null && Find.IdeoManager != null && Find.IdeoManager.classicMode;
        }

        public static float GetScenarioStatFactor(StatDef stat)
        {
            if (Find.Scenario != null)
            {
                return Find.Scenario.GetStatFactor(stat);
            }
            else
            {
                return 1f;
            }
        }
    }
}

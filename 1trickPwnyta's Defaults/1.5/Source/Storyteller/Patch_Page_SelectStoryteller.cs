using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Storyteller
{
    [HarmonyPatch(typeof(Page_SelectStoryteller))]
    [HarmonyPatch(nameof(Page_SelectStoryteller.PreOpen))]
    public static class Patch_Page_SelectStoryteller_PreOpen
    {
        public static void Postfix(ref StorytellerDef ___storyteller, ref DifficultyDef ___difficulty, ref Difficulty ___difficultyValues)
        {
            ___storyteller = DefDatabase<StorytellerDef>.GetNamed(DefaultsSettings.DefaultStoryteller);
            ___difficulty = DefDatabase<DifficultyDef>.GetNamed(DefaultsSettings.DefaultDifficulty);
            ___difficultyValues = DefaultsSettings.DefaultDifficultyValues.GetDifficultyValues();
            if (ModsConfig.AnomalyActive)
            {
                ___difficultyValues.AnomalyPlaystyleDef = DefDatabase<AnomalyPlaystyleDef>.GetNamed(DefaultsSettings.DefaultAnomalyPlaystyle);
                if (Find.Scenario.standardAnomalyPlaystyleOnly)
                {
                    ___difficultyValues.AnomalyPlaystyleDef = AnomalyPlaystyleDefOf.Standard;
                }
            }
            Find.GameInitData.permadeathChosen = true;
            Find.GameInitData.permadeath = DefaultsSettings.DefaultPermadeath;
        }
    }
}

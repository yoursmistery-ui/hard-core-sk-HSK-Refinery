using Defaults.Compatibility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Storyteller
{
    [HarmonyPatchCategory("Storyteller")]
    [HarmonyPatch(typeof(Page_SelectStoryteller))]
    [HarmonyPatch(nameof(Page_SelectStoryteller.PreOpen))]
    public static class Patch_Page_SelectStoryteller_PreOpen
    {
        public static void Postfix(ref StorytellerDef ___storyteller, ref DifficultyDef ___difficulty, ref Difficulty ___difficultyValues)
        {
            ___storyteller = Settings.Get<StorytellerDef>(Settings.STORYTELLER);
            ___difficulty = Settings.Get<DifficultyDef>(Settings.DIFFICULTY);
            ___difficultyValues = Settings.Get<Difficulty>(Settings.DIFFICULTY_VALUES).Copy();
            if (Find.Scenario.standardAnomalyPlaystyleOnly)
            {
                ___difficultyValues.AnomalyPlaystyleDef = AnomalyPlaystyleDefOf.Standard;
            }
            Find.GameInitData.permadeathChosen = true;
            Find.GameInitData.permadeath = Settings.GetValue<bool>(Settings.PERMADEATH);
            ModCompatibilityUtility_NoPause.ApplyNoPauseOptions();
        }
    }
}

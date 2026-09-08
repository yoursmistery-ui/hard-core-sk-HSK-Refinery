using Defaults.Storyteller;
using HarmonyLib;
using Verse;

namespace Defaults.Compatibility
{
    public static class ModCompatibilityUtility_NoPause
    {
        private static readonly bool noPauseActive = AccessTools.TypeByName("NoPauseChallenge.StorytellerUI_DrawStorytellerSelectionInterface_Patch") != null;

        public static void ApplyNoPauseOptions()
        {
            if (noPauseActive)
            {
                NoPauseOptions options = Settings.Get<NoPauseOptions>(Settings.NO_PAUSE_OPTIONS);
                AccessTools.TypeByName("NoPauseChallenge.Main").Field("noPauseEnabled").SetValue(null, options.NoPause);
                AccessTools.TypeByName("NoPauseChallenge.Main").Field("halfSpeedEnabled").SetValue(null, options.HalfSpeed);
            }
        }

        public static void SetNoPauseOptions()
        {
            if (noPauseActive)
            {
                NoPauseOptions options = Settings.Get<NoPauseOptions>(Settings.NO_PAUSE_OPTIONS);
                options.NoPause = (bool)AccessTools.TypeByName("NoPauseChallenge.Main").Field("noPauseEnabled").GetValue(null);
                options.HalfSpeed = (bool)AccessTools.TypeByName("NoPauseChallenge.Main").Field("halfSpeedEnabled").GetValue(null);
            }
        }

        public static void WriteNoPauseOptions(ref NoPauseOptions defaultNoPauseOptions)
        {
            if (noPauseActive)
            {
                Scribe_Deep.Look(ref defaultNoPauseOptions, Settings.NO_PAUSE_OPTIONS);
                if (Scribe.mode == LoadSaveMode.PostLoadInit && defaultNoPauseOptions == null)
                {
                    defaultNoPauseOptions = new NoPauseOptions();
                }
            }
        }

        public static void ResetNoPauseOptions(ref NoPauseOptions defaultNoPauseOptions, bool forced)
        {
            if (noPauseActive)
            {
                if (forced || defaultNoPauseOptions == null)
                {
                    defaultNoPauseOptions = new NoPauseOptions();
                }
            }
        }
    }
}

using Defaults.Defs;
using Defaults.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.Fishing
{
    public class Dialog_FishingZoneSettings : Dialog_SettingsCategory
    {
        private string repeatCountEditBuffer;
        private string targetCountEditBuffer;
        private string unpauseAtCountEditBuffer;

        public Dialog_FishingZoneSettings(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(450f, 450f);

        public override void DoSettings(Rect rect)
        {
            FishingZoneOptions options = Settings.Get<FishingZoneOptions>(Settings.FISHING_ZONE_OPTIONS);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            Listing_Standard topSection = listing.BeginSection(170f);

            if (topSection.ButtonText(options.DefaultFishRepeatMode.GetLabel()))
            {
                List<FloatMenuOption> menuOptions = new List<FloatMenuOption>();
                foreach (FishRepeatMode mode in Enum.GetValues(typeof(FishRepeatMode)))
                {
                    menuOptions.Add(new FloatMenuOption(mode.GetLabel(), () => options.DefaultFishRepeatMode = mode));
                }
                Find.WindowStack.Add(new FloatMenu(menuOptions));
            }
            topSection.Gap(10f);

            switch (options.DefaultFishRepeatMode)
            {
                case FishRepeatMode.RepeatCount:
                    topSection.IntEntry(ref options.DefaultFishRepeatCount, ref repeatCountEditBuffer);
                    break;
                case FishRepeatMode.TargetCount:
                    topSection.IntEntry(ref options.DefaultFishTargetCount, ref targetCountEditBuffer);
                    topSection.Gap(10f);
                    topSection.CheckboxLabeled("PauseWhenSatisfied".Translate(), ref options.DefaultFishPause);
                    if (options.DefaultFishPause)
                    {
                        topSection.Label("UnpauseWhenYouHave".Translate() + ": " + options.DefaultFishUnpauseCount);
                        topSection.IntEntry(ref options.DefaultFishUnpauseCount, ref unpauseAtCountEditBuffer);
                        if (options.DefaultFishUnpauseCount >= options.DefaultFishTargetCount)
                        {
                            options.DefaultFishUnpauseCount = Mathf.Max(0, options.DefaultFishTargetCount - 1);
                            unpauseAtCountEditBuffer = options.DefaultFishUnpauseCount.ToStringCached();
                        }
                    }
                    break;
            }

            listing.EndSection(topSection);
            listing.Gap(10f);

            Listing_Standard bottomSection = listing.BeginSection(50f);
            bottomSection.Label("MinimumPopulation".Translate() + ": " + options.DefaultFishTargetPopulation.ToStringPercent(), tooltip: "MinimumPopulationDesc".Translate());
            options.DefaultFishTargetPopulation = bottomSection.Slider(options.DefaultFishTargetPopulation, 0f, 1f);
            listing.EndSection(bottomSection);

            listing.End();
        }
    }
}

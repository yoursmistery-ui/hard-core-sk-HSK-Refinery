using Defaults.WorldSettings;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.MapSettings
{
    public class Dialog_MapSettings : Window
    {
        public Dialog_MapSettings()
        {
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override Vector2 InitialSize => new Vector2(483f, 500f);

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard { ColumnWidth = 200f };
            listing.Begin(inRect.AtZero());

            Text.Font = GameFont.Medium;
            listing.Label("MapSize".Translate());
            Text.Font = GameFont.Small;
            IEnumerable<int?> enumerable = DefaultSettingWorker_MapSize.MapSizes.AsEnumerable();
            if (Prefs.TestMapSizes)
            {
                enumerable = enumerable.Concat(DefaultSettingWorker_MapSize.TestMapSizes);
            }
            foreach (int? num in enumerable)
            {
                if (num == 200)
                {
                    listing.Label("MapSizeSmall".Translate());
                }
                else if (num == 250)
                {
                    listing.Gap(10f);
                    listing.Label("MapSizeMedium".Translate());
                }
                else if (num == 300)
                {
                    listing.Gap(10f);
                    listing.Label("MapSizeLarge".Translate());
                }
                else if (num == 350)
                {
                    listing.Gap(10f);
                    listing.Label("MapSizeExtreme".Translate());
                }
                string label = "MapSizeDesc".Translate(num.Value, num.Value * num.Value);
                if (listing.RadioButton(label, Settings.GetValue<int>(Settings.MAP_SIZE) == num, 0f, null, null))
                {
                    Settings.SetValue(Settings.MAP_SIZE, num.Value);
                }
            }
            listing.NewColumn();

            Text.Font = GameFont.Medium;
            listing.Label("MapStartSeason".Translate());
            Text.Font = GameFont.Small;
            listing.Label("");
            Season startingSeason = Settings.GetValue<Season>(Settings.STARTING_SEASON);
            if (listing.RadioButton("MapStartSeasonDefault".Translate(), startingSeason == Season.Undefined, 0f, null, null))
            {
                Settings.SetValue(Settings.STARTING_SEASON, Season.Undefined);
            }
            if (listing.RadioButton(Season.Spring.LabelCap(), startingSeason == Season.Spring, 0f, null, null))
            {
                Settings.SetValue(Settings.STARTING_SEASON, Season.Spring);
            }
            if (listing.RadioButton(Season.Summer.LabelCap(), startingSeason == Season.Summer, 0f, null, null))
            {
                Settings.SetValue(Settings.STARTING_SEASON, Season.Summer);
            }
            if (listing.RadioButton(Season.Fall.LabelCap(), startingSeason == Season.Fall, 0f, null, null))
            {
                Settings.SetValue(Settings.STARTING_SEASON, Season.Fall);
            }
            if (listing.RadioButton(Season.Winter.LabelCap(), startingSeason == Season.Winter, 0f, null, null))
            {
                Settings.SetValue(Settings.STARTING_SEASON, Season.Winter);
            }
            listing.End();
        }
    }
}

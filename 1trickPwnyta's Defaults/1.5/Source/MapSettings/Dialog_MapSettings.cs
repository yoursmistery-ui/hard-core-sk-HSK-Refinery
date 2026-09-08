using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.MapSettings
{
    public class Dialog_MapSettings : Window
    {
        private static readonly int[] MapSizes = new int[]
        {
            200,
            225,
            250,
            275,
            300,
            325
        };

        private static readonly int[] TestMapSizes = new int[]
        {
            350,
            400
        };

        public Dialog_MapSettings()
        {
            this.doCloseX = true;
            this.doCloseButton = true;
            this.optionalTitle = "Defaults_MapSettings".Translate();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(483f, 500f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.ColumnWidth = 200f;
            listing.Begin(inRect.AtZero());

            Text.Font = GameFont.Medium;
            listing.Label("MapSize".Translate(), -1f, null);
            Text.Font = GameFont.Small;
            IEnumerable<int> enumerable = MapSizes.AsEnumerable<int>();
            if (Prefs.TestMapSizes)
            {
                enumerable = enumerable.Concat(TestMapSizes);
            }
            foreach (int num in enumerable)
            {
                if (num == 200)
                {
                    listing.Label("MapSizeSmall".Translate(), -1f, null);
                }
                else if (num == 250)
                {
                    listing.Gap(10f);
                    listing.Label("MapSizeMedium".Translate(), -1f, null);
                }
                else if (num == 300)
                {
                    listing.Gap(10f);
                    listing.Label("MapSizeLarge".Translate(), -1f, null);
                }
                else if (num == 350)
                {
                    listing.Gap(10f);
                    listing.Label("MapSizeExtreme".Translate(), -1f, null);
                }
                string label = "MapSizeDesc".Translate(num, num * num);
                if (listing.RadioButton(label, DefaultsSettings.DefaultMapSize == num, 0f, null, null))
                {
                    DefaultsSettings.DefaultMapSize = num;
                }
            }
            listing.NewColumn();

            Text.Font = GameFont.Medium;
            listing.Label("MapStartSeason".Translate(), -1f, null);
            Text.Font = GameFont.Small;
            listing.Label("", -1f, null);
            if (listing.RadioButton("MapStartSeasonDefault".Translate(), DefaultsSettings.DefaultStartingSeason == Season.Undefined, 0f, null, null))
            {
                DefaultsSettings.DefaultStartingSeason = Season.Undefined;
            }
            if (listing.RadioButton(Season.Spring.LabelCap(), DefaultsSettings.DefaultStartingSeason == Season.Spring, 0f, null, null))
            {
                DefaultsSettings.DefaultStartingSeason = Season.Spring;
            }
            if (listing.RadioButton(Season.Summer.LabelCap(), DefaultsSettings.DefaultStartingSeason == Season.Summer, 0f, null, null))
            {
                DefaultsSettings.DefaultStartingSeason = Season.Summer;
            }
            if (listing.RadioButton(Season.Fall.LabelCap(), DefaultsSettings.DefaultStartingSeason == Season.Fall, 0f, null, null))
            {
                DefaultsSettings.DefaultStartingSeason = Season.Fall;
            }
            if (listing.RadioButton(Season.Winter.LabelCap(), DefaultsSettings.DefaultStartingSeason == Season.Winter, 0f, null, null))
            {
                DefaultsSettings.DefaultStartingSeason = Season.Winter;
            }
            listing.End();
        }
    }
}

using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.WorldSettings
{
    public class Dialog_WorldSettings : Window
    {
        private static readonly List<float> PlanetCoverages = new List<float>
        {
            0.3f,
            0.5f,
            1f
        };

        private List<FactionDef> factions;

        public Dialog_WorldSettings()
        {
            this.doCloseX = true;
            this.doCloseButton = true;
            this.optionalTitle = "Defaults_WorldSettings".Translate();
            factions = DefaultsSettings.DefaultFactions.Select(f => DefDatabase<FactionDef>.GetNamedSilentFail(f)).Where(f => f != null).ToList();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return Page.StandardSize;
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            DefaultsSettings.DefaultFactions = factions.Select(f => f.defName).ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect mainRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - Window.CloseButSize.y);
            Rect rect2 = new Rect(mainRect.x, mainRect.y, mainRect.width / 2, mainRect.height);
            Widgets.BeginGroup(rect2);
            Text.Font = GameFont.Small;
            float y = 0f;
            float widgetWidth = rect2.width - 200f;

            Widgets.Label(new Rect(0f, y, 200f, 30f), "PlanetCoverage".Translate());
            Rect rect4 = new Rect(200f, y, widgetWidth, 30f);
            if (Widgets.ButtonText(rect4, DefaultsSettings.DefaultPlanetCoverage.ToStringPercent()))
            {
                Find.WindowStack.Add(new FloatMenu(PlanetCoverages.Select(c => new FloatMenuOption(c.ToStringPercent(), delegate()
                {
                    if (DefaultsSettings.DefaultPlanetCoverage != c)
                    {
                        DefaultsSettings.DefaultPlanetCoverage = c;
                        if (DefaultsSettings.DefaultPlanetCoverage == 1f)
                        {
                            Messages.Message("MessageMaxPlanetCoveragePerformanceWarning".Translate(), MessageTypeDefOf.CautionInput, false);
                        }
                    }
                })).ToList()));
            }
            TooltipHandler.TipRegionByKey(new Rect(0f, y, rect4.xMax, rect4.height), "PlanetCoverageTip");
            y += 40f;

            Widgets.Label(new Rect(0f, y, 200f, 30f), "PlanetRainfall".Translate());
            Rect rect5 = new Rect(200f, y, widgetWidth, 30f);
            DefaultsSettings.DefaultOverallRainfall = (OverallRainfall)Mathf.RoundToInt(Widgets.HorizontalSlider(rect5, (float)DefaultsSettings.DefaultOverallRainfall, 0f, (float)(OverallRainfallUtility.EnumValuesCount - 1), true, "PlanetRainfall_Normal".Translate(), "PlanetRainfall_Low".Translate(), "PlanetRainfall_High".Translate(), 1f));
            y += 40f;

            Widgets.Label(new Rect(0f, y, 200f, 30f), "PlanetTemperature".Translate());
            Rect rect6 = new Rect(200f, y, widgetWidth, 30f);
            DefaultsSettings.DefaultOverallTemperature = (OverallTemperature)Mathf.RoundToInt(Widgets.HorizontalSlider(rect6, (float)DefaultsSettings.DefaultOverallTemperature, 0f, (float)(OverallTemperatureUtility.EnumValuesCount - 1), true, "PlanetTemperature_Normal".Translate(), "PlanetTemperature_Low".Translate(), "PlanetTemperature_High".Translate(), 1f));
            y += 40f;

            Widgets.Label(new Rect(0f, y, 200f, 30f), "PlanetPopulation".Translate());
            Rect rect7 = new Rect(200f, y, widgetWidth, 30f);
            DefaultsSettings.DefaultOverallPopulation = (OverallPopulation)Mathf.RoundToInt(Widgets.HorizontalSlider(rect7, (float)DefaultsSettings.DefaultOverallPopulation, 0f, (float)(OverallPopulationUtility.EnumValuesCount - 1), true, "PlanetPopulation_Normal".Translate(), "PlanetPopulation_Low".Translate(), "PlanetPopulation_High".Translate(), 1f));
            y += 40f;

            if (ModsConfig.BiotechActive)
            {
                Widgets.Label(new Rect(0f, y, 200f, 30f), "PlanetPollution".Translate());
                Rect rect8 = new Rect(200f, y, widgetWidth, 30f);
                DefaultsSettings.DefaultPollution = Widgets.HorizontalSlider(rect8, DefaultsSettings.DefaultPollution, 0f, 1f, true, DefaultsSettings.DefaultPollution.ToStringPercent(), null, null, 0.05f);
            }
            
            Widgets.EndGroup();

            Rect rect9 = new Rect(mainRect.x + mainRect.xMax - mainRect.width / 2 + 24f, mainRect.y, mainRect.width / 2 - 24f, mainRect.height - 30f);
            WorldFactionsUIUtility.DoWindowContents(rect9, factions, true);
            Rect rect10 = new Rect(rect9.xMax - 24f, rect9.y, 24f, 24f);
            UIUtility.DrawCheckButton(rect10, UIUtility.LockIcon, "Defaults_LockSetting".Translate(), ref DefaultsSettings.DefaultFactionsLock);

            if (Widgets.ButtonText(new Rect(mainRect.x + mainRect.width - 150f, mainRect.y + mainRect.height - 30f, 150f, 30f), "ResetFactions".Translate()))
            {
                factions = FactionsUtility.GetDefaultSelectableFactions();
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
        }
    }
}

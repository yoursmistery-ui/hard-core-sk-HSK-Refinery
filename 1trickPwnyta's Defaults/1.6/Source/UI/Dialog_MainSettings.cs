using Defaults.Defs;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.UI
{
    public class Dialog_MainSettings : Dialog_Common
    {
        private static readonly Vector2 settingsCategoryButtonSize = new Vector2(150f, 120f);
        private static readonly float settingsCategoryButtonMargin = 30f;

        private float y;
        private Vector2 scrollPosition;
        private HashSet<DefaultSettingsCategoryDef> initiallyEnabledCategories;

        public Dialog_MainSettings()
        {
            closeOnClickedOutside = false;
        }

        public override Vector2 InitialSize => new Vector2(730f + StandardMargin * 2, 620f + CloseButSize.y + StandardMargin * 2);

        protected override bool DoSearchWidget => true;

        protected override IEnumerable<FloatMenuOption> QuickOptions
        {
            get
            {
                foreach (FloatMenuOption option in DefaultSettingsCategoryDefOf.General.QuickOptions)
                {
                    yield return option;
                }

                yield return new FloatMenuOption("Defaults_ResetAllSettings".Translate(), () =>
                {
                    Find.WindowStack.Add(new Dialog_MessageBox("Defaults_ConfirmResetAllSettings".Translate(), "Confirm".Translate(), DefaultsSettings.ResetAllSettings, "GoBack".Translate(), null, null, true, DefaultsSettings.ResetAllSettings));
                });

                if (DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading.Any(c => !c.Enabled))
                {
                    yield return new FloatMenuOption("Defaults_EnableAll".Translate(), () =>
                    {
                        foreach (DefaultSettingsCategoryDef def in DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading)
                        {
                            def.Enabled = true;
                        }
                    });
                }

                if (DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading.Any(c => c.Enabled && c.canDisable))
                {
                    yield return new FloatMenuOption("Defaults_DisableAll".Translate(), () =>
                    {
                        foreach (DefaultSettingsCategoryDef def in DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading.Where(c => c.canDisable))
                        {
                            def.Enabled = false;
                        }
                    });
                }
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            initiallyEnabledCategories = DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading.Where(d => d.Enabled && d.canDisable).ToHashSet();
        }

        public override void PreClose()
        {
            base.PreClose();
            DefaultsMod.SaveSettings();
            HashSet<DefaultSettingsCategoryDef> currentlyEnabledCategories = DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading.Where(d => d.Enabled && d.canDisable).ToHashSet();
            if (!initiallyEnabledCategories.SetEquals(currentlyEnabledCategories))
            {
                Find.WindowStack.Add(new Dialog_MessageBox("Defaults_RestartRequiredDesc".Translate(), "Defaults_Restart".Translate(), GenCommandLine.Restart, "Defaults_NotNow".Translate(), null, "Defaults_RestartRequired".Translate(), false, GenCommandLine.Restart));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect.TopPartPixels(40f), DefaultsMod.PACKAGE_NAME);

            float width = settingsCategoryButtonSize.x * 4 + settingsCategoryButtonMargin * 3;
            Rect outRect = new Rect(inRect.x + (inRect.width - width) / 2, inRect.y + 40f, inRect.width - (inRect.width - width) / 2, inRect.height - 40f - CloseButSize.y - 10f);
            Rect viewRect = new Rect(0f, 0f, width, y);
            y = 0f;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            IEnumerable<DefaultSettingsCategoryDef> categories = DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading;
            if (CommonSearchWidget.filter.Active)
            {
                categories = categories.Where(c => c.Matches(CommonSearchWidget.filter));
            }

            if (CommonSearchWidget.filter.Active)
            {
                using (new TextBlock(GameFont.Medium))
                {
                    Widgets.Label(new Rect(viewRect.x, y, viewRect.width, Text.LineHeight), ref y, "Defaults_SearchResults".Translate());
                    y += 10f;
                }
            }
            if (categories.Any())
            {
                DoCategories(viewRect, ref y, categories);
            }
            if (CommonSearchWidget.filter.Active)
            {
                IEnumerable<DefaultSettingDef> settings = DefDatabase<DefaultSettingDef>.AllDefsListForReading.Where(s =>
                    s.Matches(CommonSearchWidget.filter)
                );
                if (settings.Any())
                {
                    if (categories.Any())
                    {
                        y += settingsCategoryButtonMargin;
                    }
                    DoSettings(viewRect, ref y, settings);
                }

                if (!categories.Any() && !settings.Any())
                {
                    Widgets.Label(new Rect(viewRect.x, y, viewRect.width, Text.LineHeight), ref y, "Defaults_NoSearchResults".Translate(CommonSearchWidget.filter.Text));
                }
            }

            Widgets.EndScrollView();
        }

        private void DoCategories(Rect rect, ref float y, IEnumerable<DefaultSettingsCategoryDef> categories)
        {
            if (CommonSearchWidget.filter.Active)
            {
                Widgets.Label(new Rect(rect.x, y, rect.width, Text.LineHeight), ref y, "Defaults_DefaultSettingsCategories".Translate());
                Widgets.DrawLineHorizontal(rect.x, y, rect.width);
                y += 10f;
            }

            float x = 0f;
            foreach (DefaultSettingsCategoryDef def in categories)
            {
                Rect buttonRect = new Rect(x, y, settingsCategoryButtonSize.x, settingsCategoryButtonSize.y);
                def.Worker.DoButton(buttonRect);
                x += buttonRect.width + settingsCategoryButtonMargin;
                if (x + settingsCategoryButtonSize.x > rect.xMax)
                {
                    x = 0f;
                    y += settingsCategoryButtonSize.y + settingsCategoryButtonMargin;
                }
            }
            if (x > 0)
            {
                y += settingsCategoryButtonSize.y;
            }
            else
            {
                y -= settingsCategoryButtonMargin;
            }
        }

        private void DoSettings(Rect rect, ref float y, IEnumerable<DefaultSettingDef> settings)
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, Text.LineHeight), ref y, "Defaults_DefaultSettings".Translate());
            Widgets.DrawLineHorizontal(rect.x, y, rect.width);
            y += 10f;
            y += UIUtility.DoSettingsList(new Rect(rect.x, y, rect.width, rect.height), settings);
        }
    }
}

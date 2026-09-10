using Defaults.Defs;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.UI
{
    public abstract class Dialog_SettingsCategory : Dialog_Common
    {
        private readonly DefaultSettingsCategoryDef category;

        public Dialog_SettingsCategory(DefaultSettingsCategoryDef category)
        {
            this.category = category;
        }

        public virtual string Title => category.LabelCap;

        protected virtual bool ShowQuickOptionSettingsInWindow => false;

        protected virtual bool ShowResetOption => true;

        protected virtual string ResetOptionText => "Defaults_ResetTheseSettings".Translate();

        protected virtual void OnResetOptionClicked()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(ResetOptionWarning, "Confirm".Translate(), category.Worker.ResetSettings, "GoBack".Translate(), null, null, true, category.Worker.ResetSettings));
        }

        protected virtual TaggedString ResetOptionWarning => "Defaults_ConfirmResetTheseSettings".Translate(category.label);

        protected virtual bool ShowAdditionalSettingsOption => true;

        protected override IEnumerable<FloatMenuOption> QuickOptions
        {
            get
            {
                if (!ShowQuickOptionSettingsInWindow)
                {
                    foreach (FloatMenuOption option in category.QuickOptions)
                    {
                        yield return option;
                    }
                }

                if (ShowAdditionalSettingsOption)
                {
                    List<DefaultSettingDef> additionalSettings = category.DefaultSettings.Where(s => !s.showInQuickOptions && !s.hideInAdditionalSettings).OrderBy(s => s.uiOrder).ToList();
                    if (additionalSettings.Any())
                    {
                        yield return new FloatMenuOption("Defaults_AdditionalSettings".Translate() + "...", () =>
                        {
                            Find.WindowStack.Add(new Dialog_AdditionalSettings(additionalSettings, category.Worker.AdditionalSettingsDialogWidth));
                        });
                    }
                }

                if (ShowResetOption)
                {
                    yield return new FloatMenuOption(ResetOptionText, OnResetOptionClicked);
                }

                foreach (FloatMenuOption option in category.Worker.FloatMenuOptions)
                {
                    yield return option;
                }
            }
        }

        protected virtual bool DoSettingsWhenDisabled => true;

        public abstract void DoSettings(Rect rect);

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            float y = 0f;

            if (!Title.NullOrEmpty())
            {
                using (new TextBlock(GameFont.Medium))
                {
                    GUI.DrawTexture(inRect.TopPartPixels(Text.LineHeight).LeftPartPixels(Text.LineHeight), category.Worker.Icon);
                    Widgets.Label(inRect.RightPartPixels(inRect.width - Text.LineHeight - Margin), Title);
                    y += Text.LineHeight + Margin - 4f;
                }
            }

            if (!category.Enabled)
            {
                TaggedString message = "Defaults_SettingsCategoryDisabled".Translate(category.LabelCap);
                Rect disabledRect = new Rect(inRect.x, y, inRect.width, Mathf.Max(Text.CalcHeight(message, inRect.width - 100f) + 6f, 30f));
                Widgets.DrawRectFast(disabledRect, Color.red.WithAlpha(0.25f));
                using (new TextBlock(TextAnchor.MiddleLeft))
                {
                    Widgets.Label(disabledRect.LeftPartPixels(disabledRect.width - 100f).ContractedBy(3f), message);
                }
                if (Widgets.ButtonText(disabledRect.RightPartPixels(100f).MiddlePartPixels(100f, 30f).ContractedBy(3f), "Defaults_Enable".Translate()))
                {
                    category.Enabled = true;
                    SoundDefOf.Click.PlayOneShot(null);
                }
                y += disabledRect.height + Margin;
            }

            Rect settingsRect = new Rect(inRect.x, inRect.y + y, inRect.width, inRect.height - CloseButSize.y - 10f - y);
            if (category.Worker.WasEnabledAtStartup || DoSettingsWhenDisabled)
            {
                DoSettings(settingsRect);
            }
            else
            {
                using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(settingsRect, "Defaults_EnableCategoryToViewSettings".Translate(category.label));
            }
        }
    }
}

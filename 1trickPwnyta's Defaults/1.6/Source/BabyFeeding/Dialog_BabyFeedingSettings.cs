using Defaults.Defs;
using Defaults.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.BabyFeeding
{
    public class Dialog_BabyFeedingSettings : Dialog_SettingsCategory
    {
        private static Vector2 scrollPosition;

        private float y;

        public Dialog_BabyFeedingSettings(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(500f, 638f);

        public override void DoSettings(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);

            listing.Label("AutofeedSectionHeader".Translate().CapitalizeFirst());
            listing.GapLine();
            BabyFeedingOptions options = Settings.Get<BabyFeedingOptions>(Settings.BABY_FEEDING);
            DoAutofeedRow(listing, "Defaults_BirtherParent", options.BirtherParent, mode => options.BirtherParent = mode);
            DoAutofeedRow(listing, "Defaults_ParentLactating", options.ParentLactating, mode => options.ParentLactating = mode);
            DoAutofeedRow(listing, "Defaults_ParentNonlactating", options.ParentNonlactating, mode => options.ParentNonlactating = mode);
            DoAutofeedRow(listing, "Defaults_NonparentLactating", options.NonparentLactating, mode => options.NonparentLactating = mode);
            DoAutofeedRow(listing, "Defaults_NonparentNonlactating", options.NonparentNonlactating, mode => options.NonparentNonlactating = mode);

            listing.Gap(24f);

            Rect lockRect = new Rect(rect.width - 24f, listing.CurHeight, 24f, 24f);
            UIUtility.DoLockButton(lockRect, ref options.locked);
            listing.Label("BabyFoodConsumables".Translate().CapitalizeFirst());
            listing.GapLine();
            Rect outRect = listing.GetRect(150f);
            Rect viewRect = new Rect(0f, 0f, rect.width, y);
            y = 0f;
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            foreach (ThingDef def in ITab_Pawn_Feeding.BabyConsumableFoods)
            {
                DoConsumableRow(new Rect(viewRect.x, y, viewRect.width, Text.LineHeight), def);
            }
            Widgets.EndScrollView();

            listing.End();
        }

        private void DoAutofeedRow(Listing_Standard listing, string key, AutofeedMode mode, Action<AutofeedMode> callback)
        {
            if (listing.ButtonTextLabeled(key.Translate(), mode.Translate().CapitalizeFirst()))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (AutofeedMode option in Enum.GetValues(typeof(AutofeedMode)))
                {
                    list.Add(new FloatMenuOption(option.Translate().CapitalizeFirst(), () =>
                    {
                        callback(option);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
        }

        private void DoConsumableRow(Rect rect, ThingDef def)
        {
            Widgets.DefIcon(new Rect(rect.x, rect.y, rect.height, rect.height), def);
            Widgets.Label(new Rect(rect.x + rect.height + 12f, rect.y, rect.width / 2f, rect.height), def.LabelCap);
            Widgets.InfoCardButton(rect.xMax - 24f, rect.y, def);
            BabyFeedingOptions options = Settings.Get<BabyFeedingOptions>(Settings.BABY_FEEDING);
            bool allowed = options.AllowedConsumables.Contains(def);
            Widgets.Checkbox(rect.xMax - 24f - 12f - 24f, rect.y, ref allowed);
            if (allowed)
            {
                options.AllowedConsumables.Add(def);
            }
            else
            {
                options.AllowedConsumables.Remove(def);
            }
            y += rect.height;
        }
    }
}

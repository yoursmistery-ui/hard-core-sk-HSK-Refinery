using Defaults.Defs;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.UI
{
    public abstract class Dialog_SettingsCategory_List : Dialog_SettingsCategory
    {
        private static Vector2 scrollPosition;

        private float settingsHeight;
        private float totalHeight;
        private readonly List<DefaultSettingDef> settings;

        public Dialog_SettingsCategory_List(DefaultSettingsCategoryDef category) : base(category)
        {
            settings = category.DefaultSettings.OrderBy(d => d.uiOrder).ToList();
        }

        protected override bool ShowAdditionalSettingsOption => false;

        public virtual float DoPostSettings(Rect rect) => 0f;

        public override void DoSettings(Rect rect)
        {
            Rect viewRect = new Rect(0f, 0f, rect.width - 20f, totalHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            settingsHeight = UIUtility.DoSettingsList(viewRect, ShowQuickOptionSettingsInWindow ? settings : settings.Where(s => !s.showInQuickOptions)) + Margin;
            float postSettingsHeight = DoPostSettings(new Rect(viewRect.x, settingsHeight, viewRect.width, totalHeight));
            if (postSettingsHeight > 0f)
            {
                Widgets.DrawLineHorizontal(viewRect.x, settingsHeight - Margin / 2, viewRect.width);
            }

            totalHeight = settingsHeight + postSettingsHeight;
            Widgets.EndScrollView();
        }
    }
}

using Defaults.Defs;
using Defaults.UI;
using UnityEngine;

namespace Defaults.General
{
    public class Dialog_GeneralSettings : Dialog_SettingsCategory_List
    {
        public Dialog_GeneralSettings(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(600f, 350f);

        protected override bool ShowResetOption => false;

        protected override bool ShowQuickOptionSettingsInWindow => true;
    }
}

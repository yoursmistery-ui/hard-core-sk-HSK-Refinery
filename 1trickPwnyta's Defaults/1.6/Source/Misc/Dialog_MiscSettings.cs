using Defaults.Defs;
using Defaults.UI;
using UnityEngine;

namespace Defaults.Misc
{
    public class Dialog_MiscSettings : Dialog_SettingsCategory_List
    {
        public Dialog_MiscSettings(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(600f, 550f);

        protected override bool ShowResetOption => false;
    }
}

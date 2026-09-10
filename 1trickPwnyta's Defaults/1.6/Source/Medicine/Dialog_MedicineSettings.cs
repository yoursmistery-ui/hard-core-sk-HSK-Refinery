using Defaults.Defs;
using Defaults.UI;
using UnityEngine;
using Verse;

namespace Defaults.Medicine
{
    [StaticConstructorOnStartup]
    public class Dialog_MedicineSettings : Dialog_SettingsCategory_List
    {
        public Dialog_MedicineSettings() : base(DefaultSettingsCategoryDefOf.Medicine)
        {
        }

        public override Vector2 InitialSize => new Vector2(475f, 580f);
    }
}

using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.Misc
{
    public class DefaultSettingsCategoryWorker_Misc : DefaultSettingsCategoryWorker
    {
        public DefaultSettingsCategoryWorker_Misc(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_MiscSettings(def));
        }
    }
}

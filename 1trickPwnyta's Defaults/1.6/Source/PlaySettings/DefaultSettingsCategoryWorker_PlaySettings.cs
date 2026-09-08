using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.PlaySettings
{
    public class DefaultSettingsCategoryWorker_PlaySettings : DefaultSettingsCategoryWorker
    {
        public DefaultSettingsCategoryWorker_PlaySettings(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_PlaySettings());
        }
    }
}

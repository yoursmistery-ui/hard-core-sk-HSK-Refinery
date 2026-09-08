using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.General
{
    public class DefaultSettingsCategoryWorker_General : DefaultSettingsCategoryWorker
    {
        public DefaultSettingsCategoryWorker_General(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_GeneralSettings(def));
        }
    }
}

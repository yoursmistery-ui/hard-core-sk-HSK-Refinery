using Defaults.Defs;

namespace Defaults.Misc.MechWorkModes
{
    public class DefaultSettingWorker_WorkModeSecond : DefaultSettingWorker_MechWorkMode
    {
        public DefaultSettingWorker_WorkModeSecond(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.MECH_WORK_MODE_SECOND;
    }
}

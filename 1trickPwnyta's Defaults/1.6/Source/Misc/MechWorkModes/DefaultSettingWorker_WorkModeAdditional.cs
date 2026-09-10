using Defaults.Defs;

namespace Defaults.Misc.MechWorkModes
{
    public class DefaultSettingWorker_WorkModeAdditional : DefaultSettingWorker_MechWorkMode
    {
        public DefaultSettingWorker_WorkModeAdditional(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.MECH_WORK_MODE_ADDITIONAL;
    }
}

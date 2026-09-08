using Defaults.Defs;

namespace Defaults.Misc.TargetTemperature
{
    public class DefaultSettingWorker_TargetTempHeater : DefaultSettingWorker_TargetTemp
    {
        public DefaultSettingWorker_TargetTempHeater(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.TARGET_TEMP_HEATER;
    }
}

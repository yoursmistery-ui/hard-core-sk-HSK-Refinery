using Defaults.Defs;

namespace Defaults.Misc.TargetTemperature
{
    public class DefaultSettingWorker_TargetTempCooler : DefaultSettingWorker_TargetTemp
    {
        public DefaultSettingWorker_TargetTempCooler(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.TARGET_TEMP_COOLER;
    }
}

using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.General
{
    public class DefaultSettingWorker_SettingsBackupOptions : DefaultSettingWorker_Options<SettingsBackupOptions>
    {
        public DefaultSettingWorker_SettingsBackupOptions(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.SETTINGS_BACKUP_OPTIONS;

        protected override SettingsBackupOptions Default => new SettingsBackupOptions();

        protected override void Configure()
        {
            Find.WindowStack.Add(new Dialog_SettingsBackupOptions());
        }
    }
}

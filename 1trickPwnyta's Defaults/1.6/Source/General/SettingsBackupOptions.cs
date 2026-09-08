using System.IO;
using Verse;

namespace Defaults.General
{
    public class SettingsBackupOptions : IExposable
    {
        private static readonly string defaultBackupPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "DefaultSettingsBackup");

        public string BackupPath = defaultBackupPath;
        public SettingsBackupFrequency Frequency = SettingsBackupFrequency.Startup;
        public SettingsBackupDuration Duration = SettingsBackupDuration.Count;
        public int DurationCount = 5;
        public int DurationDays = 7;

        public void ExposeData()
        {
            Scribe_Values.Look(ref BackupPath, "BackupPath", defaultBackupPath);
            Scribe_Values.Look(ref Frequency, "Frequency", SettingsBackupFrequency.Startup);
            Scribe_Values.Look(ref Duration, "Duration", SettingsBackupDuration.Count);
            Scribe_Values.Look(ref DurationCount, "DurationCount", 5);
            Scribe_Values.Look(ref DurationDays, "DurationDays", 7);
        }
    }

    public enum SettingsBackupFrequency
    {
        Never,
        Startup,
        OnChange
    }

    public enum SettingsBackupDuration
    {
        Forever,
        Count,
        TimeInterval
    }
}

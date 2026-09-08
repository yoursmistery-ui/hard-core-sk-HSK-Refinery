using Verse;
using HarmonyLib;
using System.Linq;
using Defaults.Defs;
using UnityEngine;
using RimWorld;
using Defaults.UI;
using Defaults.General;

namespace Defaults
{
    [StaticConstructorOnStartup]
    public static class DefaultsModInitializer
    {
        static DefaultsModInitializer()
        {
            DefaultsSettings.PreLoadSettings();
            DefaultsMod.Settings = DefaultsMod.Mod.GetSettings<DefaultsSettings>();
            DefaultsSettings.CheckForNewContent();

            SettingsBackupOptions backupOptions = Settings.Get<SettingsBackupOptions>(Settings.SETTINGS_BACKUP_OPTIONS);
            if (backupOptions.Frequency == SettingsBackupFrequency.Startup)
            {
                SettingsBackupUtility.BackUpNow();
            }
            if (backupOptions.Frequency == SettingsBackupFrequency.Never)
            {
                // If backups are never, at least do just a purge on game start
                SettingsBackupUtility.PurgeBackups();
            }

            Harmony harmony = new Harmony(DefaultsMod.PACKAGE_ID);
            harmony.PatchAllUncategorized();
            foreach (DefaultSettingsCategoryDef def in DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading.Where(d => d.Enabled && d.canDisable))
            {
                harmony.PatchCategory(def.defName);
            }

            Log.Info("Ready.");
        }
    }

    public class DefaultsMod : Mod
    {
        public const string PACKAGE_ID = "defaults.1trickPwnyta";
        public const string PACKAGE_NAME = "1trickPwnyta's Defaults";

        public static DefaultsMod Mod;
        public static DefaultsSettings Settings;

        public static void SaveSettings(bool triggerOnChangeBackup = true)
        {
            Settings.Write();
            SettingsBackupUtility.FlushPinnedFiles();
            if (triggerOnChangeBackup)
            {
                SettingsBackupOptions backupOptions = Defaults.Settings.Get<SettingsBackupOptions>(Defaults.Settings.SETTINGS_BACKUP_OPTIONS);
                if (backupOptions.Frequency == SettingsBackupFrequency.OnChange)
                {
                    SettingsBackupUtility.BackUpNow();
                }
            }
        }

        public DefaultsMod(ModContentPack content) : base(content)
        {
            Mod = this;
        }

        public override string SettingsCategory() => PACKAGE_NAME;

        /* 
         * Not typically called at all because we patch it out, but this adds compatibility with mods 
         * that change the Mod Options UI such as Mod Options Sort
         */
        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Widgets.ButtonText(inRect.MiddlePartPixels(300f, 45f), "Defaults_OpenMainSettings".Translate()))
            {
                Find.WindowStack.Add(new Dialog_MainSettings());
            }
        }
    }
}

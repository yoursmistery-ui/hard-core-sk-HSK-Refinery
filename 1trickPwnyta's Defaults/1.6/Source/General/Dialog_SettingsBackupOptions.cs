using Defaults.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.General
{
    public class Dialog_SettingsBackupOptions : Dialog_Common
    {
        private string durationCountEditBuffer;
        private string durationDaysEditBuffer;

        public override Vector2 InitialSize => new Vector2(750f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            SettingsBackupOptions options = Settings.Get<SettingsBackupOptions>(Settings.SETTINGS_BACKUP_OPTIONS);
            float y = 0f;

            using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect, "Defaults_SettingsBackupOptionsTitle".Translate());
            y += 45f;

            string desc = "Defaults_SettingsBackupOptionsDesc".Translate();
            float descHeight = Text.CalcHeight(desc, inRect.width);
            Widgets.Label(new Rect(inRect.x, inRect.y + y, inRect.width, descHeight), desc);
            y += descHeight + 5f;

            // Backup path
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(new Rect(inRect.x, inRect.y + y, inRect.width, 30f), "Defaults_SettingsBackupPath".Translate());
            string backupPathSafe = options.BackupPath;
            options.BackupPath = Widgets.TextField(new Rect(inRect.x + 150f, inRect.y + y, inRect.width - 260f, 30f), options.BackupPath);
            try
            {
                Path.GetFullPath(Path.Combine(options.BackupPath, SettingsBackupUtility.BackupName + SettingsBackupUtility.settingsExt));
            }
            catch
            {
                options.BackupPath = backupPathSafe;
            }
            if (Widgets.ButtonText(new Rect(inRect.xMax - 100f, inRect.y + y, 100f, 30f), "OpenFolder".Translate()))
            {
                SettingsBackupUtility.OpenBackupFolder();
            }
            y += 35f;

            // Backup schedule rect
            Rect scheduleRect = new Rect(inRect.x, inRect.y + y + Text.LineHeight / 2, inRect.width, 110f);
            Widgets.DrawBoxSolidWithOutline(scheduleRect, Widgets.WindowBGFillColor, Color.white);
            string scheduleLabel = "Defaults_SettingsBackupSchedule".Translate();
            Vector2 size = Text.CalcSize(scheduleLabel);
            Rect scheduleLabelRect = new Rect(scheduleRect.x + 5f, scheduleRect.y - Text.LineHeight / 2, size.x + 10f, size.y);
            Widgets.DrawRectFast(scheduleLabelRect, Widgets.WindowBGFillColor);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(scheduleLabelRect.MiddlePartPixels(size.x, size.y), scheduleLabel);
            y += Text.LineHeight / 2 + 5f;

            // Backup frequency
            Rect frequencyRect = new Rect(scheduleRect.x + 10f, inRect.y + y, scheduleRect.width - 20f, 30f);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(frequencyRect.LeftHalf(), "Defaults_SettingsBackupFrequency".Translate());
            if (Widgets.ButtonText(frequencyRect.RightHalf(), options.Frequency.GetLabel()))
            {
                List<FloatMenuOption> menuOptions = new List<FloatMenuOption>();
                foreach (SettingsBackupFrequency frequency in Enum.GetValues(typeof(SettingsBackupFrequency)))
                {
                    menuOptions.Add(new FloatMenuOption(frequency.GetLabel(), () => options.Frequency = frequency));
                }
                Find.WindowStack.Add(new FloatMenu(menuOptions));
            }
            y += 35f;

            // Backup duration
            Rect durationRect = new Rect(scheduleRect.x + 10f, inRect.y + y, scheduleRect.width - 20f, 30f);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(durationRect.LeftHalf(), "Defaults_SettingsBackupDuration".Translate());
            if (Widgets.ButtonText(durationRect.RightHalf(), options.Duration.GetLabel()))
            {
                List<FloatMenuOption> menuOptions = new List<FloatMenuOption>();
                foreach (SettingsBackupDuration duration in Enum.GetValues(typeof(SettingsBackupDuration)))
                {
                    menuOptions.Add(new FloatMenuOption(duration.GetLabel(), () => options.Duration = duration));
                }
                Find.WindowStack.Add(new FloatMenu(menuOptions));
            }
            y += 35f;
            if (options.Duration != SettingsBackupDuration.Forever)
            {
                Rect durationOptionRect = new Rect(scheduleRect.x + 10f, inRect.y + y, scheduleRect.width - 20f, 30f).RightHalf();
                using (new TextBlock(TextAnchor.MiddleRight)) Widgets.Label(durationOptionRect.LeftHalf(), "Defaults_X".Translate());
                if (options.Duration == SettingsBackupDuration.Count)
                {
                    UIUtility.IntEntry(durationOptionRect.RightHalf(), ref options.DurationCount, ref durationCountEditBuffer);
                }
                if (options.Duration == SettingsBackupDuration.TimeInterval)
                {
                    UIUtility.IntEntry(durationOptionRect.RightHalf(), ref options.DurationDays, ref durationDaysEditBuffer);
                }
            }
            y += 40f;

            // Back up now
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.y + y, inRect.width / 2 - 5f, 30f), "Defaults_BackupNow".Translate()))
            {
                Find.WindowStack.Add(new Dialog_TextField_String(filename =>
                {
                    string fullFilename = filename + SettingsBackupUtility.settingsExt;
                    if (File.Exists(Path.GetFullPath(Path.Combine(options.BackupPath, fullFilename))))
                    {
                        Find.WindowStack.Add(new Dialog_Confirm("Defaults_ConfirmOverwriteBackup".Translate(fullFilename), confirmBackup));
                    }
                    else
                    {
                        confirmBackup();
                    }

                    void confirmBackup()
                    {
                        DefaultsMod.SaveSettings(false);
                        if (SettingsBackupUtility.BackUpNow(filename))
                        {
                            Messages.Message("Defaults_SettingsBackupComplete".Translate(fullFilename), MessageTypeDefOf.PositiveEvent, false);
                        }
                    }
                }, title: "Defaults_BackupNow".Translate(), prompt: "Defaults_EnterBackupName".Translate(), defaultValue: SettingsBackupUtility.BackupName, validator: filename =>
                {
                    if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        return "Defaults_InvalidFilename".Translate();
                    }
                    try
                    {
                        Path.GetFullPath(Path.Combine(options.BackupPath, filename + SettingsBackupUtility.settingsExt));
                    }
                    catch (PathTooLongException)
                    {
                        return "Defaults_PathTooLong".Translate();
                    }
                    catch
                    {
                        return "Defaults_InvalidFilename".Translate();
                    }
                    return AcceptanceReport.WasAccepted;
                }));
            }

            // Restore from backup
            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width / 2 + 5f, inRect.y + y, inRect.width / 2 - 5f, 30f), "Defaults_RestoreFromBackup".Translate()))
            {
                if (SettingsBackupUtility.GetBackupFiles().Any())
                {
                    try
                    {
                        Find.WindowStack.Add(new Dialog_SelectOne<FileInfo>(
                            "Defaults_RestoreFromBackup".Translate(),
                            "Defaults_SelectBackupFile".Translate(),
                            () => SettingsBackupUtility.GetBackupFiles().OrderByDescending(f => f.IsPinned()).ThenByDescending(f => f.LastWriteTime),
                            file =>
                            {
                                SettingsBackupUtility.RestoreBackup(file.Name);
                            },
                            equals: (a, b) => a.Name == b.Name,
                            destructive: true,
                            toString: f => f.Name + "  " + f.LastWriteTime.ToString(CultureInfo.InstalledUICulture).Colorize(ColoredText.DateTimeColor),
                            doSideOptions: (rect, file) =>
                            {
                                Rect sideOptionsRect = rect.RightPartPixels(60f);
                                Rect pinRect = sideOptionsRect.LeftHalf().ContractedBy(3f);
                                bool pinned = file.IsPinned();
                                if (Widgets.ButtonImage(pinRect, pinned ? UIUtility.PinTex : UIUtility.PinOutlineTex, pinned ? Color.white : Widgets.InactiveColor, tooltip: "Defaults_PinBackup".Translate()))
                                {
                                    if (pinned)
                                    {
                                        file.Unpin();
                                        SoundDefOf.Tick_Low.PlayOneShot(null);
                                    }
                                    else
                                    {
                                        file.Pin();
                                        SoundDefOf.Tick_High.PlayOneShot(null);
                                    }
                                }
                                Rect deleteRect = sideOptionsRect.RightHalf().ContractedBy(3f);
                                if (Widgets.ButtonImage(deleteRect, TexButton.Delete, tooltip: "Defaults_DeleteBackup".Translate()))
                                {
                                    try
                                    {
                                        file.Delete();
                                        SoundDefOf.Click.PlayOneShot(null);
                                    }
                                    catch
                                    {
                                        Messages.Message("Defaults_DeleteBackupFailed".Translate(file.Name), MessageTypeDefOf.RejectInput, false);
                                    }
                                }
                                return sideOptionsRect.width;
                            }));
                    }
                    catch (Exception e)
                    {
                        Messages.Message("Defaults_SettingsRestoreFailed".Translate(e.ToString()), MessageTypeDefOf.RejectInput, false);
                    }
                }
                else
                {
                    Messages.Message("Defaults_NoBackupsToRestore".Translate(options.BackupPath), MessageTypeDefOf.RejectInput, false);
                }
            }
            y += 35f;
        }
    }
}

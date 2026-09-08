using RimWorld;
using System.Collections.Generic;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.StockpileZones
{
    [StaticConstructorOnStartup]
    public class Dialog_StockpileZones : Window
    {
        public static Texture2D DefaultStockpileIcon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile", true);
        public static Texture2D DumpingStockpileIcon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_DumpingStockpile", true);
        public static Texture2D CorpseStockpileIcon = ContentFinder<Texture2D>.Get(ModsConfig.AnomalyActive ? "UI/Icons/CorpseStockpileZone" : "UI/Designators/ZoneCreate_DumpingStockpile", true);

        private static Vector2 scrollPos;
        private static ZoneType selectedZoneType;
        private ThingFilterUI.UIState state = new ThingFilterUI.UIState();

        public Dialog_StockpileZones()
        {
            this.doCloseX = true;
            this.doCloseButton = true;
            this.optionalTitle = "Defaults_StockpileZones".Translate();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(860f, 600f);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            selectedZoneType = null;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float buttonWidth = 80f;
            float buttonHeight = 30f;
            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width - buttonWidth, 0f, buttonWidth, buttonHeight), "Defaults_AddNew".Translate()))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (object obj in Enum.GetValues(typeof(StorageSettingsPreset)))
                {
                    StorageSettingsPreset preset = (StorageSettingsPreset)obj;
                    list.Add(new FloatMenuOption(preset.PresetName().CapitalizeFirst(), delegate ()
                    {
                        ZoneType newZoneType = new ZoneType(preset.PresetName().CapitalizeFirst() + " " + (DefaultsSettings.DefaultStockpileZones.Count + 1), preset);
                        DefaultsSettings.DefaultStockpileZones.Add(newZoneType);
                        selectedZoneType = newZoneType;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }

            float rowWidth = inRect.width - 300f - 16f;
            float rowHeight = 64f;
            Widgets.BeginScrollView(new Rect(inRect.x, inRect.y + buttonHeight, inRect.width - 300f, inRect.height - buttonHeight - Window.CloseButSize.y), ref scrollPos, new Rect(0f, 0f, rowWidth, rowHeight * DefaultsSettings.DefaultStockpileZones.Count));
            float y = 0f;

            Text.Anchor = TextAnchor.MiddleLeft;
            for (int i = 0; i < DefaultsSettings.DefaultStockpileZones.Count; i++)
            {
                ZoneType type = DefaultsSettings.DefaultStockpileZones[i];

                if (i < DefaultsSettings.DefaultStockpileZones.Count - 1)
                {
                    if (Widgets.ButtonImage(new Rect(3f, y + (rowHeight - 18f) / 2, 18f, 18f), TexButton.ReorderDown, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                    {
                        ZoneType next = DefaultsSettings.DefaultStockpileZones[i + 1];
                        DefaultsSettings.DefaultStockpileZones[i + 1] = type;
                        DefaultsSettings.DefaultStockpileZones[i] = next;
                        SoundDefOf.Click.PlayOneShotOnCamera(null);
                    }
                }
                if (i > 0)
                {
                    if (Widgets.ButtonImage(new Rect(24f + 3f, y + (rowHeight - 18f) / 2, 18f, 18f), TexButton.ReorderUp, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                    {
                        ZoneType prev = DefaultsSettings.DefaultStockpileZones[i - 1];
                        DefaultsSettings.DefaultStockpileZones[i - 1] = type;
                        DefaultsSettings.DefaultStockpileZones[i] = prev;
                        SoundDefOf.Click.PlayOneShotOnCamera(null);
                    }
                }

                GUI.DrawTexture(new Rect(48f, y, rowHeight, rowHeight), type.Icon);

                Widgets.Label(new Rect(48f + rowHeight + 8f, y, rowWidth - rowHeight - 8f - 24f - 24f - 24f - 8f, rowHeight), type.Name);

                if (Widgets.ButtonImage(new Rect(rowWidth - 24f - 24f - 24f - 8f, y + (rowHeight - 24f) / 2, 24f, 24f), TexButton.Copy, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                {
                    DefaultsSettings.DefaultStockpileZones.Add(new ZoneType(type.Preset.PresetName().CapitalizeFirst() + " " + (DefaultsSettings.DefaultStockpileZones.Count + 1), type));
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }

                if (type.DesignatorType == typeof(Designator_ZoneAddStockpile_Custom))
                {
                    if (Widgets.ButtonImage(new Rect(rowWidth - 24f - 24f - 8f, y + (rowHeight - 24f) / 2, 24f, 24f), TexButton.Rename, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                    {
                        Find.WindowStack.Add(new Dialog_RenameZoneType(type));
                    }
                }

                if (type.DesignatorType == typeof(Designator_ZoneAddStockpile_Custom))
                {
                    if (Widgets.ButtonImage(new Rect(rowWidth - 24f - 8f, y + (rowHeight - 24f) / 2, 24f, 24f), TexButton.Delete, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                    {
                        DefaultsSettings.DefaultStockpileZones.Remove(type);
                        i--;
                        SoundDefOf.Click.PlayOneShotOnCamera(null);
                    }
                }

                Rect rowRect = new Rect(0f, y, rowWidth, rowHeight);
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedZoneType = type;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }
                Widgets.DrawHighlightIfMouseover(rowRect);
                if (type == selectedZoneType)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }

                y += rowHeight;
            }
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.EndScrollView();

            if (DefaultsSettings.DefaultStockpileZones.Contains(selectedZoneType))
            {
                if (Widgets.ButtonText(new Rect(inRect.width - 300f, inRect.y + buttonHeight, 160f, buttonHeight), "Priority".Translate() + ": " + selectedZoneType.Priority.Label().CapitalizeFirst()))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (object obj in Enum.GetValues(typeof(StoragePriority)))
                    {
                        StoragePriority storagePriority = (StoragePriority)obj;
                        if (storagePriority != StoragePriority.Unstored)
                        {
                            StoragePriority localPr = storagePriority;
                            list.Add(new FloatMenuOption(localPr.Label().CapitalizeFirst(), delegate ()
                            {
                                selectedZoneType.Priority = localPr;
                            }));
                        }
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                }

                Rect lockRect = new Rect(inRect.width - 24f, inRect.y + buttonHeight, 24f, 24f);
                UIUtility.DrawCheckButton(lockRect, UIUtility.LockIcon, "Defaults_LockSetting".Translate(), ref selectedZoneType.locked);

                Rect filterRect = new Rect(inRect.width  - 300f, inRect.y + buttonHeight * 2, 300f, inRect.height - buttonHeight * 2 - Window.CloseButSize.y);
                ThingFilterUI.DoThingFilterConfigWindow(filterRect, state, selectedZoneType.filter, StorageSettings.EverStorableFixedSettings().filter, 8);
            }
        }
    }
}

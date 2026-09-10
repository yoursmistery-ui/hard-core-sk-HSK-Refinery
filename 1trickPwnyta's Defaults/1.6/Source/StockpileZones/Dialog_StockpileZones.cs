using Defaults.Defs;
using Defaults.StockpileZones.Buildings;
using Defaults.UI;
using Defaults.Workers;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.StockpileZones
{
    [StaticConstructorOnStartup]
    public class Dialog_StockpileZones : Dialog_SettingsCategory
    {
        public static Texture2D DefaultStockpileIcon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Stockpile", true);
        public static Texture2D DumpingStockpileIcon = ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_DumpingStockpile", true);
        public static Texture2D CorpseStockpileIcon = ContentFinder<Texture2D>.Get(ModsConfig.AnomalyActive ? "UI/Icons/CorpseStockpileZone" : "UI/Designators/ZoneCreate_DumpingStockpile", true);

        private static float y;
        private static Vector2 scrollPos;
        private static ZoneType selectedZoneType;
        private readonly ThingFilterUI.UIState state = new UIState_Ext();
        private static ZoneType clipboard;
        private static readonly List<StorageTab> tabs = new List<StorageTab>()
        {
            new StorageTab_Stockpile(() => currentTab = tabs[0], () => currentTab == tabs[0]),
            new StorageTab_Building(() => currentTab = tabs[1], () => currentTab == tabs[1])
        };
        private static StorageTab currentTab = tabs[0];
        private static readonly float buttonWidth = 80f;
        private static readonly float controlHeight = 30f;

        public Dialog_StockpileZones(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(860f, 640f);

        protected override TaggedString ResetOptionWarning => currentTab.ResetWarning;

        protected override void OnResetOptionClicked()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(ResetOptionWarning, "Confirm".Translate(), currentTab.ResetSettings, "GoBack".Translate(), null, null, true, currentTab.ResetSettings));
        }

        protected override IList ReorderableItems => Settings.Get<List<ZoneType>>(Settings.STOCKPILE_ZONES);

        public override void DoSettings(Rect rect)
        {
            currentTab.DoControls(new Rect(rect.x, rect.y, rect.width, controlHeight));

            float rowWidth = rect.width - 300f - 16f;
            float rowHeight = 64f;
            Rect tabsRect = new Rect(rect.x, rect.y + controlHeight + 10f, rowWidth, 1f);
            TabDrawer.DrawTabs(tabsRect, tabs);

            Rect outRect = new Rect(rect.x, rect.y + controlHeight + 10f, rect.width - 300f, rect.height - controlHeight - 10f);
            Rect viewRect = new Rect(0f, 0f, rowWidth, y);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            y = 0f;
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                if (currentTab.DoTab(rowWidth, rowHeight, ReorderableGroup))
                {
                    reorderableRect = outRect;
                }
            }
            Widgets.EndScrollView();

            if (selectedZoneType != null)
            {
                bool hidePriority = selectedZoneType is ZoneType_Building b && (!typeof(IStoreSettingsParent).IsAssignableFrom(b.buildingDef.thingClass) || typeof(IThingHolderWithDrawnPawn).IsAssignableFrom(b.buildingDef.thingClass));
                if (!hidePriority && Widgets.ButtonText(new Rect(rect.width - 300f, rect.y + controlHeight + 10f, 160f, controlHeight), "Priority".Translate() + ": " + selectedZoneType.priority.Label().CapitalizeFirst()))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (object obj in Enum.GetValues(typeof(StoragePriority)))
                    {
                        StoragePriority storagePriority = (StoragePriority)obj;
                        if (storagePriority != StoragePriority.Unstored)
                        {
                            StoragePriority localPr = storagePriority;
                            list.Add(new FloatMenuOption(localPr.Label().CapitalizeFirst(), () =>
                            {
                                selectedZoneType.priority = localPr;
                            }));
                        }
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                }

                Rect lockRect = new Rect(rect.width - 24f, rect.y + controlHeight + 10f, 24f, 24f);
                UIUtility.DoLockButton(lockRect, ref selectedZoneType.locked);

                Rect filterRect = new Rect(rect.width - 300f, rect.y + controlHeight * 2 + 10f, 300f, rect.height - controlHeight * 2 - 10f);
                StorageSettings parentStorageSettings = StorageSettings.EverStorableFixedSettings();
                if (selectedZoneType is ZoneType_Building zoneTypeBuilding)
                {
                    parentStorageSettings = zoneTypeBuilding.buildingDef.building.fixedStorageSettings;
                }
                ThingFilterUI.DoThingFilterConfigWindow(filterRect, state, selectedZoneType.filter, parentStorageSettings.filter, TreeOpenMasks.Storage);
            }
        }

        private static void DoZoneTypeButton(ZoneType zone, float x, float rowWidth, float rowHeight)
        {
            Rect iconRect = new Rect(x, y, rowHeight, rowHeight);
            using (new TextBlock(zone.IconColor)) Widgets.DrawTextureFitted(iconRect, zone.Icon, 1f);
            Widgets.Label(new Rect(x + rowHeight + 8f, y, rowWidth - rowHeight - 8f - 24f - 24f - 24f - 8f, rowHeight), zone.Name);

            if (Widgets.ButtonImage(new Rect(rowWidth - 24f - 24f - 24f - 24f - 8f, y + (rowHeight - 24f) / 2, 24f, 24f), TexButton.Copy, Color.white, Color.white * GenUI.SubtleMouseoverColor))
            {
                clipboard = zone;
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }

            if (clipboard != null && Widgets.ButtonImage(new Rect(rowWidth - 24f - 24f - 24f - 8f, y + (rowHeight - 24f) / 2, 24f, 24f), TexButton.Paste, Color.white, Color.white * GenUI.SubtleMouseoverColor))
            {
                zone.priority = clipboard.priority;
                ThingFilter filter = new ThingFilter();
                filter.CopyAllowancesFrom(clipboard.filter);
                zone.filter.CopyAllowancesFrom(filter);
                if (zone is ZoneType_Building zoneTypeBuilding)
                {
                    zoneTypeBuilding.filter.SetDisallowAll(zoneTypeBuilding.buildingDef.building.fixedStorageSettings.filter.AllowedThingDefs);
                    foreach (SpecialThingFilterDef def in (typeof(ThingFilter).Field("disallowedSpecialFilters").GetValue(zoneTypeBuilding.buildingDef.building.fixedStorageSettings.filter) as List<SpecialThingFilterDef>))
                    {
                        zoneTypeBuilding.filter.SetAllow(def, false);
                    }
                    foreach (SpecialThingFilterDef def in (typeof(ThingFilter).Field("disallowedSpecialFilters").GetValue(filter) as List<SpecialThingFilterDef>).Where(s => zoneTypeBuilding.buildingDef.building.fixedStorageSettings.filter.IsValidSpecialThingFilter(s)))
                    {
                        zoneTypeBuilding.filter.SetAllow(def, false);
                    }
                }
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
            }

            Rect rowRect = new Rect(0f, y, rowWidth, rowHeight);
            if (!ReorderableWidget.Dragging)
            {
                if (Widgets.ButtonInvisible(new Rect(x, y, rowWidth - x, rowHeight)))
                {
                    selectedZoneType = zone;
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                }
                Widgets.DrawHighlightIfMouseover(rowRect);
                if (zone == selectedZoneType)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
            }
        }

        private abstract class StorageTab : TabRecord
        {
            public StorageTab(string label, Action clickedAction, Func<bool> selected) : base(label.CapitalizeFirst(), clickedAction, selected)
            {
            }

            public abstract string Label { get; }

            public abstract TaggedString ResetWarning { get; }

            public abstract void ResetSettings();

            public abstract bool DoTab(float rowWidth, float rowHeight, int reorderableGroup);

            public virtual void DoControls(Rect rect)
            {
            }
        }

        private class StorageTab_Stockpile : StorageTab
        {
            public static readonly string name = "Defaults_StockpileStorageSettings".Translate();

            public StorageTab_Stockpile(Action clickedAction, Func<bool> selected) : base(name, clickedAction, selected)
            {
            }

            public override string Label => name;

            public override TaggedString ResetWarning => "Defaults_ConfirmResetStockpileZoneSettings".Translate();

            public override bool DoTab(float rowWidth, float rowHeight, int reorderableGroup)
            {
                List<ZoneType> stockpileZones = Settings.Get<List<ZoneType>>(Settings.STOCKPILE_ZONES);

                for (int i = 0; i < stockpileZones.Count; i++)
                {
                    ZoneType zone = stockpileZones[i];

                    if (zone.DesignatorType == typeof(Designator_ZoneAddStockpile_Custom))
                    {
                        if (Widgets.ButtonImage(new Rect(rowWidth - 24f - 24f - 8f, y + (rowHeight - 24f) / 2, 24f, 24f), TexButton.Rename, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                        {
                            Find.WindowStack.Add(new Dialog_RenameZoneType(zone));
                        }
                    }

                    if (zone.DesignatorType == typeof(Designator_ZoneAddStockpile_Custom))
                    {
                        if (Widgets.ButtonImage(new Rect(rowWidth - 24f - 8f, y + (rowHeight - 24f) / 2, 24f, 24f), TexButton.Delete, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                        {
                            stockpileZones.Remove(zone);
                            i--;
                            SoundDefOf.Click.PlayOneShotOnCamera(null);
                        }
                    }

                    DoZoneTypeButton(zone, 40f, rowWidth, rowHeight);
                    Rect dragRect = new Rect(0f, y + rowHeight / 2 - 20f, 40f, 40f).ContractedBy(5f);
                    Rect draggableRect = new Rect(0f, y, rowWidth, rowHeight);
                    UIUtility.DoDraggable(reorderableGroup, draggableRect, dragRect, dragRect);

                    y += rowHeight;
                }

                if (!stockpileZones.Contains(selectedZoneType))
                {
                    selectedZoneType = null;
                }

                return true;
            }

            public override void DoControls(Rect rect)
            {
                List<ZoneType> stockpileZones = Settings.Get<List<ZoneType>>(Settings.STOCKPILE_ZONES);

                if (Widgets.ButtonText(new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height), "Defaults_AddNew".Translate()))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (StorageSettingsPreset preset in Enum.GetValues(typeof(StorageSettingsPreset)))
                    {
                        list.Add(new FloatMenuOption(preset.PresetName().CapitalizeFirst(), () =>
                        {
                            ZoneType newZoneType = new ZoneType(preset.PresetName().CapitalizeFirst() + " " + (stockpileZones.Count + 1), preset);
                            stockpileZones.Add(newZoneType);
                            selectedZoneType = newZoneType;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                }

                if (clipboard != null && !(clipboard is ZoneType_Building) && Widgets.ButtonImage(new Rect(rect.xMax - buttonWidth - 10f - 24f, rect.y, controlHeight, controlHeight), TexButton.Paste, Color.white, Color.white * GenUI.SubtleMouseoverColor))
                {
                    stockpileZones.Add(new ZoneType(clipboard.preset.PresetName().CapitalizeFirst() + " " + (stockpileZones.Count + 1), clipboard));
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }
            }

            public override void ResetSettings()
            {
                DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Storage>().ResetStockpileZoneSettings(true);
            }

        }

        private class StorageTab_Building : StorageTab
        {
            public static readonly string name = "Defaults_BuildingStorageSettings".Translate();

            public StorageTab_Building(Action clickedAction, Func<bool> selected) : base(name, clickedAction, selected)
            {
            }

            public override string Label => name;

            public override TaggedString ResetWarning => "Defaults_ConfirmResetTheseSettings".Translate(Label);

            public override bool DoTab(float rowWidth, float rowHeight, int reorderableGroup)
            {
                List<ZoneType_Building> buildingStorageSettings = Settings.Get<Dictionary<ThingDef, ZoneType_Building>>(Settings.BUILDING_STORAGE).Values.ToList();
                foreach (ZoneType_Building zone in buildingStorageSettings.OrderBy(s => s.buildingDef.uiOrder))
                {
                    DoZoneTypeButton(zone, 0f, rowWidth, rowHeight);
                    y += rowHeight;
                }
                if (!buildingStorageSettings.Contains(selectedZoneType))
                {
                    selectedZoneType = null;
                }
                return false;
            }

            public override void ResetSettings()
            {
                DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Storage>().ResetBuildingStorageSettings(true);
            }
        }
    }
}

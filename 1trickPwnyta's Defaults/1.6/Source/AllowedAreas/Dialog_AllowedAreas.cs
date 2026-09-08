using Defaults.Defs;
using Defaults.UI;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.AllowedAreas
{
    [StaticConstructorOnStartup]
    public class Dialog_AllowedAreas : Dialog_SettingsCategory
    {
        private static readonly Texture2D fullAreaTex = ContentFinder<Texture2D>.Get("UI/Defaults_FullArea");

        private AllowedArea toRemove;
        private bool painting;
        private AllowedArea paintingArea;

        public Dialog_AllowedAreas(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(700f, 548f);

        protected override TaggedString ResetOptionWarning => "Defaults_ConfirmResetAllowedAreaSettings".Translate();

        protected override IList ReorderableItems => Settings.Get<List<AllowedArea>>(Settings.ALLOWED_AREAS);

        public override void DoSettings(Rect rect)
        {
            if (Event.current.rawType == EventType.MouseUp || Input.GetMouseButtonUp(0))
            {
                painting = false;
            }

            DoLeftSide(new Rect(rect.x, rect.y, rect.width / 2 - 15f, rect.height));
            DoRightSide(new Rect(rect.x + rect.width / 2 + 15f, rect.y, rect.width / 2 - 15f, rect.height));
        }

        private void DoLeftSide(Rect rect)
        {
            List<AllowedArea> allowedAreas = Settings.Get<List<AllowedArea>>(Settings.ALLOWED_AREAS);
            float y = rect.y;
            float rowHeight = 30f;
            Rect allowedAreasRect = new Rect(rect.x, rect.y, rect.width, rect.height - rowHeight - 10f);
            reorderableRect = allowedAreasRect;

            bool highlight = false;
            foreach (AllowedArea area in allowedAreas)
            {
                Rect rowRect = new Rect(rect.x, y, rect.width, rowHeight);
                DoAllowedAreaRow(rowRect, area, highlight);
                y += rowHeight;
                highlight = !highlight;
            }
            if (toRemove != null)
            {
                allowedAreas.Remove(toRemove);
                Settings.Get<Dictionary<PawnType, AllowedArea>>(Settings.ALLOWED_AREAS_PAWN).RemoveAll(p => p.Value == toRemove);
                toRemove = null;
            }

            if (allowedAreas.Count < 10 && Widgets.ButtonText(new Rect(rect.x, rect.yMax - rowHeight, rect.width, rowHeight), "NewArea".Translate()))
            {
                AllowedArea area = new AllowedArea(AllowedArea.FindUnusedName());
                allowedAreas.Add(area);
                SoundDefOf.Click.PlayOneShot(null);
            }
        }

        private void DoAllowedAreaRow(Rect rect, AllowedArea area, bool highlight)
        {
            Widgets.DrawRectFast(rect, Widgets.MenuSectionBGFillColor);
            if (highlight)
            {
                Widgets.DrawLightHighlight(rect);
            }
            if (!ReorderableWidget.Dragging)
            {
                Widgets.DrawHighlightIfMouseover(rect);
                MouseoverSounds.DoRegion(rect);
            }

            float x = rect.x;
            Rect colorRect = new Rect(x, rect.y, rect.height, rect.height).ContractedBy(3f);
            Widgets.DrawRectFast(colorRect, area.color);
            TooltipHandler.TipRegion(colorRect, "Defaults_ClickToChangeAllowedAreaColor".Translate());
            if (Widgets.ButtonInvisible(colorRect))
            {
                Find.WindowStack.Add(new Dialog_ColorPickerAllowedArea(area));
            }
            x += rect.height + 8f;

            Rect labelRect = new Rect(x, rect.y, rect.width - x - rect.height * 3, rect.height);
            string label = area.name;
            while (Text.CalcSize(label).x > labelRect.width)
            {
                label = label.Substring(0, label.Length - 4) + "...";
            }
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(labelRect, label);

            x = rect.xMax;
            if (Widgets.ButtonImage(new Rect(x - rect.height, rect.y, rect.height, rect.height).ContractedBy(3f), TexButton.Delete, tooltip: "Delete".Translate()))
            {
                toRemove = area;
                SoundDefOf.Click.PlayOneShot(null);
            }
            x -= rect.height;

            if (Widgets.ButtonImage(new Rect(x - rect.height, rect.y, rect.height, rect.height).ContractedBy(3f), TexButton.Rename, tooltip: "Rename".Translate()))
            {
                Find.WindowStack.Add(new Dialog_RenameAllowedArea(area));
            }
            x -= rect.height;

            UIUtility.DoCheckButton(new Rect(x - rect.height, rect.y, rect.height, rect.height).ContractedBy(3f), fullAreaTex, area.full ? "Defaults_FullAreaTip".Translate() : "Defaults_EmptyAreaTip".Translate(), ref area.full);
            x -= rect.height;

            UIUtility.DoDraggable(ReorderableGroup, rect, tipRect: labelRect);
        }

        private void DoRightSide(Rect rect)
        {
            List<AllowedArea> allowedAreas = Settings.Get<List<AllowedArea>>(Settings.ALLOWED_AREAS);
            Dictionary<PawnType, AllowedArea> allowedPawnAreas = Settings.Get<Dictionary<PawnType, AllowedArea>>(Settings.ALLOWED_AREAS_PAWN);
            float rowHeight = 30f;
            float y = rect.y;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, rowHeight), "Defaults_DefaultPawnAllowedAreas".Translate());
            y += rowHeight;
            foreach (PawnType allowedPawn in Enum.GetValues(typeof(PawnType)))
            {
                if (allowedPawn.IsActive())
                {
                    AllowedArea area = allowedPawnAreas.TryGetValue(allowedPawn);
                    Rect rowRect = new Rect(rect.x, y, rect.width, rowHeight);
                    using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(new Rect(rowRect.x, rowRect.y, rowRect.width / 2, rowRect.height), allowedPawn.GetLabel().CapitalizeFirst());
                    Rect buttonRect = new Rect(rowRect.x + rowRect.width / 2, rowRect.y, rowRect.width / 2, rowRect.height);
                    Widgets.DrawRectFast(buttonRect, area?.color ?? Color.grey);
                    Widgets.DrawHighlightIfMouseover(buttonRect);
                    using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(buttonRect, area?.name ?? "NoAreaAllowed".Translate());
                    Widgets.DraggableResult result = Widgets.ButtonInvisibleDraggable(buttonRect);
                    if (result == Widgets.DraggableResult.Pressed)
                    {
                        Find.WindowStack.Add(new FloatMenu(new[] { null, DefaultSettingsCategoryWorker_AllowedAreas.HomeArea }.Concat(allowedAreas).Select(a => new FloatMenuOption(a?.name ?? "NoAreaAllowed".Translate(), () =>
                        {
                            allowedPawnAreas[allowedPawn] = a;
                        }, BaseContent.WhiteTex, a?.color ?? Color.grey)).ToList()));
                    }
                    else if (result == Widgets.DraggableResult.Dragged)
                    {
                        painting = true;
                        paintingArea = area;
                    }
                    if (painting && Mouse.IsOver(rowRect) && Input.GetMouseButton(0) && area != paintingArea)
                    {
                        allowedPawnAreas[allowedPawn] = paintingArea;
                        SoundDefOf.Click.PlayOneShot(null);
                    }
                    y += rowRect.height;
                }
            }
        }
    }
}

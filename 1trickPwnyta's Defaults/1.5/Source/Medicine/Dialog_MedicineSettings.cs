using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.Medicine
{
    public class Dialog_MedicineSettings : Window
    {
        private static bool medicalCarePainting = false;
        private Texture2D[] careTextures;

        public Dialog_MedicineSettings()
        {
            this.doCloseX = true;
            this.doCloseButton = true;
            careTextures = new Texture2D[5];
            careTextures[0] = ContentFinder<Texture2D>.Get("UI/Icons/Medical/NoCare", true);
            careTextures[1] = ContentFinder<Texture2D>.Get("UI/Icons/Medical/NoMeds", true);
            careTextures[2] = ThingDefOf.MedicineHerbal.uiIcon;
            careTextures[3] = ThingDefOf.MedicineIndustrial.uiIcon;
            careTextures[4] = ThingDefOf.MedicineUltratech.uiIcon;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(406f, 640f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float num = 0f;
            using (new TextBlock(GameFont.Medium))
            {
                Widgets.Label(inRect, ref num, "Defaults_Medicine".Translate());
            }
            Text.Font = GameFont.Small;
            Widgets.Label(inRect, ref num, "DefaultMedicineSettingsDesc".Translate());
            num += 10f;
            Text.Anchor = TextAnchor.MiddleLeft;
            DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForColonist, "MedGroupColonists", "MedGroupColonistsDesc");
            DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForPrisoner, "MedGroupPrisoners", "MedGroupPrisonersDesc");
            if (ModsConfig.IdeologyActive)
            {
                DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForSlave, "MedGroupSlaves", "MedGroupSlavesDesc");
            }
            if (ModsConfig.AnomalyActive)
            {
                DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForGhouls, "MedGroupGhouls", "MedGroupGhoulsDesc");
            }
            DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForTamedAnimal, "MedGroupTamedAnimals", "MedGroupTamedAnimalsDesc");
            num += 17f;
            DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForFriendlyFaction, "MedGroupFriendlyFaction", "MedGroupFriendlyFactionDesc");
            DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForNeutralFaction, "MedGroupNeutralFaction", "MedGroupNeutralFactionDesc");
            DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForHostileFaction, "MedGroupHostileFaction", "MedGroupHostileFactionDesc");
            num += 17f;
            DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForNoFaction, "MedGroupNoFaction", "MedGroupNoFactionDesc");
            DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForWildlife, "MedGroupWildlife", "MedGroupWildlifeDesc");
            if (ModsConfig.AnomalyActive)
            {
                DoRow(inRect, ref num, ref DefaultsSettings.DefaultCareForEntities, "MedGroupEntities", "MedGroupEntitiesDesc");
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DoRow(Rect rect, ref float y, ref MedicalCareCategory category, string labelKey, string tipKey)
        {
            Rect rect2 = new Rect(rect.x, y, rect.width, 28f);
            Rect rect3 = new Rect(rect.x, y, 230f, 28f);
            Rect rect4 = new Rect(230f, y, 140f, 28f);
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawLightHighlight(rect2);
            }
            TooltipHandler.TipRegionByKey(rect2, tipKey);
            Widgets.LabelFit(rect3, labelKey.Translate());
            MedicalCareSetter(rect4, ref category);
            y += 34f;
        }

        public void MedicalCareSetter(Rect rect, ref MedicalCareCategory medCare)
        {
            Rect rect2 = new Rect(rect.x, rect.y, rect.width / 5f, rect.height);
            for (int i = 0; i < 5; i++)
            {
                MedicalCareCategory mc = (MedicalCareCategory)i;
                Widgets.DrawHighlightIfMouseover(rect2);
                MouseoverSounds.DoRegion(rect2);
                GUI.DrawTexture(rect2, careTextures[i]);
                Widgets.DraggableResult draggableResult = Widgets.ButtonInvisibleDraggable(rect2, false);
                if (draggableResult == Widgets.DraggableResult.Dragged)
                {
                    medicalCarePainting = true;
                }
                if ((medicalCarePainting && Mouse.IsOver(rect2) && medCare != mc) || draggableResult.AnyPressed())
                {
                    medCare = mc;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                if (medCare == mc)
                {
                    Widgets.DrawBox(rect2, 2, null);
                }
                if (Mouse.IsOver(rect2))
                {
                    TooltipHandler.TipRegion(rect2, () => mc.GetLabel().CapitalizeFirst(), 632165 + i * 17);
                }
                rect2.x += rect2.width;
            }
            if (!Input.GetMouseButton(0))
            {
                medicalCarePainting = false;
            }
        }
    }
}

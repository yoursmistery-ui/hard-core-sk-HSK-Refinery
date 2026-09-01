using RimWorld;
using System;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace QualityBuilder
{
	public class _Designator_SkilledBuilder : Designator
	{
        private static QualityCategory? curQualityCat;

		// RimWorld 1.6: DraggableDimensions was removed; drag style is chosen via DrawStyleCategory.
		public override DrawStyleCategoryDef DrawStyleCategory
		{
			get
			{
				return DefDatabase<DrawStyleCategoryDef>.GetNamed("Default2D");
			}
		}

		public _Designator_SkilledBuilder()
		{
			this.defaultLabel = Translator.Translate("QualityBuilderOn.Label");
			this.defaultDesc = Translator.Translate("QualityBuilderOn.Desc");
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                this.icon = ContentFinder<Texture2D>.Get("Skilled", true);
            });
            this.soundDragSustain = SoundDefOf.Designate_DragStandard;
			this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			this.useMouseIcon = true;
			this.soundSucceeded = SoundDefOf.Designate_Haul;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 c)
		{
            if (!GenGrid.InBounds(c, Find.CurrentMap) || GridsUtility.Fogged(c, Find.CurrentMap))
                return false;
			Thing firstBuilding = QualityBuilder.GetFirstBuildingBuildingOrFrame(c);
            return CanDesignateThing(firstBuilding);
		}

		public override void DesignateSingleCell(IntVec3 c)
		{
			this.DesignateThing(QualityBuilder.GetFirstBuildingBuildingOrFrame(c));
		}

		public override AcceptanceReport CanDesignateThing(Thing t)
		{
            if (t == null || (!t.def.IsFrame && !t.def.IsBlueprint))
                return false;
            // Only buildings that actually have quality (CompQuality on the final product) may get the
            // quality-build designation. The injected CompProperties_QualityBuilderr on the blueprint/frame
            // def marks exactly those. Checking def.comps (instead of the instance comp) also covers
            // blueprints/frames that existed before the comp injection (loaded saves).
            if (t.def.comps == null || !t.def.comps.OfType<CompProperties_QualityBuilderr>().Any())
                return false;
            if (QualityBuilder.hasDesignation(t) && QualityBuilder.hasQuality(t, getDesiredQuality(t)))
                return false;
            return AcceptanceReport.WasAccepted;
		}

		public override void DesignateThing(Thing t)
		{
            QualityCategory curQual = getDesiredQuality(t);
            QualityBuilder.setSkilled(t, curQual, true);
            CompQualityBuilder cmp = QualityBuilder.getCompQualityBuilder(t as ThingWithComps);
            if (cmp != null)
                cmp.desiredMinQuality = curQual;
		}

        private QualityCategory getDesiredQuality(Thing t)
        {
            return getDesiredQuality(t.Map);
        }

        private QualityCategory getDesiredQuality(Map map)
        {
            return curQualityCat.HasValue ? curQualityCat.Value : QualityBuilderModSettings.getDefaultMinQualitySetting(map);
        }



        public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
		}

        public override string Desc
        {
            get
            {
                String qualityName = Translator.Translate("QualityCategory_" + Enum.GetName(typeof(QualityCategory), getDesiredQuality(Find.CurrentMap)));
                NamedArgument arg = NamedArgumentUtility.Named(qualityName, "qualityName");
                return "QualityBuilderOn.Desc".Translate(arg);
            }
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                return QualityBuilder.getFloatingOptions(item => curQualityCat = item);
            }
        }
    }
}

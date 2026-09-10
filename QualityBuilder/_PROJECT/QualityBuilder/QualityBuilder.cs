using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using System.Linq;

namespace QualityBuilder
{
	public static class QualityBuilder
	{

        public static bool QUALITYBUILDER_ON = true;

		public static Thing GetFirstBuildingBuildingOrFrame(Map map, IntVec3 c)
		{
            if (map == null)
                return null;
			List<Thing> list = map.thingGrid.ThingsListAt(c);
			for (int i = 0; i < list.Count; i++)
			{
                var cur = list[i];
                if (cur == null || cur.def == null)
                    continue;
                bool flag = (cur.def.IsFrame || cur.def.IsBlueprint) && getCompQualityBuilder(cur) != null;
				if (flag)
                    return cur;
			}
            return null;
		}

        public static Thing GetFirstBuildingBuildingOrFrame(IntVec3 c)
        {
            return GetFirstBuildingBuildingOrFrame(Find.CurrentMap, c);
        }

        public static DesignationDef getDesignationDef(QualityCategory cat)
        {
            String name = "SkilledBuilder";
            if (cat == QualityCategory.Poor)
                name += "2";
            else if (cat == QualityCategory.Normal)
                name += "3";
            else if (cat == QualityCategory.Good)
                name += "4";
            else if (cat == QualityCategory.Excellent)
                name += "5";
            else if (cat == QualityCategory.Masterwork)
                name += "6";
            else if (cat == QualityCategory.Legendary)
                name += "7";
            return DefDatabase<DesignationDef>.GetNamed(name, true);
        }

        public static bool hasDesignation(Thing t)
        {
            return getDesignationOnThing(t) != null;
        }

        public static Designation getDesignationOnThing(Thing thing)
        {
            Map thingMap = getMapForThing(thing);
            if (thingMap == null)
                return null;
            return thingMap.designationManager.AllDesignationsOn(thing).Where(d => d.def.defName.StartsWith("SkilledBuilder")).FirstOrFallback();
        }

        public static void setSkilled(Thing thing, QualityCategory? cat, bool add)
        {
            Map thingMap = getMapForThing(thing);
            if (thingMap == null)
                return;
            Designation desOnThing = getDesignationOnThing(thing);
            if (desOnThing != null)
                thingMap.designationManager.RemoveDesignation(desOnThing);
            QualityCategory curCat = cat.HasValue ? cat.Value : QualityBuilderModSettings.getDefaultMinQualitySetting(thing.Map);
            if (add)
                thingMap.designationManager.AddDesignation(new Designation(thing, getDesignationDef(curCat)));
            setSkilledInComp(thing, curCat, add);
        }

        private static void setSkilledInComp(Thing thing, QualityCategory curCat, bool add)
        {
            CompQualityBuilder cmp = getOrCreateCompQualityBuilder(thing);
            if (cmp == null)
                return;
            cmp.isSkilled = add;
            cmp.desiredMinQuality = curCat;
        }

        public static CompQualityBuilder getCompQualityBuilder(Thing thing)
        {
            if (thing == null)
                return null;
            if (!typeof(ThingWithComps).IsAssignableFrom(thing.GetType()))
                return null;
            ThingWithComps withComps = thing as ThingWithComps;
            return withComps?.GetComp<CompQualityBuilder>();
        }

        // A blueprint/frame def whose final building has quality got a CompProperties_QualityBuilderr injected
        // into def.comps at map init. Instances that existed before the injection (loaded saves) lack the comp,
        // so lazily add it when one is needed.
        public static CompQualityBuilder getOrCreateCompQualityBuilder(Thing thing)
        {
            CompQualityBuilder cmp = getCompQualityBuilder(thing);
            if (cmp != null)
                return cmp;
            if (thing == null)
                return null;
            if (!typeof(ThingWithComps).IsAssignableFrom(thing.GetType()))
                return null;
            ThingWithComps withComps = thing as ThingWithComps;
            if (!(withComps.def.IsBlueprint || withComps.def.IsFrame))
                return null;
            if (withComps.def.comps == null || !withComps.def.comps.OfType<CompProperties_QualityBuilderr>().Any())
                return null;
            CompQualityBuilder newComp = new CompQualityBuilder();
            newComp.parent = withComps;
            newComp.Initialize(new CompProperties_QualityBuilderr());
            withComps.AllComps.Add(newComp);
            return newComp;
        }

        // One-shot pass over the current map: give existing blueprint/frame instances the comp if their def has
        // quality. Called once per map/save load while playing (see ModInitializerComponent.Update).
        public static void ensureCompsOnExistingBlueprintsAndFrames()
        {
            Map curMap = Find.CurrentMap;
            if (curMap == null)
                return;
            int added = 0;
            foreach (Thing th in curMap.spawnedThings)
            {
                if (th == null)
                    continue;
                if (!(th.def.IsBlueprint || th.def.IsFrame))
                    continue;
                if (getCompQualityBuilder(th) != null)
                    continue;
                if (th.def.comps == null || !th.def.comps.OfType<CompProperties_QualityBuilderr>().Any())
                    continue;
                if (getOrCreateCompQualityBuilder(th) != null)
                    added++;
            }
            if (added > 0)
                Log.Message("QualityBuilder added component to '" + added + "' existing blueprints/frames");
        }

        public static bool hasQuality(Thing thing, QualityCategory cat)
        {
            CompQualityBuilder cmp = getCompQualityBuilder(thing);
            if (cmp == null)
                return false;
            return cmp.desiredMinQuality == cat;
        }

        public static Map getMapForThing(Thing thing)
        {
            if (thing == null)
                return null;
            Map curMap = thing.Map;
            if (curMap == null)
                curMap = Find.CurrentMap;
            return curMap;
        }

        public static QualityBuilderMod getMod()
        {
            return LoadedModManager.GetMod<QualityBuilderMod>();
        }

        public delegate void SetQuality(QualityCategory cat);

        public static IEnumerable<FloatMenuOption> getFloatingOptions(SetQuality action)
        {
            List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
            foreach (QualityCategory item in Enum.GetValues(typeof(QualityCategory)))
                floatOptionList.Add(buildFloatMenuOption(action, item));
            return floatOptionList;
        }

        public static FloatMenuOption buildFloatMenuOption(SetQuality action, QualityCategory cat)
        {
            return new FloatMenuOption(Translator.Translate("QualityCategory_" + Enum.GetName(typeof(QualityCategory), cat)), new Action(delegate
            {
                action.Invoke(cat);
            }));
        }

        public static int getPawnConstructionSkill(Pawn pawn)
        {
            if (pawn.IsColonyMech)
                return pawn.RaceProps.mechFixedSkillLevel;
            else if (pawn.IsColonist)
                return pawn.skills.GetSkill(SkillDefOf.Construction).Level;
            return 0;
        }

        public static int getBestConstructorSkill(Map curMap)
        {
            return getPawnConstructionSkill(curMap.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => pawnCanConstruct(p)).OrderByDescending<Pawn, int>(p => getPawnConstructionSkill(p)).FirstOrDefault<Pawn>());
        }

        public static bool pawnCanConstruct(Pawn pawn)
        {
            if (pawn.RaceProps.Humanlike && pawn.skills != null && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Construction) && pawnCapable(pawn))
                return true;
            else if (pawn.IsColonyMech && pawn.GetOverseer() != null && pawn.RaceProps.mechEnabledWorkTypes.Contains(WorkTypeDefOf.Construction))
                return true;
            return false;
        }

        private static bool pawnCapable(Pawn p)
        {
            Pawn_HealthTracker healthTracker = p.health;
            PawnCapacitiesHandler capacitiesHandler = healthTracker.capacities;
            return capacitiesHandler.CapableOf(PawnCapacityDefOf.Manipulation) && capacitiesHandler.CapableOf(PawnCapacityDefOf.Moving);
        }
    }
}

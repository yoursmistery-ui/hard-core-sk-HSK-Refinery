using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

using System.Reflection;

namespace FacilityCrashFix
{
    [StaticConstructorOnStartup]
    public static class FacilityCrashFixInit
    {
        static FacilityCrashFixInit()
        {
            Harmony harmony = new Harmony("local.facilitycrashfix");
            harmony.PatchAll();
            PatchHaulNullGuards(harmony);
            PatchDesignatorVisibility(harmony);
        }

        private static void PatchDesignatorVisibility(Harmony harmony)
        {
            try
            {
                MethodInfo getter = AccessTools.PropertyGetter(
                    typeof(DesignationCategoryDef),
                    "ResolvedAllowedDesignators");
                if (getter != null)
                {
                    harmony.Patch(
                        getter,
                        postfix: new HarmonyMethod(typeof(FacilityCrashFixInit).GetMethod(
                            "ResolvedDesignatorsFilter",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                }
            }
            catch (Exception e)
            {
                Log.Warning("[FacilityCrashFix] Designator visibility filter not applied: " + e.Message);
            }
        }

        private static void PatchHaulNullGuards(Harmony harmony)
        {
            try
            {
                MethodInfo fogged = AccessTools.Method(typeof(GridsUtility), "Fogged", new[] { typeof(Thing) });
                if (fogged != null)
                {
                    harmony.Patch(
                        fogged,
                        prefix: new HarmonyMethod(typeof(FacilityCrashFixInit).GetMethod(
                            "FoggedGuard",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                }

                MethodInfo haulFast = AccessTools.Method(
                    typeof(Verse.AI.HaulAIUtility),
                    "PawnCanAutomaticallyHaulFast",
                    new[] { typeof(Pawn), typeof(Thing), typeof(bool) });
                if (haulFast != null)
                {
                    harmony.Patch(
                        haulFast,
                        prefix: new HarmonyMethod(typeof(FacilityCrashFixInit).GetMethod(
                            "HaulFastGuard",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                }

                Type architectSenseItem = AccessTools.TypeByName("ArchitectSense.Designator_SubCategoryItem");
                MethodInfo materialsAvailable = null;
                if (architectSenseItem != null)
                {
                    materialsAvailable = architectSenseItem.GetMethod(
                        "get_MaterialsAvailable",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                if (materialsAvailable != null)
                {
                    harmony.Patch(
                        materialsAvailable,
                        prefix: new HarmonyMethod(typeof(FacilityCrashFixInit).GetMethod(
                            "MaterialsAvailableGuard",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                }
            }
            catch (Exception e)
            {
                Log.Warning("[FacilityCrashFix] Haul null guard not applied: " + e.Message);
            }
        }

        private static bool FoggedGuard(Thing t, ref bool __result)
        {
            if (t == null || t.Map == null)
            {
                __result = true;
                return false;
            }

            return true;
        }

        private static bool HaulFastGuard(Pawn p, Thing t, bool forced, ref bool __result)
        {
            if (t == null || t.Map == null)
            {
                __result = false;
                return false;
            }

            return true;
        }

        private static bool MaterialsAvailableGuard(ref bool __result)
        {
            __result = true;
            return false;
        }

        private static IEnumerable<Designator> ResolvedDesignatorsFilter(IEnumerable<Designator> __result)
        {
            if (__result == null)
            {
                return null;
            }

            return __result.Where(d => d != null && d.Visible);
        }
    }

    [HarmonyPatch(typeof(CompFacility), "DrawPlaceMouseAttachmentsToPotentialThingsToLinkTo")]
    public static class FacilityPreviewSafeReplacement
    {
        private static bool Prefix(float curX, ref float curY, ThingDef myDef, IntVec3 myPos, Rot4 myRot, Map map)
        {
            try
            {
                if (myDef == null || map == null || map.listerThings == null)
                {
                    return false;
                }

                CompProperties_Facility props = myDef.GetCompProperties<CompProperties_Facility>();
                if (props == null || props.linkableBuildings == null)
                {
                    return false;
                }

                int num = 0;
                for (int i = 0; i < props.linkableBuildings.Count; i++)
                {
                    ThingDef targetDef = props.linkableBuildings[i];
                    if (targetDef == null)
                    {
                        continue;
                    }

                    foreach (Thing thing in map.listerThings.ThingsOfDef(targetDef))
                    {
                        if (thing == null)
                        {
                            continue;
                        }

                        CompAffectedByFacilities comp = thing.TryGetComp<CompAffectedByFacilities>();
                        if (comp != null && comp.CanPotentiallyLinkTo(myDef, myPos, myRot))
                        {
                            num++;
                            if (num == 1)
                            {
                                DrawLine(curX, ref curY, "FacilityPotentiallyLinkedTo".Translate() + ":");
                            }
                            DrawLine(curX, ref curY, "  - " + thing.LabelCap);
                        }
                    }
                }

                if (num == 0)
                {
                    DrawLine(curX, ref curY, "FacilityNoPotentialLinks".Translate());
                }
            }
            catch (Exception e)
            {
                Log.Warning("[FacilityCrashFix] Suppressed facility preview error: " + e.Message);
            }

            return false;
        }

        private static void DrawLine(float curX, ref float curY, string text)
        {
            float lineHeight = Text.LineHeight;
            Widgets.Label(new Rect(curX, curY, 999f, lineHeight), text);
            curY += lineHeight;
        }
    }
}


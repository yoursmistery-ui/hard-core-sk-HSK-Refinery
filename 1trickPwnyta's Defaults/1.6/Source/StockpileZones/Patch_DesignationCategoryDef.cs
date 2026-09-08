using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.StockpileZones
{
    [HarmonyPatchCategory("Storage")]
    [HarmonyPatch(typeof(DesignationCategoryDef))]
    [HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedDesignators), MethodType.Getter)]
    public static class Patch_DesignationCategoryDef_get_AllResolvedDesignators
    {
        public static void Postfix(DesignationCategoryDef __instance, ref List<Designator> __result)
        {
            if (__instance == DesignationCategoryDefOf.Zone)
            {
                __result = PatchUtility_DesignationCategoryDef.AddDesignators(__result).ToList();
            }
        }
    }

    [HarmonyPatchCategory("Storage")]
    [HarmonyPatch(typeof(DesignationCategoryDef))]
    [HarmonyPatch(nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MethodType.Getter)]
    public static class Patch_DesignationCategoryDef_get_ResolvedAllowedDesignators
    {
        public static void Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            if (__instance == DesignationCategoryDefOf.Zone)
            {
                __result = PatchUtility_DesignationCategoryDef.AddDesignators(__result);
            }
        }
    }

    [HarmonyPatchCategory("Storage")]
    [HarmonyPatch(typeof(DesignationCategoryDef))]
    [HarmonyPatch(nameof(DesignationCategoryDef.AllResolvedAndIdeoDesignators), MethodType.Getter)]
    public static class Patch_DesignationCategoryDef_get_AllResolvedAndIdeoDesignators
    {
        public static void Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result)
        {
            if (__instance == DesignationCategoryDefOf.Zone)
            {
                __result = PatchUtility_DesignationCategoryDef.AddDesignators(__result);
            }
        }
    }

    public static class PatchUtility_DesignationCategoryDef
    {
        public static IEnumerable<Designator> AddDesignators(IEnumerable<Designator> __result)
        {
            Designator builtInStockpileDesignator = __result.FirstOrDefault(d => d is Designator_ZoneAddStockpile_Resources);
            Designator builtInDumpingStockpileDesignator = __result.FirstOrDefault(d => d is Designator_ZoneAddStockpile_Dumping);

            foreach (Designator designator in __result)
            {
                if (designator != builtInStockpileDesignator && designator != builtInDumpingStockpileDesignator)
                {
                    yield return designator;
                }

                if (designator is Designator_Deconstruct)
                {
                    List<ZoneType> stockpileZones = Settings.Get<List<ZoneType>>(Settings.STOCKPILE_ZONES);
                    if (stockpileZones != null)
                    {
                        foreach (ZoneType type in stockpileZones)
                        {
                            if (type.DesignatorType == typeof(Designator_ZoneAddStockpile_Resources))
                            {
                                yield return builtInStockpileDesignator;
                            }
                            if (type.DesignatorType == typeof(Designator_ZoneAddStockpile_Dumping))
                            {
                                yield return builtInDumpingStockpileDesignator;
                            }
                            if (type.DesignatorType == typeof(Designator_ZoneAddStockpile_Custom))
                            {
                                yield return new Designator_ZoneAddStockpile_Custom(type) { isOrder = true };
                            }
                        }
                    }
                }
            }
        }
    }
}

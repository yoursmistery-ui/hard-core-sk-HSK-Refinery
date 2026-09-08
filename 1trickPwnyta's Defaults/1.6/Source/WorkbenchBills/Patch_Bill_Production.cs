using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.WorkbenchBills
{
    [HarmonyPatchCategory("WorkbenchBills")]
    [HarmonyPatch(typeof(Bill_Production))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(RecipeDef), typeof(Precept_ThingStyle) })]
    public static class Patch_Bill_Production_ctor
    {
        public static bool enabled = true;

        public static void Postfix(Bill_Production __instance)
        {
            if (enabled)
            {
                __instance.ingredientSearchRadius = Settings.GetValue<float>(Settings.BILL_INGREDIENT_SEARCH_RADIUS);
                if (__instance.ingredientSearchRadius >= 100f)
                {
                    __instance.ingredientSearchRadius = 999f;
                }

                IntRange range = Settings.GetValue<IntRange>(Settings.BILL_ALLOWED_SKILL_RANGE);
                __instance.allowedSkillRange.min = range.min;
                // To support mods that increase max skill level, count 20 as unlimited and therefore do not change the max
                if (range.max < 20)
                {
                    __instance.allowedSkillRange.max = range.max;
                }

                __instance.SetStoreMode(Settings.Get<BillStoreModeDef>(Settings.BILL_STORE_MODE));
            }
        }
    }

    [HarmonyPatchCategory("WorkbenchBills")]
    [HarmonyPatch(typeof(Bill_Production))]
    [HarmonyPatch(nameof(Bill_Production.ShouldDoNow))]
    public static class Patch_Bill_Production_ShouldDoNow
    {
        public static void Postfix(Bill_Production __instance, ref bool __result)
        {
            __result &= !__instance.IsSuspendedOrUnavailable();
        }
    }
}

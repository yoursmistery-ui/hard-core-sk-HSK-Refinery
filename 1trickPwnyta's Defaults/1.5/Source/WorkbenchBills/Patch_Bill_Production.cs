using HarmonyLib;
using RimWorld;

namespace Defaults.WorkbenchBills
{
    // Patched manually in mod constructor
    public static class Patch_Bill_Production_ctor
    {
        public static bool Enabled = true;

        public static void Postfix(Bill_Production __instance)
        {
            if (Enabled)
            {
                __instance.ingredientSearchRadius = DefaultsSettings.DefaultBillIngredientSearchRadius;

                __instance.allowedSkillRange.min = DefaultsSettings.DefaultBillAllowedSkillRange.min;
                // To support mods that increase max skill level, count 20 as unlimited and therefore do not change the max
                if (DefaultsSettings.DefaultBillAllowedSkillRange.max < 20)
                {
                    __instance.allowedSkillRange.max = DefaultsSettings.DefaultBillAllowedSkillRange.max;
                }

                __instance.SetStoreMode(DefaultsSettings.DefaultBillStoreMode);
            }
        }
    }

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

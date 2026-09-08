using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Medicine
{
    [HarmonyPatchCategory("Medicine")]
    [HarmonyPatch(typeof(RimWorld.PlaySettings))]
    [HarmonyPatch(MethodType.Constructor)]
    public static class Patch_PlaySettings_ctor
    {
        public static void Postfix(RimWorld.PlaySettings __instance)
        {
            __instance.defaultCareForColonist = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_COLONIST);
            __instance.defaultCareForPrisoner = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_PRISONER);
            if (ModsConfig.IdeologyActive)
            {
                __instance.defaultCareForSlave = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_SLAVE);
            }
            __instance.defaultCareForTamedAnimal = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_TAMED_ANIMAL);
            __instance.defaultCareForFriendlyFaction = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_FRIENDLY_FACTION);
            __instance.defaultCareForNeutralFaction = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_NEUTRAL_FACTION);
            __instance.defaultCareForHostileFaction = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_HOSTILE_FACTION);
            __instance.defaultCareForNoFaction = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_NO_FACTION);
            __instance.defaultCareForWildlife = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_WILDLIFE);
            if (ModsConfig.AnomalyActive)
            {
                __instance.defaultCareForEntities = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_ENTITY);
                __instance.defaultCareForGhouls = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_GHOUL);
            }
        }
    }
}

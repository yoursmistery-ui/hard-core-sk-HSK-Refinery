using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Medicine
{
    [HarmonyPatchCategory("Medicine")]
    [HarmonyPatch(typeof(BackCompatibilityConverter_1_4))]
    [HarmonyPatch(nameof(BackCompatibilityConverter_1_4.PostExposeData))]
    public static class Patch_BackCompatibilityConverter_1_4_PostExposeData
    {
        public static void Postfix(object obj)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RimWorld.PlaySettings playSettings;
                if ((playSettings = (obj as RimWorld.PlaySettings)) != null)
                {
                    playSettings.defaultCareForColonist = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_COLONIST);
                    playSettings.defaultCareForPrisoner = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_PRISONER);
                    if (ModsConfig.IdeologyActive)
                    {
                        playSettings.defaultCareForSlave = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_SLAVE);
                    }
                    playSettings.defaultCareForTamedAnimal = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_TAMED_ANIMAL);
                    playSettings.defaultCareForFriendlyFaction = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_FRIENDLY_FACTION);
                    playSettings.defaultCareForNeutralFaction = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_NEUTRAL_FACTION);
                    playSettings.defaultCareForHostileFaction = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_HOSTILE_FACTION);
                    playSettings.defaultCareForNoFaction = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_NO_FACTION);
                    playSettings.defaultCareForWildlife = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_WILDLIFE);
                    if (ModsConfig.AnomalyActive)
                    {
                        playSettings.defaultCareForEntities = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_ENTITY);
                        playSettings.defaultCareForGhouls = Settings.GetValue<MedicalCareCategory>(Settings.DEFAULT_CARE_GHOUL);
                    }
                }
            }
        }
    }
}

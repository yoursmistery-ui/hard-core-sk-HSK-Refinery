using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Medicine
{
    [HarmonyPatch(typeof(BackCompatibilityConverter_1_4), nameof(BackCompatibilityConverter_1_4.PostExposeData))]
    public static class Patch_BackCompatibilityConverter_1_4_PostExposeData
    {
        public static void Postfix(object obj)
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RimWorld.PlaySettings playSettings;
                if ((playSettings = (obj as RimWorld.PlaySettings)) != null)
                {
                    playSettings.defaultCareForColonist = DefaultsSettings.DefaultCareForColonist;
                    playSettings.defaultCareForPrisoner = DefaultsSettings.DefaultCareForPrisoner;
                    playSettings.defaultCareForSlave = DefaultsSettings.DefaultCareForSlave;
                    playSettings.defaultCareForTamedAnimal = DefaultsSettings.DefaultCareForTamedAnimal;
                    playSettings.defaultCareForFriendlyFaction = DefaultsSettings.DefaultCareForFriendlyFaction;
                    playSettings.defaultCareForNeutralFaction = DefaultsSettings.DefaultCareForNeutralFaction;
                    playSettings.defaultCareForHostileFaction = DefaultsSettings.DefaultCareForHostileFaction;
                    playSettings.defaultCareForNoFaction = DefaultsSettings.DefaultCareForNoFaction;
                    playSettings.defaultCareForWildlife = DefaultsSettings.DefaultCareForWildlife;
                    playSettings.defaultCareForEntities = DefaultsSettings.DefaultCareForEntities;
                    playSettings.defaultCareForGhouls = DefaultsSettings.DefaultCareForGhouls;
                }
            }
        }
    }
}

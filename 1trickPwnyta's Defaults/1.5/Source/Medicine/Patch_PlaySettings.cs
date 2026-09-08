using RimWorld;

namespace Defaults.Medicine
{
    // Patched manually in mod constructor
    public static class Patch_PlaySettings_ctor
    {
        public static void Postfix(RimWorld.PlaySettings __instance)
        {
            __instance.defaultCareForColonist = DefaultsSettings.DefaultCareForColonist;
            __instance.defaultCareForPrisoner = DefaultsSettings.DefaultCareForPrisoner;
            __instance.defaultCareForSlave = DefaultsSettings.DefaultCareForSlave;
            __instance.defaultCareForTamedAnimal = DefaultsSettings.DefaultCareForTamedAnimal;
            __instance.defaultCareForFriendlyFaction = DefaultsSettings.DefaultCareForFriendlyFaction;
            __instance.defaultCareForNeutralFaction = DefaultsSettings.DefaultCareForNeutralFaction;
            __instance.defaultCareForHostileFaction = DefaultsSettings.DefaultCareForHostileFaction;
            __instance.defaultCareForNoFaction = DefaultsSettings.DefaultCareForNoFaction;
            __instance.defaultCareForWildlife = DefaultsSettings.DefaultCareForWildlife;
            __instance.defaultCareForEntities = DefaultsSettings.DefaultCareForEntities;
            __instance.defaultCareForGhouls = DefaultsSettings.DefaultCareForGhouls;
        }
    }
}

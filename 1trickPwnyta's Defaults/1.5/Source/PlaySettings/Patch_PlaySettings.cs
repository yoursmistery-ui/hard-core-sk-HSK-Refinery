namespace Defaults.PlaySettings
{
    // Patched manually in mod constructor
    public static class Patch_PlaySettings_ctor
    {
        public static void Postfix(RimWorld.PlaySettings __instance)
        {
            __instance.autoRebuild = DefaultsSettings.DefaultAutoRebuild;
            __instance.autoHomeArea = DefaultsSettings.DefaultAutoHomeArea;
            __instance.useWorkPriorities = DefaultsSettings.DefaultManualPriorities;
        }
    }
}

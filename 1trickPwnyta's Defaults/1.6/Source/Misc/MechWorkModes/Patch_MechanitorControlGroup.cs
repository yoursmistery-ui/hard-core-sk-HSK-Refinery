using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Misc.MechWorkModes
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(MechanitorControlGroup))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(Pawn_MechanitorTracker) })]
    [HarmonyPatchMod("Ludeon.RimWorld.Biotech")]
    public static class Patch_MechanitorControlGroup
    {
        public static void Postfix(ref MechWorkModeDef ___workMode)
        {
            ___workMode = Settings.Get<MechWorkModeDef>(Settings.MECH_WORK_MODE_ADDITIONAL);
        }
    }
}

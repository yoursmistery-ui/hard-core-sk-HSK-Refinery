using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Misc.MechWorkModes
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(Pawn_MechanitorTracker))]
    [HarmonyPatch("Notify_ControlGroupAmountMayChanged")]
    [HarmonyPatchMod("Ludeon.RimWorld.Biotech")]
    public static class Patch_Pawn_MechanitorTracker
    {
        public static void Prefix(Pawn_MechanitorTracker __instance)
        {
            if (__instance.controlGroups.Count == 0 && __instance.TotalAvailableControlGroups > 1)
            {
                MechanitorControlGroup group1 = new MechanitorControlGroup(__instance);
                group1.SetWorkMode(Settings.Get<MechWorkModeDef>(Settings.MECH_WORK_MODE_FIRST));
                __instance.controlGroups.Add(group1);
                MechanitorControlGroup group2 = new MechanitorControlGroup(__instance);
                group2.SetWorkMode(Settings.Get<MechWorkModeDef>(Settings.MECH_WORK_MODE_SECOND));
                __instance.controlGroups.Add(group2);
            }
        }
    }
}

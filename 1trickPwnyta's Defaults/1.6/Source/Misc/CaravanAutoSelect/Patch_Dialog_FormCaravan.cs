using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace Defaults.Misc.CaravanAutoSelect
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(Dialog_FormCaravan))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(Map), typeof(bool), typeof(Action), typeof(bool), typeof(IntVec3?) })]
    public static class Patch_Dialog_FormCaravan
    {
        public static void Postfix(ref bool ___autoSelectTravelSupplies, bool reform)
        {
            if (!reform)
            {
                ___autoSelectTravelSupplies = Settings.GetValue<bool>(Settings.CARAVAN_AUTO_SELECT);
            }
        }
    }
}

using HarmonyLib;
using Verse;

namespace Defaults.Medicine
{
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction
    {
        public static void Postfix(Pawn __instance)
        {
            MedicineUtility.SetMedicineToCarry(__instance, __instance.inventoryStock);
        }
    }
}

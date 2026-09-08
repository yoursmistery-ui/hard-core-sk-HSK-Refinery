using HarmonyLib;
using Verse;

namespace Defaults.Medicine
{
    [HarmonyPatch(typeof(PawnGenerator), "GenerateNewPawnInternal")]
    public static class Patch_PawnGenerator_GenerateNewPawnInternal
    {
        public static void Postfix(Pawn __result)
        {
            MedicineUtility.SetMedicineToCarry(__result, __result.inventoryStock);
        }
    }
}

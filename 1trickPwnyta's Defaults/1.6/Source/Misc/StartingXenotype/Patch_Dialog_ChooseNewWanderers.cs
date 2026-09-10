using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Misc.StartingXenotype
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(Dialog_ChooseNewWanderers))]
    [HarmonyPatch(nameof(Dialog_ChooseNewWanderers.PreOpen))]
    [HarmonyPatchMod("Ludeon.RimWorld.Biotech")]
    public static class Patch_Dialog_ChooseNewWanderers_PreOpen
    {
        public static void Postfix()
        {
            StartingXenotypeUtility.InitializeStartingPawns();
        }
    }

    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(Dialog_ChooseNewWanderers))]
    [HarmonyPatch("DrawPawnList")]
    [HarmonyPatchMod("Ludeon.RimWorld.Biotech")]
    public static class Patch_Dialog_ChooseNewWanderers_DrawPawnList
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            CodeInstruction instruction = instructionsList.Find(i => i.Calls(typeof(StartingPawnUtility).Method(nameof(StartingPawnUtility.AddNewPawn))));
            instruction.operand = typeof(Patch_Dialog_ChooseNewWanderers_DrawPawnList).Method(nameof(AddNewPawn));
            return instructionsList;
        }

        private static void AddNewPawn(int index)
        {
            StartingPawnUtility.AddNewPawn(index);
            StartingXenotypeUtility.InitializeStartingPawn(index);
        }
    }
}

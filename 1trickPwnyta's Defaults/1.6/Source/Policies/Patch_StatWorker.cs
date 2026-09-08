using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Defaults.Policies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(StatWorker))]
    [HarmonyPatch(nameof(StatWorker.ShouldShowFor))]
    public static class Patch_StatWorker_ShouldShowFor
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];
                if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == typeof(Find).Method("get_IdeoManager"))
                {
                    instruction.operand = typeof(PatchUtility_StatWorker).Method(nameof(PatchUtility_StatWorker.IsGameStartedInClassicMode));
                    i++;
                }
                yield return instruction;
            }
        }
    }

    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(StatWorker))]
    [HarmonyPatch(nameof(StatWorker.GetAdditionalOffsetsAndFactorsExplanation))]
    public static class Patch_StatWorker_GetExplanationFinalizePart
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == typeof(Find).Method("get_Scenario"))
                {
                    instruction.opcode = OpCodes.Nop;
                    instruction.operand = null;
                }
                if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == typeof(Scenario).Method(nameof(Scenario.GetStatFactor)))
                {
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchUtility_StatWorker).Method(nameof(PatchUtility_StatWorker.GetScenarioStatFactor)));
                    continue;
                }

                yield return instruction;
            }
        }
    }

    public static class PatchUtility_StatWorker
    {
        public static bool IsGameStartedInClassicMode()
        {
            return Find.World != null && Find.IdeoManager != null && Find.IdeoManager.classicMode;
        }

        public static float GetScenarioStatFactor(StatDef stat)
        {
            return Find.Scenario != null ? Find.Scenario.GetStatFactor(stat) : 1f;
        }
    }
}

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Defaults.Policies
{
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
                if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == DefaultsRefs.m_Find_get_IdeoManager)
                {
                    instruction.operand = DefaultsRefs.m_StatUtility_IsGameStartedInClassicMode;
                    i++;
                }
                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(StatWorker))]
    [HarmonyPatch(nameof(StatWorker.GetExplanationFinalizePart))]
    public static class Patch_StatWorker_GetExplanationFinalizePart
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == DefaultsRefs.m_Find_get_Scenario)
                {
                    instruction.opcode = OpCodes.Nop;
                    instruction.operand = null;
                }
                if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == DefaultsRefs.m_Scenario_GetStatFactor)
                {
                    yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_StatUtility_GetScenarioStatFactor);
                    continue;
                }

                yield return instruction;
            }
        }
    }
}

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Defaults.Storyteller
{
    [HarmonyPatch(typeof(Difficulty))]
    [HarmonyPatch(nameof(Difficulty.ExposeData))]
    public static class Patch_Difficulty_ExposeData
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            bool foundMode = false;

            Label retLabel = il.DefineLabel();
            Label modeLabel = il.DefineLabel();

            List<CodeInstruction> instructionsList = instructions.ToList();
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];
                if (instruction.opcode == OpCodes.Ldarg_0)
                {
                    CodeInstruction nextInstruction = instructionsList[i + 1];
                    if (nextInstruction.opcode == OpCodes.Ldflda && (FieldInfo)nextInstruction.operand == DefaultsRefs.f_Difficulty_anomalyPlaystyleDef)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_object_GetType);
                        yield return new CodeInstruction(OpCodes.Ldtoken, typeof(DifficultySub));
                        yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_Type_GetTypeFromHandle);
                        yield return new CodeInstruction(OpCodes.Beq_S, modeLabel);
                    }
                    if (nextInstruction.opcode == OpCodes.Ldsfld && (FieldInfo)nextInstruction.operand == DefaultsRefs.f_AnomalyPlaystyleDefOf_Standard)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_object_GetType);
                        yield return new CodeInstruction(OpCodes.Ldtoken, typeof(DifficultySub));
                        yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_Type_GetTypeFromHandle);
                        yield return new CodeInstruction(OpCodes.Beq_S, retLabel);
                    }
                }
                if (!foundMode && instruction.opcode == OpCodes.Ldsfld && (FieldInfo)instruction.operand == DefaultsRefs.f_Scribe_mode)
                {
                    instruction.labels.Add(modeLabel);
                    foundMode = true;
                }
                if (instruction.opcode == OpCodes.Ret)
                {
                    instruction.labels.Add(retLabel);
                }

                yield return instruction;
            }
        }
    }
}

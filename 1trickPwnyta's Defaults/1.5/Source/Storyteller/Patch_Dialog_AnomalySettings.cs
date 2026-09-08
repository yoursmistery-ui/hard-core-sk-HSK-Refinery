using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Defaults.Storyteller
{
    // Patched manually in mod constructor
    public static class Patch_Dialog_AnomalySettings_ctor
    {
        public static void Postfix(Difficulty difficulty, ref AnomalyPlaystyleDef ___anomalyPlaystyleDef)
        {
            if (difficulty is DifficultySub)
            {
                ___anomalyPlaystyleDef = DefDatabase<AnomalyPlaystyleDef>.GetNamed(DefaultsSettings.DefaultAnomalyPlaystyle);
            }
        }
    }

    [HarmonyPatch(typeof(Dialog_AnomalySettings))]
    [HarmonyPatch(nameof(Dialog_AnomalySettings.DoWindowContents))]
    public static class Patch_Dialog_AnomalySettings_DoWindowContents
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == DefaultsRefs.m_Difficulty_set_AnomalyPlaystyleDef)
                {
                    instruction.operand = DefaultsRefs.m_Dialog_Storyteller_SetAnomalyPlaystyleDef;
                }

                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(Dialog_AnomalySettings))]
    [HarmonyPatch("DrawPlaystyles")]
    public static class Patch_Dialog_AnomalySettings_DrawPlaystyles
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundScenario = false;
            bool foundStloc2 = false;

            foreach (CodeInstruction instruction in instructions)
            {
                if (!foundScenario && instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == DefaultsRefs.m_Find_get_Scenario)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, DefaultsRefs.f_Dialog_AnomalySettings_difficulty);
                    yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_Dialog_Storyteller_NonStandardAnomalyPlaystylesAllowed);
                    foundScenario = true;
                    continue;
                }
                if (foundScenario && !foundStloc2)
                {
                    if (instruction.opcode == OpCodes.Stloc_2)
                    {
                        yield return instruction;
                        foundStloc2 = true;
                    }
                    continue;
                }

                yield return instruction;
            }
        }
    }
}

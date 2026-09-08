using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Defaults.Storyteller
{
    [HarmonyPatchCategory("Storyteller")]
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
                if (!foundScenario && instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == typeof(Find).Method("get_Scenario"))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchUtility_Dialog_AnomalySettings).Method(nameof(PatchUtility_Dialog_AnomalySettings.NonStandardAnomalyPlaystylesAllowed)));
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

    public static class PatchUtility_Dialog_AnomalySettings
    {
        public static bool NonStandardAnomalyPlaystylesAllowed(AnomalyPlaystyleDef def)
        {
            return Find.WindowStack.IsOpen<Dialog_Storyteller>() || !Find.Scenario.standardAnomalyPlaystyleOnly || def == AnomalyPlaystyleDefOf.Standard;
        }
    }
}

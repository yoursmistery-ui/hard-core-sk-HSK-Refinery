using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Defaults.Misc.TargetTemperature
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(CompTempControl))]
    [HarmonyPatch("<CompGetGizmosExtra>b__14_2")]
    public static class Patch_CompTempControl_CompGetGizmosExtra_b__14_2
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 21f)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchUtility_CompTempControl).Method(nameof(PatchUtility_CompTempControl.GetDefaultTargetTemperature)));
                    continue;
                }

                yield return instruction;
            }
        }
    }

    public static class PatchUtility_CompTempControl
    {
        public static float GetDefaultTargetTemperature(CompTempControl comp)
        {
            if (comp.parent is Building_Heater)
            {
                return Settings.GetValue<float>(Settings.TARGET_TEMP_HEATER);
            }
            else if (comp.parent is Building_Cooler)
            {
                return Settings.GetValue<float>(Settings.TARGET_TEMP_COOLER);
            }
            return 21f;
        }
    }
}

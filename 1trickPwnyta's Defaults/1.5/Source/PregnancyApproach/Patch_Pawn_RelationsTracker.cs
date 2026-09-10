using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Defaults.PregnancyApproach
{
    [HarmonyPatch(typeof(Pawn_RelationsTracker))]
    [HarmonyPatch(nameof(Pawn_RelationsTracker.GetPregnancyApproachForPartner))]
    public static class Patch_Pawn_RelationsTracker
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_0)
                {
                    yield return new CodeInstruction(OpCodes.Ldsfld, typeof(DefaultsSettings).Field(nameof(DefaultsSettings.DefaultPregnancyApproach)));
                    continue;
                }

                yield return instruction;
            }
        }
    }
}

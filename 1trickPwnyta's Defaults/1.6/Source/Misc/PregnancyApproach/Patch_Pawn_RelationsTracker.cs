using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Defaults.Misc.PregnancyApproach
{
    [HarmonyPatchCategory("Misc")]
    [HarmonyPatch(typeof(Pawn_RelationsTracker))]
    [HarmonyPatch(nameof(Pawn_RelationsTracker.GetPregnancyApproachForPartner))]
    [HarmonyPatchMod("Ludeon.RimWorld.Biotech")]
    public static class Patch_Pawn_RelationsTracker
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4_0)
                {
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchUtility_Pawn_RelationsTracker).Method(nameof(PatchUtility_Pawn_RelationsTracker.GetDefaultPregnancyApproach)));
                    continue;
                }

                yield return instruction;
            }
        }
    }

    public static class PatchUtility_Pawn_RelationsTracker
    {
        public static RimWorld.PregnancyApproach GetDefaultPregnancyApproach() => Settings.GetValue<RimWorld.PregnancyApproach>(Settings.PREGNANCY_APPROACH);
    }
}

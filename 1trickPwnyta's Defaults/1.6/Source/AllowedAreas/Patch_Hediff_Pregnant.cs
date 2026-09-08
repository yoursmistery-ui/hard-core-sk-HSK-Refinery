using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Defaults.AllowedAreas
{
    [HarmonyPatchCategory("AllowedAreas")]
    [HarmonyPatch(typeof(Hediff_Pregnant))]
    [HarmonyPatch(nameof(Hediff_Pregnant.DoBirthSpawn))]
    public static class Patch_Hediff_Pregnant
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int index = instructionsList.FirstIndexOf(i => i.operand is MethodInfo info && info == typeof(TaleRecorder).Method(nameof(TaleRecorder.RecordTale)));
            instructionsList.InsertRange(index, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(PatchUtility_Hediff_Pregnant).Method(nameof(PatchUtility_Hediff_Pregnant.SetDefaultAllowedArea)))
            });
            return instructionsList;
        }
    }

    public static class PatchUtility_Hediff_Pregnant
    {
        public static void SetDefaultAllowedArea(Pawn pawn, Pawn mother)
        {
            AllowedAreaUtility.SetDefaultAllowedArea(pawn, mother: mother);
        }
    }
}

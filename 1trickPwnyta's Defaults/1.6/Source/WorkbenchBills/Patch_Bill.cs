using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Defaults.WorkbenchBills
{
    [HarmonyPatchCategory("WorkbenchBills")]
    [HarmonyPatch(typeof(Bill))]
    [HarmonyPatch(nameof(Bill.DoInterface))]
    public static class Patch_Bill
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction lastSuspendedInstruction = instructions.Last(i => i.opcode == OpCodes.Ldfld && (FieldInfo)i.operand == typeof(Bill).Field(nameof(Bill.suspended)));
            lastSuspendedInstruction.opcode = OpCodes.Call;
            lastSuspendedInstruction.operand = typeof(BillUtility).Method(nameof(BillUtility.IsSuspendedOrUnavailable));

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand.ToString() == "SuspendedCaps")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = typeof(PatchUtility_Bill).Method(nameof(PatchUtility_Bill.GetDisabledString));
                }
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 70f)
                {
                    instruction.operand = 80f;
                }
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 140f)
                {
                    instruction.operand = 160f;
                }

                yield return instruction;
            }
        }
    }

    public static class PatchUtility_Bill
    {
        public static string GetDisabledString(this Bill bill)
        {
            return bill.suspended ? "SuspendedCaps" : !bill.recipe.AvailableNow ? "Defaults_UnavailableCaps" : "";
        }
    }
}

using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Defaults.WorldSettings
{
    [HarmonyPatchCategory("World")]
    [HarmonyPatch(typeof(WorldFactionsUIUtility))]
    [HarmonyPatch(nameof(WorldFactionsUIUtility.DoRow))]
    public static class Patch_WorldFactionsUIUtility
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int index = instructionsList.FirstIndexOf(i => i.opcode == OpCodes.Call && i.operand is MethodInfo info && info == typeof(Current).Method("get_Game"));
            Label label = (Label)instructionsList.First(i => i.opcode == OpCodes.Leave_S).operand;
            instructionsList.InsertRange(index, new[]
            {
                new CodeInstruction(OpCodes.Call, typeof(Current).Method("get_Game")),
                new CodeInstruction(OpCodes.Brfalse, label)
            });
            return instructionsList;
        }
    }
}

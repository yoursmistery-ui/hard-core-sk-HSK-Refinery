using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Defaults.UI
{
    [HarmonyPatch(typeof(ThingFilterUI))]
    [HarmonyPatch(nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    public static class Patch_ThingFilterUI
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int readIndex = instructionsList.FindIndex(i => i.opcode == OpCodes.Ldsfld && i.operand is FieldInfo f && f == typeof(ThingFilterUI).Field("viewHeight"));
            instructionsList.InsertRange(readIndex + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, typeof(PatchUtility_ThingFilterUI).Method(nameof(PatchUtility_ThingFilterUI.GetViewHeight)))
            });
            int writeIndex = instructionsList.FindIndex(i => i.opcode == OpCodes.Stsfld && i.operand is FieldInfo f && f == typeof(ThingFilterUI).Field("viewHeight"));
            instructionsList[writeIndex].opcode = OpCodes.Ldsflda;
            instructionsList.InsertRange(writeIndex + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, typeof(PatchUtility_ThingFilterUI).Method(nameof(PatchUtility_ThingFilterUI.SetViewHeight)))
            });
            return instructionsList;
        }
    }

    public static class PatchUtility_ThingFilterUI
    {
        public static float GetViewHeight(float viewHeight, ThingFilterUI.UIState state) => state is UIState_Ext ext ? ext.viewHeight : viewHeight;

        public static void SetViewHeight(float value, ref float viewHeight, ThingFilterUI.UIState state)
        {
            if (state is UIState_Ext ext)
            {
                ext.viewHeight = value;
            }
            else
            {
                viewHeight = value;
            }
        }
    }
}

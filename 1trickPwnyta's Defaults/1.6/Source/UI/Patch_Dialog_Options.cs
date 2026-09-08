using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace Defaults.UI
{
    [HarmonyPatch(typeof(Dialog_Options))]
    [HarmonyPatch("DoModOptions")]
    public static class Patch_Dialog_Options
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int index = instructionsList.FirstIndexOf(i => i.operand is MethodInfo info && info == typeof(WindowStack).Method(nameof(WindowStack.Add)));
            instructionsList.Insert(index, new CodeInstruction(OpCodes.Call, typeof(PatchUtility_Dialog_Options).Method(nameof(PatchUtility_Dialog_Options.GetModSettingsWindow))));
            return instructionsList;
        }
    }

    public static class PatchUtility_Dialog_Options
    {
        public static Window GetModSettingsWindow(Window window)
        {
            if (window is Dialog_ModSettings modSettingsWindow)
            {
                if (typeof(Dialog_ModSettings).Field("mod").GetValue(modSettingsWindow) is DefaultsMod)
                {
                    return new Dialog_MainSettings();
                }
            }
            return window;
        }
    }
}

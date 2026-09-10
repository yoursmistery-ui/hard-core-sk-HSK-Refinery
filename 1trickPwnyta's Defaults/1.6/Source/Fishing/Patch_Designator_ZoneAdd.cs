using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace Defaults.Fishing
{
    [HarmonyPatchCategory("Fishing")]
    [HarmonyPatch(typeof(Designator_ZoneAdd))]
    [HarmonyPatch(nameof(Designator_ZoneAdd.DesignateMultiCell))]
    [HarmonyPatchMod("Ludeon.RimWorld.Odyssey")]
    public static class Patch_Designator_ZoneAdd
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            CodeInstruction[] insertInstructions = new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(Designator_ZoneAdd).PropertyGetter("SelectedZone")),
                new CodeInstruction(OpCodes.Call, typeof(Patch_Designator_ZoneAdd).Method(nameof(InitializeFishingZone)))
            };
            int firstIndex = instructionsList.FindIndex(i => i.Calls(typeof(Zone).Method(nameof(Zone.AddCell))));
            instructionsList.InsertRange(firstIndex + 1, insertInstructions);
            int lastIndex = instructionsList.FindLastIndex(i => i.Calls(typeof(Zone).Method(nameof(Zone.AddCell))));
            instructionsList.InsertRange(lastIndex + 1, insertInstructions);
            return instructionsList;
        }

        private static void InitializeFishingZone(Zone zone)
        {
            if (zone is Zone_Fishing fishingZone)
            {
                FishingUtility.SetDefaultFishingZoneSettings(fishingZone);
            }
        }
    }
}

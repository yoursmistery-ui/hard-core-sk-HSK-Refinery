using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace Defaults.StockpileZones.Buildings
{
    [HarmonyPatchCategory("Storage")]
    [HarmonyPatch(typeof(Blueprint_Storage))]
    [HarmonyPatch(nameof(Blueprint_Storage.PostMake))]
    public static class Patch_Blueprint_Storage
    {
        /* 
         * It would be easy to call SetDefaultBuildingStorageSettings in a postfix, but calling it with a transpiler 
         * at the very end of the method forces this to be executed before any postfixes from other mods that want to 
         * set their own storage settings on the blueprint, thus making it compatible with such mods.
         */
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            instructionsList.InsertRange(instructionsList.Count - 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0)
                {
                    labels = instructionsList.Last().labels.ListFullCopy()
                },
                new CodeInstruction(OpCodes.Call, typeof(Blueprint_Storage).PropertyGetter(nameof(Blueprint_Storage.BuildDef))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, typeof(BuildingUtility).Method(nameof(BuildingUtility.SetDefaultBuildingStorageSettings)))
            });
            instructions.Last().labels.Clear();
            return instructionsList;
        }
    }
}

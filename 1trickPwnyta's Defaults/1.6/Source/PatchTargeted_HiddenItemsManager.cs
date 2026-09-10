using Defaults.UI;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Defaults
{
    [HarmonyPatch]
    public static class PatchTargeted_HiddenItemsManager
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return typeof(Listing_TreeThingFilter).Method("DoCategoryChildren");
            yield return typeof(QuickSearchFilter).Method(nameof(QuickSearchFilter.Matches), new[] { typeof(ThingDef) });
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            instructionsList.RemoveAll(i => i.operand is MethodInfo info && info == typeof(Find).Method("get_HiddenItemsManager"));
            instructionsList.First(i => i.operand is MethodInfo info && info == typeof(HiddenItemsManager).Method(nameof(HiddenItemsManager.Hidden))).operand = typeof(PatchUtility_HiddenItemsManager).Method(nameof(PatchUtility_HiddenItemsManager.ThingIsHidden));
            return instructionsList;
        }
    }

    public static class PatchUtility_HiddenItemsManager
    {
        public static bool ThingIsHidden(ThingDef def) => !Find.WindowStack.IsOpen<Dialog_MainSettings>() && Find.HiddenItemsManager.Hidden(def);
    }
}

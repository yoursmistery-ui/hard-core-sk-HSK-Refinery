using HarmonyLib;
using Verse;

namespace Defaults
{
    [HarmonyPatch(typeof(UIRoot_Entry))]
    [HarmonyPatch(nameof(UIRoot_Entry.Init))]
    public static class Patch_UIRoot_Entry
    {
        public static void Postfix()
        {
            DLCUtility.HandleNewDLCs();
        }
    }
}

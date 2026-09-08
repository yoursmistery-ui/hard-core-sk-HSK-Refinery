using HarmonyLib;
using Verse;

namespace Defaults.Policies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(Dialog_InfoCard.Hyperlink))]
    [HarmonyPatch(nameof(Dialog_InfoCard.Hyperlink.IsHidden), MethodType.Getter)]
    public static class Patch_Dialog_InfoCard_Hyperlink
    {
        public static bool Prefix(ref bool __result)
        {
            if (Current.Game == null || Find.HiddenItemsManager == null)
            {
                __result = false;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}

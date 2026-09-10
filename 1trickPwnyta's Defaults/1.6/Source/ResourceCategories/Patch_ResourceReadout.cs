using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace Defaults.ResourceCategories
{
    [HarmonyPatchCategory("ResourceCategories")]
    [HarmonyPatch(typeof(ResourceReadout))]
    [HarmonyPatch(MethodType.Constructor)]
    public static class Patch_ResourceReadout_ctor
    {
        public static void Postfix()
        {
            foreach (ThingCategoryDef def in DefDatabase<ThingCategoryDef>.AllDefs.Where(c => c.resourceReadoutRoot))
            {
                TreeNodeUtility.DoNode(def.treeNode);
            }
        }
    }
}

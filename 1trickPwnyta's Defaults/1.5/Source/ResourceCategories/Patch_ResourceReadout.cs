using System.Linq;
using Verse;

namespace Defaults.ResourceCategories
{
    // Patched manually in mod constructor
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

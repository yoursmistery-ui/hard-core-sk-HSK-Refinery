using System.Collections.Generic;
using Verse;

namespace Defaults.ResourceCategories
{
    public static class TreeNodeUtility
    {
        public static void DoNode(TreeNode_ThingCategory node)
        {
            node.SetOpen(32, Settings.Get<List<ThingCategoryDef>>(Settings.EXPANDED_RESOURCE_CATEGORIES).Contains(node.catDef));
            foreach (TreeNode_ThingCategory childNode in node.ChildCategoryNodes)
            {
                DoNode(childNode);
            }
        }
    }
}

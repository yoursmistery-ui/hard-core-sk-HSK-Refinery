using Verse;

namespace Defaults.ResourceCategories
{
    public static class TreeNodeUtility
    {
        public static void DoNode(TreeNode_ThingCategory node)
        {
            node.SetOpen(32, DefaultsSettings.DefaultExpandedResourceCategories.Contains(node.catDef.defName));
            foreach (TreeNode_ThingCategory childNode in node.ChildCategoryNodes)
            {
                DoNode(childNode);
            }
        }
    }
}

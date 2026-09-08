using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.ResourceCategories
{
    public class Listing_ResourceCategories : Listing_Tree
    {
        public void DoCategory(TreeNode_ThingCategory node, int nestLevel)
        {
            Rect rect = new Rect(0f, curY, LabelWidth, lineHeight) { xMin = XAtIndentLevel(nestLevel) + 18f };
            Rect rect2 = rect;
            rect2.width = 80f;
            rect2.yMax -= 3f;
            rect2.yMin += 3f;
            GUI.DrawTexture(rect2, SolidColorMaterials.NewSolidColorTexture(new Color(0.1f, 0.1f, 0.1f, 0.6f)));
            if (Mouse.IsOver(rect))
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            Rect rect3 = new Rect(rect);
            rect3.width = (rect3.height = 28f);
            rect3.y = rect.y + rect.height / 2f - rect3.height / 2f;
            GUI.DrawTexture(rect3, node.catDef.icon);
            Rect rect4 = new Rect(rect) { xMin = rect3.xMax + 6f };
            Widgets.Label(rect4, node.catDef.LabelCap);
            bool isEnabled = IsEnabled(node);
            Widgets.Checkbox(new Vector2(rect.width - 24f, curY + 2f), ref isEnabled);
            SetEnabled(node, isEnabled);
            EndLine();
            if (isEnabled)
            {
                DoCategoryChildren(node, nestLevel + 1);
            }
        }

        public void DoCategoryChildren(TreeNode_ThingCategory node, int indentLevel)
        {
            foreach (TreeNode_ThingCategory treeNode_ThingCategory in node.ChildCategoryNodes)
            {
                if (!treeNode_ThingCategory.catDef.resourceReadoutRoot)
                {
                    DoCategory(treeNode_ThingCategory, indentLevel);
                }
            }
        }

        private bool IsEnabled(TreeNode_ThingCategory node)
        {
            return Settings.Get<List<ThingCategoryDef>>(Settings.EXPANDED_RESOURCE_CATEGORIES).Contains(node.catDef);
        }

        private void SetEnabled(TreeNode_ThingCategory node, bool enabled)
        {
            List<ThingCategoryDef> expandedCategories = Settings.Get<List<ThingCategoryDef>>(Settings.EXPANDED_RESOURCE_CATEGORIES);
            if (enabled)
            {
                if (!expandedCategories.Contains(node.catDef))
                {
                    expandedCategories.Add(node.catDef);
                }
            }
            else
            {
                if (expandedCategories.Contains(node.catDef))
                {
                    expandedCategories.Remove(node.catDef);
                    foreach (TreeNode_ThingCategory childNode in node.ChildCategoryNodes)
                    {
                        if (!childNode.catDef.resourceReadoutRoot)
                        {
                            SetEnabled(childNode, false);
                        }
                    }
                }
            }
        }
    }
}

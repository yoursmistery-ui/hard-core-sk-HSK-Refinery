using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ApparelAxisRootFixHSK
{
    // 服饰三轴分类的显示根节点修正。
    //
    // 背景(2026-09-09 服饰分类重整, 见 docs/05_文化服饰异种/服饰分类重整_实施记录.md):
    //   我们给 910 件服饰各挂了 ①穿戴位置 ②用途场景 ③科技档位 三个正交分类,
    //   三根轴 HSK_ApparelPos / HSK_ApparelUse / HSK_ApparelTech 都挂在 Apparel 下,
    //   且每根轴的子树都包含全部 910 件(每件在每根轴下恰有一个叶分类)。
    //
    // 根因(反编译 Assembly-CSharp 确认):
    //   Verse.ThingFilter.RecalculateDisplayRootCategory 会遍历 ThingCategoryNodeDatabase.
    //   allThingCategoryNodes, 在"子树包含全部 allowedDefs"的节点里取**遍历序最后一个**
    //   作为 DisplayRootCategory。带 parentFilter 的筛选窗口(工作台账单的原料选择、
    //   分解/熔化衣物等)用 parentFilter.DisplayRootCategory 当树根 —— 于是三根轴里
    //   声明序最后的 HSK_ApparelTech 劫持了所有此类窗口, 玩家只能看到 6 个科技档叶节点。
    //   存储区筛选(parentFilter 为 null)走 ThingCategoryNodeDatabase.RootNode, 不受影响。
    //
    // 本补丁: postfix RecalculateDisplayRootCategory, 若算出的显示根是三根轴之一,
    //   回退到其公共祖先 Apparel —— 该窗口恢复为显示 五枝(Apparel 原有全部子树),
    //   是原结果的超集, 只损失"自动收窄"的便利, 不损失任何可选内容。
    //   其余情况(只允许某几类服饰的窄过滤)计算结果不在三轴根上, 原样放行。
    //
    // 纯显示层修正, 不改 allowedDefs / 不改存档数据。
    [StaticConstructorOnStartup]
    public static class ApparelAxisRootFix
    {
        private static readonly HashSet<string> AxisRoots = new HashSet<string>
        {
            "HSK_ApparelPos",
            "HSK_ApparelUse",
            "HSK_ApparelTech"
        };

        static ApparelAxisRootFix()
        {
            Harmony harmony = new Harmony("local.hskfixpack.apparelaxisroot");
            MethodInfo target = AccessTools.Method(typeof(ThingFilter), "RecalculateDisplayRootCategory");
            MethodInfo postfix = AccessTools.Method(typeof(ApparelAxisRootFix), "Postfix");
            if (target != null && postfix != null)
            {
                harmony.Patch(target, null, new HarmonyMethod(postfix));
            }
        }

        public static void Postfix(ThingFilter __instance)
        {
            try
            {
                TreeNode_ThingCategory root = __instance.DisplayRootCategory;
                if (root == null || root.catDef == null || !AxisRoots.Contains(root.catDef.defName))
                {
                    return;
                }
                ThingCategoryDef apparel = DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Apparel");
                if (apparel == null || apparel.treeNode == null)
                {
                    return;
                }
                __instance.DisplayRootCategory = apparel.treeNode;
            }
            catch (Exception)
            {
                // 显示层修正失败时保持原行为, 不打扰玩家。
            }
        }
    }
}

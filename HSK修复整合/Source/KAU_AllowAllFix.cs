// Keyz' Allow Utilities 工具栏"全部允许"按钮(并入 HSK 修复整合, HSKFixPack.dll)
//
// 需求: 在 Orders(计划) 设计器栏加一个一键"允许全图物品"的按钮。
// 原 mod 的 "Allow all across map" 只绑在 Home 快捷键 + 右键"禁止"浮动菜单, 工具栏没有对应按钮,
// 其 DLL 里也没有 AllowAll 的 Designator 类。
// 方案: 自定义 Designator, 点击任意格即调用原 mod 的 KeyzAllowUtilities.KeyHandler.AllowAll(map, false, null, excludeCorpses)。
//   - 第三方类型全走反射(AccessTools), 编译期不硬引用 Keyz DLL;
//   - 未装 Keyz 时补丁的 MayRequire 不注入本类, 运行时也走不到; 双保险;
//   - 图标复用原版绿色打勾 UI/Widgets/CheckOn(复选框"选中"勾);
//   - 不绑热键, 避免与原 mod 的 Home 键处理重复触发;
//   - 点击效果: 允许全图所有被禁止的可搬运物品, 固定排除尸体(腐烂实体), 不看 mod 设置。
// 编译: 并入 HSKFixPack.dll(系统 csc 旧编译器, C#5, 禁用 ?. 语法)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KAU_AllowAllFix
{
    public class Designator_KAU_AllowAll : Designator
    {
        public Designator_KAU_AllowAll()
        {
            defaultLabel = "全部允许";
            defaultDesc = "允许全图所有被禁止的物品（尸体/腐烂实体除外）。";
            icon = ContentFinder<Texture2D>.Get("UI/Widgets/CheckOn", true);
            useMouseIcon = true;
        }

        // 点击菜单按钮即执行(选中瞬间跑一次并立刻取消选中), 无需再去地图上点一下。
        public override void Selected()
        {
            base.Selected();
            AllowAllOnMap();
            Find.DesignatorManager.Deselect();
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return true;
        }

        // 单击路径: 点一下即允许全图。
        public override void DesignateSingleCell(IntVec3 c)
        {
            AllowAllOnMap();
        }

        // 拖拽路径: 一次拖拽只执行一次(覆盖基类的逐格循环)。
        public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
        {
            AllowAllOnMap();
        }

        private void AllowAllOnMap()
        {
            try
            {
                Type handler = AccessTools.TypeByName("KeyzAllowUtilities.KeyHandler");
                if (handler == null)
                {
                    Log.Error("[HSKFix] 未找到 KeyzAllowUtilities.KeyHandler，无法执行全部允许。");
                    return;
                }
                MethodInfo allowAll = AccessTools.Method(handler, "AllowAll", new Type[]
                {
                    typeof(Map), typeof(bool), typeof(Def), typeof(bool)
                });
                if (allowAll == null)
                {
                    Log.Error("[HSKFix] 未找到 KeyzAllowUtilities.KeyHandler.AllowAll。");
                    return;
                }

                // 固定 excludeCorpses=true: 允许全图被禁止的可搬运物品, 排除尸体/腐烂实体(不看 mod 设置)。
                allowAll.Invoke(null, new object[] { Map, false, null, true });
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] 全部允许执行失败: " + e);
            }
        }
    }
}

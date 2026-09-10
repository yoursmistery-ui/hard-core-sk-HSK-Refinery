// Dubs Mint Menus 建筑菜单 NRE 防御补丁(DubsMintMenusNreFix, 并入 HSK 修复整合)
//
// 问题: 1.6 打开建筑(Architect)菜单时每帧刷屏:
//   Root level exception in OnGUI(): System.NullReferenceException
//   at DubsMintMenus.MainTabWindow_MayaMenu.jimmyNoMates (Verse.Designator des, System.String v)
//   at ... <RefreshDesignatorCaches>b__2 (Verse.Designator x)
//   ... GizmoGridDrawer / ArchitectCategoryTab.DesignationTabOnGUI
//
// 成因: DMM 在 Settings.wheels(mint 快捷轮)非空时, 每次绘制 gizmo 都调
//   RefreshDesignatorCaches(), 拿 wheel 项去和建筑面板全部 designator 做
//   jimmyNoMates 匹配。jimmyNoMates 无条件解引用:
//     * Designator_Place.PlacingDef.defName   (PlacingDef 为 null 时 NRE)
//     * 非 Place 分支 element/des.GetType()   (null 元素时 NRE)
//   当建筑面板内出现"不规矩"的 designator(如第三方 dropdown 含 null 元素,
//   或 PlacingDef 悬空)即每帧 NRE。实测触发环境含 ArchitectSense /
//   KeyzAllowUtilities 等改装建筑面板/提供特殊 designator 的 mod。
//
// 方案: 给 jimmyNoMates 加 Prefix 做空值防御——对 des==null、PlacingDef==null
//   的 Place、含 null/坏元素 的 Dropdown 直接返回 false(视为不匹配, 跳过原方法),
//   健康对象放行原逻辑。行为等价且不抛异常;保留 mint wheel 全部功能。
// 兼容: 未装 Dubs Mint Menus 时 AccessTools.TypeByName 返回 null, 自动跳过。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DubsMintMenusNreFix
{
    [StaticConstructorOnStartup]
    public static class DubsMintMenusNreFixInit
    {
        // 原版 RimWorld.Designator_Dropdown.elements 为 private 字段(DMM 走 IL
        // 级访问),此处用反射读取以检查坏元素。
        private static readonly FieldInfo ElementsField =
            AccessTools.Field(typeof(Designator_Dropdown), "elements");

        static DubsMintMenusNreFixInit()
        {
            try
            {
                Type maya = AccessTools.TypeByName("DubsMintMenus.MainTabWindow_MayaMenu");
                if (maya == null)
                {
                    return;
                }
                MethodInfo target = AccessTools.Method(
                    maya,
                    "jimmyNoMates",
                    new Type[] { typeof(Designator), typeof(string) });
                if (target == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.dubsmintmenus");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(DubsMintMenusNreFixInit).GetMethod(
                        "Prefix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched DubsMintMenus.MainTabWindow_MayaMenu.jimmyNoMates (null-guard prefix)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] Dubs Mint Menus NRE fix failed: " + e);
            }
        }

        private static bool Prefix(Designator des, string v, ref bool __result)
        {
            // des 本身为 null: 直接判为不匹配, 不执行原方法(原方法会对 null 走
            // GetType() 分支抛 NRE)。
            if (des == null)
            {
                __result = false;
                return false;
            }

            // Dropdown: 逐元素检查 null 元素与 PlacingDef 悬空的 Place 子项。
            Designator_Dropdown dd = des as Designator_Dropdown;
            if (dd != null && ElementsField != null)
            {
                List<Designator> elements = ElementsField.GetValue(dd) as List<Designator>;
                if (elements == null)
                {
                    __result = false;
                    return false;
                }
                for (int i = 0; i < elements.Count; i++)
                {
                    Designator element = elements[i];
                    if (element == null)
                    {
                        __result = false;
                        return false;
                    }
                    Designator_Place ep = element as Designator_Place;
                    if (ep != null && ep.PlacingDef == null)
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            // 普通 Place: PlacingDef 悬空时原方法访问 .defName 抛 NRE。
            Designator_Place place = des as Designator_Place;
            if (place != null && place.PlacingDef == null)
            {
                __result = false;
                return false;
            }

            // 健康对象: 放行原方法。
            return true;
        }
    }
}

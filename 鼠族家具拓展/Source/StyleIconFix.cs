// 鼠族家具拓展Demo - 文化风格菜单图标修复
//
// 背景: 文化风格适配(Patches/文化风格适配.xml)把普通 Wall 挂到原版 StyleCategoryDef
//   Floristian 的 thingDefStyles -> RKFC_Wall_Style(Graphic_Appearances, 鼠族墙纹理)。
//   游戏内已建的风格化墙正常显示鼠族纹理, 但建筑师菜单栏的墙按钮图标仍是原版墙图标,
//   不会随风格替换 —— 这是原版引擎行为(反编译 1.6.4871 确认):
//     - Designator_Build.UpdateIcon 只调用 entDef.GetUIIconForStuff(stuffDef),
//       从不考虑 Ideo 风格;
//     - 建筑师菜单按钮走 Command.DrawIcon(直接画 Command.icon 字段), 只有
//       Designator_Build.DrawIcon(信息卡大图/预建幽灵)才会把风格传给
//       Widgets.DefIcon -> Widgets.ThingIcon -> GetIconFor(带 ThingStyleDef);
//     - ThingStyleDef.ResolveUIIcon: 定义了 uiIconPath 时用该贴图, 否则回退
//       graphic 整张图集材质(= 复杂图案)。
//
// 修法: Harmony 后置补丁 Designator_Build.UpdateIcon —— 风格化墙(及其它风格建筑)的
//   菜单图标改用玩家主 Ideo 对应该建筑的 ThingStyleDef.UIIcon。前提是该风格定义了
//   专用 uiIconPath(即 RKFC_Wall_Style 的 GL_Wall_MenuIcon, 2026-08-14 已生成);
//   无专用图标的风格(如原版风格)保持原样, 避免整图集回退。材料着色由绘制时的
//   IconDrawColor(GetColorForStuff)保留, 按钮图标会随所选材料变色, 与原版行为一致。
//
// 编译(系统 csc, C#5):
//   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:library
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
//     /r:"<RimWorld>\Mods\Harmony\Current\Assemblies\0Harmony.dll"
//     /out:StyleIconFix.dll StyleIconFix.cs
using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using Verse;

namespace RKFC_StyleIconFix
{
    [StaticConstructorOnStartup]
    public static class StyleIconFixInit
    {
        static StyleIconFixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(Designator_Build), "UpdateIcon");
                if (target == null)
                {
                    Log.Error("[RKFC_StyleIconFix] Designator_Build.UpdateIcon not found");
                    return;
                }
                Harmony harmony = new Harmony("local.ratkin.furniture.styleiconfix");
                harmony.Patch(target,
                    postfix: new HarmonyMethod(
                        typeof(StyleIconFixInit).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[RKFC_StyleIconFix] patched Designator_Build.UpdateIcon");
            }
            catch (Exception e)
            {
                Log.Error("[RKFC_StyleIconFix] patch failed: " + e);
            }
        }

        // UpdateIcon 后置: 有专用风格菜单图标的建筑, 把按钮图标换成风格图标。
        private static void Postfix(Designator_Build __instance)
        {
            try
            {
                // 只有风格化 ThingDef(如 Wall)适用; 地形等无风格概念。
                ThingDef ent = Traverse.Create(__instance).Field("entDef").GetValue<ThingDef>();
                if (ent == null)
                {
                    return;
                }
                ThingStyleDef style = __instance.ThingStyleDefNonPreceptSource;
                if (style == null)
                {
                    return;
                }
                // 只换带专用菜单图标的风格(本 mod 的 RKFC_Wall_Style 有 uiIconPath),
                // 无专用图标的风格 UIIcon 会回退整张图集材质, 保持原样不引入复杂图案。
                if (GenText.NullOrEmpty(style.uiIconPath))
                {
                    return;
                }
                Texture2D icon = style.UIIcon;
                if (icon == null)
                {
                    return;
                }
                __instance.icon = icon;
            }
            catch (Exception)
            {
                // 读档/无 Ideo 等边缘情况保持原图标, 不影响游戏。
            }
        }
    }
}

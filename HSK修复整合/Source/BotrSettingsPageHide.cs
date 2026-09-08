// BotrSettingsPageHide.cs — 隐藏边境拓展(BOTR) 的 mod 设置页
//
// 需求(2026-09-05): BOTR 的设置页滑条太多、用户不想配置,由 HSK 修复整合直接定参。
// 做法: ①`SettingsCategory()` 返回 null → 选项里整个标签页消失;
//       ②`DoSettingsWindowContents` 前缀直接跳过 → 即使被其它入口调起也不渲染任何控件。
// 设置值仍从 Config/Mod_边境拓展HSK_BordersOfTheRimMod.xml 读取(已直接写入 battleIntervalDays=5),
// BOTR 其余项保持 Scribe 默认(与用户一直以来的实际游玩状态一致)。要改值改 XML,游戏关闭时操作。
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。第三方类型全程反射,mod 不在场就静默不挂。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace BotrSettingsPageHide
{
    [StaticConstructorOnStartup]
    public static class BspInit
    {
        static BspInit()
        {
            try
            {
                Type t = AccessTools.TypeByName("BordersOfTheRim.BordersOfTheRimMod");
                if (t == null) return;                                   // 没装 BOTR,静默跳过
                Harmony h = new Harmony("local.ratkin.hskfix.botrsettingspagehide");
                MethodBase cat = AccessTools.Method(t, "SettingsCategory");
                if (cat != null)
                {
                    h.Patch(cat, null, new HarmonyMethod(typeof(BspInit), "Cat_Post"), null, null);
                }
                MethodBase draw = AccessTools.Method(t, "DoSettingsWindowContents");
                if (draw != null)
                {
                    h.Patch(draw, new HarmonyMethod(typeof(BspInit), "Draw_Pre"), null, null, null);
                }
            }
            catch (Exception e)
            {
                Log.Error("[BSP] 挂载失败: " + e);
            }
        }

        static void Cat_Post(ref string __result)
        {
            __result = null;                                             // 选项列表里不再显示该页
        }

        static bool Draw_Pre()
        {
            return false;                                                // 双保险:即使被调起也不渲染
        }
    }
}

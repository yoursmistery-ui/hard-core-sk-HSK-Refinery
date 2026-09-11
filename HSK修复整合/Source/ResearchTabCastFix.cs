// 研究标签页强转崩溃修复(2026-09-10)
//
// 现象:点右下角"需要研究项目"警示条 → Root level exception in OnGUI(): System.InvalidCastException
//         at RimWorld.Alert_NeedResearchProject.OnClick() ← Alert.DrawAt ← AlertsReadout.AlertsReadoutOnGUI
// 根因(反编译确认): 原版把研究标签窗口硬转成 RimWorld.MainTabWindow_Research:
//         ((MainTabWindow_Research)MainButtonDefOf.Research.TabWindow).CurTab / .Select(...)
//   而本环境 ResearchTree SK(qwerty19106.researchtreesk)把 Research 主按钮的 tabWindowClass 换成
//   ResearchTreeSK.MainTabWindow_ResearchTree(派生自 MainTabWindow,不是 MainTabWindow_Research),∴ 强转必炸。
//   原版受影响位置:
//     - Alert_NeedResearchProject.OnClick   (本档实测崩溃点)
//     - Alert_NeedAnomalyProject.OnClick    (同类,点"异常研究可用"警示条时炸)
//     - Hyperlink.ActivateHyperlink         (点任何"需要研究 X"蓝色研究链接时炸)
//     - Building_VoidMonolith.OpenActivatedDialog / Dialog_EntityCodex.LeftRect(异象专属,未触发,暂不处理)
//   ResearchTreeSK 自身没有补这些点(全 Mods 目录二进制扫描无 Alert_NeedResearchProject 引用)。
//
// 方案: 三处前缀做原版等价的事——切到研究标签页;只有当窗口确实是 MainTabWindow_Research 时才设 CurTab / Select,
//   换成 SK 研究树时就是"打开研究界面"这一步(SK 树没有标签页概念、也没有 Select API)。
//   超链接只在"纯研究链接"(仅填 researchProject)时接管,其它组合原样交回原版,不改变原版行为。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKFixResearchTabCast
{
    [StaticConstructorOnStartup]
    public static class ResearchTabCastFix
    {
        static ResearchTabCastFix()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.researchtabcast");
                Type holder = typeof(ResearchTabCastFix);
                PatchAlert(harmony, holder, typeof(Alert_NeedResearchProject), "PrefixAlertMain");
                PatchAlert(harmony, holder, typeof(Alert_NeedAnomalyProject), "PrefixAlertAnomaly");
                MethodInfo activate = AccessTools.Method(typeof(Dialog_InfoCard.Hyperlink), "ActivateHyperlink");
                if (activate != null)
                {
                    harmony.Patch(activate, prefix: new HarmonyMethod(
                        holder.GetMethod("PrefixHyperlink", BindingFlags.Static | BindingFlags.NonPublic)));
                }
                // 挂载确认不打日志(用户口径:正式 DLL 零启动噪声),失败仍报 Error
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] ResearchTabCastFix 挂载失败: " + e);
            }
        }

        private static void PatchAlert(Harmony harmony, Type holder, Type alertType, string prefixName)
        {
            MethodInfo target = AccessTools.Method(alertType, "OnClick");
            if (target == null)
            {
                return;
            }
            harmony.Patch(target, prefix: new HarmonyMethod(
                holder.GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic)));
        }

        private static void OpenResearchTab(ResearchTabDef tab)
        {
            Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Research);
            MainTabWindow_Research window = MainButtonDefOf.Research.TabWindow as MainTabWindow_Research;
            if (window != null && tab != null)
            {
                window.CurTab = tab;
            }
        }

        private static bool PrefixAlertMain()
        {
            OpenResearchTab(ResearchTabDefOf.Main);
            return false;
        }

        private static bool PrefixAlertAnomaly()
        {
            OpenResearchTab(DefDatabase<ResearchTabDef>.GetNamedSilentFail("Anomaly"));
            return false;
        }

        private static bool PrefixHyperlink(Dialog_InfoCard.Hyperlink __instance)
        {
            if (__instance.IsHidden || __instance.researchProject == null)
            {
                return true;
            }
            if (__instance.ideo != null || __instance.quest != null || __instance.HasGeneOwnerThing
                || __instance.thing != null || __instance.def != null || __instance.worldObject != null
                || __instance.titleDef != null || __instance.faction != null)
            {
                return true; // 复合链接原样交给原版,不接管
            }
            Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Research);
            MainTabWindow_Research window = MainButtonDefOf.Research.TabWindow as MainTabWindow_Research;
            if (window != null)
            {
                window.Select(__instance.researchProject);
            }
            return false;
        }
    }
}

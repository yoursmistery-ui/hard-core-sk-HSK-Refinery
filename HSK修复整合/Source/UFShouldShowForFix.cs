// 万能发酵桶(Core_SK 内置 UniversalFermenter_SK.dll)StatWorker.ShouldShowFor 补丁空引用修复。
//
// 问题: Hardcore SK 整合的 UniversalFermenter(万能发酵桶)模块自带的 Harmony 补丁
//       UniversalFermenterSK.StatWorker_ShouldShowForPatch 挂在原版 StatWorker.ShouldShowFor 上,
//       其 Prefix 方法体开头直接访问 req.Thing.get_Def(),没有判空。
//       当 InfoCard 以"纯定义模式"(Def-only, req.Thing == null)渲染时——典型场景是
//       研究界面(ResearchTreeSK)点击研究节点解锁的物品缩略图——该 Prefix 抛
//       NullReferenceException,破坏 RimWorld 属性面板 StatsReportUtility 的静态状态
//       (reportStats 列表与游标索引错位),导致此后所有物品详情卡渲染时
//       reportStats[索引] 越界,报 ArgumentOutOfRangeException 并伴随 GUIClip 不平衡,
//       表现为"研究界面点第一个物品正常、第二个及之后全部崩溃、所有物品都这样"。
//       此崩溃与鼠族各 mod 无关(仅因鼠族物品多而被高频触发)。
//
// 方案: 对该 Prefix 方法再打一层守卫前缀——当 req.Thing == null(纯定义模式)时直接跳过
//       UniversalFermenter 的 Prefix 方法体。纯定义模式下没有万能发酵桶实例,其 stat
//       隐藏逻辑本就无意义;req.Thing != null 时完全走原逻辑,不影响万能发酵桶自身
//       详情卡。根治 NRE 与后续雪崩,副作用为零。
//
// 定位方式: AccessTools.TypeByName 字符串定位,不编译期引用 UniversalFermenter_SK.dll,
//       类型/方法不存在时自动跳过(打日志),不影响其他补丁。
//
// 2026-08-23 新增(诊断自 Player.log Ref C14D7D5E NRE + Ref 2B2493B3 ArgumentOutOfRange)。

using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace UFShouldShowForFix
{
    [StaticConstructorOnStartup]
    public static class UFShouldShowForFixInit
    {
        static UFShouldShowForFixInit()
        {
            try
            {
                Type patchType = HarmonyLib.AccessTools.TypeByName("UniversalFermenterSK.StatWorker_ShouldShowForPatch");
                if (patchType == null)
                {
                    Log.Message("[HSKFix] UniversalFermenterSK.StatWorker_ShouldShowForPatch not found, skip");
                    return;
                }
                MethodInfo prefixMethod = patchType.GetMethod("Prefix",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (prefixMethod == null)
                {
                    Log.Message("[HSKFix] UF StatWorker_ShouldShowForPatch.Prefix method not found, skip");
                    return;
                }
                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("local.hskfix.uf_shouldshowfor");
                harmony.Patch(prefixMethod,
                    prefix: new HarmonyLib.HarmonyMethod(
                        typeof(UFShouldShowForFixInit).GetMethod("GuardPrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched UF StatWorker_ShouldShowForPatch.Prefix with null-thing guard");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] UF ShouldShowFor guard patch failed: " + e);
            }
        }

        // 纯定义模式(req.Thing == null, 如研究界面解锁物品详情卡)时返回 false,
        // 跳过 UniversalFermenter 的 Prefix 方法体,避免其解引用 null Thing 抛 NRE。
        // 有实例 Thing(游戏内实际物体,含万能发酵桶自身)时返回 true,走原有逻辑。
        private static bool GuardPrefix(StatRequest req)
        {
            return req.Thing != null;
        }
    }
}

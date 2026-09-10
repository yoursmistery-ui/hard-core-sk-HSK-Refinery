// TechAdvancingScanThrottle.cs — Tech Advancing 全量重扫节流(只跳过"没变化的重复扫")
//
// 依据(2026-09-08 反编译 TechAdvancing.dll + 多会话日志实测):
//   TA_ResearchManager 以 [HarmonyPatch] 挂原版 ResearchManager.ReapplyAllMods 的 Postfix,
//   Postfix 里**无条件**调用 UpdateFinishedProjectCounts() —— 每次研究完成/读档都全量遍历
//   DefDatabase 全部 ResearchProjectDef(HSK 环境数百项), 对每项跑 projectHasTechprintsRecursive
//   (递归查前置树, seenResearchProjDefNames 逐项新建零剪枝), 再对每个 IsFinished 项目调
//   ReapplyAllMods()。日志实测: 每次触发 ~887~1003ms 冻结(15:06/16:04 会话多现, 读档后
//   连续 ReapplyAllMods 时同完成集被反复全扫)。
//
// 修法(前缀指纹跳过, 不改任何计算内容): 计算"完成集指纹" = f(def 总数, 各 IsFinished 项目
//   defName, 完成项数)。指纹未变 → 完成集没变 → store(Total/Finished/nonIgnoredTechs) 与上次
//   等价, 直接 return false 跳过整个原方法; 指纹变了(新研究完成/读档恢复)才放行真扫。
//   读档后连续多次 ReapplyAllMods(同档同完成集)只有第一次真扫, 后续全部 0 成本命中跳过。
//   Config 页直接调 UpdateFinishedProjectCounts 的场景: 完成集未变时 store 本就无需刷新,
//   跳过安全; 完成集变了(手动改科技等)指纹必变, 照常放行。不吞任何真实变化。
//
// 成本: 指纹遍历 AllDefsListForReading 一遍 O(N), 数百次 int 乘加 + 仅对 IsFinished 项做
//   defName 哈希(几十~几百次), 亚毫秒级, 远低于被跳过的那次全量递归重扫。
//
// 门控: AccessTools.TypeByName("TechAdvancing.TA_ResearchManager") 反射定位, 不编译期引用
//   TechAdvancing.dll; 未装 TA 时 TypeByName 返回 null 整体跳过, 零副作用。方法签名变了
//   (无参 static void UpdateFinishedProjectCounts) 匹配失败即跳过, 安全。
//
// 日志前缀 [TechAdvScan]。编译: 并入 HSKFixPack.dll(系统 csc, C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TechAdvancingScanThrottle
{
    [StaticConstructorOnStartup]
    public static class TechAdvancingScanThrottleInit
    {
        private static int lastFp = int.MinValue;      // 上次放行真扫时的完成集指纹
        private static bool firstPass = true;

        static TechAdvancingScanThrottleInit()
        {
            try
            {
                Type taType = AccessTools.TypeByName("TechAdvancing.TA_ResearchManager");
                if (taType == null)
                {
                    Log.Message("[TechAdvScan] TA_ResearchManager not found (Tech Advancing absent) — skip");
                    return;
                }
                MethodInfo method = AccessTools.Method(taType, "UpdateFinishedProjectCounts", Type.EmptyTypes);
                if (method == null)
                {
                    Log.Warning("[TechAdvScan] UpdateFinishedProjectCounts() not found — skip");
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.techadvscanthrottle");
                harmony.Patch(method, prefix: new HarmonyMethod(
                    AccessTools.Method(typeof(TechAdvancingScanThrottleInit), "Prefix")));
                Log.Message("[TechAdvScan] patched TA UpdateFinishedProjectCounts (fingerprint skip)");
            }
            catch (Exception e)
            {
                Log.Warning("[TechAdvScan] patch failed: " + e);
            }
        }

        // 完成集指纹: def 总数 + 完成项数 + 各 IsFinished 项 defName 哈希。
        // 只对 IsFinished 项做字符串哈希(通常远小于全量), 非完成项仅以 Count 参与,
        // 避免每次全量字符串哈希的固定成本。
        private static int ComputeFingerprint()
        {
            List<ResearchProjectDef> all = DefDatabase<ResearchProjectDef>.AllDefsListForReading;
            int h = 17;
            int done = 0;
            for (int i = 0; i < all.Count; i++)
            {
                ResearchProjectDef p = all[i];
                if (p != null && p.IsFinished)
                {
                    h = h * 31 + p.defName.GetHashCode();
                    done++;
                }
            }
            h = h * 31 + all.Count;
            h = h * 31 + done;
            return h;
        }

        private static bool Prefix()
        {
            try
            {
                int fp = ComputeFingerprint();
                if (firstPass)
                {
                    firstPass = false;
                    lastFp = fp;
                    return true;                             // 首次(读档后)必真扫一次建立 store
                }
                if (fp == lastFp)
                {
                    return false;                            // 完成集没变 → store 仍有效, 跳过全量重扫
                }
                lastFp = fp;
                return true;                                 // 有项目完成/取消 → 放行真扫
            }
            catch
            {
                return true;                                 // 任何异常都放行原方法, 保守
            }
        }
    }
}

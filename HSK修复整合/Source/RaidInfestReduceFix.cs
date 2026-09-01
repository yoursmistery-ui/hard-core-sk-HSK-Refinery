// 袭击与虫族威胁点数整体 -30% 修正。
//
// 背景: 整体加强了敌对势力的购枪预算后,袭击(尤其按科研等级爬升的高科技阶段)
//       强度偏高。要求"只降低袭击与虫族,其他威胁保持原样"——机械集群、
//       空投仓、异象事件等如果也降,游戏会失去挑战性。
//
// 挂点(经反编译 1.6.4871 rev590 确认):
//  1. 袭击: 所有袭击事件(人类/部落/机械族袭击,以及 Core_SK 自定义的
//     PortalRaid / EnemyShipPart / Zeon 等)最终都在
//     IncidentWorker_Raid.TryGenerateRaidInfo 里调用
//     IncidentWorker_Raid.AdjustedRaidPoints(points, ...) 得到实际袭击点数,
//     再据此生成 Pawn 与战利品。→ Postfix 把返回值 ×0.7。
//     机械集群(IncidentWorker_MechCluster)、空投仓等其它威胁不经过此方法,
//     天然不受影响,无需额外判断。
//  2. 虫族: 地表虫族 IncidentWorker_Infestation 与污染包虫族
//     IncidentWorker_WastepackInfestation 的 TryExecuteWorker 直接读
//     parms.points 决定隧道/虫茧数量。→ Prefix 提前把 parms.points ×0.7。
//     触发判定(CanFireNowSub)仍用原点数,事件照常触发,仅规模降低。
//     深钻虫族(DeepDrillInfestation)不走点数(固定单隧道),跳过。
//
// 兼容性: Core_SK 仅 transpiler 了 StorytellerUtility.DefaultThreatPointsNow
//         (HSK 威胁点数基础计算),未 patch AdjustedRaidPoints / 虫族 worker,
//         本补丁与其无冲突。乘 0.7 后 AdjustedRaidPoints 底部仍有
//         Max(points, MinimumPoints*1.05) 最低点保护,小袭击不会被压过线。
//
// 2026-08-23 新增。
using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace RaidInfestReduceFix
{
    [StaticConstructorOnStartup]
    public static class RaidInfestReduceFixInit
    {
        // 0.7 = 整体降低 3 成。
        private const float Multiplier = 0.7f;

        static RaidInfestReduceFixInit()
        {
            try
            {
                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("local.hskfix.raid_infest_reduce");

                // 袭击: AdjustedRaidPoints 返回最终袭击点数,Postfix 乘 0.7。
                MethodInfo adjusted = HarmonyLib.AccessTools.Method(
                    typeof(IncidentWorker_Raid), "AdjustedRaidPoints");
                if (adjusted == null)
                {
                    Log.Error("[HSKFix] RaidInfestReduce: IncidentWorker_Raid.AdjustedRaidPoints not found");
                }
                else
                {
                    harmony.Patch(adjusted,
                        postfix: new HarmonyLib.HarmonyMethod(
                            typeof(RaidInfestReduceFixInit).GetMethod("RaidPointsPostfix",
                                BindingFlags.Static | BindingFlags.NonPublic)));
                }

                // 虫族: 地表虫族 + 污染包虫族,Prefix 把 parms.points 乘 0.7。
                PatchInfestationWorker<IncidentWorker_Infestation>(harmony);
                PatchInfestationWorker<IncidentWorker_WastepackInfestation>(harmony);

                Log.Message("[HSKFix] raid & infestation threat points x0.7 applied");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] RaidInfestReduce patch failed: " + e);
            }
        }

        private static void PatchInfestationWorker<T>(HarmonyLib.Harmony harmony) where T : IncidentWorker
        {
            MethodInfo m = HarmonyLib.AccessTools.Method(typeof(T), "TryExecuteWorker");
            if (m == null)
            {
                Log.Error("[HSKFix] RaidInfestReduce: " + typeof(T).Name + ".TryExecuteWorker not found");
                return;
            }
            harmony.Patch(m,
                prefix: new HarmonyLib.HarmonyMethod(
                    typeof(RaidInfestReduceFixInit).GetMethod("InfestationPointsPrefix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
        }

        // 袭击点数返回值乘 0.7。
        private static void RaidPointsPostfix(ref float __result)
        {
            __result *= Multiplier;
        }

        // 虫族点数乘 0.7(IncidentParms 是引用类型,直接改字段对原方法生效)。
        private static void InfestationPointsPrefix(IncidentParms parms)
        {
            if (parms != null)
            {
                parms.points *= Multiplier;
            }
        }
    }
}

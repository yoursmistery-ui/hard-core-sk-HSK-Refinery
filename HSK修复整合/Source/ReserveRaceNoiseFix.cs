// 良性预留竞态日志抑制(2026-08-16 用户反馈日志:
//   "Could not reserve Thing_Seed_Cotton4246621(current stack count: 1) (layer: null) for 茑萝
//    for job SowWithSeeds ... Existing reservers: [0] 欧石楠 (job: SowWithSeeds ...)")
//
// 根因(反编译 1.6.4871 + SeedsPlease Seeds.dll 全链确认):
//   SeedsPlease 播种(SowWithSeeds → SeedsPlease.JobDriver_PlantSowWithSeeds)的种子(B)预留
//   发生在任务开始后(ReserveSeedsIfWillPlantWholeStack 及拾取时 Pawn_CarryTracker.TryStartCarry
//   → ReservationUtility.Reserve errorOnFailed=true),而 WorkGiver_GrowerSowWithSeeds 选种子时
//   只做 CanReserve 快照检查——两个殖民者同一 tick 内都被分配播种时会指向同一包种子,
//   先到者预留成功,后到者预留失败 → 原版 ReservationManager.LogCouldNotReserveError 打
//   Log.Error(带 "Existing reservers:" 清单)。任务以 Incompletable 结束、小人重新评估,
//   可自愈,纯日志噪音。同类竞态在 CE 及其它"多人抢同一物品"场景也会出现。
//
// 修复: Harmony 前缀挂 Verse.AI.ReservationManager.LogCouldNotReserveError——仅当存在
//   "阻塞性预留者"(同 target+layer 且 RespectsReservationsOf 成立,即原方法会列出
//   "Existing reservers" 的情形,意味着失败原因就是"别人先占了")时返回 false 跳过原方法
//   (静默)。若没有任何阻塞性预留者(失败原因为目标无效/被禁用/不可达等真实问题),
//   照常放行报错。门控逻辑与原方法内部的 existing-reserver 判定完全一致。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ReserveRaceNoiseFix
{
    [StaticConstructorOnStartup]
    public static class ReserveRaceNoiseFixInit
    {
        static ReserveRaceNoiseFixInit()
        {
            try
            {
                MethodInfo logCouldNotReserve = AccessTools.Method(
                    typeof(ReservationManager), "LogCouldNotReserveError",
                    new Type[] { typeof(Pawn), typeof(Job), typeof(LocalTargetInfo), typeof(int), typeof(int), typeof(ReservationLayerDef) });
                if (logCouldNotReserve == null)
                {
                    Log.Warning("[HSKFix] ReservationManager.LogCouldNotReserveError not found");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.reserverace");
                harmony.Patch(
                    logCouldNotReserve,
                    prefix: new HarmonyMethod(
                        typeof(ReserveRaceNoiseFixInit).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));

                Log.Message("[HSKFix] patched ReservationManager.LogCouldNotReserveError: benign reservation races (target held by another pawn) are silent");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] reservation race fix failed: " + e);
            }
        }

        private static bool Prefix(ReservationManager __instance, Pawn claimant, LocalTargetInfo target, ReservationLayerDef layer)
        {
            System.Collections.Generic.List<ReservationManager.Reservation> list = __instance.ReservationsReadOnly;
            for (int i = 0; i < list.Count; i++)
            {
                ReservationManager.Reservation r = list[i];
                if (r.Target == target && (layer == null || r.Layer == layer) && RespectsReservationsOf(claimant, r.Claimant))
                {
                    // 另一小人已占该 target+layer → 良性竞态,原方法只会打错误,跳过。
                    return false;
                }
            }
            // 无阻塞性预留者 → 失败另有原因(目标无效/禁用/不可达等),保持报错。
            return true;
        }

        // 复刻 1.6.4871 私有静态 ReservationManager.RespectsReservationsOf(Pawn, Pawn)。
        private static bool RespectsReservationsOf(Pawn newClaimant, Pawn oldClaimant)
        {
            if (newClaimant == oldClaimant)
            {
                return true;
            }
            if (newClaimant.Faction == null || oldClaimant.Faction == null)
            {
                return false;
            }
            if (newClaimant.Faction == oldClaimant.Faction)
            {
                return true;
            }
            if (!newClaimant.Faction.HostileTo(oldClaimant.Faction))
            {
                return true;
            }
            if (oldClaimant.HostFaction != null && oldClaimant.HostFaction == newClaimant.HostFaction)
            {
                return true;
            }
            if (newClaimant.HostFaction != null)
            {
                if (oldClaimant.HostFaction != null)
                {
                    return true;
                }
                if (newClaimant.HostFaction == oldClaimant.Faction)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

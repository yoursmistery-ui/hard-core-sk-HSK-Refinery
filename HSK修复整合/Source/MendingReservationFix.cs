// Mending mod (Core_SK_MendAndRecycle, notfood.MendAndRecycle) 预留释放错位日志修复
// (2026-08-16 用户反馈日志: "Tried to release Thing_RK_Muffler4079577 that wasn't reserved by 松茸")
//
// 根因(反编译 1.6.4871 + Mending.dll 全链确认):
//   Mending 的 JobDriver_Recycle/JobDriver_Mend 在 DoBill() 的 tickAction 里手动管理
//   被处理物品的预留:
//       ... if (objectThing.HitPoints <= 0) {
//             reservationManager.Release(job.targetB, pawn, job);  // 释放自己占的预留
//             objectThing.Destroy(); ...
//       }
//   但同一 tickAction 里还有两处先决检查:
//       if (objectThing == null || objectThing.Destroyed) EndCurrentJob(...);
//       if (!tableThing.UsableForBillsAfterFueling()) EndCurrentJob(...);
//   EndCurrentJob → JobDriver 清理 → ReleaseAllClaimedBy(pawn) 把该小人全部预留释放掉,
//   且这两处 EndCurrentJob **没有 return**,tickAction 继续执行到本 tick 末的
//   Release(job.targetB, ...): 此时物品还活着,但预留已被清理 → 1.6 的
//   ReservationManager.Release 走到 `reservation==null && !target.ThingDestroyed` 分支,
//   Log.Error("Tried to release X that wasn't reserved by Y")。
//   物品随后照样被 Destroy+回收(该分支原方法只打日志、不删任何东西——要找的预留本就不存在),
//   因此这是纯日志噪音,无功能后果。
//
// 修复: Harmony 前缀挂 Verse.AI.ReservationManager.Release(LocalTargetInfo, Pawn, Job),
//   仅当 ①当前作业是 Mending 的 Mend/Recycle(job.jobDef.driverClass 命中)且
//   ②该 (target, claimant, job) 三组确实无预留时,返回 false 跳过原方法(等价于静默);
//   其余情况(预留存在→正常释放、其它 mod/任务→不干预)一律放行原方法。
//   不误伤其它调用: 跳过分支与原方法的唯一差异就是少打这条错误。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace MendingReservationFix
{
    [StaticConstructorOnStartup]
    public static class MendingReservationFixInit
    {
        private static Type recycleDriver;
        private static Type mendDriver;

        static MendingReservationFixInit()
        {
            try
            {
                recycleDriver = AccessTools.TypeByName("Mending.JobDriver_Recycle");
                mendDriver = AccessTools.TypeByName("Mending.JobDriver_Mend");
                if (recycleDriver == null && mendDriver == null)
                {
                    // Mending 未安装,无需修复。
                    return;
                }

                MethodInfo release = AccessTools.Method(
                    typeof(ReservationManager), "Release",
                    new Type[] { typeof(LocalTargetInfo), typeof(Pawn), typeof(Job) });
                if (release == null)
                {
                    Log.Warning("[HSKFix] ReservationManager.Release(LocalTargetInfo, Pawn, Job) not found");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.mendingreservation");
                harmony.Patch(
                    release,
                    prefix: new HarmonyMethod(
                        typeof(MendingReservationFixInit).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));

                Log.Message("[HSKFix] patched ReservationManager.Release: Mending mend/recycle reservation-desync log suppressed");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] Mending reservation fix failed: " + e);
            }
        }

        private static bool Prefix(ReservationManager __instance, LocalTargetInfo target, Pawn claimant, Job job)
        {
            if (claimant == null || job == null || job.def == null)
            {
                return true;
            }

            Type driver = job.def.driverClass;
            if (driver != recycleDriver && driver != mendDriver)
            {
                return true;
            }

            // 预留确实存在 → 让原方法正常释放。
            if (__instance.ReservedBy(target, claimant, job))
            {
                return true;
            }

            // 预留不存在 → 原方法只会打 "wasn't reserved" 错误(或 "Releasing destroyed thing"),
            // 跳过即静默该噪音,不改变任何状态。
            return false;
        }
    }
}

// 任务异常恢复日志落盘(2026-08-16 用户反馈「万代兰 started 10 jobs in one tick」风暴)
//
// 背景: 2026-08-16 日志尾部 7 条 "started 10 jobs in one tick" 全指向同一个
//   RK_HandTailoringBench(鼠族手工裁缝台)DoBill 任务,多小人(万代兰/七色堇/米莉安)
//   循环启动失败任务。原始异常本应由 vanilla TryStartErrorRecoverJob 的 Log.Error 打出,
//   但游戏在该风暴中途异常关闭,Player.log 尾部乱序/缓冲丢失,异常没落盘 → 无法定位根因。
//
// 方案: Harmony 前缀挂 Verse.AI.JobUtility.TryStartErrorRecoverJob(该方法是任务 driver
//   抛异常的唯一入口,异常参数非 null 即真实根因),把 message + 完整异常追加写入
//   `<persistentDataPath>/JobErrorRecover.log`(即 Player.log 同目录),文件即时落盘,
//   即使游戏随后崩溃/强杀也不丢失。纯诊断,不改变任何游戏行为。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI;

namespace JobErrorRecoverLog
{
    [StaticConstructorOnStartup]
    public static class JobErrorRecoverLogInit
    {
        private static string LogPath;

        static JobErrorRecoverLogInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(JobUtility), "TryStartErrorRecoverJob",
                    new Type[] { typeof(Pawn), typeof(string), typeof(Exception), typeof(JobDriver) });
                if (target == null)
                {
                    Log.Warning("[HSKFix] JobUtility.TryStartErrorRecoverJob not found");
                    return;
                }

                LogPath = Path.Combine(Application.persistentDataPath, "JobErrorRecover.log");

                Harmony harmony = new Harmony("local.hskfixpack.joberrorrecoverlog");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(JobErrorRecoverLogInit).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));

                Log.Message("[HSKFix] patched JobUtility.TryStartErrorRecoverJob: recover exceptions written to " + LogPath);
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] job error recover log fix failed: " + e);
            }
        }

        private static void Prefix(Pawn pawn, string message, Exception exception, JobDriver concreteDriver)
        {
            try
            {
                string line = string.Format(
                    "[{0:yyyy-MM-dd HH:mm:ss}] {1}: {2}\n{3}\n\tcurrentJob={4}\n\n",
                    DateTime.Now,
                    (pawn != null) ? pawn.ToStringSafe() : "null",
                    message,
                    (exception != null) ? exception.ToString() : "(no exception)",
                    (pawn != null && pawn.jobs != null && pawn.jobs.curJob != null) ? pawn.jobs.curJob.ToStringSafe() : "null");
                File.AppendAllText(LogPath, line);
            }
            catch (Exception)
            {
                // 日志写入失败不影响游戏。
            }
        }
    }
}

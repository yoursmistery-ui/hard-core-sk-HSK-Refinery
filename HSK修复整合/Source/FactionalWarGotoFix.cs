// Factional War 追击 Goto 死循环修复(2026-08-25 用户授权)
//
// 背景: Factional War Continued(SR.ModRimworld.FactionalWarContinued)的
// JobGiverAIGotoNearestHostileFactionMember.TryGiveJob 发 Goto 任务时不校验目标可达性。
// 当目标不可达(墙内/跨地图/已死)时,Common Sense 的 GoToCellSafe 补丁在任务开跑的
// 同一 tick 立即判失败 → 任务结束 → ThinkNode 重新找任务 → 同一 giver 又返回同一 Goto
// → 同 tick 内循环 10 次 → 引擎抛 "started 10 jobs in one tick"(2026-08-25 日志,
// 蝾螈 10-jobs,栈顶 jobGiver=SR.ModRimWorld.FactionalWar.JobGiverAIGotoNearestHostileFactionMember)。
//
// 方案: 给 TryGiveJob 打 Postfix: 返回的 Job 目标已销毁/已死/跨地图/不可达时覆盖为 null,
// 让 ThinkNode 落到下一个 giver(JobGiverAISapper 破墙等)或待机,不再反复刷 Goto。
// 判定与 CommonSense 的 GoToCellSafe 一致,但把"不可达"提前到 job 发放前,
// 消除 StartJob 后立即失败导致的同 tick 循环。倒地目标不拦(仍需走过去处决)。
// 用 AccessTools.TypeByName 运行时定位,不直接引用 FactionalWar.dll(workshop mod,
// 更新不被覆盖;补丁放 HSK修复整合 里永续生效)。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FactionalWarGotoFix
{
    [StaticConstructorOnStartup]
    public static class FactionalWarGotoFixInit
    {
        private static int lastLogTick = -99999;

        static FactionalWarGotoFixInit()
        {
            try
            {
                Type giverType = AccessTools.TypeByName("SR.ModRimWorld.FactionalWar.JobGiverAIGotoNearestHostileFactionMember");
                if (giverType == null)
                {
                    return; // Factional War 未装,跳过
                }
                MethodInfo tryGiveJob = AccessTools.Method(giverType, "TryGiveJob");
                if (tryGiveJob == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.factionalwargotofix");
                harmony.Patch(tryGiveJob, postfix: new HarmonyMethod(
                    typeof(FactionalWarGotoFixInit).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[FactionalWarGotoFix] patched JobGiverAIGotoNearestHostileFactionMember.TryGiveJob");
            }
            catch (Exception e)
            {
                Log.Error("[FactionalWarGotoFix] patch failed: " + e);
            }
        }

        private static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result == null || pawn == null || pawn.Map == null)
            {
                return;
            }
            try
            {
                LocalTargetInfo target = __result.targetA;
                bool bad = false;
                if (target.HasThing)
                {
                    Thing t = target.Thing;
                    if (t == null || t.Destroyed || t.Map != pawn.Map)
                    {
                        bad = true;
                    }
                    else
                    {
                        Pawn tp = t as Pawn;
                        if (tp != null && tp.Dead)
                        {
                            bad = true;
                        }
                    }
                    if (!bad && !pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly))
                    {
                        bad = true;
                    }
                }
                else if (target.IsValid && !pawn.CanReach(target.Cell, PathEndMode.Touch, Danger.Deadly))
                {
                    bad = true;
                }
                if (bad)
                {
                    __result = null;
                    // 节流日志(每 5 秒最多一条,防刷屏)
                    if (Find.TickManager != null && Find.TickManager.TicksGame - lastLogTick > 300)
                    {
                        lastLogTick = Find.TickManager.TicksGame;
                        Log.Message("[FactionalWarGotoFix] suppressed unreachable Goto job for " + pawn.LabelShort);
                    }
                }
            }
            catch (Exception e)
            {
                // 判定失败保持原 job 继续原逻辑,不拦截
                Log.Warning("[FactionalWarGotoFix] check failed: " + e);
            }
        }
    }
}

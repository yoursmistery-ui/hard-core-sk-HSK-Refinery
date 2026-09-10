// Factional War 追击 Goto 死循环修复(2026-08-25 用户授权;2026-09-04 补强)
//
// 背景: Factional War Continued(SR.ModRimWorld.FactionalWarContinued)的
// JobGiverAIGotoNearestHostileFactionMember.TryGiveJob 发 Goto 任务时不校验目标可达性,
// 也不校验"是否已经到位"。两类自旋:
//   A. 目标不可达(墙内/跨地图/已死):Common Sense 的 GoToCellSafe 在开跑同 tick 判失败
//      → 结束 → ThinkNode 重找 → 同一 giver 又返回同一 Goto → 同 tick 循环 10 次。
//   B. 目标可达且存活,但发单者已到位(同格/相邻):Goto 的 MoveTo toil 当 tick 立即
//      PatherArrived 完成 → 结束 → 重发同一 Goto → 同 tick 循环 10 次。
//      (2026-09-04 日志:克劳卡/河马/Ol/鲸 对 Thing_Human 刷 "started 10 jobs in one tick",
//       栈顶全是 Notify_PatherArrived,旧版只拦 A 类,故 suppressed 一条未打、循环照旧。)
//
// 方案(Postfix 三重拦截,任一命中即把 __result 覆盖为 null,让 ThinkNode 落到下一 giver 或待机):
//   1) 目标失效/跨图/已死/不可达 —— A 类。
//   2) 已与目标同格或相邻(DistanceTo<=1)—— B 类稳态,再发 Goto 只会当 tick 秒到空转。
//   3) 同一 tick 内对同一目标重复放行 —— B 类兜底(Common Sense 让非相邻目标也秒到的情形),
//      直接切断 10-jobs 自旋。用按 thingID 索引的当 tick 记忆表,换 tick 即清空,不跨 tick 持有引用。
//   4) 跨 tick 换工作冷却(2026-09-04 用户要求) —— 放行一次 Goto 后,同一 pawn 在
//      CooldownTicks(=300,约 5 秒游戏时间)内不再被这个 giver 发新 job,防止 ThinkTree 每 tick
//      重评估导致的"高频换工作/刷新"。lastGiveTick 只存 thingID→tick(纯 int,无对象引用),
//      低频扫描清过期项,字典不会无界增长。倒地目标不拦(仍需走过去处决)。用 AccessTools.TypeByName 运行时定位,不直接引用
//      FactionalWar.dll(workshop mod,更新不被覆盖;补丁放 HSK修复整合 里永续生效)。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Collections.Generic;
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

        // 当 tick 放行记忆:key = pawn.thingIDNumber。换 tick 全清,只保留本 tick 记录,
        // 因此绝不跨 tick 持有 Thing/Pawn 引用(无泄漏)。
        private class RecentGoto
        {
            public bool isThing;
            public Thing target;
            public IntVec3 cell;
        }

        private static readonly Dictionary<int, RecentGoto> recent = new Dictionary<int, RecentGoto>();
        private static int lastTick = -1;

        // 跨 tick 换工作冷却:同一 pawn 放行一次后 CooldownTicks 内不再发新 job。
        // 只存 thingID→tick(纯 int,无对象引用),低频扫描清过期项。
        private const int CooldownTicks = 300;
        private static readonly Dictionary<int, int> lastGiveTick = new Dictionary<int, int>();
        private static int lastPruneTick = -99999;
        private static readonly List<int> staleBuffer = new List<int>();

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
                int ticks = (Find.TickManager != null) ? Find.TickManager.TicksGame : -1;
                if (ticks != lastTick)
                {
                    recent.Clear();
                    lastTick = ticks;
                }

                LocalTargetInfo target = __result.targetA;
                bool bad = false;

                // 1) 不可达 / 已死 / 跨图 / 已销毁
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

                // 2) 已到位(同格/相邻):Goto 当 tick 秒到,再发无意义
                if (!bad && target.HasThing)
                {
                    if (pawn.Position.DistanceTo(target.Thing.Position) <= 1)
                    {
                        bad = true;
                    }
                }

                // 3) 同 tick 对同一目标已放行过 → 这就是 10-jobs 自旋,拦
                if (!bad)
                {
                    RecentGoto prev;
                    if (recent.TryGetValue(pawn.thingIDNumber, out prev))
                    {
                        bool same = false;
                        if (target.HasThing && prev.isThing && prev.target == target.Thing)
                        {
                            same = true;
                        }
                        else if (!target.HasThing && !prev.isThing && prev.cell == target.Cell)
                        {
                            same = true;
                        }
                        if (same)
                        {
                            bad = true;
                        }
                    }
                }

                if (bad)
                {
                    __result = null;
                    // 节流日志(每 5 秒最多一条,防刷屏)
                    if (Find.TickManager != null && ticks - lastLogTick > 300)
                    {
                        lastLogTick = ticks;
                        Log.Message("[FactionalWarGotoFix] suppressed self-spinning/unreachable Goto job for " + pawn.LabelShort);
                    }
                }
                else
                {
                    // 4) 跨 tick 换工作冷却:距上次放行 < CooldownTicks 拒绝再发,防高频刷新
                    int lastT;
                    if (ticks >= 0 && lastGiveTick.TryGetValue(pawn.thingIDNumber, out lastT)
                        && ticks - lastT < CooldownTicks)
                    {
                        __result = null;
                        if (Find.TickManager != null && ticks - lastLogTick > 300)
                        {
                            lastLogTick = ticks;
                            Log.Message("[FactionalWarGotoFix] 300t job-change cooldown suppressed re-issue for " + pawn.LabelShort);
                        }
                        return;
                    }

                    // 放行:记录本 tick(冷却起点)+ 当 tick 记忆
                    if (ticks >= 0)
                    {
                        lastGiveTick[pawn.thingIDNumber] = ticks;
                    }
                    RecentGoto rg = new RecentGoto();
                    rg.isThing = target.HasThing;
                    rg.target = target.HasThing ? target.Thing : null;
                    rg.cell = target.HasThing ? target.Thing.Position : target.Cell;
                    recent[pawn.thingIDNumber] = rg;
                    PruneLastGive(ticks);
                }
            }
            catch (Exception e)
            {
                // 判定失败保持原 job 继续原逻辑,不拦截
                Log.Warning("[FactionalWarGotoFix] check failed: " + e);
            }
        }

        // 低频清理冷却表:最多每 ~1800 tick 扫一次(远大于 300 冷却),移除已过期项,
        // 保证字典不无界增长;绝不每 tick 遍历。
        private static void PruneLastGive(int ticks)
        {
            if (ticks - lastPruneTick < 1800)
            {
                return;
            }
            lastPruneTick = ticks;
            if (lastGiveTick.Count == 0)
            {
                return;
            }
            staleBuffer.Clear();
            foreach (KeyValuePair<int, int> kv in lastGiveTick)
            {
                if (ticks - kv.Value > CooldownTicks)
                {
                    staleBuffer.Add(kv.Key);
                }
            }
            for (int i = 0; i < staleBuffer.Count; i++)
            {
                lastGiveTick.Remove(staleBuffer[i]);
            }
        }
    }
}

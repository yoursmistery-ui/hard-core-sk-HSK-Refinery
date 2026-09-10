// PathFinderThrottle.cs — 原版 1.6 多线程寻路 PathFinderTick 的"削峰限流"
//
// 依据(2026-09-09 PerfSnap v4 MapTick 分解段实锤):
//   MapPreTick 内 PathFinder.PathFinderTick 在**大规模寻路突发**(战斗/袭击波入场/框选大队移动)
//   时单 tick 会一次性消化 workQueue 里全部到期请求:
//     ComputeWorkThisTick() 把 "TickStart <= now" 的请求全部取出(无上限)
//     → GatherData + ScheduleGridJobs + ScheduleBatchedPathJobs 主线程全量打包
//     → 下一 tick 的 ForceCompleteScheduledJobs 还要阻塞等这批并行 job 全部算完。
//   实测 18:46 战斗局: PathFinderTick 平均 2906us/tick(全窗口 5713ms/1966tick),
//   单次 max = 3203ms(游戏冻结 3.2 秒!)——与 MapPreTick 1202ms / MapPostTick 1652ms /
//   AM.AnimationManager 1652ms 的尖峰同步出现, 即"灾难卡顿"的根。
//   平静局(18:47)同条目仅 9.4us/tick → 该成本 100% 是突发负载税, 不是设计浪费。
//
// 修法(纯削峰, 不碰寻路正确性/不碰 worker 线程): patch ComputeWorkThisTick 的 Postfix,
//   把一次取出的请求数限制到 MaxPerTick(默认 32):
//   - 超出的请求移回 workQueue, 并把私有 tickStart 顺延到后续 tick(每批 MaxPerTick 个顺延 1 tick),
//     使雪崩请求被"摊平"到多个 tick 消化 —— 单 tick 打包/等待量封顶。
//   - 平静时队列几乎为空(远低于上限), Postfix 只做一次 Count 比较即返回, 零影响。
//   - Pawn_PathFollower 在 curPathRequest 未就绪时只是原地等待(TryGetPath false → 本 tick 不动),
//     不会每 tick 重发请求 → 限流不会引发队列雪崩或丢请求, 只是路径最多晚几 tick 就绪。
//   - 寻路总吞吐不变(请求只是延后处理), 但单 tick 主线程开销从"全部+等待"降到"32 个", 
//     秒级冻结(1.2~3.2s)被摊成每 tick 亚毫秒级。
//
// 代价(用户"慢一点可接受"范围内): 战斗极端雪崩(数百请求同 tick 涌入)时, 末位 pawn 拿到路径
//   最多延迟 数量/32 tick(300 请求 ≈ 10 tick ≈ 0.17s@1x)。单 tick 请求持续超过 32/tick 的
//   超大规模战斗下, 寻路整体会排队变慢——但不会再出现单次数秒的整游戏冻结。
//
// 热开关(免重新编译): Config 目录放 PathFinderThrottle.off 即整体禁用(每 300 tick 探测)。
// 日志前缀 [PathFinderThrottle]。编译: 并入 HSKFixPack.dll(系统 csc, C#5)。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace PathFinderThrottle
{
    [StaticConstructorOnStartup]
    public static class PathFinderThrottleInit
    {
        // 每 tick 最多放行进并行管线的寻路请求数。超过的顺延后续 tick。
        // 32 ≈ 单 tick 打包+等待 ~0.7-1ms 的量级(参考 96 请求/tick ≈ 2.9ms 的实测线性外推)。
        // 想更保守(更少卡顿, 寻路更慢)调小; 想更激进调大。
        private const int MaxPerTick = 32;
        // 热开关与日志探测间隔
        private const int ProbeInterval = 300;
        // 热开关文件路径 = <Config>/PathFinderThrottle.off
        private static readonly string OffFile = Path.Combine(GenFilePaths.ConfigFolderPath, "PathFinderThrottle.off");

        private static bool mounted;
        private static bool disabled;
        private static int probeTick = -99999;

        private static readonly FieldInfo fWorkQueue = AccessTools.Field(typeof(PathFinder), "workQueue");
        private static readonly FieldInfo fTmpWork = AccessTools.Field(typeof(PathFinder), "tmpCurrentWork");
        private static readonly FieldInfo fTickStart = AccessTools.Field(typeof(PathRequest), "tickStart");

        static PathFinderThrottleInit()
        {
            try
            {
                if (mounted) return;
                mounted = true;
                // 字段解析失败(版本不匹配)时直接放弃挂载, 不打扰游戏
                if (fWorkQueue == null || fTmpWork == null || fTickStart == null)
                {
                    Log.Warning("[PathFinderThrottle] 字段解析失败(游戏版本不匹配?), 补丁未挂载。");
                    return;
                }
                Harmony h = new Harmony("Ratkin.HSKFix.PathFinderThrottle");
                h.Patch(AccessTools.Method(typeof(PathFinder), "ComputeWorkThisTick"),
                    null, new HarmonyMethod(typeof(PathFinderThrottleInit), "Limit"));
                Log.Message("[PathFinderThrottle] 已挂载: 每 tick 寻路请求上限 " + MaxPerTick);
            }
            catch (Exception e)
            {
                Log.Warning("[PathFinderThrottle] 挂载失败: " + e.Message);
            }
        }

        // Postfix on PathFinder.ComputeWorkThisTick —— 原方法已把全部到期请求放入 tmpCurrentWork,
        // 这里把超出 MaxPerTick 的部分移回 workQueue 并顺延 tickStart, 使其在后续 tick 再被取出。
        public static void Limit(PathFinder __instance)
        {
            try
            {
                // 热开关: 每 ProbeInterval tick 探测一次文件(有文件 = 禁用)
                int t = GenTicks.TicksGame;
                if (t >= probeTick + ProbeInterval)
                {
                    probeTick = t;
                    disabled = File.Exists(OffFile);
                    if (disabled) return;
                }
                else if (disabled) return;

                object oTmp = fTmpWork.GetValue(__instance);
                if (oTmp == null) return;
                List<PathRequest> tmp = (List<PathRequest>)oTmp;
                if (tmp.Count <= MaxPerTick) return;

                object oWq = fWorkQueue.GetValue(__instance);
                if (oWq == null) return;
                List<PathRequest> wq = (List<PathRequest>)oWq;
                int now = GenTicks.TicksGame;
                // 从尾部移除超出部分; 第 i 个超出者顺延到 (i-Max)/Max + 1 tick 之后
                for (int i = tmp.Count - 1; i >= MaxPerTick; i--)
                {
                    PathRequest r = tmp[i];
                    tmp.RemoveAt(i);
                    int delay = ((i - MaxPerTick) / MaxPerTick) + 1;
                    fTickStart.SetValue(r, now + delay);
                    wq.Add(r);
                }
            }
            catch (Exception)
            {
                // 任何异常都不外泄(限流失败也只是回到原版全量行为)
            }
        }
    }
}

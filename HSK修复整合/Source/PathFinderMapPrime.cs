// PathFinderMapPrime.cs — 把寻路数据源的「全量重算」从首个游戏 tick 挪到读图/建图那一刻
//
// 背景(2026-09-11 PerfSnap 多快照实锤, 21 份 full 快照里 6 份命中):
//   MapTick 子项分解 里 PathFinderMapData.GatherData 反复出现**单次数百万微秒**的记录:
//     09-09 18:46 → 3203ms   09-09 23:35 → 6067ms   09-10 12:04 → 3382ms
//     09-09 23:01 → 2148ms   09-09 23:47 → 2.4ms(平静对照组)  09-10 13:05 → 2408ms
//   同一时刻 MapPreTick / MapPostTick / AM.AnimationManager 全部报出数百ms~2s 的同步尖峰
//   (它们只是被这个全局停顿带累, 不是元凶)。
//
// 原版机制(反编译 Verse.PathFinderMapData, 字节级确认):
//   private int lastGatherTick = -1;                  // 唯一触发全量重算的状态位
//   private void Notify_MapDirtied() { lastGatherTick = -1; }   // 只订阅 map.events.MapFogged
//   public bool GatherData(IEnumerable<PathRequest> requests)
//   {
//       if (lastGatherTick == GenTicks.TicksGame) return false;   // 同 tick 只做一次
//       ... 收集 cellDeltas ...
//       if (lastGatherTick >= 0)  Parallel.ForEach(sources, s => s.UpdateIncrementally(...));  // 增量, ~0.1-0.5ms
//       else                      Parallel.ForEach(sources, s => s.ComputeAll(requests));       // 全图重算, 2.4-6.1s
//       lastGatherTick = GenTicks.TicksGame;
//   }
//   ★ 且 IPathFinderDataSource 的 13 个实现里 ComputeAll(IEnumerable<PathRequest>) **全部忽略 requests 参数**
//     (ConnectivitySource.ComputeAll(IEnumerable<PathRequest> _) 即证) —— 全量重算与请求内容无关,
//     所以**可以在没有任何路径请求时就把数据算好**(预热)。
//   ★ 触发点: lastGatherTick == -1 只在 (a) 构造(读图/建图) 和 (b) MapFogged(FogGrid.SetAllFogged,
//     即地图生成雾格/重置迷雾) 时出现。每次读档/进图恰好一次 → 表现为"每次进图必定冻一下"。
//
// 修法(纯搬运, 不改寻路语义/不改任何数据源实现):
//   钉一个 PostFix 在 Verse.Map.FinalizeInit 末尾 —— 此时地图已建好(读档)或已读进(生成)、
//   FogGrid 已是终态、雾格 setter 已写完, 但玩家还没回到游戏画面(TickManager 尚未开始 tick)。
//   在此主动调一次 PathFinderMapData.GatherData(空请求列表) 走全量重算分支, 于是:
//     · 昂贵重算发生在**载入画面里**(本就有其它初始化停顿, 玩家感知被吸收);
//     · lastGatherTick 被设成"当前 tick", 首个真正的 PathFinderTick 走增量分支,
//       原本会卡在游戏第一帧的 2.4~6.1s 冻结直接消失。
//   ComputeAll 忽略请求内容, 所以空列表预热出来的数据与原本首个请求驱动的重算**逐格等价**。
//   热开关(免重编译): Config 目录放 PathFinderMapPrime.off。
//
// 风险与兜底: FinalizeInit 每张图只跑一次, 预热本身失败(字段/方法解析不到)就整段跳过,
//   lastGatherTick 保持 -1, 完全退回原版行为, 不打扰游戏。
//
// 编译: 并入 HSKFixPack.dll(系统 csc, C#5)。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace PathFinderMapPrime
{
    [StaticConstructorOnStartup]
    public static class PathFinderMapPrimeInit
    {
        // 热开关文件路径 = <Config>/PathFinderMapPrime.off
        private static readonly string OffFile =
            Path.Combine(GenFilePaths.ConfigFolderPath, "PathFinderMapPrime.off");

        private static readonly FieldInfo fPathFinder =
            AccessTools.Field(typeof(Map), "pathFinder");
        private static readonly FieldInfo fMapData =
            AccessTools.Field(typeof(PathFinder), "mapData");
        private static readonly MethodInfo mGather =
            AccessTools.Method(typeof(PathFinderMapData), "GatherData");

        private static readonly List<PathRequest> empty =
            new List<PathRequest>();

        private static bool mounted;

        static PathFinderMapPrimeInit()
        {
            try
            {
                if (mounted) return;
                mounted = true;
                if (fPathFinder == null || fMapData == null || mGather == null)
                {
                    Log.Warning("[PathFinderMapPrime] 成员解析失败(游戏版本不匹配?), 未挂载。");
                    return;
                }
                Harmony h = new Harmony("Ratkin.HSKFix.PathFinderMapPrime");
                h.Patch(AccessTools.Method(typeof(Map), "FinalizeInit"),
                    null, new HarmonyMethod(typeof(PathFinderMapPrimeInit), "FinalizeInit_Postfix"));
                Log.Message("[PathFinderMapPrime] 已挂载: 寻路全量重算前移到地图载入阶段。");
            }
            catch (Exception e)
            {
                Log.Warning("[PathFinderMapPrime] 挂载失败: " + e.Message);
            }
        }

        // Postfix on Verse.Map.FinalizeInit —— 见文件头注释: 此时读图/建图收尾、雾格终态、尚未开 tick。
        public static void FinalizeInit_Postfix(Map __instance)
        {
            try
            {
                if (__instance == null) return;
                if (File.Exists(OffFile)) return;

                object pf = fPathFinder.GetValue(__instance);
                if (pf == null) return;
                object md = fMapData.GetValue(pf);
                if (md == null) return;

                // 走原版全量重算分支(ComputeAll 忽略 requests 内容), 之后 lastGatherTick=当前 tick,
                // 首个 PathFinderTick 只会做增量更新。
                mGather.Invoke(md, new object[] { empty });
            }
            catch (Exception e)
            {
                // 预热失败绝不外泄: lastGatherTick 仍是 -1, 行为等同原版。
                Log.WarningOnce("[PathFinderMapPrime] 预热失败(退回原版): " + e.Message, 0x5B1D7D);
            }
        }
    }
}

// 注册表丢失根因追踪器(2026-08-16,与 ReloadRegistryFix 配套)
//
// 目的: ReloadRegistryFix 是症状自愈;本追踪器负责抓现行 —— 在注册表被
// 异常破坏的【那一刻】把调用栈写进日志,直接指认肇事 mod,以便修根因
// (修好后自愈可降级/移除)。
//
// 检测原理(反编译 1.6.4871 确认):
//   - 合法注销 listerThings / haulSource 的唯一原版路径是 Thing.DeSpawn
//     (listerThings.Remove 于 961 行,spawnState 直到 998 行才清零 —— 即
//     Remove 时物品仍处于 Spawned,不能单靠 Spawned 判断异常);
//   - 因此用「DeSpawn 深度计数器」做廉价预判: 前缀挂在基类 Thing.DeSpawn 上
//     进出计数;若 ListerThings.Remove / RemoveHaulSource 发生在 DeSpawn 之外
//     且目标仍 Spawned → 异常,抓 Environment.StackTrace 指认调用方;
//   - Thing.DeSpawn 用 finalizer 保证异常路径也会递减计数;
//   - 子类 override 不调 base 而自行 Remove 的 mod 会被正确标记(栈里就是它)。
// ⚠️ 2026-08-16 关键修正: ListerThings 有两级实例 —— map.listerThings(use=Global,
//    配方原料搜索依赖的地图级)与 每个 Region 的 region.ListerThings(use=Region)。
//    Region 级 Remove/Clear 是 Thing.set_Position 移动换格时
//    RegionListersUpdater.DeregisterInRegions 的正常注销(vanilla 每次移动都触发),
//    不是注册表丢失。首版误报 1.4 万条洪水后已加 use==Global 门控: 只盯地图级。
//
// 性能: 正常游戏里 DeSpawn 高频发生,但追踪器在 DeSpawn 进/出只做一次
// 整数 ±;Remove 前缀只做两个判断(计数==0 && Spawned),均为纳秒级;
// 抓调用栈只在异常路径(限流: 前 10 次全栈,之后每 100 次摘要一行)。
//
// 日志前缀 [RegistryLeakTracer];复现「配方找不到原料」后查该关键字即可。
// ⚠️ 2026-09-04 修正: Map Preview 后台线程 Dispose 临时预览地图会触发
//    地图级 Clear/Remove(清空时 0 件,正常清理) → 三个 prefix 统一加
//    「目标须在 Find.Maps 中」门控,只盯真实游戏地图,消误报。
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RegistryLeakTracer
{
    [StaticConstructorOnStartup]
    public static class RegistryLeakTracerInit
    {
        private const int MaxFullStacks = 10;
        private const int SummaryEvery = 100;

        [ThreadStatic]
        private static int despawnDepth;

        private static int removeHits = 0;
        private static int removeLogged = 0;
        private static int sourceHits = 0;
        private static int sourceLogged = 0;
        private static int clearLogged = 0;

        static RegistryLeakTracerInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.registryleaktracer");

                // 1) DeSpawn 深度计数(prefix + finalizer,异常路径也保证递减)
                harmony.Patch(AccessTools.Method(typeof(Thing), "DeSpawn"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(RegistryLeakTracerInit), "DeSpawnPrefix")),
                    finalizer: new HarmonyMethod(AccessTools.Method(typeof(RegistryLeakTracerInit), "DeSpawnFinalizer")));

                // 2) ListerThings.Remove: DeSpawn 之外移除仍 Spawned 的物品 = 注册表丢失现场
                //    ⚠️ 只追踪 Map 级(use==Global): Region 级 Remove 是物品移动时
                //    set_Position → RegionListersUpdater.DeregisterInRegions 的【正常】
                //    区域登记清理,高频触发、不是泄漏 —— 2026-08-16 实测 18 万+ 次误报
                //    全是 Region 级(set_Position 路径),刷屏日志。过滤后仅剩真泄漏。
                harmony.Patch(AccessTools.Method(typeof(ListerThings), "Remove"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(RegistryLeakTracerInit), "ListerRemovePrefix")));

                // 3) HaulDestinationManager.RemoveHaulSource: 同理(衣柜/货架等存储建筑)
                harmony.Patch(AccessTools.Method(typeof(HaulDestinationManager), "RemoveHaulSource"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(RegistryLeakTracerInit), "RemoveHaulSourcePrefix")));

                // 4) ListerThings.Clear: 运行中整表清空 = 大规模丢失现场(限流 2 次)
                harmony.Patch(AccessTools.Method(typeof(ListerThings), "Clear"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(RegistryLeakTracerInit), "ListerClearPrefix")));

                Log.Message("[RegistryLeakTracer] active: watching ListerThings.Remove/Clear + RemoveHaulSource outside DeSpawn");
            }
            catch (Exception e)
            {
                Log.Error("[RegistryLeakTracer] patch failed: " + e);
            }
        }

        private static void DeSpawnPrefix()
        {
            despawnDepth++;
        }

        private static void DeSpawnFinalizer(Exception __exception)
        {
            if (despawnDepth > 0)
            {
                despawnDepth--;
            }
        }

        private static void ListerRemovePrefix(ListerThings __instance, Thing t)
        {
            try
            {
                // ⚠️ 只盯地图级 ListerThings(use=Global)。Region 级(use=Region)的
                // Remove 是 Thing.set_Position 移动时 RegionListersUpdater.DeregisterInRegions
                // 的正常注销(每次换格都发生),不是注册表丢失;2026-08-16 曾误报 1.4 万条
                // "listerThings.Remove outside DeSpawn" 洪水,已加此门控。
                if (__instance.use != ListerThingsUse.Global)
                {
                    return;
                }
                if (despawnDepth > 0 || t == null || !t.Spawned)
                {
                    return;
                }
                // 预览/临时地图(Map Preview 等)的注销是正常清理,只盯真实游戏地图。
                if (!IsRealGameMap(t.Map))
                {
                    return;
                }
                removeHits++;
                if (removeLogged >= MaxFullStacks)
                {
                    if (removeHits % SummaryEvery == 1)
                    {
                        Log.Warning("[RegistryLeakTracer] listerThings.Remove outside DeSpawn still happening: " +
                            removeHits + " hits so far (stacks suppressed, first " + MaxFullStacks + " in log above)");
                    }
                    return;
                }
                removeLogged++;
                Log.Warning("[RegistryLeakTracer] ANOMALY: listerThings.Remove called outside DeSpawn on spawned thing " +
                    Describe(t) + " — this is the registry-loss moment. Caller stack:\n" + Environment.StackTrace);
            }
            catch (Exception e)
            {
                Log.Warning("[RegistryLeakTracer] remove-trace failed: " + e.Message);
            }
        }

        private static void RemoveHaulSourcePrefix(IHaulSource source)
        {
            try
            {
                if (despawnDepth > 0)
                {
                    return;
                }
                Thing thing = source as Thing;
                if (thing == null || !thing.Spawned)
                {
                    return;
                }
                // 预览/临时地图(Map Preview 等)的注销是正常清理,只盯真实游戏地图。
                if (!IsRealGameMap(thing.Map))
                {
                    return;
                }
                sourceHits++;
                if (sourceLogged >= MaxFullStacks)
                {
                    if (sourceHits % SummaryEvery == 1)
                    {
                        Log.Warning("[RegistryLeakTracer] RemoveHaulSource outside DeSpawn still happening: " +
                            sourceHits + " hits so far (stacks suppressed)");
                    }
                    return;
                }
                sourceLogged++;
                Log.Warning("[RegistryLeakTracer] ANOMALY: HaulDestinationManager.RemoveHaulSource called outside DeSpawn on spawned source " +
                    Describe(thing) + " — storage will be invisible to bills. Caller stack:\n" + Environment.StackTrace);
            }
            catch (Exception e)
            {
                Log.Warning("[RegistryLeakTracer] source-trace failed: " + e.Message);
            }
        }

        private static void ListerClearPrefix(ListerThings __instance)
        {
            try
            {
                // 只盯地图级(Global);Region 级 Clear 是区域重建的正常路径。
                if (__instance.use != ListerThingsUse.Global)
                {
                    return;
                }
                // 2026-09-04 修正: Map Preview mod 游玩中在后台线程生成预览地图,
                // DisposeMap 会 Clear 临时地图自己的 lister(其 use 也是 Global),
                // 属正常清理而非注册表丢失。1.6 的 ListerThings 不持有 map 引用,
                // 故用引用比对: 仅当该实例是 Find.Maps 中某张真实地图的
                // listerThings 时才报告(临时预览图不在其中)。
                bool isRealMapLister = false;
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i].listerThings == __instance)
                    {
                        isRealMapLister = true;
                        break;
                    }
                }
                if (!isRealMapLister)
                {
                    return;
                }
                if (Current.ProgramState != ProgramState.Playing)
                {
                    return;
                }
                if (clearLogged >= 2)
                {
                    return;
                }
                clearLogged++;
                int n = (__instance != null) ? __instance.AllThings.Count : 0;
                Log.Warning("[RegistryLeakTracer] ANOMALY: ListerThings.Clear during play (" +
                    n + " things listed). Caller stack:\n" + Environment.StackTrace);
            }
            catch (Exception e)
            {
                Log.Warning("[RegistryLeakTracer] clear-trace failed: " + e.Message);
            }
        }

        // 2026-09-04 新增: Map Preview 会在后台线程生成临时预览地图再 Dispose,
        // 其对临时地图(Global 级)的 Clear/Remove 是正常清理,不是注册表丢失。
        // 仅当目标所在地图是 Find.Maps 中的真实游戏地图时才值得报警。
        private static bool IsRealGameMap(Map map)
        {
            if (map == null)
            {
                return false;
            }
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] == map)
                {
                    return true;
                }
            }
            return false;
        }

        private static string Describe(Thing thing)
        {
            if (thing == null)
            {
                return "null";
            }
            string defName = (thing.def != null) ? thing.def.defName : "null";
            return defName + "@" + thing.Position + " on map " + ((thing.Map != null) ? thing.Map.Tile.ToString() : "?");
        }
    }
}

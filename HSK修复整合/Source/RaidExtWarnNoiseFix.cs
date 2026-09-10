// Raid Extension(偷猎/伐木)警告消音 + 扫描节流 v3(2026-08-21 用户要求)
//
// 背景: Raid Extension(SR.ModRimWorld.RaidExtension)的 IncidentWorkerPoaching /
// IncidentWorkerLogging 在 CanFireNowSub 里用 Log.Warning 报告"地图上没动物/没树",
// 且每次调用都全图扫描(map.mapPawns.AllPawnsSpawned / map.spawnedThings)。
// HSK 的 SK.RaidHelperComponent(MapComponentTick -> ResolveFrenzyEvent)每 tick 对事件池
// 逐个做可用性检查 -> 这两个事件每 tick 全图扫描一次动物/树(性能浪费 + 日志刷屏)。
//
// 方案(prefix 完全接管 CanFireNowSub / TryExecuteWorker):
//   1. 长缓存: 偷猎/伐木一个月最多出现两三次,"地图有无目标"完全不需要实时性,
//      目标扫描与 SOS2 判断缓存 5 游戏天(300,000 ticks = 每月约 3 次扫描);
//      ConditionalWeakTable 按 Map 缓存,键弱引用,地图卸载/存档切换自动回收无泄漏。
//   2. 消音: 接管后不再执行原方法,Log.Warning/Error 分支全部不触发,日志彻底安静。
//   3. 行为一致: 子判断全部反射调用原实现——
//      - base: RimWorld.IncidentWorker_PawnsArrive.CanFireNowSub(反编译确认=
//        parms.faction!=null || CandidateFactions.Any(),便宜无扫描)
//      - SOS2: RE Util.HarmonyUtil.IsSOS2SpaceMap(缓存)
//      - 目标: RE MapExtension.IsAnimalTargetExist(map,1.2f) / IsTreeExist(map)(缓存)
//      - 敌对派系: RE/基类 CandidateFactions(parms,false) 实时遍历(派系数少,非扫描)
//      RE 更新后阈值/逻辑自动跟随,不漂移;RE 未加载时自动放行,无副作用。
//   4. TryExecuteWorker(低频,仅 Poaching)做实时扫描(不缓存): 它只在事件被选中
//      执行时调用(一个月几次),实时确认目标存在,避免"缓存过期后生成空事件"。
//
// 性能: CanFireNowSub 每 tick 仅 2 次小反射 + 派系遍历(微秒级),全图扫描 5 天才一次;
//       日志零刷屏。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RaidExtWarnNoiseFix
{
    [StaticConstructorOnStartup]
    public static class RaidExtWarnNoiseFixInit
    {
        // 目标/SOS2 扫描节流:5 游戏天 = 1,200,000/3 ticks(1 quadrum=15 天=900,000 ticks,
        // 每月约 3 次)。事件一个月最多出现两三次,无需实时性。
        private const int CacheIntervalTicks = 5 * 60000;

        private static MethodInfo s_candidateFactions; // RimWorld.IncidentWorker_PawnsArrive.CandidateFactions
        private static MethodInfo s_isAnimalTargetExist; // RE MapExtension.IsAnimalTargetExist
        private static MethodInfo s_isTreeExist;         // RE MapExtension.IsTreeExist
        private static MethodInfo s_sos2;                // RE Util.HarmonyUtil.IsSOS2SpaceMap

        private class MapTargetCache
        {
            public int cachedAtTick;
            public bool hasTarget;
        }

        // 键(地图)弱引用:地图卸载/存档切换后条目自动回收,不会持有 Map 引用造成泄漏
        private static readonly ConditionalWeakTable<Map, MapTargetCache> s_animalCache = new ConditionalWeakTable<Map, MapTargetCache>();
        private static readonly ConditionalWeakTable<Map, MapTargetCache> s_treeCache = new ConditionalWeakTable<Map, MapTargetCache>();
        private static readonly ConditionalWeakTable<Map, MapTargetCache> s_sos2Cache = new ConditionalWeakTable<Map, MapTargetCache>();

        static RaidExtWarnNoiseFixInit()
        {
            try
            {
                s_candidateFactions = AccessTools.Method("RimWorld.IncidentWorker_PawnsArrive:CandidateFactions");
                s_isAnimalTargetExist = AccessTools.Method("SR.ModRimWorld.RaidExtension.MapExtension:IsAnimalTargetExist");
                s_isTreeExist = AccessTools.Method("SR.ModRimWorld.RaidExtension.MapExtension:IsTreeExist");
                s_sos2 = AccessTools.Method("SR.ModRimWorld.RaidExtension.Util.HarmonyUtil:IsSOS2SpaceMap");

                Harmony harmony = new Harmony("local.hskfixpack.raidxtnwarnfix");
                PatchMethod(harmony, "SR.ModRimWorld.RaidExtension.IncidentWorkerPoaching", "CanFireNowSub", "PoachingCanFirePrefix");
                PatchMethod(harmony, "SR.ModRimWorld.RaidExtension.IncidentWorkerPoaching", "TryExecuteWorker", "PoachingTryExecPrefix");
                PatchMethod(harmony, "SR.ModRimWorld.RaidExtension.IncidentWorkerLogging", "CanFireNowSub", "LoggingCanFirePrefix");
            }
            catch (Exception e)
            {
                Log.Error("[RaidExtWarnNoiseFix] patch failed: " + e);
            }
        }

        private static void PatchMethod(Harmony harmony, string typeName, string methodName, string prefixName)
        {
            Type t = AccessTools.TypeByName(typeName);
            if (t == null)
            {
                Log.Message("[RaidExtWarnNoiseFix] type not found: " + typeName + " (Raid Extension not loaded?)");
                return;
            }
            MethodInfo mi = AccessTools.Method(t, methodName);
            if (mi == null)
            {
                Log.Message("[RaidExtWarnNoiseFix] method not found: " + typeName + "." + methodName);
                return;
            }
            MethodInfo pre = typeof(RaidExtWarnNoiseFixInit).GetMethod(prefixName, BindingFlags.Public | BindingFlags.Static);
            if (pre == null)
            {
                Log.Error("[RaidExtWarnNoiseFix] prefix method missing: " + prefixName);
                return;
            }
            harmony.Patch(mi, prefix: new HarmonyMethod(pre));
            Log.Message("[RaidExtWarnNoiseFix] patched " + typeName + "." + methodName);
        }

        // ---- 节流核心:缓存有效期内直接返回,过期才反射重算一次并刷新 ----
        private static bool CachedBool(Map map, ConditionalWeakTable<Map, MapTargetCache> cache, Func<Map, bool> calc)
        {
            MapTargetCache entry = null;
            if (cache.TryGetValue(map, out entry))
            {
                if (GenTicks.TicksGame - entry.cachedAtTick < CacheIntervalTicks)
                {
                    return entry.hasTarget;
                }
            }
            bool val = calc(map);
            cache.Remove(map);
            cache.Add(map, new MapTargetCache { cachedAtTick = GenTicks.TicksGame, hasTarget = val });
            return val;
        }

        // ---- 反射子判断;方法缺失/调用异常时按"放行"处理 ----
        private static bool ScanAnimals(Map map)
        {
            if (s_isAnimalTargetExist == null) return true;
            try { return (bool)s_isAnimalTargetExist.Invoke(null, new object[] { map, 1.2f }); }
            catch (Exception e) { Log.Warning("[RaidExtWarnNoiseFix] IsAnimalTargetExist invoke failed: " + e.Message); return true; }
        }

        private static bool ScanTrees(Map map)
        {
            if (s_isTreeExist == null) return true;
            try { return (bool)s_isTreeExist.Invoke(null, new object[] { map }); }
            catch (Exception e) { Log.Warning("[RaidExtWarnNoiseFix] IsTreeExist invoke failed: " + e.Message); return true; }
        }

        private static bool EvalSos2(Map map)
        {
            if (s_sos2 == null) return false;
            try { return (bool)s_sos2.Invoke(null, new object[] { map }); }
            catch (Exception e) { Log.Warning("[RaidExtWarnNoiseFix] IsSOS2SpaceMap invoke failed: " + e.Message); return false; }
        }

        // 敌对派系检查(实时,非扫描): RE 原逻辑 = CandidateFactions(parms,false).Any(f=>HostileTo(f,OfPlayer))
        private static bool HasHostileFaction(IncidentWorker instance, IncidentParms parms)
        {
            if (s_candidateFactions == null) return true;
            try
            {
                IEnumerable<Faction> candidates = (IEnumerable<Faction>)s_candidateFactions.Invoke(instance, new object[] { parms, false });
                foreach (Faction f in candidates)
                {
                    if (f != null && FactionUtility.HostileTo(f, Faction.OfPlayer)) return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Log.Warning("[RaidExtWarnNoiseFix] CandidateFactions invoke failed: " + e.Message);
                return true;
            }
        }

        // ---- 完全接管 CanFireNowSub(与 RE 原方法同序同义,全图扫描走 5 天缓存) ----
        private static bool EvaluateCanFire(IncidentWorker instance, IncidentParms parms, bool poaching)
        {
            // 1. base.CanFireNowSub(PawnsArrive): parms.faction != null || CandidateFactions.Any()
            //    注意: 不能反射调用 s_baseCanFire.Invoke(instance, ...) —— CanFireNowSub 是虚方法,
            //    MethodInfo.Invoke 走虚表分派, instance 实际是 IncidentWorkerPoaching/Logging,
            //    虚调用会再次命中本类被 patch 的 CanFireNowSub → EvaluateCanFire → 无限递归 → 栈溢出。
            //    内联基类逻辑(反编译确认 = parms.faction != null || CandidateFactions().Any()):
            if (parms.faction == null)
            {
                bool anyFaction = false;
                if (s_candidateFactions != null)
                {
                    try
                    {
                        IEnumerable<Faction> baseCandidates =
                            (IEnumerable<Faction>)s_candidateFactions.Invoke(instance, new object[] { parms, false });
                        if (baseCandidates != null)
                        {
                            foreach (Faction f in baseCandidates)
                            {
                                if (f != null)
                                {
                                    anyFaction = true;
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning("[RaidExtWarnNoiseFix] base CandidateFactions invoke failed: " + e.Message);
                        anyFaction = true;
                    }
                }
                else
                {
                    anyFaction = true; // 基类方法缺失时放行
                }
                if (!anyFaction) return false;
            }

            // 2. map 校验
            Map map = parms.target as Map;
            if (map == null) return false;

            // 3. SOS2 太空图排除(5 天缓存)
            if (CachedBool(map, s_sos2Cache, EvalSos2)) return false;

            // 4. 目标存在性(5 天缓存,每月约 3 次全图扫描)
            if (poaching)
            {
                if (!CachedBool(map, s_animalCache, ScanAnimals)) return false;
            }
            else
            {
                if (!CachedBool(map, s_treeCache, ScanTrees)) return false;
            }

            // 5. 敌对派系(实时,派系数少,非扫描)
            return HasHostileFaction(instance, parms);
        }

        public static bool PoachingCanFirePrefix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            __result = EvaluateCanFire(__instance, parms, true);
            return false; // 完全接管,原方法(含 Log.Warning)不再执行
        }

        public static bool LoggingCanFirePrefix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            __result = EvaluateCanFire(__instance, parms, false);
            return false;
        }

        // ---- TryExecuteWorker(仅 Poaching,低频):实时扫描确认,不缓存 ----
        // 注意: 不能反射调用 s_baseTryExecute.Invoke(__instance, ...) —— TryExecuteWorker 也是
        // 虚方法(反编译确认 IncidentWorkerPoaching.TryExecuteWorker = VIRTUAL),MethodInfo.Invoke
        // 走虚表分派会再次命中本类被 patch 的 TryExecuteWorker → PoachingTryExecPrefix → 无限递归。
        // 修复: prefix 只做实时扫描确认,确认通过后 return true 让原方法(被 patch 的)执行。
        // 原方法的 Log.Warning 只在"无目标"分支触发,而我们已经先扫描确认有目标,执行原方法安静。
        // 扫描失败/无目标时 return false 拦截,避免生成空事件(同时不触发原方法的警告)。
        public static bool PoachingTryExecPrefix(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            Map map = parms.target as Map;
            if (map == null) { __result = false; return false; }
            if (!ScanAnimals(map)) { __result = false; return false; } // 实时,事件被选中才调用,一个月几次
            return true; // 有目标,放行原方法执行(不反射调基类,避免虚分派递归)
        }
    }
}

// WorldPathGridPacer.cs — 世界路径成本每日重算 分帧化(常驻,无配置页)
//
// 问题(2026-09-05 [TP2] 实测): RimWorld.Planet.WorldPathGrid.WorldPathGridTick 每个游戏日
// (60000 tick)一次性把**整颗星球所有格**的"感知移动成本"重算一遍。本存档世界 295732 格,
// 单 tick 实测 259~274ms —— 每天准时一记硬卡顿。
//
// 原版逻辑(反编译):
//   WorldPathGridTick():  对每个 layer,若 layerAllPathCostsRecalculatedDayOfYear[layer] != 今天 → RecalculateLayerPerceivedPathCosts(layer)
//   RecalculateLayer(layer): dict[layer]=-1; 逐格 RecalculatePerceivedMovementDifficultyAt(new PlanetTile(i,layer), out needsRecache, null);
//                            若有任何变化 → Find.WorldReachability.ClearCache();  外层再 dict[layer]=今天
// 逐格计算用的是**原版公开方法**,我们只是把它从"一 tick 跑完"改成"每 tick 跑一小块",
// 算的东西、写的数组、清的缓存完全一致。
//
// 接管方式: 前缀吞掉 WorldPathGridTick,自己按 ChunkTilesPerTick 推进;
// 一层跑完才把"今日已重算"写进原版字典(所以中途读档/退出,第二天会自然重跑,不会留下半套数据)。
// 若中途有格被改写,同样调 WorldReachability.ClearCache()。
//
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。第三方/私有字段全反射,拿不到就不接管(退回原版)。
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace WorldPathGridPacer
{
    [StaticConstructorOnStartup]
    public static class WpgInit
    {
        const int ChunkTilesPerTick = 2500;     // ≈2.3ms/tick,29.5 万格约 118 tick(2 游戏秒)摊完

        static FieldInfo fiDayOfYear;           // 私有 Dictionary<PlanetLayer,int>
        static bool ready, broken;

        // 进行中的一层
        static PlanetLayer curLayer;
        static int curIdx, curCount;
        static bool anyRecache, active;

        static WpgInit()
        {
            try
            {
                fiDayOfYear = AccessTools.Field(typeof(WorldPathGrid), "layerAllPathCostsRecalculatedDayOfYear");
                if (fiDayOfYear == null)
                {
                    broken = true;
                    Log.Error("[WPGP] 找不到 layerAllPathCostsRecalculatedDayOfYear,分帧化未启用(退回原版)");
                    return;
                }
                MethodBase m = AccessTools.Method(typeof(WorldPathGrid), "WorldPathGridTick");
                if (m == null)
                {
                    broken = true;
                    return;
                }
                new Harmony("local.ratkin.hskfix.wpgp").Patch(m,
                    new HarmonyMethod(typeof(WpgInit), "Pre"), null, null, null);
                ready = true;
            }
            catch (Exception e)
            {
                broken = true;
                Log.Error("[WPGP] 挂载失败: " + e);
            }
        }

        static bool Pre(WorldPathGrid __instance)
        {
            if (broken || Find.WorldGrid == null || GenTicks.TicksAbs <= 0) return true;
            int today = GenDate.DayOfYear(GenTicks.TicksAbs, 0f);
            IDictionary dayDict = fiDayOfYear.GetValue(__instance) as IDictionary;
            if (dayDict == null) return true;

            if (!active)
            {
                PlanetLayer need = FirstLayerNeedingRecompute(dayDict, today);
                if (need == null) return false;                     // 今天已算完,吞掉原版空转
                active = true;
                curLayer = need;
                curIdx = 0;
                anyRecache = false;
                curCount = need.TilesCount;
                dayDict[need] = -1;                                 // 与原版一致:重算期间标记未完成
            }

            bool needsRecache = false;
            int end = curIdx + ChunkTilesPerTick;
            if (end > curCount) end = curCount;
            for (int i = curIdx; i < end; i++)
            {
                __instance.RecalculatePerceivedMovementDifficultyAt(new PlanetTile(i, curLayer), out needsRecache, null);
                if (needsRecache) anyRecache = true;
            }
            curIdx = end;

            if (curIdx >= curCount)
            {
                dayDict[curLayer] = today;                          // 整层算完才写"今日已算"
                if (anyRecache)
                {
                    try { Find.WorldReachability.ClearCache(); }
                    catch (Exception) { }
                }
                active = false;
                curLayer = null;
            }
            return false;
        }

        static PlanetLayer FirstLayerNeedingRecompute(IDictionary dayDict, int today)
        {
            try
            {
                IEnumerable layers = Find.WorldGrid.PlanetLayers as IEnumerable;
                if (layers == null) return Find.WorldGrid.Surface;
                foreach (object o in layers)
                {
                    PlanetLayer pl = o as PlanetLayer;
                    if (pl == null && o != null)
                    {
                        PropertyInfo pi = o.GetType().GetProperty("Value");
                        if (pi != null) pl = pi.GetValue(o, null) as PlanetLayer;
                    }
                    if (pl == null) continue;
                    object v = dayDict[pl];
                    if (v is int && (int)v != today) return pl;
                }
            }
            catch (Exception)
            {
                broken = true;      // 结构对不上就退回原版,不再接管
            }
            return null;
        }
    }
}

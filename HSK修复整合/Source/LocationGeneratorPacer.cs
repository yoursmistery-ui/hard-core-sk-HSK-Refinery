// LocationGeneratorPacer.cs — 原版地标生成器的周期检查 O(格×对象) → O(对象) (常驻,无配置页)
//
// 问题(2026-09-06 [TP2] 实测): RimWorld.Planet.WorldComponent_LocationGenerator.WorldComponentTick
// 每 90000 tick(层 hash 错开,≈超速档下每 100 秒)对**每层星球所有格**调一次
// WorldObjectsHolder.AnyGeneratedWorldLocationAt(tile) —— 而该方法本身是对全部世界对象线性扫。
// 本存档 ~29 万格 × 数千世界对象 ≈ 上亿次比较,单 tick 实测 116~138ms,多会话准点复现
// (@7062198 → @7152198 间隔分毫不差)。且多数时候结论只是"数量达标,不生成"。
//
// 原版逻辑(反编译 _tmp/rw_core_decomp):
//   foreach layer:  若 IsTickInterval(layer.GetHashCode().HashOffset(), 90000):
//     目标 = RoundToInt(layer.Def.generatedLocationFactor * worldLocationsTarget)
//     若 layer.Tiles.Count(tile => AnyGeneratedWorldLocationAt(tile)) < 目标 → GenerateUntilTarget(layer)
//   AnyGeneratedWorldLocationAt(tile) = 线性扫 worldObjects 找 isGeneratedLocation && Tile==tile。
//
// 本前缀语义严格等价: 同样的 90000 tick 节奏/目标值/补生成调用,只把"数有地标的不同格数"
// 改为对 worldObjects 单次遍历收集 distinct tile(PlanetTile 作 HashSet 键),
// O(格×对象) → O(对象),~120ms → 亚毫秒。拿不到私有成员就整段退回原版。
//
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LocationGeneratorPacer
{
    [StaticConstructorOnStartup]
    public static class LgpInit
    {
        public static readonly Type WcType = AccessTools.TypeByName("RimWorld.Planet.WorldComponent_LocationGenerator");
        internal static readonly FieldInfo FiTarget = WcType != null
            ? AccessTools.Field(WcType, "worldLocationsTarget") : null;
        internal static readonly MethodInfo MiGenLayer = WcType != null
            ? AccessTools.Method(WcType, "GenerateUntilTarget", new Type[] { typeof(PlanetLayer) }) : null;

        static LgpInit()
        {
            try
            {
                if (WcType == null || FiTarget == null || MiGenLayer == null)
                {
                    Log.Warning("[HSKFix] 未找到原版 WorldComponent_LocationGenerator 及其私有成员,地标生成器加速不接管");
                    return;
                }
                Harmony h = new Harmony("local.ratkin.hskfix.locgen");
                h.Patch(AccessTools.Method(WcType, "WorldComponentTick"),
                    new HarmonyMethod(typeof(LocationGeneratorPacer), "Prefix"));
                Log.Message("[HSKFix] 原版地标生成器周期检查已接管(O(格×对象)→O(对象),原 90000t 一次 ~120ms 停顿应消失)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] LocationGeneratorPacer 挂载失败: " + e);
            }
        }
    }

    public static class LocationGeneratorPacer
    {
        // 层→该层已有地标的 distinct tile 集(仅检查 tick 填充,检查完即清)
        private static readonly Dictionary<PlanetLayer, HashSet<PlanetTile>> seenTiles =
            new Dictionary<PlanetLayer, HashSet<PlanetTile>>();

        public static bool Prefix(WorldComponent __instance)
        {
            try
            {
                World world = __instance.world;
                if (world == null || world.grid == null)
                {
                    return true;                      // 拿不到网格,退回原版
                }
                int targetBase;
                object tv = LgpInit.FiTarget.GetValue(__instance);
                if (tv is int)
                {
                    targetBase = (int)tv;
                }
                else
                {
                    float cov = world.PlanetCoverage;   // 原版 ctor 同款公式兜底
                    targetBase = cov < 0.051f ? 3 : (cov < 0.301f ? 8 : (cov < 0.501f ? 12 : 20));
                }

                bool anyDue = false;
                foreach (PlanetLayer layer in world.grid.PlanetLayers.Values)
                {
                    if (layer != null && GenTicks.IsTickInterval(layer.GetHashCode().HashOffset(), 90000))
                    {
                        anyDue = true;
                        break;
                    }
                }
                if (!anyDue)
                {
                    return false;                     // 未到检查点,原版本体也只会空转 → 直接吞掉
                }

                // 到检查点: 一次遍历 worldObjects,按层收集有地标的 distinct tile
                seenTiles.Clear();
                List<WorldObject> wos = Find.WorldObjects.AllWorldObjects;
                for (int i = 0; i < wos.Count; i++)
                {
                    WorldObject wo = wos[i];
                    if (wo == null || !wo.isGeneratedLocation)
                    {
                        continue;
                    }
                    PlanetTile tile = wo.Tile;
                    if (!tile.Valid || tile.Layer == null)
                    {
                        continue;
                    }
                    HashSet<PlanetTile> set;
                    if (!seenTiles.TryGetValue(tile.Layer, out set))
                    {
                        set = seenTiles[tile.Layer] = new HashSet<PlanetTile>();
                    }
                    set.Add(tile);
                }

                List<PlanetLayer> dueLayers = new List<PlanetLayer>();
                foreach (PlanetLayer layer in world.grid.PlanetLayers.Values)
                {
                    if (layer != null && GenTicks.IsTickInterval(layer.GetHashCode().HashOffset(), 90000))
                    {
                        dueLayers.Add(layer);
                    }
                }
                for (int i = 0; i < dueLayers.Count; i++)
                {
                    PlanetLayer layer = dueLayers[i];
                    int target = Mathf.RoundToInt(layer.Def.generatedLocationFactor * (float)targetBase);
                    HashSet<PlanetTile> set;
                    int count = seenTiles.TryGetValue(layer, out set) ? set.Count : 0;
                    if (count < target)
                    {
                        LgpInit.MiGenLayer.Invoke(__instance, new object[] { layer });
                    }
                }
                return false;                         // 已按等价逻辑完成,跳过原版 O(格×对象) 扫描
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] 地标生成器前缀异常,本次退回原版逻辑: " + e.Message);
                return true;
            }
        }
    }
}

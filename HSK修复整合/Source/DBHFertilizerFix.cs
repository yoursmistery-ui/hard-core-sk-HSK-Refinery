// DBH 肥料适配(2026-08-18)
//
// 背景: Dubs Bad Hygiene 的施肥(肥田)工作 WorkGiver_PlaceFertilizer 硬编码只认
// Biosolids(生物有机肥料,堆肥器用粪便污泥发酵制成),导致 Core_SK / HSK 的
// Fertilizer(肥料)无法用于肥田——地图上没有 Biosolids 时施肥工作直接跳过,
// 取物也只取 Biosolids。
//
// 修法: 两个 Harmony 前缀补丁,把「找肥料」与「是否跳过」都扩展为
// Biosolids 或 Fertilizer 任一存在即可:
//   1) FindAllFuel(private static)完全替换: 同时收集 Biosolids 与 Fertilizer;
//   2) ShouldSkip 完全复制原逻辑,物品存在性检查扩展为两者任一。
// 执行端 JobDriver_PlaceFertilizer.Refuel 不检查物品类型,队列给什么撒什么、
// 肥力效果相同,无需修改。Fertilizer 缺失(非 HSK 环境)时自动回退原版行为。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DBHFertilizerFix
{
    [StaticConstructorOnStartup]
    public static class DBHFertilizerFixInit
    {
        static DBHFertilizerFixInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.dbhfertilizer");

                MethodInfo findFuel = AccessTools.Method(typeof(DubsBadHygiene.WorkGiver_PlaceFertilizer), "FindAllFuel");
                if (findFuel == null)
                {
                    Log.Warning("[DBHFertilizerFix] WorkGiver_PlaceFertilizer.FindAllFuel not found");
                }
                else
                {
                    harmony.Patch(findFuel, prefix: new HarmonyMethod(typeof(Patch_FindAllFuel), "Prefix"));
                }

                MethodInfo shouldSkip = AccessTools.Method(typeof(DubsBadHygiene.WorkGiver_PlaceFertilizer), "ShouldSkip");
                if (shouldSkip == null)
                {
                    Log.Warning("[DBHFertilizerFix] WorkGiver_PlaceFertilizer.ShouldSkip not found");
                }
                else
                {
                    harmony.Patch(shouldSkip, prefix: new HarmonyMethod(typeof(Patch_ShouldSkip), "Prefix"));
                }
            }
            catch (Exception e)
            {
                Log.Error("[DBHFertilizerFix] patch init failed: " + e);
            }
        }
    }

    public static class Patch_FindAllFuel
    {
        // 完全替换原 FindAllFuel: 同时收集 Biosolids 与 Fertilizer
        public static bool Prefix(Pawn pawn, IntVec3 position, int quant, ref List<Thing> __result)
        {
            __result = FertilizerHelper.FindAllFuelBoth(pawn, position, quant);
            return false;
        }
    }

    public static class Patch_ShouldSkip
    {
        // 完全替换原 ShouldSkip: 复制原逻辑,仅把「只有 Biosolids」扩展为「Biosolids 或 Fertilizer」
        public static bool Prefix(DubsBadHygiene.WorkGiver_PlaceFertilizer __instance, Pawn pawn, bool forced, ref bool __result)
        {
            if (DubsBadHygiene.Settings.LiteMode)
            {
                __result = true;
                return false;
            }
            if (pawn.Map.areaManager.Get<DubsBadHygiene.Area_Fertilizer>() == null)
            {
                __result = true;
                return false;
            }

            ThingDef fertilizerDef = DefDatabase<ThingDef>.GetNamedSilentFail("Fertilizer");
            int maxSpace = pawn.carryTracker.MaxStackSpaceEver(DubsBadHygiene.DubDef.Biosolids);
            if (fertilizerDef != null)
            {
                maxSpace = Mathf.Max(maxSpace, pawn.carryTracker.MaxStackSpaceEver(fertilizerDef));
            }
            if (maxSpace < 10)
            {
                __result = true;
                return false;
            }

            bool hasBiosolids = pawn.Map.listerThings.ThingsOfDef(DubsBadHygiene.DubDef.Biosolids).Count > 0;
            bool hasFertilizer = fertilizerDef != null && pawn.Map.listerThings.ThingsOfDef(fertilizerDef).Count > 0;
            if (!hasBiosolids && !hasFertilizer)
            {
                __result = true;
                return false;
            }

            List<Thing> fuel = FertilizerHelper.FindAllFuelBoth(pawn, pawn.Position, 10);
            if (GenList.NullOrEmpty(fuel) || fuel.Sum((Thing x) => x.stackCount) < 10)
            {
                JobFailReason.Is(Translator.Translate("NoFertilizerFound").ToString());
                __result = true;
                return false;
            }

            __instance.FoundCell = false;
            __instance.AnyFertilizer = true;
            __instance.hy = pawn.Map.GetComponent<DubsBadHygiene.MapComponent_Hygiene>();
            __result = false;
            return false;
        }
    }

    public static class FertilizerHelper
    {
        // 与 DBH 原版 FindAllFuel 相同,但同时查找 Biosolids 与 Fertilizer
        public static List<Thing> FindAllFuelBoth(Pawn pawn, IntVec3 position, int quant)
        {
            Region region = GridsUtility.GetRegion(position, pawn.Map, RegionType.Set_Passable);
            TraverseParms traverseParams = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false, true);
            CollectState state = new CollectState();
            ThingDef defBiosolids = DubsBadHygiene.DubDef.Biosolids;
            ThingDef defFertilizer = DefDatabase<ThingDef>.GetNamedSilentFail("Fertilizer");

            RegionTraverser.BreadthFirstTraverse(region,
                delegate (Region from, Region r)
                {
                    return r.Allows(traverseParams, false);
                },
                delegate (Region r)
                {
                    if (CollectFromRegion(r, r.ListerThings.ThingsOfDef(defBiosolids), pawn, state, quant))
                    {
                        return true;
                    }
                    if (defFertilizer != null && CollectFromRegion(r, r.ListerThings.ThingsOfDef(defFertilizer), pawn, state, quant))
                    {
                        return true;
                    }
                    return false;
                },
                99999,
                RegionType.Set_Passable);
            return state.Chosen;
        }

        private static bool CollectFromRegion(Region reg, List<Thing> things, Pawn pawn, CollectState state, int quant)
        {
            foreach (Thing item in things)
            {
                if (Validator(item, pawn) && !state.Chosen.Contains(item)
                    && ReachabilityWithinRegion.ThingFromRegionListerReachable(item, reg, PathEndMode.InteractionCell, pawn))
                {
                    state.Chosen.Add(item);
                    state.Accumulated += item.stackCount;
                    if (state.Accumulated >= quant)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool Validator(Thing x, Pawn pawn)
        {
            if (ForbidUtility.IsForbidden(x, pawn))
            {
                return false;
            }
            return ReservationUtility.CanReserve(pawn, x, 1, -1, null, false);
        }

        private class CollectState
        {
            public List<Thing> Chosen = new List<Thing>();
            public int Accumulated;
        }
    }
}

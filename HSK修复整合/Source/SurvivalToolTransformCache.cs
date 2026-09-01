// 缓存 SurvivalToolsLite 的 StatPart_SurvivalTool.TransformValue 的乘数结果。
//
// 与 SurvivalToolStatCache(缓存 GetBestSurvivalTool)配套:
//  - GetBestSurvivalTool 缓存:消除 GetAllUsableSurvivalTools 的 LINQ 扫描(工具选择)
//  - 本缓存:消除 TransformValue 剩余链路的每次调用开销 —— 包括
//    HasSurvivalToolFor 里 GetStatFactorFromList(tool.WorkStatFactors.ToList(), stat)
//    的迭代器 + GetModExtension LINQ + ToList 分配 + 因子扫描
//
// 原理:TransformValue 对 val 的净效果是「乘上一个乘数」(工具因子 或 NoToolStatFactor),
// 乘数只随工具装卸/捡起变化(频率低)。用 __state 记输入值,postfix 用
// |val_after| / |val_before| 反推乘数并缓存,按 (pawn, parentStat) 键。
// TTL 120 tick(2 秒),工具变化最多延迟 2 秒反映,无感;硬核模式切换等极端场景
// 最多 2 秒过期。
//
// 未装 SurvivalToolsLite 时 TypeByName 返回 null,自动跳过、零副作用。
// 编译:并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SurvivalToolTransformCache
{
    [StaticConstructorOnStartup]
    public static class SurvivalToolTransformCacheInit
    {
        private const int TTL = 120;
        private const int CleanupInterval = 60000;
        private const float Epsilon = 1e-6f;

        private struct Entry
        {
            public float multiplier;
            public int tick;
        }

        private static readonly Dictionary<Pawn, Dictionary<StatDef, Entry>> cache =
            new Dictionary<Pawn, Dictionary<StatDef, Entry>>();
        private static int lastCleanupTick = -1;

        static SurvivalToolTransformCacheInit()
        {
            try
            {
                Type partType = AccessTools.TypeByName("SurvivalToolsLite.StatPart_SurvivalTool");
                if (partType == null)
                {
                    return;
                }
                MethodInfo method = AccessTools.Method(partType, "TransformValue",
                    new Type[] { typeof(StatRequest), typeof(float).MakeByRefType() });
                if (method == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.survivaltooltransformcache");
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(SurvivalToolTransformCacheInit), "Prefix"),
                    postfix: new HarmonyMethod(typeof(SurvivalToolTransformCacheInit), "Postfix"));
                Log.Message("[HSKFix] SurvivalToolTransformCache: cached StatPart_SurvivalTool.TransformValue multiplier (TTL " + TTL + " ticks)");
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] SurvivalToolTransformCache patch failed: " + e);
            }
        }

        private static bool Prefix(object __instance, StatRequest req, ref float val, ref float __state)
        {
            try
            {
                __state = val;
                StatPart part = __instance as StatPart;
                if (part == null || part.parentStat == null)
                {
                    return true;
                }
                Pawn pawn = req.Thing as Pawn;
                if (pawn == null)
                {
                    return true;
                }
                Dictionary<StatDef, Entry> d;
                if (!cache.TryGetValue(pawn, out d))
                {
                    return true;
                }
                Entry e;
                if (d.TryGetValue(part.parentStat, out e) && GenTicks.TicksGame - e.tick <= TTL)
                {
                    val *= e.multiplier;
                    return false;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        private static void Postfix(object __instance, StatRequest req, ref float val, float __state)
        {
            try
            {
                StatPart part = __instance as StatPart;
                if (part == null || part.parentStat == null)
                {
                    return;
                }
                Pawn pawn = req.Thing as Pawn;
                if (pawn == null || Math.Abs(__state) < Epsilon)
                {
                    return;
                }
                float multiplier = Math.Abs(val) / Math.Abs(__state);
                Dictionary<StatDef, Entry> d;
                if (!cache.TryGetValue(pawn, out d))
                {
                    d = new Dictionary<StatDef, Entry>();
                    cache[pawn] = d;
                }
                d[part.parentStat] = new Entry { multiplier = multiplier, tick = GenTicks.TicksGame };
                int now = GenTicks.TicksGame;
                if (now - lastCleanupTick > CleanupInterval)
                {
                    lastCleanupTick = now;
                    Cleanup();
                }
            }
            catch
            {
            }
        }

        private static void Cleanup()
        {
            List<Pawn> toRemove = null;
            foreach (KeyValuePair<Pawn, Dictionary<StatDef, Entry>> kv in cache)
            {
                if (kv.Key == null || kv.Key.Destroyed)
                {
                    (toRemove ?? (toRemove = new List<Pawn>())).Add(kv.Key);
                }
            }
            if (toRemove != null)
            {
                foreach (Pawn p in toRemove)
                {
                    cache.Remove(p);
                }
            }
        }
    }
}

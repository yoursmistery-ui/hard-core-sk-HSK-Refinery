// 缓存原版 StatPart_GearStatOffset.TransformValue 的装备偏移结果。
//
// DPA profile 实测:StatPart_GearStatOffset(Stat 分类 0.033ms + TransformValue 0.018ms)
// 是仅次于 STL 工具的属性热点。它每次被调用遍历 pawn.apparel.WornApparel + 武器,
// 对每件装备做 GetStatValue(apparelStat) + StatWorker.StatOffsetFromGear(递归应用
// 该 stat 的全部 StatPart),是加法型偏移(非乘法)。
//
// 方案:与工具缓存同款 —— __state 记输入值,postfix 用 val_after - val_before 反推
// 偏移量并缓存,按 (pawn, parentStat) 键,TTL 120 tick(2 秒)。装备装卸最多延迟 2 秒
// 反映,无感。加法型偏移无需绝对值处理(正负由 subtract 标志决定,自动保留)。
// 未命中的早期返回(非 pawn / 无装备)也正确缓存为 0 偏移。
//
// 编译:并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GearStatOffsetCache
{
    [StaticConstructorOnStartup]
    public static class GearStatOffsetCacheInit
    {
        private const int TTL = 120;
        private const int CleanupInterval = 60000;

        private struct Entry
        {
            public float offset;
            public int tick;
        }

        private static readonly Dictionary<Pawn, Dictionary<StatDef, Entry>> cache =
            new Dictionary<Pawn, Dictionary<StatDef, Entry>>();
        private static int lastCleanupTick = -1;

        static GearStatOffsetCacheInit()
        {
            try
            {
                Type partType = AccessTools.TypeByName("RimWorld.StatPart_GearStatOffset");
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
                Harmony harmony = new Harmony("local.hskfixpack.gearstatoffsetcache");
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(GearStatOffsetCacheInit), "Prefix"),
                    postfix: new HarmonyMethod(typeof(GearStatOffsetCacheInit), "Postfix"));
                Log.Message("[HSKFix] GearStatOffsetCache: cached StatPart_GearStatOffset offset (TTL " + TTL + " ticks)");
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] GearStatOffsetCache patch failed: " + e);
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
                    val += e.offset;
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
                if (pawn == null)
                {
                    return;
                }
                float offset = val - __state;
                Dictionary<StatDef, Entry> d;
                if (!cache.TryGetValue(pawn, out d))
                {
                    d = new Dictionary<StatDef, Entry>();
                    cache[pawn] = d;
                }
                d[part.parentStat] = new Entry { offset = offset, tick = GenTicks.TicksGame };
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

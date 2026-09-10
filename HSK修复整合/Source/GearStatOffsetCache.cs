// 缓存原版 StatPart_GearStatOffset.TransformValue 的装备偏移结果。
//
// DPA profile 实测:StatPart_GearStatOffset(Stat 分类 0.033ms + TransformValue 0.018ms)
// 是仅次于 STL 工具的属性热点。它每次被调用遍历 pawn.apparel.WornApparel + 武器,
// 对每件装备做 GetStatValue(apparelStat) + StatWorker.StatOffsetFromGear(递归应用
// 该 stat 的全部 StatPart),是加法型偏移(非乘法)。
//
// 方案:与工具缓存同款 —— __state 记输入值,postfix 用 val_after - val_before 反推
// 偏移量并缓存,按 (pawn, parentStat) 键,TTL 120 tick(2 秒)兜底 + **装备指纹即时失效**
// (2026-09-08: 件数/各件 thingIDNumber/主手武器混合哈希,换装穿脱当 tick 就重算,
//  不再依赖 TTL → 根治"脱衣服后舒适温度延迟 2 秒";诊断日志同时下线)。
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
            public int fp;                                       // 缓存时装备指纹(穿着/武器变动即时失效)
        }

        private static readonly Dictionary<Pawn, Dictionary<StatDef, Entry>> cache =
            new Dictionary<Pawn, Dictionary<StatDef, Entry>>();
        private static int lastCleanupTick = -1;

        // 装备指纹(2026-09-08): 件数 + 各件 thingIDNumber + 主手武器 id 的混合累乘。
        // 取代原「只比穿着件数」的诊断判据, 并升级为真正的失效依据 —— 换装/穿脱的当个
        // tick 就重算, 根治「脱衣服后舒适温度还挂着旧值」; 同时诊断日志全部下线
        // (09-08 实测该诊断 735 行/局, 其中大量是 worn 4↔3 摆动但偏移值根本没变,
        //  纯噪声 + 每次摆动 4 行字符串分配)。
        // 成本: 几到十几次整数乘加(遍历的是内部 List 字段, 零分配), 远低于被缓存掉的
        // 那次真实计算(逐件 GetStatValue + StatWorker.StatOffsetFromGear 递归 StatPart)。
        private static int ApparelFingerprint(Pawn pawn)
        {
            if (pawn.apparel == null)
            {
                return -1;
            }
            List<Apparel> worn = pawn.apparel.WornApparel;
            if (worn == null)
            {
                return -1;
            }
            int h = 17;
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i] != null)
                {
                    h = h * 31 + worn[i].thingIDNumber;
                }
            }
            ThingWithComps prim = pawn.equipment != null ? pawn.equipment.Primary : null;
            h = h * 31 + worn.Count;
            h = h * 31 + (prim != null ? prim.thingIDNumber : 0);
            return h;
        }

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
                Log.Message("[HSKFix] GearStatOffsetCache: cached StatPart_GearStatOffset offset (TTL " + TTL + " ticks, 诊断日志=变化触发 STALE/CHANGE)");
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
                if (d.TryGetValue(part.parentStat, out e) && GenTicks.TicksGame - e.tick <= TTL &&
                    ApparelFingerprint(pawn) == e.fp)
                {
                    val += e.offset;
                    return false;
                }
                return true;                                     // 装备指纹变了 → 当 tick 重算
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
                d[part.parentStat] = new Entry
                {
                    offset = offset,
                    tick = GenTicks.TicksGame,
                    fp = ApparelFingerprint(pawn)
                };
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

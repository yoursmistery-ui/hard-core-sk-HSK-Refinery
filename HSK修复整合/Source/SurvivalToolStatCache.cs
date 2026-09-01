// 缓存 SurvivalToolsLite 的 GetBestSurvivalTool,降低属性(Stat)计算的每 tick 开销。
//
// 根因(DPA profile 实测):StatPart_SurvivalTool:TransformValue 是属性计算里最大的
// 单项(约 0.07ms/tick,3.1%),它每次被调用都走:
//   HasSurvivalToolFor → GetBestSurvivalTool → GetAllUsableSurvivalTools
//     (equipment LINQ Where + GetHeldSurvivalTools().ToList() + Where(IndexOf) 二次
//      遍历 + Concat + 再 ToList())  → 再双循环找最佳工具
// 人多/工具多时被工作速度类属性高频调用(48 号给 SmeltingSpeed/GeneralLaborSpeed 等
// 十几项属性挂了 StatPart_SurvivalTool),产生大量 LINQ 分配与重复扫描。
//
// 方案:按 (pawn, stat) 缓存最佳工具,TTL 60 tick(1 秒)。工具装卸/捡起的变化最多
// 延迟 1 秒反映到工作速度,游戏内无感。缓存对象用 object 持有(不编译期引用 STL),
// 未装 SurvivalToolsLite 时 TypeByName 返回 null,自动跳过、零副作用。
//
// 清理:每 60000 tick(1 游戏日)剔除已销毁的 pawn 条目,防止缓存泄漏。
// 编译:并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SurvivalToolStatCache
{
    [StaticConstructorOnStartup]
    public static class SurvivalToolStatCacheInit
    {
        private const int TTL = 60;
        private const int CleanupInterval = 60000;

        private struct Entry
        {
            public object tool;
            public int tick;
        }

        private static readonly Dictionary<Pawn, Dictionary<StatDef, Entry>> cache =
            new Dictionary<Pawn, Dictionary<StatDef, Entry>>();
        private static int lastCleanupTick = -1;

        static SurvivalToolStatCacheInit()
        {
            try
            {
                Type util = AccessTools.TypeByName("SurvivalToolsLite.SurvivalToolUtility");
                if (util == null)
                {
                    return;
                }
                MethodInfo method = AccessTools.Method(util, "GetBestSurvivalTool",
                    new Type[] { typeof(Pawn), typeof(StatDef) });
                if (method == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.survivaltoolstatcache");
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(SurvivalToolStatCacheInit), "Prefix"),
                    postfix: new HarmonyMethod(typeof(SurvivalToolStatCacheInit), "Postfix"));
                Log.Message("[HSKFix] SurvivalToolStatCache: cached GetBestSurvivalTool (TTL " + TTL + " ticks)");
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] SurvivalToolStatCache patch failed: " + e);
            }
        }

        private static bool Prefix(Pawn pawn, StatDef stat, ref object __result)
        {
            try
            {
                if (pawn == null || stat == null)
                {
                    return true;
                }
                Dictionary<StatDef, Entry> d;
                if (!cache.TryGetValue(pawn, out d))
                {
                    return true;
                }
                Entry e;
                if (d.TryGetValue(stat, out e) && GenTicks.TicksGame - e.tick <= TTL)
                {
                    __result = e.tool;
                    return false;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }

        private static void Postfix(Pawn pawn, StatDef stat, object __result)
        {
            try
            {
                if (pawn == null || stat == null)
                {
                    return;
                }
                Dictionary<StatDef, Entry> d;
                if (!cache.TryGetValue(pawn, out d))
                {
                    d = new Dictionary<StatDef, Entry>();
                    cache[pawn] = d;
                }
                d[stat] = new Entry { tool = __result, tick = GenTicks.TicksGame };
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

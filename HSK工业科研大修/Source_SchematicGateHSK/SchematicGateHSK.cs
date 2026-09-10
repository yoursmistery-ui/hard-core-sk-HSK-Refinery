// ============================================================================
//  SchematicGateHSK — MO(中世纪大修)图纸门禁体系 独立子模块 (v1.0, 2026-08-27)
// ============================================================================
//  语义复刻自 MedievalOverhaul.dll 反编译 (ResearchProjectsDefs / ResearchProjectDef_CanBeResearchedAt):
//    1) 研究节点挂 SchematicRequiredExt(schematicDef=X) → X 图纸书未就位时不可开始研究。
//    2) "就位" = 任一殖民地的研究台, 其所在房间(Building_Bookcase.HeldBooks)的书架里有该图纸书。
//       - CanStartNow: 全局存在性(跨图扫描, 按 schematicDef 缓存 250 tick)
//       - CanBeResearchedAt(bench): 按研究台逐台判定 (缓存 per-bench 250 tick, 修复 MO 原版
//         全局单槽缓存会跨研究台串结果的 bug)
//    3) 图纸书 = 原版 Book: 放置/书架存储; 阅读(原版休闲读书或研读)时按 OnReadingTick 分摊
//       加目标科技科研值 (ReadingOutcomeDoerGainResearch 机制, 同 MO GainResearchDefinable)。
//    4) 获取: 商人 StockGenerator_SchematicTrader(按稀有度掉 0~1 本) + BuyTradeTag 回购;
//       纯数据驱动, 不打其他 mod 补丁。
//
//  独立程序集, 不触碰 BlueprintUnlockHSK(蓝图书系列解锁制) — 两套门禁并存、目标节点互不重叠。
// ============================================================================
using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SchematicGateHSK
{
    // ---- Harmony 注册 -------------------------------------------------------
    [StaticConstructorOnStartup]
    public static class SchematicGateInit
    {
        static SchematicGateInit()
        {
            Harmony harmony = new Harmony("local.ratkin.schematicgate");
            try
            {
                harmony.Patch(
                    AccessTools.PropertyGetter(typeof(ResearchProjectDef), "CanStartNow"),
                    postfix: new HarmonyMethod(typeof(Patch_CanStartNow).GetMethod("Postfix")));
                harmony.Patch(
                    AccessTools.Method(typeof(ResearchProjectDef), "CanBeResearchedAt",
                        new Type[] { typeof(Building_ResearchBench), typeof(bool) }),
                    postfix: new HarmonyMethod(typeof(Patch_CanBeResearchedAt).GetMethod("Postfix")));
            }
            catch (Exception e)
            {
                Log.Error("[SchematicGateHSK] patch failed: " + e);
            }
        }
    }

    // ---- 数据层 -------------------------------------------------------------
    // 挂在 ResearchProjectDef 上: 该科技需要指定图纸书"就位"。
    public class SchematicRequiredExt : DefModExtension
    {
        public ThingDef schematicDef;
    }

    // 挂在图纸书 ThingDef 上: 阅读加哪门科技的科研值。
    public class SchematicBookExt : DefModExtension
    {
        public string targetTech = "";
        public float gainMultiplier = 1f;

        public static SchematicBookExt Get(ThingDef def)
        {
            if (def == null)
            {
                return null;
            }
            return def.GetModExtension<SchematicBookExt>();
        }
    }

    public static class SchematicDatabase
    {
        private static List<ThingDef> books; // 懒加载缓存

        public static List<ThingDef> AllBookDefs()
        {
            if (books == null)
            {
                books = new List<ThingDef>();
                List<ThingDef> all = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int i = 0; i < all.Count; i++)
                {
                    if (SchematicBookExt.Get(all[i]) != null)
                    {
                        books.Add(all[i]);
                    }
                }
            }
            return books;
        }
    }

    // ---- 门禁判定 -----------------------------------------------------------
    // 书架检查仿 MO: 研究台所在房间(room.region)内全部 Building_Bookcase 的 HeldBooks。
    public static class SchematicPresence
    {
        private const int CacheTicks = 250;

        // CanStartNow: 按 schematicDef 全局缓存
        private class StartEntry
        {
            public bool value;
            public int staleAt;
        }
        private static readonly Dictionary<ThingDef, StartEntry> startCache = new Dictionary<ThingDef, StartEntry>();

        // CanBeResearchedAt: 按研究台缓存 (修 MO 单槽串台 bug)
        private class BenchEntry
        {
            public ThingDef def;
            public bool value;
            public int staleAt;
        }
        private static readonly Dictionary<Building_ResearchBench, BenchEntry> benchCache =
            new Dictionary<Building_ResearchBench, BenchEntry>();

        public static bool HasInAnyBenchRoom(ThingDef schematicDef)
        {
            int now = Find.TickManager.TicksGame;
            StartEntry e;
            if (startCache.TryGetValue(schematicDef, out e) && now < e.staleAt)
            {
                return e.value;
            }
            bool found = false;
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count && !found; i++)
            {
                List<Building> blist = maps[i].listerBuildings.allBuildingsColonist;
                for (int j = 0; j < blist.Count && !found; j++)
                {
                    Building_ResearchBench bench = blist[j] as Building_ResearchBench;
                    if (bench != null && BenchHasBook(bench, schematicDef))
                    {
                        found = true;
                    }
                }
            }
            if (e == null)
            {
                e = new StartEntry();
                startCache[schematicDef] = e;
            }
            e.value = found;
            e.staleAt = now + CacheTicks;
            return found;
        }

        public static bool HasInBenchRoom(Building_ResearchBench bench, ThingDef schematicDef)
        {
            int now = Find.TickManager.TicksGame;
            BenchEntry e;
            if (benchCache.TryGetValue(bench, out e) && e.def == schematicDef && now < e.staleAt)
            {
                return e.value;
            }
            bool v = BenchHasBook(bench, schematicDef);
            if (e == null)
            {
                e = new BenchEntry();
                benchCache[bench] = e;
            }
            e.def = schematicDef;
            e.value = v;
            e.staleAt = now + CacheTicks;
            return v;
        }

        private static bool BenchHasBook(Building_ResearchBench bench, ThingDef schematicDef)
        {
            // 与 MO 一致: 研究台所在房间的 uniqueContainedThings 找书架
            Room room = RegionAndRoomQuery.GetRoom((Thing)(object)bench, RegionType.Set_All);
            if (room == null)
            {
                return false;
            }
            List<Thing> things = new List<Thing>(room.ContainedThings<Thing>());
            for (int i = 0; i < things.Count; i++)
            {
                Building_Bookcase bookcase = things[i] as Building_Bookcase;
                if (bookcase == null)
                {
                    continue;
                }
                IReadOnlyList<Book> held = bookcase.HeldBooks;
                for (int j = 0; j < held.Count; j++)
                {
                    if (((Thing)held[j]).def == schematicDef)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    // ---- Harmony 补丁 -------------------------------------------------------
    [HarmonyPatch]
    public static class Patch_CanStartNow
    {
        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            SchematicRequiredExt ext = __instance.GetModExtension<SchematicRequiredExt>();
            if (ext == null || ext.schematicDef == null)
            {
                return;
            }
            if (!SchematicPresence.HasInAnyBenchRoom(ext.schematicDef))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_CanBeResearchedAt
    {
        public static void Postfix(ResearchProjectDef __instance, Building_ResearchBench bench, ref bool __result)
        {
            if (!__result || bench == null)
            {
                return;
            }
            SchematicRequiredExt ext = __instance.GetModExtension<SchematicRequiredExt>();
            if (ext == null || ext.schematicDef == null)
            {
                return;
            }
            if (!SchematicPresence.HasInBenchRoom(bench, ext.schematicDef))
            {
                __result = false;
            }
        }
    }

    // ---- 书籍效果 (阅读加科研值, 仿 MO GainResearchDefinable) ----------------
    public class SchematicBookProperties : BookOutcomeProperties
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(SchematicBookDoer);
            }
        }
    }

    public class SchematicBookDoer : ReadingOutcomeDoerGainResearch
    {
        public override void OnBookGenerated(Pawn author = null)
        {
            base.OnBookGenerated(author);
            values.Clear();
            SchematicBookExt ext = SchematicBookExt.Get(Book.def);
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return;
            }
            ResearchProjectDef proj = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
            if (proj == null)
            {
                return;
            }
            values[proj] = base.GetBaseValue() * ext.gainMultiplier;
        }

        public override string GetBenefitsString(Pawn reader = null)
        {
            SchematicBookExt ext = SchematicBookExt.Get(Book.def);
            if (ext == null)
            {
                return "";
            }
            ResearchProjectDef proj = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
            if (proj == null)
            {
                return "";
            }
            return "阅读时推进【" + (string)proj.LabelCap + "】研究进度; 放入研究台同房间书架可解锁开始该研究";
        }
        // 超链接由 ThingDef XML <descriptionHyperlinks> 提供 (Verse.DefHyperlink), 无需代码
    }

    // ---- 商人投放 -----------------------------------------------------------
    // 稀有掉落 0~N 本图纸书 (仿蓝图书 generator, 独立池)。tier 暂不分档: 池子小, 靠权重稀缺。
    public class StockGenerator_SchematicTrader : StockGenerator
    {
        public override IEnumerable<Thing> GenerateThings(PlanetTile tile, Faction factionForStockGen = null)
        {
            List<ThingDef> pool = SchematicDatabase.AllBookDefs();
            if (pool.Count == 0)
            {
                yield break;
            }
            // 每请求一次, 35% 概率携带一本随机图纸 (多商队累计自然铺开)
            if (Rand.Value < 0.35f)
            {
                ThingDef picked = pool.RandomElement();
                Thing t = ThingMaker.MakeThing(picked);
                if (t != null)
                {
                    yield return t;
                }
            }
        }

        public override bool HandlesThingDef(ThingDef td)
        {
            return td != null && SchematicBookExt.Get(td) != null;
        }
    }
}

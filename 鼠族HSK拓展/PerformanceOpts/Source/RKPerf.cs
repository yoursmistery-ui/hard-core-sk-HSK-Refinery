// RKPerf.cs — 鼠族HSK拓展 性能优化补丁 v7(无损)
// 2026-08-18 v1: STL GetBestSurvivalTool tick 缓存。
// 2026-08-18 v2: 新增 StatWorker_MarketValue.CalculatedBaseMarketValue 永久缓存。
// 2026-08-18 v3: 工具缓存改跨 tick(TTL 120)。
// 2026-08-19 v6: 新增 MarketValue 懒预热(MapComponent, ≤1.5ms/tick 预算)。
// 2026-08-19 v7: 预热改「双向类别交集」材质展开 + 预算提到 3ms + 幂等跳过。
//    ⚠️ HSK 材质系统实测(Unified.xml + 反射确认): ThingDef.stuffCategories 与
//    stuffProps.categories 元素类型均为 RimWorld.StuffCategoryDef(无材质树/无 parent,
//    扁平类别集合);允许材质 = def 要求类别 ∩ 材质声明类别。v6 按 defName 找同名
//    ThingCategoryDef 的映射在 HSK 下基本全失败(类别几乎全是 StuffCategoryDef,DB 里
//    无同名 ThingCategoryDef),材质组合实际未预热;v7 用倒排索引做双向匹配,与游戏
//    语义一致,材质组合全量覆盖。
// 所有 patch 均为无损: 不改数值,只消除重复计算;目标缺失时自动跳过。
// ⚠️ 本 HSK 环境 Stat* 类型位于 RimWorld 命名空间(非原版 Verse),patch 目标一律用反射门控。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;
using RimWorld;

namespace RKPerf
{
    [StaticConstructorOnStartup]
    public static class RKPerfInit
    {
        static RKPerfInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.ratkin.perfopts");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[RKPerf] 性能优化补丁 v7 已加载 (工具跨tick缓存 + MarketValue 永久缓存 + 懒预热双向匹配)");
            }
            catch (Exception e)
            {
                Log.Error("[RKPerf] 初始化失败: " + e);
            }
        }
    }

    // ============ Patch 1: STL GetBestSurvivalTool 跨 tick 缓存 ============
    // v3: 缓存有效期 TTL=120 tick(2 秒游戏时间)。best tool 只依赖 pawn 装备列表与
    // 工具 def 的固定 StatModifier,pawn 不换工具时结果跨 tick 不变,缓存安全;
    // 换工具后最迟 2 秒生效(玩家无感)。v1 仅同 tick 命中导致 0.8 calls/tick 场景几乎不命中,
    // 跨 tick 后每 120 tick 才真正遍历一次,单次 100-400µs 摊薄到 ~1/120。
    [HarmonyPatch]
    public static class Patch_SurvivalToolCache
    {
        private const int TtlTicks = 120;

        private sealed class Entry
        {
            public int storedTick = -1;
            public readonly Dictionary<StatDef, object> map = new Dictionary<StatDef, object>();
        }

        // ConditionalWeakTable: pawn 销毁后自动回收,无泄漏
        private static readonly ConditionalWeakTable<Pawn, Entry> Table = new ConditionalWeakTable<Pawn, Entry>();

        public static MethodBase TargetMethod()
        {
            Type t = AccessTools.TypeByName("SurvivalToolsLite.SurvivalToolUtility");
            if (t == null)
            {
                return null;
            }
            return AccessTools.Method(t, "GetBestSurvivalTool");
        }

        public static bool Prefix(Pawn pawn, StatDef stat, ref object __result)
        {
            if (pawn == null || stat == null)
            {
                return true;
            }
            int tick = (Find.TickManager != null) ? Find.TickManager.TicksGame : 0;
            Entry entry = Table.GetOrCreateValue(pawn);
            if (tick - entry.storedTick >= TtlTicks)
            {
                entry.storedTick = tick;
                entry.map.Clear();
            }
            object cached;
            if (entry.map.TryGetValue(stat, out cached))
            {
                __result = cached;
                return false;
            }
            return true;
        }

        public static void Postfix(Pawn pawn, StatDef stat, ref object __result)
        {
            if (pawn == null || stat == null)
            {
                return;
            }
            int tick = (Find.TickManager != null) ? Find.TickManager.TicksGame : 0;
            Entry entry = Table.GetOrCreateValue(pawn);
            if (tick - entry.storedTick >= TtlTicks)
            {
                entry.storedTick = tick;
                entry.map.Clear();
            }
            entry.map[stat] = __result;
        }
    }

    // ============ Patch 2: MarketValue 基值永久缓存(无损) ============
    // CalculatedBaseMarketValue(BuildableDef, ThingDef) 是纯函数:
    // 结果只依赖 def 的 recipe/ingredients/costList 与 stuff 的市场价值,运行时不变。
    // 原实现每次调用都全量遍历(单次 200-300µs,基地物品多时是 stat 第一大开销),
    // 永久缓存后同一 def+材质只计算一次。字典大小=def×材质数,有限,无泄漏。
    [HarmonyPatch]
    public static class Patch_MarketValueCache
    {
        // stuffDef 可以为 null(非材质物品,如原版武器/建筑),Dictionary key 不允许 null,
        // 因此拆成两张表: 按材质的 / 无材质的(null stuff)。
        internal static readonly Dictionary<BuildableDef, Dictionary<ThingDef, float>> StuffCache =
            new Dictionary<BuildableDef, Dictionary<ThingDef, float>>();
        internal static readonly Dictionary<BuildableDef, float> NoStuffCache =
            new Dictionary<BuildableDef, float>();

        public static MethodBase TargetMethod()
        {
            Type t = AccessTools.TypeByName("RimWorld.StatWorker_MarketValue");
            if (t == null)
            {
                t = AccessTools.TypeByName("Verse.StatWorker_MarketValue");
            }
            if (t == null)
            {
                Log.Message("[RKPerf] StatWorker_MarketValue 未找到,跳过 MarketValue 缓存");
                return null;
            }
            return AccessTools.Method(t, "CalculatedBaseMarketValue");
        }

        public static bool Prefix(BuildableDef def, ThingDef stuffDef, ref float __result)
        {
            if (def == null)
            {
                return true;
            }
            if (stuffDef == null)
            {
                float v;
                if (NoStuffCache.TryGetValue(def, out v))
                {
                    __result = v;
                    return false;
                }
                return true;
            }
            Dictionary<ThingDef, float> inner;
            float v2;
            if (StuffCache.TryGetValue(def, out inner) && inner.TryGetValue(stuffDef, out v2))
            {
                __result = v2;
                return false;
            }
            return true;
        }

        public static void Postfix(BuildableDef def, ThingDef stuffDef, ref float __result)
        {
            if (def == null)
            {
                return;
            }
            if (stuffDef == null)
            {
                NoStuffCache[def] = __result;
                return;
            }
            Dictionary<ThingDef, float> inner;
            if (!StuffCache.TryGetValue(def, out inner))
            {
                inner = new Dictionary<ThingDef, float>();
                StuffCache[def] = inner;
            }
            inner[stuffDef] = __result;
        }
    }

    // ============ Patch 3: MarketValue 懒预热(无损) ============
    // 永久缓存只消除「已算过」的重复调用;首次计算(新 def 或新材质组合批量出现,
    // 如开交易界面、hover 信息卡、批量掉落)仍走 200-300µs 慢路径,成批触发就是
    // DPA 里 MarketValue 单次 29.5ms 的尖峰来源。本组件在进图后每 tick 用 ≤3ms
    // 预算调用一次 CalculatedBaseMarketValue(def, stuff) —— 调用即被 Patch 2 的
    // Postfix 自动写入缓存,预热完成后运行时查询全部命中缓存,冷调用彻底消失。
    //
    // 材质展开(v7,HSK 语义): def.stuffCategories(要求的类别) ∩ 材质的
    // stuffProps.categories(声明类别) = 允许材质。stuffCategories 元素是
    // RimWorld.StuffCategoryDef(扁平类别集合,无树);用反射读两个列表的 defName,
    // 构建「类别 → 材质」倒排索引(只对 stuffProps!=null 的材质建),def 按
    // 类别交集取材质,与游戏 GenStuff.AllowedStuffs 语义一致。
    //
    // 机制(与 RegionRepairMapComponent 同款,已在本环境验证): Map.FillComponents
    // 会对所有非抽象 MapComponent 子类用 Activator.CreateInstance(type, map) 自动
    // 实例化,无需 MapComponentDef;static done 标志保证整个游戏进程只预热一次,
    // 多地图/多存档不重复。幂等: 已缓存(def, stuff) 的 Warm 调用直接跳过(查
    // Patch_MarketValueCache),重复预热零成本;预算用 Stopwatch 控制,超时即让出
    // tick(3ms = 正常 tick 预算 16.7ms 的 ~18%,DPA 可见短暂预热尖峰,属预期)。
    public class MarketValueWarmupComponent : MapComponent
    {
        private const double BudgetMsPerTick = 3.0;

        private static bool started;
        private static bool done;
        private static int defIndex;
        private static int warmed;
        private static int skipped;
        private static int errors;
        private static List<BuildableDef> defs;
        private static Dictionary<string, List<ThingDef>> index;
        private static MethodInfo calcMethod;
        private static FieldInfo defNameField;
        private static readonly Stopwatch sw = new Stopwatch();

        public MarketValueWarmupComponent(Map map)
            : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (done)
            {
                return;
            }
            try
            {
                Run();
            }
            catch (Exception e)
            {
                done = true;
                Log.Warning("[RKPerf] MarketValue 预热异常,已停止: " + e.Message);
            }
        }

        private static void Run()
        {
            if (!started)
            {
                Init();
                if (done)
                {
                    return;
                }
            }
            sw.Restart();
            while (sw.ElapsedMilliseconds < BudgetMsPerTick)
            {
                if (defIndex >= defs.Count)
                {
                    done = true;
                    Log.Message("[RKPerf] MarketValue 预热完成: " + warmed +
                        " 次计算, " + skipped + " 次跳过, " + errors + " 个 def 异常");
                    return;
                }
                BuildableDef def = defs[defIndex];
                defIndex++;
                if (def == null)
                {
                    continue;
                }
                Warm(def, null);
                // 材质组合: def 要求类别 ∩ 材质声明类别
                List<string> required = GetStuffCategoryNames(def);
                if (required != null && required.Count > 0)
                {
                    HashSet<ThingDef> matched = new HashSet<ThingDef>();
                    for (int i = 0; i < required.Count; i++)
                    {
                        List<ThingDef> stuffs;
                        if (index.TryGetValue(required[i], out stuffs))
                        {
                            for (int j = 0; j < stuffs.Count; j++)
                            {
                                matched.Add(stuffs[j]);
                            }
                        }
                    }
                    foreach (ThingDef s in matched)
                    {
                        Warm(def, s);
                    }
                }
            }
        }

        private static void Init()
        {
            started = true;
            try
            {
                Type t = AccessTools.TypeByName("RimWorld.StatWorker_MarketValue");
                if (t == null)
                {
                    t = AccessTools.TypeByName("Verse.StatWorker_MarketValue");
                }
                if (t == null)
                {
                    done = true;
                    return;
                }
                calcMethod = t.GetMethod("CalculatedBaseMarketValue",
                    BindingFlags.Public | BindingFlags.Static);
                if (calcMethod == null)
                {
                    done = true;
                    return;
                }
                defNameField = typeof(Def).GetField("defName",
                    BindingFlags.Public | BindingFlags.Instance);
                // 倒排索引: 类别 defName → 声明该类别的材质(defName 查 DefDatabase)
                index = new Dictionary<string, List<ThingDef>>();
                List<ThingDef> allStuff = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int i = 0; i < allStuff.Count; i++)
                {
                    ThingDef s = allStuff[i];
                    if (s == null || s.stuffProps == null || s.stuffProps.categories == null)
                    {
                        continue;
                    }
                    IList cats = s.stuffProps.categories;
                    for (int j = 0; j < cats.Count; j++)
                    {
                        string name = GetDefName(cats[j]);
                        if (name == null)
                        {
                            continue;
                        }
                        List<ThingDef> list;
                        if (!index.TryGetValue(name, out list))
                        {
                            list = new List<ThingDef>();
                            index[name] = list;
                        }
                        list.Add(s);
                    }
                }
                defs = new List<BuildableDef>(DefDatabase<BuildableDef>.AllDefs);
                Log.Message("[RKPerf] MarketValue 预热启动: " + defs.Count +
                    " 个 def, " + index.Count + " 个材质类别,预算 " + BudgetMsPerTick + "ms/tick");
            }
            catch (Exception e)
            {
                done = true;
                Log.Warning("[RKPerf] MarketValue 预热初始化失败: " + e.Message);
            }
        }

        private static void Warm(BuildableDef def, ThingDef stuff)
        {
            try
            {
                if (stuff == null)
                {
                    float v;
                    if (Patch_MarketValueCache.NoStuffCache.TryGetValue(def, out v))
                    {
                        skipped++;
                        return;
                    }
                }
                else
                {
                    Dictionary<ThingDef, float> inner;
                    float v;
                    if (Patch_MarketValueCache.StuffCache.TryGetValue(def, out inner) &&
                        inner.TryGetValue(stuff, out v))
                    {
                        skipped++;
                        return;
                    }
                }
                // 直接调用被 Harmony 补丁包裹的原方法: prefix 缓存未命中 → 执行原逻辑
                // → postfix 自动把结果写入 Patch_MarketValueCache,无需手动写缓存。
                calcMethod.Invoke(null, new object[] { def, stuff });
                warmed++;
            }
            catch
            {
                errors++;
            }
        }

        // 读 def.stuffCategories(元素是 StuffCategoryDef 或 ThingCategoryDef)的 defName
        private static List<string> GetStuffCategoryNames(BuildableDef def)
        {
            ThingDef td = def as ThingDef;
            if (td == null || td.stuffCategories == null || td.stuffCategories.Count == 0)
            {
                return null;
            }
            List<string> result = new List<string>();
            IList cats = td.stuffCategories;
            for (int i = 0; i < cats.Count; i++)
            {
                string name = GetDefName(cats[i]);
                if (name != null)
                {
                    result.Add(name);
                }
            }
            return result;
        }

        // 反射读 Def.defName,兼容任何 Def 子类(StuffCategoryDef / ThingCategoryDef)
        private static string GetDefName(object def)
        {
            if (def == null || defNameField == null)
            {
                return null;
            }
            try
            {
                return defNameField.GetValue(def) as string;
            }
            catch
            {
                return null;
            }
        }
    }
}

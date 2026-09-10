using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

// ============================================================================
//  宝石暴击(MO RecipeExtension_Mine 的等价复刻, 零 MO DLL)
// ----------------------------------------------------------------------------
//  MO 原机制: 每 600 tick 工时 1% 概率额外掉 1 个宝石(6 档)+ 金矿。
//  我们: RecipeDef 挂 MineCritProperties, Harmony postfix 挂在
//        GenRecipe.MakeRecipeProducts 出口, 按配方工时折算概率:
//            chance = critChance * (recipe.workAmount / workPerRoll)
//        → 单次小额配方(采 1 煤 200 工时)概率低, 批量配方(1000 工时)概率高, 与 MO 同曲线。
//  性能(AGENTS §9): 只在"配方产物生成"这一事件点触发, 无 tick、无轮询、无反射缓存缺失;
//        扩展对象在首次读取后缓存在 RecipeDef 上(GetModExtension 自带缓存)。
//  掉落物: 用 ThingMaker 直接造, 走同一 dominantIngredient 逻辑无关(宝石非 stuff)。
// ============================================================================
namespace BlueprintUnlockHSK
{
    public class MineCritProperties : DefModExtension
    {
        public float critChance = 0.01f;      // 每 workPerRoll 工时的暴击概率
        public int workPerRoll = 600;
        public List<string> critThings = new List<string>();
        public int critCountMin = 1;
        public int critCountMax = 1;
        public string letterLabel;            // 可空: 不弹信
        public string letterText;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            if (critThings == null || critThings.Count == 0)
            {
                yield return "critThings 为空, 暴击不会产出任何东西";
            }
            if (workPerRoll <= 0)
            {
                yield return "workPerRoll 必须 > 0";
            }
        }
    }

    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts", new Type[]
    {
        typeof(RecipeDef), typeof(Pawn), typeof(List<Thing>), typeof(Thing), typeof(IBillGiver),
        typeof(Precept_ThingStyle), typeof(ThingStyleDef), typeof(int?)
})]
    internal static class Patch_MakeRecipeProducts_Crit
    {
        private static readonly HashSet<RecipeDef> _missing = new HashSet<RecipeDef>();

        public static void Postfix(RecipeDef recipeDef, ref IEnumerable<Thing> __result)
        {
            if (recipeDef == null || __result == null)
            {
                return;
            }
            MineCritProperties ext = recipeDef.GetModExtension<MineCritProperties>();
            if (ext == null || ext.critThings == null || ext.critThings.Count == 0 || ext.workPerRoll <= 0)
            {
                return;
            }
            float chance = ext.critChance * (recipeDef.workAmount / (float)ext.workPerRoll);
            if (chance > 0.35f)
            {
                chance = 0.35f;   // 上限保护: 批量配方也不该必出
            }
            if (!Rand.Chance(chance))
            {
                return;
            }
            List<Thing> products = new List<Thing>(__result);
            int lo = Math.Max(1, ext.critCountMin);
            int hi = Math.Max(lo, ext.critCountMax);
            int n = Rand.RangeInclusive(lo, hi);
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                string name = ext.critThings.RandomElement<string>();
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null)
                {
                    if (_missing.Add(recipeDef))
                    {
                        Log.Warning("[BlueprintUnlockHSK] 配方 " + recipeDef.defName + " 的宝石暴击池缺 def: " + name);
                    }
                    continue;
                }
                Thing t = ThingMaker.MakeThing(def);
                if (t == null)
                {
                    continue;
                }
                t.stackCount = Math.Min(def.stackLimit, Rand.RangeInclusive(1, 3));
                products.Add(t);
                any = true;
            }
            if (!any)
            {
                __result = products;
                return;
            }
            if (!ext.letterLabel.NullOrEmpty())
            {
                Find.LetterStack.ReceiveLetter(ext.letterLabel,
                    (ext.letterText ?? "岩层里露出一层闪亮的晶体——这次采掘挖到了宝石。").Replace("{0}", recipeDef.LabelCap),
                    LetterDefOf.PositiveEvent, null, null, null);
            }
            __result = products;
        }
    }

    // ============================================================================
    //  石材按地面岩种出料(替代 MO 矿井逐岩种 5 条配方)
    // ----------------------------------------------------------------------------
    //  矿井/采掘点只能建在可打磨岩石地面上, 于是"采掘石料"这一条配方在运行时读建筑
    //  所在格地形(RoughGraniteFloor 等), 去掉 Rough/Floor 段拼成 Chunk<岩种> 产出;
    //  拼不出对应碎块(火山岩/已采掘地面等)时回落 fallback(碎石 CrushedStone)。
    //  性能(AGENTS §9): 只在产物生成事件点触发一次字符串处理+查表, 无 tick、无遍历、无反射。
    // ============================================================================
    public class MineStoneByTerrainProperties : DefModExtension
    {
        public string fallback = "CrushedStone";   // 无法识别岩种时的回落产物
    }

    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts", new Type[]
    {
        typeof(RecipeDef), typeof(Pawn), typeof(List<Thing>), typeof(Thing), typeof(IBillGiver),
        typeof(Precept_ThingStyle), typeof(ThingStyleDef), typeof(int?)
    })]
    internal static class Patch_MineStoneByTerrain
    {
        private const string ChunkPrefix = "Chunk";
        private const string RoughPrefix = "Rough";
        private const string FloorSuffix = "Floor";

        public static void Postfix(RecipeDef recipeDef, ref IEnumerable<Thing> __result, IBillGiver billGiver)
        {
            if (recipeDef == null || __result == null)
            {
                return;
            }
            MineStoneByTerrainProperties ext = recipeDef.GetModExtension<MineStoneByTerrainProperties>();
            if (ext == null)
            {
                return;
            }
            List<Thing> products = new List<Thing>(__result);
            // 只在含 Chunk* 占位产物时介入, 避免误改其它配方
            bool touched = false;
            for (int i = 0; i < products.Count; i++)
            {
                Thing t = products[i];
                if (t == null || t.def == null || !t.def.defName.StartsWith(ChunkPrefix))
                {
                    continue;
                }
                if (!touched)
                {
                    ThingDef target = ResolveChunk(billGiver, ext) ?? ResolveFallback(ext);
                    if (target == null || target == t.def)
                    {
                        return;   // 目标与占位一致或查不到, 原样保留
                    }
                    _targetCache = target;   // 复用到循环外
                    touched = true;
                }
                Thing replacement = ThingMaker.MakeThing(_targetCache);
                if (replacement == null)
                {
                    continue;
                }
                replacement.stackCount = Math.Min(_targetCache.stackLimit, t.stackCount);
                products[i] = replacement;
            }
            if (touched)
            {
                __result = products;
            }
        }

        // 缓存本轮解析出的目标 def(单帧内复用, 避免多条 Chunk 产物重复解析地形)
        private static ThingDef _targetCache;

        private static ThingDef ResolveChunk(IBillGiver billGiver, MineStoneByTerrainProperties ext)
        {
            Building b = billGiver as Building;
            if (b == null || b.Map == null || !b.PositionHeld.IsValid)
            {
                return null;
            }
            TerrainDef terr = b.Map.terrainGrid.TerrainAt(b.PositionHeld);
            if (terr == null)
            {
                return null;
            }
            string rock = terr.defName;
            if (rock.StartsWith(RoughPrefix))
            {
                rock = rock.Substring(RoughPrefix.Length);
            }
            if (rock.EndsWith(FloorSuffix))
            {
                rock = rock.Substring(0, rock.Length - FloorSuffix.Length);
            }
            return DefDatabase<ThingDef>.GetNamedSilentFail(ChunkPrefix + rock);
        }

        private static ThingDef ResolveFallback(MineStoneByTerrainProperties ext)
        {
            if (ext.fallback.NullOrEmpty())
            {
                return null;
            }
            return DefDatabase<ThingDef>.GetNamedSilentFail(ext.fallback);
        }
    }

}

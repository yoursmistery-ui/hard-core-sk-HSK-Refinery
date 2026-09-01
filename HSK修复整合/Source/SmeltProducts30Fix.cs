// 熔炼产物统一 30%(2026-08-10 用户要求;2026-08-11 追加堆叠乘算)
//
// 背景: 原版 Thing.SmeltProducts 只返还 25%,且要求「非精密(intricate)且
// 材料本身 smeltable」,零部件/木头等会被跳过,还需要每个武器单独配
// smeltProducts 才有产出 —— 复杂、缺漏多。用户要求: 默认按 30% 比例返还
// 全部制造材料,不再逐件额外计算。
//
// 方案: Harmony 前缀替换 Verse.Thing.SmeltProducts(float):
//   1) 有制造材料(costListAdjusted 非空)的: 每种材料一律返还
//      单件材料数 × 堆叠数 × 30%(GenMath.RoundRandom,与 1.6.4871 原版
//      同样的取整方式),不再跳过 intricate/不可熔炼材料,也不再叠加
//      smeltProducts(统一 30% 规则,无需逐件维护);
//      ⚠️ 2026-08-11 堆叠乘算: 原版 SmeltProducts 只按「单件」costList 计算,
//      不乘 stackCount —— 武器/衣物单件堆叠=1 无影响,但熔炼弹药
//      (RKHSKSmeltAmmo,specialProducts=Smelted)整批投入 1-100 发时,
//      传入的 Thing 是实际消耗发数的堆叠(SplitOff 后),必须乘 stackCount
//      才能按「每发材料 × 发数 × 30%」结算(反编译 1.6.4871 确认);
//   2) 无制造材料的: 先保留 smeltProducts 兜底(矿渣 ChunkSlagSteel 等,
//      防止 ExtractMetalFromSlag 失去产出);仍无且是弹药的(CE/鼠族弹药
//      ThingDef 一律不写 costList,制造材料只存在于生产配方),从生产配方
//      推导每发材料 —— 每批配料 ÷ 批产量,再乘熔炼发数与 30% 返还;
//      (类别过滤如 SLDBar/Metallic 取该类中 BaseMarketValue 最低的代表物品;
//       ⚠️ 2026-08-18 用户要求: 金属条固定返 钢(Steel/低碳钢)、木板固定返
//       红木板(RedWoodPlank),其余不变 —— 见 AmmoRecipeCostCache.Build);
//   3) 两者都没有的: 直接不返还任何东西(返回空列表;不能返回 null,
//      否则 Core_SK 的 MakeRecipeProducts 对 SmeltProducts 结果 foreach 会崩)。
// 对全部熔炼配方生效(SmeltWeapon / SmeltApparel / SmeltOrDestroyThing 等,
// 均走 specialProducts=Smelted → Thing.SmeltProducts)。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SmeltProducts30Fix
{
    [StaticConstructorOnStartup]
    public static class SmeltProducts30FixInit
    {
        private const float ReturnRatio = 0.30f;

        static SmeltProducts30FixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(Thing), "SmeltProducts", new Type[] { typeof(float) });
                if (target == null)
                {
                    Log.Warning("[SmeltProducts30Fix] Thing.SmeltProducts not found");
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.smeltproducts30");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(SmeltProducts30FixInit).GetMethod(
                        "SmeltProductsPrefix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[SmeltProducts30Fix] patched Thing.SmeltProducts (30% of all materials)");
            }
            catch (Exception e)
            {
                Log.Error("[SmeltProducts30Fix] patch failed: " + e);
            }
        }

        private static bool SmeltProductsPrefix(Thing __instance, ref IEnumerable<Thing> __result)
        {
            try
            {
                List<ThingDefCountClass> costListAdj = __instance.def.CostListAdjusted(__instance.Stuff);
                List<Thing> products = new List<Thing>();
                if (costListAdj != null && costListAdj.Count > 0)
                {
                    int stack = __instance.stackCount;
                    for (int i = 0; i < costListAdj.Count; i++)
                    {
                        int num = GenMath.RoundRandom((float)costListAdj[i].count * stack * ReturnRatio);
                        if (num > 0)
                        {
                            Thing thing = ThingMaker.MakeThing(costListAdj[i].thingDef);
                            thing.stackCount = num;
                            products.Add(thing);
                        }
                    }
                }
                else if (__instance.def.smeltProducts != null && __instance.def.smeltProducts.Count > 0)
                {
                    for (int i = 0; i < __instance.def.smeltProducts.Count; i++)
                    {
                        ThingDefCountClass thingDefCountClass = __instance.def.smeltProducts[i];
                        Thing thing2 = ThingMaker.MakeThing(thingDefCountClass.thingDef);
                        thing2.stackCount = thingDefCountClass.count;
                        products.Add(thing2);
                    }
                }
                else if (IsAmmoDef(__instance.def))
                {
                    Dictionary<ThingDef, float> perUnit = AmmoRecipeCostCache.Get(__instance.def);
                    if (perUnit != null && perUnit.Count > 0)
                    {
                        int stack = __instance.stackCount;
                        foreach (KeyValuePair<ThingDef, float> kv in perUnit)
                        {
                            int num = GenMath.RoundRandom(kv.Value * stack * ReturnRatio);
                            if (num > 0)
                            {
                                Thing thing3 = ThingMaker.MakeThing(kv.Key);
                                thing3.stackCount = num;
                                products.Add(thing3);
                            }
                        }
                    }
                }
                // 都没有 → 保持空列表,不返还任何东西。
                __result = products;
                return false;
            }
            catch (Exception e)
            {
                Log.Error("[SmeltProducts30Fix] prefix failed, fallback to vanilla: " + e);
                return true;
            }
        }

        // 弹药判定: 类别链含 CE Ammo / 鼠族 RK_ThingCategory_Ammo,或 thingClass 名含 Ammo。
        private static bool IsAmmoDef(ThingDef def)
        {
            if (def.thingCategories != null)
            {
                for (int i = 0; i < def.thingCategories.Count; i++)
                {
                    ThingCategoryDef cat = def.thingCategories[i];
                    while (cat != null)
                    {
                        if (cat.defName == "Ammo" || cat.defName == "RK_ThingCategory_Ammo")
                        {
                            return true;
                        }
                        cat = cat.parent;
                    }
                }
            }
            if (def.thingClass != null && def.thingClass.Name.IndexOf("Ammo", StringComparison.Ordinal) >= 0)
            {
                return true;
            }
            return false;
        }

        // 弹药每发材料缓存(CE/鼠族弹药 ThingDef 无 costList,材料只在生产配方里)。
        private static class AmmoRecipeCostCache
        {
            private static readonly Dictionary<ThingDef, Dictionary<ThingDef, float>> Cache =
                new Dictionary<ThingDef, Dictionary<ThingDef, float>>();

            public static Dictionary<ThingDef, float> Get(ThingDef def)
            {
                Dictionary<ThingDef, float> result;
                if (Cache.TryGetValue(def, out result))
                {
                    return result;
                }
                result = Build(def);
                Cache[def] = result;
                return result;
            }

            private static Dictionary<ThingDef, float> Build(ThingDef def)
            {
                Dictionary<ThingDef, float> perUnit = new Dictionary<ThingDef, float>();
                try
                {
                    List<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefsListForReading;
                    for (int i = 0; i < recipes.Count; i++)
                    {
                        RecipeDef rd = recipes[i];
                        if (rd.products == null || rd.ingredients == null)
                        {
                            continue;
                        }
                        float productCount = 0f;
                        for (int j = 0; j < rd.products.Count; j++)
                        {
                            if (rd.products[j].thingDef == def)
                            {
                                productCount += rd.products[j].count;
                            }
                        }
                        if (productCount <= 0f)
                        {
                            continue;
                        }
                        for (int k = 0; k < rd.ingredients.Count; k++)
                        {
                            IngredientCount ing = rd.ingredients[k];
                            if (ing == null || ing.filter == null)
                            {
                                continue;
                            }
                            float baseCount = ing.GetBaseCount();
                            if (baseCount <= 0f)
                            {
                                continue;
                            }
                            IEnumerable<ThingDef> allowed = ing.filter.AllowedThingDefs;
                            if (allowed == null)
                            {
                                continue;
                            }
                            // 2026-08-18 用户要求「直接把金属条固定成钢、木板固定成红木,
                            // 固定一个配方即可」: 金属类过滤(SLDBar/USLDBar/Metallic 等)
                            // 含 钢(Steel=低碳钢)时一律选 Steel 为代表物,不再取最低价熟铁;
                            // 木板类过滤含 红木板(RedWoodPlank)时一律选 RedWoodPlank;
                            // 其余(火药等精确 thingDefs 过滤、HeavyBar 等不含 Steel 的
                            // 类别)维持 BaseMarketValue 最低代表物不变。仅影响弹药配方反推
                            // 一条链路(RKHSKSmeltAmmo),SmeltWeapon/SmeltApparel 的
                            // costList 路径不受影响。
                            ThingDef rep = null;
                            ThingDef steelRep = null;
                            ThingDef redwoodRep = null;
                            bool first = true;
                            foreach (ThingDef candidate in allowed)
                            {
                                if (candidate.defName == "Steel")
                                {
                                    steelRep = candidate;
                                }
                                if (candidate.defName == "RedWoodPlank")
                                {
                                    redwoodRep = candidate;
                                }
                                if (first || candidate.BaseMarketValue < rep.BaseMarketValue)
                                {
                                    rep = candidate;
                                    first = false;
                                }
                            }
                            if (steelRep != null)
                            {
                                rep = steelRep;
                            }
                            else if (redwoodRep != null)
                            {
                                rep = redwoodRep;
                            }
                            if (rep == null)
                            {
                                continue;
                            }
                            float perUnitCount = baseCount / productCount;
                            float existing;
                            if (perUnit.TryGetValue(rep, out existing))
                            {
                                perUnit[rep] = existing + perUnitCount;
                            }
                            else
                            {
                                perUnit[rep] = perUnitCount;
                            }
                        }
                        break; // 只用第一个含该弹药产物的配方(通常即主配方)
                    }
                }
                catch (Exception e)
                {
                    Log.Error("[SmeltProducts30Fix] ammo recipe cost build failed: " + e);
                }
                return perUnit;
            }
        }
    }
}

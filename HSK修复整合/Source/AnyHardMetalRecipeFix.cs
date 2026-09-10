// 任意硬质金属配方 (AnyHardMetalRecipeFix, 并入 HSK 修复整合)
//
// 需求(2026-09-08 用户确认): 武器/衣物/装甲配方里钉死"低碳钢 Steel"的金属格,
// 放行全部"代钢级"硬质金属(与 _tmp/metals_doc/material_master.json 权威口径一致, 24 种):
//   原始/中世纪: 熟铁/青铜/铸铁/纯铜/坩埚钢/锻焊钢/乌兹钢
//   电力工业: 低碳钢/中碳钢/弹簧钢/AR500耐磨钢/不锈钢/工具钢/黄铜/纯铝/3003铝/6061铝/贫铀
//   太空: 艾尔梅特/金刚砂/麦克斯梅特/镍钛/碳化钨/特纳卢姆
// 不动超钢级/强化工程级/钛铁合金/贵重级(非"硬质金属"), 也不动铅/锡等塑料级软金属。
//
// 机制与 AnyPlankRecipeFix 同构: [StaticConstructorOnStartup] 一次性放宽 ThingFilter。
// 判定"金属格钉死低碳钢": filter 允许 Steel 但不允许 CastIron(若已放行铸铁说明作者已用
// 大类口径, 视为"已经任意", 不动)。命中后只做加法放行 24 种, 数量不变(1:1),
// 同步改 ingredients[].filter + fixedIngredientFilter + defaultIngredientFilter
// (账单 UI 只能在这两个边界内勾选)。启动期一次性执行, 无 Tick 开销。

namespace HSKFixPack
{
    using System;
    using System.Collections.Generic;
    using RimWorld;
    using Verse;

    [StaticConstructorOnStartup]
    public static class AnyHardMetalRecipeFix
    {
        // 代钢级硬质金属全集(缺 mod 时 GetNamedSilentFail 返回 null 自动跳过)。
        private static readonly string[] HardMetalDefNames = new string[]
        {
            "WroughtIron", "Bronze", "CastIron", "CopperBar", "CrucibleSteel",
            "ForgedSteel", "WootzSteel",
            "Steel", "CarbonSteel", "SpringSteel", "SteelBar", "FerrosiliconAlloy",
            "AlnicoAlloy", "AluminiumBar", "AnodizedAluminiumRed", "AnodizedAluminiumBlue",
            "CupronickelAlloy", "DepletedUranium",
            "AerMet", "Carborundum", "MaxametSteel", "NitinolAlloy", "PobediteAlloy", "Tennalum"
        };

        static AnyHardMetalRecipeFix()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Log.Error("[HSKFixPack] 任意硬质金属配方补丁失败: " + e);
            }
        }

        private static void Run()
        {
            var metals = new List<ThingDef>();
            foreach (var n in HardMetalDefNames)
            {
                var d = DefDatabase<ThingDef>.GetNamedSilentFail(n);
                if (d != null)
                {
                    metals.Add(d);
                }
            }
            var steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
            var castIron = DefDatabase<ThingDef>.GetNamedSilentFail("CastIron");
            if (steel == null || castIron == null || metals.Count == 0)
            {
                Log.Warning("[HSKFixPack] 任意硬质金属配方: 关键 def 缺失, 跳过");
                return;
            }

            int slots = 0;
            int recipes = 0;
            foreach (var r in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (r == null || r.products == null || r.products.Count == 0)
                {
                    continue;
                }
                if (!ProducesWeaponOrApparel(r))
                {
                    continue;
                }
                bool expandedIngredient = false;
                if (r.ingredients != null)
                {
                    foreach (var ing in r.ingredients)
                    {
                        if (ing == null || ing.filter == null)
                        {
                            continue;
                        }
                        if (IsPinnedSteel(ing.filter, steel, castIron))
                        {
                            Expand(ing.filter, metals);
                            slots++;
                            expandedIngredient = true;
                        }
                    }
                }
                // 同 AnyPlankRecipeFix: 放宽了金属格就必须同步进"天花板"(fixedIngredientFilter/
                // defaultIngredientFilter), 否则 CE/HSK 生成的配方里金属格能选、天花板不含→选不了。
                bool touched = expandedIngredient;
                if (r.fixedIngredientFilter != null &&
                    (expandedIngredient || IsPinnedSteel(r.fixedIngredientFilter, steel, castIron)))
                {
                    Expand(r.fixedIngredientFilter, metals);
                    touched = true;
                }
                if (r.defaultIngredientFilter != null &&
                    (expandedIngredient || IsPinnedSteel(r.defaultIngredientFilter, steel, castIron)))
                {
                    Expand(r.defaultIngredientFilter, metals);
                    touched = true;
                }
                if (touched)
                {
                    recipes++;
                }
            }

            Log.Message("[HSKFixPack] 任意硬质金属配方完成: 放宽金属格 " + slots + " 处 / 配方 " + recipes
                + " 条, 硬质金属种类 " + metals.Count);
        }

        // 该配方产物里只要有一件是衣物/武器/装甲即纳入(与任意木板补丁同口径)。
        private static bool ProducesWeaponOrApparel(RecipeDef r)
        {
            foreach (var p in r.products)
            {
                var d = p.thingDef;
                if (d != null && InScope(d))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool InScope(ThingDef d)
        {
            return d.IsApparel || d.IsMeleeWeapon || d.IsRangedWeapon;
        }

        // 钉死低碳钢: 允许 Steel, 但没放行铸铁(=尚未用硬质金属大类口径)。
        private static bool IsPinnedSteel(ThingFilter f, ThingDef steel, ThingDef castIron)
        {
            return f.Allows(steel) && !f.Allows(castIron);
        }

        private static void Expand(ThingFilter f, List<ThingDef> metals)
        {
            foreach (var m in metals)
            {
                f.SetAllow(m, true);
            }
        }
    }
}

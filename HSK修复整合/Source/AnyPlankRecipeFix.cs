// 任意木板配方 (AnyPlankRecipeFix, 并入 HSK 修复整合)
//
// 需求(2026-09-08 用户确认): 武器/衣物/装甲的制作配方, 木料那一格接受"任意木板"
// (木板/橡木/松木/桦木/枫木/柏木/杨木/柳木/红木/蓖麻/竹/金合欢/柚木/红树/龙血木 共 15 种,
// 与 HSK 弓箭 MakeAmmo_Arrow_* 用的木板清单一致), 不再强制某一种, 也不再收原木 WoodLog。
//
// 为什么用运行期补丁而非逐件改 XML:
//   - recipeMaker 自动配方(costList 生成)的 ingredients[].filter 是"写死单一 ThingDef",
//     纯 XML 的 costList 无法表达"任意木板"; 要把 ~30 件东洋系武器/衣甲逐条转显式 RecipeDef
//     既繁琐又易触发重复解锁告警。
//   - 机制与 StuffTechGate 完全同构: [StaticConstructorOnStartup] 在全部 def(含自动配方、
//     含 CE MakeGunCECompatible 的 XML 补丁)加载完成后, 一次性收紧/放宽 ThingFilter。
//
// 判定"这一格是钉死木料": filter 允许 WoodLog 或 WoodPlank, 但不允许 OakPlank
// (若已允许 OakPlank 说明作者已用 Woody/木板大类, 视为"已经任意", 不动, 避免误伤 HSK 自带配方)。
// 命中后: 移除 WoodLog, 放行全部 15 种木板; 同步改 fixedIngredientFilter + defaultIngredientFilter
// (账单 UI 只能在这两个边界内勾选)。数量不变(1:1)。启动期一次性执行, 无 Tick 开销。

namespace HSKFixPack
{
    using System;
    using System.Collections.Generic;
    using RimWorld;
    using Verse;

    [StaticConstructorOnStartup]
    public static class AnyPlankRecipeFix
    {
        // 与 HSK 弓箭"任意木板"清单一致。缺 mod 时 GetNamedSilentFail 返回 null 自动跳过。
        private static readonly string[] PlankDefNames = new string[]
        {
            "WoodPlank", "OakPlank", "PinePlank", "BirchPlank", "MaplePlank",
            "CypressPlank", "PoplarPlank", "WillowPlank", "RedWoodPlank", "CecropiaPlank",
            "BambooPlank", "AcaciaPlank", "TeakPlank", "MangrovePlank", "DragonwoodPlank"
        };

        static AnyPlankRecipeFix()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Log.Error("[HSKFixPack] 任意木板配方补丁失败: " + e);
            }
        }

        private static void Run()
        {
            var planks = new List<ThingDef>();
            foreach (var n in PlankDefNames)
            {
                var d = DefDatabase<ThingDef>.GetNamedSilentFail(n);
                if (d != null)
                {
                    planks.Add(d);
                }
            }
            var woodLog = DefDatabase<ThingDef>.GetNamedSilentFail("WoodLog");
            var woodPlank = DefDatabase<ThingDef>.GetNamedSilentFail("WoodPlank");
            var oakPlank = DefDatabase<ThingDef>.GetNamedSilentFail("OakPlank");
            if (planks.Count == 0)
            {
                Log.Warning("[HSKFixPack] 任意木板配方: 未找到任何木板 def, 跳过");
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
                        if (IsPinnedWood(ing.filter, woodLog, woodPlank, oakPlank))
                        {
                            Expand(ing.filter, woodLog, planks);
                            slots++;
                            expandedIngredient = true;
                        }
                    }
                }
                // 关键修复: 账单可选集受 fixedIngredientFilter / defaultIngredientFilter 这块"天花板"限制。
                // 只要放宽了任一木料格, 就必须把木板同步进天花板——否则当配方是 CE/HSK 生成的、
                // 天花板是金属/布料等大类(不含木板)时, 会出现"配方里有木板、但账单选不了"。
                // (Expand 已是"移出原木 + 放行 15 种木板", 且不动天花板里已允许的其它材质。)
                bool touched = expandedIngredient;
                if (r.fixedIngredientFilter != null &&
                    (expandedIngredient || IsPinnedWood(r.fixedIngredientFilter, woodLog, woodPlank, oakPlank)))
                {
                    Expand(r.fixedIngredientFilter, woodLog, planks);
                    touched = true;
                }
                if (r.defaultIngredientFilter != null &&
                    (expandedIngredient || IsPinnedWood(r.defaultIngredientFilter, woodLog, woodPlank, oakPlank)))
                {
                    Expand(r.defaultIngredientFilter, woodLog, planks);
                    touched = true;
                }
                if (touched)
                {
                    recipes++;
                }
            }

            Log.Message("[HSKFixPack] 任意木板配方完成: 放宽木料格 " + slots + " 处 / 配方 " + recipes
                + " 条, 木板种类 " + planks.Count);
        }

        // 该配方产物里只要有一件是衣物/武器/装甲即纳入(账单以产物归类)。
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
            // 用游戏自带判定: 三者都要求 category==Item 且非建筑, 天然排除炮塔/建筑/弹药。
            // IsMeleeWeapon 含镐/厨刀等"可作近战"的工具(RimWorld 本就把它们当近战武器), 属可接受范围。
            return d.IsApparel || d.IsMeleeWeapon || d.IsRangedWeapon;
        }

        // 钉死单一木料: 允许原木或木板, 但没放行橡木木板(=尚未用木板大类)。
        private static bool IsPinnedWood(ThingFilter f, ThingDef log, ThingDef plank, ThingDef oak)
        {
            bool pinsWood = (log != null && f.Allows(log)) || (plank != null && f.Allows(plank));
            bool alreadyBroad = oak != null && f.Allows(oak);
            return pinsWood && !alreadyBroad;
        }

        private static void Expand(ThingFilter f, ThingDef log, List<ThingDef> planks)
        {
            if (log != null)
            {
                f.SetAllow(log, false); // 木料格不再收原木
            }
            foreach (var p in planks)
            {
                f.SetAllow(p, true);
            }
        }
    }
}

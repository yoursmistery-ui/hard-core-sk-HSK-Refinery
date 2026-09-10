// 制造主导材料修复(DominantStuffFix,并入 HSK 修复整合)
//
// 问题: RimWorld 1.6 的 Verse.AI.Toils_Recipe.CalculateDominantIngredient 在
// recipe.productHasIngredientStuff 为 true 时直接返回 ingredients[0](小人先收集到的
// 第一种材料),并不校验它是否属于配方的"材质槽"(stuffCategoriesToAllow)。
// 鼠族锤子 RK_Weapon_Maul 等配方是 金属材质×70 + 任意木板×25(固定材料)+ 零部件×2:
// 只要小人先搬木板/零部件,产物材质就变成木板/零部件,锤子按木头品质制造。
//
// 方案: 给 CalculateDominantIngredient 加前缀——对 RKHSK 系列配方,
// 从已收集材料里挑出匹配"配方材质槽"(第一个带 stuffCategoriesToAllow 的 IngredientCount)
// 的那一个作为主导材料,其余情况(含其他 mod 配方)完全走原版逻辑。
// 纯制造逻辑修复,不改配方/数值;木板仍是固定消耗材料,不再参与产物材质。
// 2026-08-13 扩展: 覆盖范围从 productHasIngredientStuff=true 扩大到所有 RKHSK 材质制品
// (产物 MadeFromStuff / 材质半成品),并在半成品分支接管主导材质选择,修复
// "Tried to make UnfinishedApparel from ComponentMedieval" + "Sequence contains no matching
// element" 导致的开工失败(工作台报原料不足、重载存档才恢复)。
// 2026-08-17 扩展(复合配方零部件材质 bug): recipeMaker 自动配方的材质槽 filter 由
// SetAllowAllWhoCanMake 构建 -> 填充 allowedDefs,而非 stuffCategoriesToAllow,导致补丁的
// 材质槽识别(Pass 1)对 recipeMaker 永远落空,退化到"取第一个 IsStuff"兜底——小人补料时
// 若第一堆放的是固定零部件,产出的衣物/武器就变成零部件材质。新增 Pass 2: 用
// stuffProps.CanMake(ProducedThingDef) 判定"真材质",把固定零部件(ComponentIndustrial/
// Medieval/Spacer 等无法作为该产品材质的 IsStuff)排除在主导材质选择之外。同时修复
// 半成品分支误用 TargetIndex.C(应为 B,原分支是死代码)。

using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace DominantStuffFix
{
    [StaticConstructorOnStartup]
    public static class DominantStuffFixInit
    {
        static DominantStuffFixInit()
        {
            try
            {
                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("local.hskmvcfverbfix.dominantstuff");
                harmony.Patch(
                    HarmonyLib.AccessTools.Method(typeof(Verse.AI.Toils_Recipe), "CalculateDominantIngredient"),
                    prefix: new HarmonyLib.HarmonyMethod(typeof(DominantStuffFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched Toils_Recipe.CalculateDominantIngredient");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] dominant ingredient patch failed: " + e);
            }
        }

        private static bool Prefix(Job job, List<Thing> ingredients, ref Thing __result)
        {
            try
            {
                // 2026-08-13: 扩大到所有配方(不再限 RKHSK)。RimWorld 1.6 对 recipeMaker
                // 自动配方(productHasIngredientStuff=true)直接返回 ingredients[0],而自动配方
                // 的 costList 固定材料排在最前(如 HMC 吊带裤 ComponentMedieval×2 + 布料材质×55),
                // 小人先搬零部件时就会把它当材质 → UnfinishedApparel stuff=null → 完成时报
                // "Sequence contains no matching element"。这里对所有材质制品配方统一接管主导材质选择。
                RecipeDef recipe = job != null ? job.RecipeDef : null;
                if (recipe == null || recipe.defName == null)
                {
                    return true;
                }
                if (!IsStuffBasedRecipe(recipe))
                {
                    return true;
                }

                // 1) Existing unfinished thing (finish step): pick dominant from its stored
                //    ingredients. Vanilla uft branch throws "Sequence contains no matching
                //    element" when uft.stuff is null or missing from the ingredients.
                //    NOTE: the UFT lives at TargetIndex.B (JobDriver_DoBill IngredientInd);
                //    TargetIndex.C is the ingredient place cell. A former version read C,
                //    which is never a UFT, so this branch never ran.
                UnfinishedThing uft = job.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                if (uft != null && uft.def != null && uft.def.MadeFromStuff)
                {
                    if (uft.Stuff != null && uft.ingredients != null)
                    {
                        for (int j = 0; j < uft.ingredients.Count; j++)
                        {
                            Thing t = uft.ingredients[j];
                            if (t != null && t.def == uft.Stuff)
                            {
                                __result = t;
                                return false;
                            }
                        }
                    }
                    // uft.Stuff 缺失或不在已存原料里(历史坏档/数据异常): 从材质槽重新选,避免抛错。
                    Thing dominant = PickDominant(recipe, uft.ingredients);
                    if (dominant != null)
                    {
                        __result = dominant;
                        return false;
                    }
                    // No stuff at all in the unfinished thing: data anomaly, let vanilla handle it.
                    return true;
                }

                // 2) Normal start branch: pick dominant from the collected ingredients.
                if (ingredients == null || ingredients.Count == 0)
                {
                    return true;
                }
                Thing selected = PickDominant(recipe, ingredients);
                if (selected != null)
                {
                    __result = selected;
                    return false;
                }
                // Recipe is stuff-based but no stuff collected: let vanilla throw instead of
                // returning null and risking an NRE downstream in GenRecipe.
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] dominant ingredient prefix error: " + e);
            }
            return true;
        }

        // Whether the recipe produces a stuff-based product (MadeFromStuff product,
        // stuff-based unfinished thing, or productHasIngredientStuff).
        private static bool IsStuffBasedRecipe(RecipeDef recipe)
        {
            if (recipe.productHasIngredientStuff)
            {
                return true;
            }
            if (recipe.unfinishedThingDef != null && recipe.unfinishedThingDef.MadeFromStuff)
            {
                return true;
            }
            if (recipe.products != null)
            {
                for (int i = 0; i < recipe.products.Count; i++)
                {
                    if (recipe.products[i] != null && recipe.products[i].thingDef != null && recipe.products[i].thingDef.MadeFromStuff)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Pick the dominant ingredient:
        //  Pass 1 - recipe material slot(s): ingredients that declare stuffCategoriesToAllow
        //           (the authoritative "this is the material" filter). This covers all
        //           hand-written RKHSK recipes (rewritten by 51号 to carry stuffCategoriesToAllow).
        //  Pass 2 - genuine material for the product: any collected IsStuff whose stuffProps
        //           can actually make the produced thing. Covers recipeMaker auto-recipes
        //           (their material slot filter is built via SetAllowAllWhoCanMake -> allowedDefs,
        //           NOT stuffCategoriesToAllow, so Pass 1 never matches them) and any other
        //           recipe where the material slot isn't expressed as stuffCategoriesToAllow.
        //           Fixed "component-like" stuffs (ComponentIndustrial/Medieval/Spacer etc.) that
        //           cannot make the product are excluded, so a stacked component never wins the
        //           lottery and the garment/weapon never comes out made of components.
        //  Pass 3 - last resort: any IsStuff (mirrors vanilla, keeps things from erroring out).
        private static Thing PickDominant(RecipeDef recipe, List<Thing> things)
        {
            if (recipe.ingredients == null || things == null || things.Count == 0)
            {
                return null;
            }
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ing = recipe.ingredients[i];
                if (ing == null || ing.filter == null || !HasStuffCategories(ing.filter))
                {
                    continue;
                }
                for (int j = 0; j < things.Count; j++)
                {
                    Thing t = things[j];
                    if (t != null && t.def != null && t.def.IsStuff && ing.filter.Allows(t.def))
                    {
                        return t;
                    }
                }
            }
            for (int j = 0; j < things.Count; j++)
            {
                Thing t = things[j];
                if (t != null && t.def != null && t.def.IsStuff && CanMakeProduct(recipe, t.def))
                {
                    return t;
                }
            }
            for (int j = 0; j < things.Count; j++)
            {
                Thing t = things[j];
                if (t != null && t.def != null && t.def.IsStuff)
                {
                    return t;
                }
            }
            return null;
        }

        // Whether the stuff def is a genuine material for the recipe's produced thing
        // (its stuff category overlaps one of the product's allowed stuff categories).
        private static bool CanMakeProduct(RecipeDef recipe, ThingDef stuffDef)
        {
            try
            {
                ThingDef product = recipe.ProducedThingDef;
                if (product == null || !product.MadeFromStuff || stuffDef == null || stuffDef.stuffProps == null)
                {
                    return false;
                }
                return stuffDef.stuffProps.CanMake(product);
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] dominant ingredient can-make check error: " + e);
                return false;
            }
        }

        private static bool HasStuffCategories(ThingFilter filter)
        {
            try
            {
                FieldInfo fi = HarmonyLib.AccessTools.Field(typeof(ThingFilter), "stuffCategoriesToAllow");
                if (fi == null)
                {
                    return false;
                }
                List<StuffCategoryDef> cats = fi.GetValue(filter) as List<StuffCategoryDef>;
                return cats != null && cats.Count > 0;
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] dominant ingredient stuff category check error: " + e);
                return false;
            }
        }
    }
}

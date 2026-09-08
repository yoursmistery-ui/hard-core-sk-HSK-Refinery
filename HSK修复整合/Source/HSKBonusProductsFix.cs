// 工作台账单配方的"必出副产物"(2026-09-08 用户要求)
//
// 背景: Vile 鞣革的 TanDrum_* 是普通 RecipeDef(挂 TanningDrum 工作台账单),
// 原生 products 同时列主皮革 + 边角料/龙猫革(2 产物)。Dubs 薄荷菜单的图标
// 全程读 RecipeDef.ProducedThingDef —— 该属性在 products.Count != 1 时返回 null,
// 于是多产物配方显示通用 work 图标, 且 Dubs 无视 uiIconThing。
// bonusOutputs 字段只在 UniversalFermenterSK.RecipeDef_UF 上存在(在 Building_UF
// 内由 CompProcessor 处理), 普通 RecipeDef 写它直接报
// "doesn't correspond to any field in type RecipeDef" 且副产物丢失。
//
// 方案: 给普通 RecipeDef 挂 DefModExtension(RecipeBonusProductsExt) 声明副产物,
// products 只保留主产物(→ ProducedThingDef 非空 → Dubs 出主皮革图标);
// Harmony postfix 包装 GenRecipe.MakeRecipeProducts 的枚举结果, 在原有产物之后
// 追加扩展里的副产物 Thing(必出, 固定数量)。仅对带本扩展的配方生效, 其它配方零影响;
// TanVat_* 走 UF 自己的 bonusOutputs, 不挂本扩展, 不会双发。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起, 系统 csc, C#5: 块体/字符串字面量,
// 禁表达式体方法与 nameof)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace HSKBonusProductsFix
{
    // 挂在 RecipeDef.modExtensions 上(ThingDefCountClass 用"元素名=def/文本=数量"简写):
    //   <li Class="HSKBonusProductsFix.RecipeBonusProductsExt">
    //     <bonusProducts><Leather_Scraps>50</Leather_Scraps></bonusProducts>
    //   </li>
    public class RecipeBonusProductsExt : DefModExtension
    {
        public List<ThingDefCountClass> bonusProducts;
    }

    [StaticConstructorOnStartup]
    public static class HSKBonusProductsFixInit
    {
        static HSKBonusProductsFixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(GenRecipe), "MakeRecipeProducts");
                if (target == null)
                {
                    Log.Warning("[HSKBonusProductsFix] GenRecipe.MakeRecipeProducts not found");
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.bonusproducts");
                harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(typeof(HSKBonusProductsFixInit).GetMethod(
                        "MakeRecipeProductsPostfix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKBonusProductsFix] patched GenRecipe.MakeRecipeProducts (bonus products via DefModExtension)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKBonusProductsFix] patch failed: " + e);
            }
        }

        private static void MakeRecipeProductsPostfix(RecipeDef recipeDef, ref IEnumerable<Thing> __result)
        {
            if (recipeDef == null || __result == null)
            {
                return;
            }
            RecipeBonusProductsExt ext = null;
            if (recipeDef.modExtensions != null)
            {
                for (int i = 0; i < recipeDef.modExtensions.Count; i++)
                {
                    RecipeBonusProductsExt as_ext = recipeDef.modExtensions[i] as RecipeBonusProductsExt;
                    if (as_ext != null)
                    {
                        ext = as_ext;
                        break;
                    }
                }
            }
            if (ext == null || ext.bonusProducts == null || ext.bonusProducts.Count == 0)
            {
                return;
            }
            __result = Append(recipeDef, __result, ext);
        }

        // 惰性追加: 先把原枚举结果原样吐出(仍在主线程枚举时按需生成), 再补发副产物。
        private static IEnumerable<Thing> Append(RecipeDef recipeDef, IEnumerable<Thing> orig, RecipeBonusProductsExt ext)
        {
            foreach (Thing t in orig)
            {
                yield return t;
            }
            for (int i = 0; i < ext.bonusProducts.Count; i++)
            {
                ThingDefCountClass td = ext.bonusProducts[i];
                if (td == null || td.thingDef == null)
                {
                    continue;
                }
                Thing bonus = ThingMaker.MakeThing(td.thingDef);
                bonus.stackCount = td.count;
                yield return bonus;
            }
        }
    }
}

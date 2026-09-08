// Thing.Ingested 悬空/无 thingCategories 食材导致进食崩溃修复
//
// 症状: 动物(狼/战犬等)或殖民者走到地上的"烤肉(RoastedMeat)"前反复尝试进食却永远吃不完,
// 食物不掉营养、不消失,作业每 tick 重来。JobErrorRecover.log / Player.log 里刷屏:
//   JobDriver threw exception in toil FinalizeIngest's initAction for pawn Warg...
//   driver=JobDriver_Ingest ... A = Thing_RoastedMeat... Giver = JobGiver_GetFood
//   System.NullReferenceException
//     at Verse.Thing.Ingested (Pawn ingester, Single nutritionWanted) [0x008ad]
//
// 根因: 原版 Thing.Ingested 处理"这顿饭由哪些原料做成"(CompIngredients.ingredients)时,有一行
//   if (ModsConfig.IdeologyActive && !flag6 && compIngredients.ingredients[j].thingCategories.Contains(ThingCategoryDefOf.PlantFoodRaw))
// 直接对 ingredient 的 thingCategories 调 Contains,没有判空。当某份烤肉的原料列表里混进了
// 没有 thingCategories 的 def(实测存档里的烤肉原料含 Abomination[thingCategories 为空]、
// 以及 Corpse_XXX 尸体等),该表达式在 null 接收者上调 Contains → NRE,整个 Ingested 抛出,
// FinalizeIngest 失败,食物永不消耗。IsMeat/GetMeatSourceCategory 等其它分支都对 thingCategories
// 或 ingestible 做了判空,唯独这一行漏了 —— 属原版缺陷。
//
// 触发链: 某些流程(商队/任务奖励/尸体处理类 mod 的 ThingMaker)把尸体/Abomination 等非食物 def
// 塞进了烤肉的 CompIngredients。正常 CookRoastedMeat 只收 MeatRaw 类食材不会这样,但外部生成的
// 成品会。任何 pawn 吃这份带坏食材的烤肉都会崩。
//
// 方案: 给 Thing.Ingested 加 Harmony 前缀,在原方法体执行前,把该 Thing 的 CompIngredients.ingredients
// 里 thingCategories 为 null(或 def 本身为 null)的条目剔除。这些条目本就不是有效食物原料,剔除后
// 仅影响"这顿饭含植物原料/受崇拜动物肉/人肉"这类思想判定,不影响营养与消耗。返回 true 让原版继续。
// 有 thingCategories 的正常肉类原料保留,人肉/崇拜肉思想仍照常触发。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IngestedIngredientNullGuardFix
{
    [StaticConstructorOnStartup]
    public static class IngestedIngredientNullGuardInit
    {
        static IngestedIngredientNullGuardInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(Thing), "Ingested",
                    new Type[] { typeof(Pawn), typeof(float) });
                if (target == null)
                {
                    Log.Warning("[IngestedIngredientGuard] Thing.Ingested not found, skip.");
                    return;
                }
                Harmony harmony = new Harmony("local.hskingestedingredientguard");
                harmony.Patch(target, prefix: new HarmonyMethod(
                    typeof(IngestedIngredientNullGuardInit).GetMethod(
                        "Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[IngestedIngredientGuard] patched Thing.Ingested (null-thingCategories ingredient guard)");
            }
            catch (Exception e)
            {
                Log.Error("[IngestedIngredientGuard] patch failed: " + e);
            }
        }

        private static bool Prefix(Thing __instance)
        {
            try
            {
                ThingWithComps twc = __instance as ThingWithComps;
                if (twc == null)
                {
                    return true;
                }
                CompIngredients comp = twc.TryGetComp<CompIngredients>();
                if (comp == null || comp.ingredients == null)
                {
                    return true;
                }
                List<ThingDef> list = comp.ingredients;
                int removed = list.RemoveAll(delegate(ThingDef def)
                {
                    if (def == null)
                    {
                        return true;
                    }
                    return def.thingCategories == null;
                });
                if (removed > 0)
                {
                    Log.Message("[IngestedIngredientGuard] stripped " + removed
                        + " invalid ingredient(s) with null thingCategories from " + __instance.def.defName);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[IngestedIngredientGuard] sanitize failed: " + e);
            }
            return true;
        }
    }
}

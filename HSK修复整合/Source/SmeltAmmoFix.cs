// 熔炼弹药 - 动态数量上限(2026-08-10)
//
// 背景: RKHSKSmeltAmmo(熔炼弹药)原配方 ingredients count=100,必须凑满 100 发才能开工,
// 零散弹药(1-99 发)无法熔炼。修法分两步:
//   1) 配方加 ignoreIngredientCountTakeEntireStacks=true(原版字段):
//      WorkGiver_DoBill 选择材料时跳过「数量必须凑满 count」检查,任意数量都能开工,
//      并直接取整堆弹药作为一批投入(1-N 发均可熔炼);
//   2) 但 CE 弹药堆叠上限可达 5000(如 5x56mm 等 AmmoBases 基类),整堆消耗会超出
//      「一次最多 100 发」的设计,因此本补丁在 WorkGiver_DoBill.TryStartNewDoBillJob
//      (public static)加前缀,把 RKHSKSmeltAmmo 每批选中的弹药数量上限卡到 100 发。
// 其余配方与工作台不受影响。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SmeltAmmoFix
{
    [StaticConstructorOnStartup]
    public static class SmeltAmmoFixInit
    {
        private const int MaxAmmoPerBatch = 100;
        private const string SmeltAmmoRecipeDefName = "RKHSKSmeltAmmo";

        static SmeltAmmoFixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(WorkGiver_DoBill), "TryStartNewDoBillJob");
                if (target == null)
                {
                    Log.Warning("[SmeltAmmoFix] WorkGiver_DoBill.TryStartNewDoBillJob not found");
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.smeltammo");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(SmeltAmmoFixInit).GetMethod(
                        "CapAmmoCountPrefix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[SmeltAmmoFix] patched WorkGiver_DoBill.TryStartNewDoBillJob");
            }
            catch (Exception e)
            {
                Log.Error("[SmeltAmmoFix] patch failed: " + e);
            }
        }

        // 仅对 RKHSKSmeltAmmo 生效: 每批选中的弹药数量不超过 100 发。
        private static void CapAmmoCountPrefix(Bill bill, List<ThingCount> chosenIngThings)
        {
            if (bill == null || bill.recipe == null || bill.recipe.defName != SmeltAmmoRecipeDefName)
            {
                return;
            }
            if (chosenIngThings == null)
            {
                return;
            }
            for (int i = 0; i < chosenIngThings.Count; i++)
            {
                ThingCount tc = chosenIngThings[i];
                if (tc.Count > MaxAmmoPerBatch)
                {
                    chosenIngThings[i] = new ThingCount(tc.Thing, MaxAmmoPerBatch);
                }
            }
        }
    }
}

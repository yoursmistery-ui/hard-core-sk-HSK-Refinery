// 鼠族HSK拓展 - 熔炼 0 堆叠产物修复
//
// 背景: Core_SK(SK.Patch2_GenRecipe_MakeRecipeProducts.Postfix)在熔炼配方
// (specialProducts=Smelted)完成后,按被熔炼物品的剩余血量百分比缩放各产物数量:
//   stackCount = RoundRandom(smeltProducts.count * (HP / MaxHP))
// 但缩放后不检查是否为 0,直接把 0 堆叠物品塞进产物列表 → 放置时
// GenSpawn.Spawn 报 "Spawned thing with 0 stackCount",连带 Assorted Tweaks
// CorrectIngredients 后缀对 null 结果空引用刷屏。熔炼残血武器/弹药/手雷时必现。
//
// 本补丁: 后置于 Core_SK 的补丁(priority = LowerThanNormal),把产物列表里所有
// 0 堆叠的物品过滤掉 —— 不生成即视为零产出,与「残血装备出料少」的缩放意图一致,
// 也不影响正常配方(正常产物堆叠数恒 > 0)。
//
// 编译(系统 csc, C#5):
//   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:library
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
//     /r:"<RimWorld>\Mods\Harmony\Current\Assemblies\0Harmony.dll"
//     /out:SmeltStackFix.dll SmeltStackFix.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RKSmeltStackFix
{
    [StaticConstructorOnStartup]
    public static class SmeltStackFixInit
    {
        static SmeltStackFixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(GenRecipe), "MakeRecipeProducts");
                if (target == null)
                {
                    Log.Warning("[RKSmeltStackFix] GenRecipe.MakeRecipeProducts not found");
                    return;
                }
                Harmony harmony = new Harmony("local.ratkin.clothesweapons.smeltstackfix");
                harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(
                        typeof(SmeltStackFixInit).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic))
                    {
                        priority = Priority.LowerThanNormal
                    });
                Log.Message("[RKSmeltStackFix] patched GenRecipe.MakeRecipeProducts (0-stack product filter)");
            }
            catch (Exception e)
            {
                Log.Error("[RKSmeltStackFix] patch failed: " + e);
            }
        }

        // 后置于 Core_SK 的 Postfix: 过滤产物列表中的 0 堆叠物品。
        private static void Postfix(ref IEnumerable<Thing> __result)
        {
            if (__result == null)
            {
                return;
            }
            List<Thing> filtered = new List<Thing>();
            foreach (Thing t in __result)
            {
                if (t != null && t.stackCount > 0)
                {
                    filtered.Add(t);
                }
            }
            __result = filtered;
        }
    }
}

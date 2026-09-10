// 鼠族HSK拓展 - 酿酒台「拆箱任意酒」产物匹配
//
// 背景: HSK 每种酒箱(Crate_*)在酿酒台(Brewery/ElectricBrewery)各有一条拆箱配方
// (1 箱 -> 对应瓶装酒 75 瓶)。新增 RKHSKUnpackAnyAlcohol 一条配方接受任意酒箱,
// 但 RimWorld 的 GenRecipe.MakeRecipeProducts 无法在纯 XML 里按输入酒箱动态选择产物,
// 因此用 Harmony 前缀补丁: 仅对 RKHSKUnpackAnyAlcohol 生效,
// 根据投入的酒箱 defName 找到对应瓶装酒,直接替换产物;其余配方走原逻辑。
//
// 编译(系统 csc, C#5):
//   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:library
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
//     /r:"<RimWorld>\Mods\Harmony\Current\Assemblies\0Harmony.dll"
//     /out:AlcoholUnpackFix.dll AlcoholUnpackFix.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RKAlcoholUnpackFix
{
    [StaticConstructorOnStartup]
    public static class AlcoholUnpackFixInit
    {
        static AlcoholUnpackFixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(GenRecipe), "MakeRecipeProducts");
                if (target == null)
                {
                    Log.Warning("[RKAlcoholUnpackFix] GenRecipe.MakeRecipeProducts not found");
                    return;
                }
                Harmony harmony = new Harmony("local.ratkin.clothesweapons.alcoholunpack");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(AlcoholUnpackFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[RKAlcoholUnpackFix] patched GenRecipe.MakeRecipeProducts");
            }
            catch (Exception e)
            {
                Log.Error("[RKAlcoholUnpackFix] patch failed: " + e);
            }
        }

        // 只处理 RKHSKUnpackAnyAlcohol,其余走原逻辑。
        private static bool Prefix(ref IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients)
        {
            if (recipeDef == null || recipeDef.defName != "RKHSKUnpackAnyAlcohol")
            {
                return true;
            }
            try
            {
                Thing crate = null;
                if (ingredients != null)
                {
                    foreach (Thing t in ingredients)
                    {
                        if (t != null && t.def != null && t.def.defName.StartsWith("Crate_", StringComparison.Ordinal))
                        {
                            crate = t;
                            break;
                        }
                    }
                }
                ThingDef alcohol = AlcoholFor(crate);
                if (alcohol == null)
                {
                    // 找不到对应瓶装酒(理论只发生在未装 HSK 酒箱),回退配方默认产物。
                    return true;
                }
                Thing bottle = ThingMaker.MakeThing(alcohol, null);
                bottle.stackCount = 75;
                List<Thing> result = new List<Thing>();
                result.Add(bottle);
                __result = result;
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static ThingDef AlcoholFor(Thing crate)
        {
            if (crate == null || crate.def == null)
            {
                return null;
            }
            string defName = crate.def.defName;
            string alcoholName = null;
            switch (defName)
            {
                case "Crate_Cider": alcoholName = "Alcohol_Cider"; break;
                case "Crate_Rum": alcoholName = "Alcohol_Rum"; break;
                case "Crate_Wine": alcoholName = "Alcohol_Wine"; break;
                case "Crate_BerryWine": alcoholName = "Alcohol_BerryWine"; break;
                case "Crate_Sake": alcoholName = "Alcohol_Sake"; break;
                case "Crate_Vodka": alcoholName = "Alcohol_Vodka"; break;
                case "Crate_Tequila": alcoholName = "Alcohol_Tequila"; break;
                case "Crate_Whiskey": alcoholName = "Alcohol_Whisky"; break;
                case "Crate_Moonshine": alcoholName = "Alcohol_Moonshine"; break;
                case "Crate_Gin": alcoholName = "Alcohol_Gin"; break;
                case "Crate_Brandy": alcoholName = "Alcohol_Brandy"; break;
                case "Crate_Cognac": alcoholName = "Alcohol_Cognac"; break;
                case "Crate_Kahlua": alcoholName = "Alcohol_Kahlua"; break;
                case "Crate_Arak": alcoholName = "Alcohol_Arak"; break;
                case "Crate_Applejack": alcoholName = "Alcohol_Applejack"; break;
                default:
                    if (defName.StartsWith("Crate_", StringComparison.Ordinal))
                    {
                        alcoholName = "Alcohol_" + defName.Substring("Crate_".Length);
                    }
                    break;
            }
            if (alcoholName == null)
            {
                return null;
            }
            return DefDatabase<ThingDef>.GetNamedSilentFail(alcoholName);
        }
    }
}

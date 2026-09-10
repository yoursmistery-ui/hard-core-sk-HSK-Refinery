// 任意皮革配方 - 产物动态匹配
//
// 背景: Vile's Hell Bent for Leather Tanning 的原配方都是「固定兽皮 -> 固定产物」。
// 44_VileAnyLeatherRecipes.xml 新增两条接受「任意兽皮」的配方:
//   - BrineCure_AnyLeather(腌制站): 任意未腌制皮 Hide_X -> 对应腌制皮 Hide_XCured
//   - TanDrum_AnyLeather(鞣制缸): 任意兽皮 Hide_X -> 对应皮革 Leather_*,x25
// 但 RimWorld 的 GenRecipe.MakeRecipeProducts 无法在纯 XML 里按投入兽皮动态选择产物,
// 因此用 Harmony 前缀补丁: 仅对这两条配方生效,
// 根据投入的兽皮 defName 找到对应产物,直接替换 __result;其余配方走原逻辑。
// 机制与 tmp/vile_patches_backup/TanningDrumDynamicFix.cs 以及鼠族HSK拓展
// 28 号「酿酒台拆箱任意酒」AlcoholUnpackFix 同理。
namespace AnyLeatherRecipeFix
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using HarmonyLib;
    using RimWorld;
    using Verse;

    [StaticConstructorOnStartup]
    public static class AnyLeatherRecipeDynamicFixInit
    {
        static AnyLeatherRecipeDynamicFixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(GenRecipe), "MakeRecipeProducts");
                if (target == null)
                {
                    Log.Warning("[AnyLeatherRecipeFix] GenRecipe.MakeRecipeProducts not found");
                    return;
                }
                Harmony harmony = new Harmony("local.vile.leathertanning.anyleatherrecipefix");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(AnyLeatherRecipeDynamicFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[AnyLeatherRecipeFix] patched GenRecipe.MakeRecipeProducts");
            }
            catch (Exception e)
            {
                Log.Error("[AnyLeatherRecipeFix] patch failed: " + e);
            }
        }

        // 只处理 BrineCure_AnyLeather / TanDrum_AnyLeather,其余配方走原逻辑。
        private static bool Prefix(ref IEnumerable<Thing> __result, RecipeDef recipeDef, List<Thing> ingredients)
        {
            if (recipeDef == null || recipeDef.defName == null)
            {
                return true;
            }
            bool isBrining = recipeDef.defName == "BrineCure_AnyLeather";
            bool isTanning = recipeDef.defName == "TanDrum_AnyLeather";
            if (!isBrining && !isTanning)
            {
                return true;
            }
            try
            {
                Thing hide = null;
                if (ingredients != null)
                {
                    foreach (Thing t in ingredients)
                    {
                        if (t != null && t.def != null && t.def.defName.StartsWith("Hide_", StringComparison.Ordinal))
                        {
                            // 腌制站只收未腌制皮;鞣制缸对已腌制皮也应能鞣(对应皮革同一种)。
                            if (isBrining && t.def.defName.EndsWith("Cured", StringComparison.Ordinal))
                            {
                                continue;
                            }
                            hide = t;
                            break;
                        }
                    }
                }
                if (hide == null)
                {
                    // 投入里没找到兽皮(理论不发生),回退配方占位产物。
                    return true;
                }
                ThingDef product = ResolveProduct(hide.def, isBrining);
                if (product == null)
                {
                    return true;
                }
                Thing made = ThingMaker.MakeThing(product, null);
                int stack = isBrining ? 1 : 25;
                made.stackCount = stack;
                List<Thing> result = new List<Thing>();
                result.Add(made);
                __result = result;
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        private static ThingDef ResolveProduct(ThingDef hideDef, bool isBrining)
        {
            if (isBrining)
            {
                // 腌制: Hide_X -> Hide_XCured(任何未腌制皮都能直接 +Cured)。
                if (!hideDef.defName.StartsWith("Hide_", StringComparison.Ordinal))
                {
                    return null;
                }
                string curedName = hideDef.defName;
                if (curedName.EndsWith("Cured", StringComparison.Ordinal))
                {
                    curedName = curedName.Substring(0, curedName.Length - "Cured".Length);
                }
                if (!curedName.StartsWith("Hide_", StringComparison.Ordinal))
                {
                    curedName = "Hide_" + curedName;
                }
                return DefDatabase<ThingDef>.GetNamedSilentFail(curedName + "Cured");
            }

            // 鞣制: 兽皮 -> 对应皮革(x25),映射与鞣制缸 14 条皮革配方同构。
            string baseName = hideDef.defName;
            if (baseName.StartsWith("Hide_", StringComparison.Ordinal))
            {
                baseName = baseName.Substring("Hide_".Length);
            }
            if (baseName.EndsWith("Cured", StringComparison.Ordinal))
            {
                baseName = baseName.Substring(0, baseName.Length - "Cured".Length);
            }
            string leatherName = null;
            switch (baseName)
            {
                case "LightAnimalSkin": leatherName = "Leather_Light"; break;
                case "RodentSkin": leatherName = "Leather_Light"; break;
                case "ExoticSkin": leatherName = "Leather_Lizard"; break;
                case "PorousHide": leatherName = "Leather_Boomanimal"; break;
                case "HeavyHide": leatherName = "Leather_Elephant"; break;
                case "HeavyFurPelt": leatherName = "Leather_Heavy"; break;
                case "WaterproofSkin": leatherName = "Leather_Waterproof"; break;
                case "DurableHide": leatherName = "Leather_Rhinoceros"; break;
                case "RuggedFurPelt": leatherName = "Leather_Wolf"; break;
                case "StockHide": leatherName = "Leather_Plain"; break;
                case "SoftHide": leatherName = "Leather_Pig"; break;
                case "WarmFurPelt": leatherName = "Leather_Bluefur"; break;
                case "ImmunogenicHide": leatherName = "Leather_Nightling"; break;
                case "RichFurPelt": leatherName = "Leather_Panthera"; break;
                case "HumanSkin": leatherName = "Leather_Human"; break;
                default: leatherName = null; break;
            }
            if (leatherName == null)
            {
                return null;
            }
            return DefDatabase<ThingDef>.GetNamedSilentFail(leatherName);
        }
    }
}
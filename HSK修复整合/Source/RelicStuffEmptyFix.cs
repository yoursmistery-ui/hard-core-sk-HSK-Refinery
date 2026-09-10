// 圣物材料空列表兜底(RelicStuffEmptyFix, 并入 HSK 修复整合)
//
// 问题: 世界生成 Ideo 时 Precept_Relic.GenerateStuffFor 调
// GenStuff.AllowedStuffsFor(thingDef, false, false) 得到圣物可用材料列表,
// 直接 RandomElementByWeight —— 1.6.4871 无空列表兜底。当前环境(HSK)圣物
// def 从建筑里选(useChoicesFromBuildingDefs),若选中非 MadeFromStuff 建筑或
// 材料类别不兼容,候选列表为空 → 刷 "RandomElementByWeight with totalWeight=0"
// 且圣物 stuff 为 null(后续生成圣物物品可能出问题)。
//
// 方案: 前缀补丁在 GenerateStuffFor 前复制其筛选逻辑,列表为空时兜底
// (先放宽参数再试,仍空则取任意 IsStuff),用 ref __result 赋兜底材质并返回
// false 跳过原方法;列表非空时放行原方法。GenStuff.AllowedStuffsFor 签名在
// 1.6 为 (ThingDef, TechLevel?, bool),用反射调用并双尝试兼容 (ThingDef, bool, bool)。
//
// ⚠️ Harmony 铁律(2026-08-14 实测): GenerateStuffFor 是 static 方法,签名
// static ThingDef GenerateStuffFor(ThingDef thing, Ideo ideo) —— 前缀参数名
// 必须与原方法参数名一致(写 relicDef 会报 "Parameter "relicDef" not found")。
// static 方法没有 __instance(实例),兜底必须经 ref __result 返回,不能写
// __instance.stuff(NRE)。「排除本 ideo 其它圣物已用材质」遍历 ideo 全部
// Precept_Relic 即可——当前圣物的 stuff 在 GenerateStuffFor 调用后才赋值,
// 生成期间自身未设置,不需要也无法排除自身。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;

namespace RelicStuffEmptyFix
{
    [StaticConstructorOnStartup]
    public static class RelicStuffEmptyFixInit
    {
        static RelicStuffEmptyFixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(Precept_Relic), "GenerateStuffFor");
                if (target == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.relicstuff");
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(RelicStuffEmptyFixInit).GetMethod(
                    "Prefix", BindingFlags.Static | BindingFlags.Public)));
                Log.Message("[HSKFix] patched Precept_Relic.GenerateStuffFor (relic stuff empty fallback)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] relic stuff fix failed: " + e);
            }
        }

        private static List<ThingDef> AllowedStuffsForInvoke(ThingDef thing, object secondArg, bool thirdArg)
        {
            MethodInfo m = AccessTools.Method(typeof(GenStuff), "AllowedStuffsFor");
            return (List<ThingDef>)m.Invoke(null, new object[] { thing, secondArg, thirdArg });
        }

        private static List<ThingDef> TryAllowedStuffsFor(ThingDef thing, bool thirdArg)
        {
            try
            {
                return AllowedStuffsForInvoke(thing, (TechLevel)0, thirdArg);
            }
            catch
            {
            }
            try
            {
                return AllowedStuffsForInvoke(thing, false, thirdArg);
            }
            catch
            {
            }
            return null;
        }

        public static bool Prefix(ThingDef thing, Ideo ideo, ref ThingDef __result)
        {
            if (thing == null)
            {
                return true;
            }
            List<ThingDef> list = TryAllowedStuffsFor(thing, false);
            if (list == null)
            {
                return true;
            }
            if (ideo != null)
            {
                List<ThingDef> used = new List<ThingDef>();
                foreach (Precept p in ideo.PreceptsListForReading)
                {
                    Precept_Relic r = p as Precept_Relic;
                    if (r != null && r.stuff != null)
                    {
                        used.Add(r.stuff);
                    }
                }
                if (used.Count > 0)
                {
                    list = list.Where(x => !used.Contains(x)).ToList();
                }
            }
            if (list.Count > 0)
            {
                return true;
            }
            ThingDef fallback = null;
            List<ThingDef> relaxed = TryAllowedStuffsFor(thing, true);
            if (relaxed != null && relaxed.Count > 0)
            {
                fallback = relaxed.RandomElement();
            }
            if (fallback == null)
            {
                List<ThingDef> allStuffs = DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(d => d.IsStuff && d.stuffProps != null).ToList();
                if (allStuffs.Count > 0)
                {
                    fallback = allStuffs.RandomElement();
                }
            }
            if (fallback == null)
            {
                return true;
            }
            __result = fallback;
            Log.Warning("[HSKFix] Precept_Relic.GenerateStuffFor: no valid stuff for " + thing.defName
                + ", fell back to " + fallback.defName);
            return false;
        }
    }
}

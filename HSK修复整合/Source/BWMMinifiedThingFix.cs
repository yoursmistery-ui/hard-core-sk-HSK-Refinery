// BWM MinifiedThing 计数修复(2026-08-18)
//
// 背景: Better Workbench Management(falconne.BWM)打开账单配置界面时,
// RecipeWorkerCounter_CountProducts_Detour.CountAdditionalProducts 对"附加产品过滤"
// 中的每个 ThingDef 直接调 map.listerThings.ThingsOfDef(allowedThingDef);
// 当玩家在过滤里选过 MinifiedThing(被拆解的家具)时,RimWorld 1.6 的
// ThingsOfDef 对 MinifiedThing 会打 ErrorOnce:
//   "Tried to get ThingsOfDef of MinifiedThing, use ThingsMatching(...) instead"
// 且 ForDef(MinifiedThing) 按 def 索引查不到(MinifiedThing 不存 listsByDef),
// 返回空列表 → 计数恒为 0。BWM 自己处理 Minifiable 产品时知道用
// ThingsInGroup((ThingRequestGroup)39 = MinifiedThing),唯独前面的通用
// ThingsOfDef 分支漏防。
//
// 修法: 前缀补丁全局修正 Verse.ListerThings.ThingsOfDef —— 当 def 为
// ThingDefOf.MinifiedThing 时改走 ThingsMatching(ThingRequest.ForGroup(
// ThingRequestGroup.MinifiedThing))(1.6 官方推荐写法),跳过原方法;
// 其余 def 原样放行。修正后日志消失,且附加产品计数中 MinifiedThing
// 也能被正确统计。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BWMMinifiedThingFix
{
    [StaticConstructorOnStartup]
    public static class BWMMinifiedThingFixInit
    {
        static BWMMinifiedThingFixInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.bwmminified");

                MethodInfo thingsOfDef = AccessTools.Method(typeof(ListerThings), "ThingsOfDef");
                if (thingsOfDef == null)
                {
                    Log.Warning("[BWMMinifiedThingFix] ListerThings.ThingsOfDef not found");
                }
                else
                {
                    harmony.Patch(thingsOfDef, prefix: new HarmonyMethod(typeof(Patch_ThingsOfDef), "Prefix"));
                }
            }
            catch (Exception e)
            {
                Log.Error("[BWMMinifiedThingFix] patch init failed: " + e);
            }
        }
    }

    public static class Patch_ThingsOfDef
    {
        // MinifiedThing 不按 def 索引存储,ThingsOfDef 对它只会打日志并返回空。
        // 拦截后改走 group 查询(1.6 官方推荐写法),返回全部 MinifiedThing。
        public static bool Prefix(ListerThings __instance, ThingDef def, ref List<Thing> __result)
        {
            if (def == ThingDefOf.MinifiedThing)
            {
                __result = __instance.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing));
                return false;
            }
            return true;
        }
    }
}

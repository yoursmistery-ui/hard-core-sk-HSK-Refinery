using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;

namespace FacilityCrashFix
{
    // 修复: 原版"行乞者期望救济"(Beggars)任务描述整段塌成 "ERR: 旅行者们"。
    //
    // 根因(反编译 Assembly-CSharp 确认):
    //   QuestNode_Root_Beggars.RunInt 开头调用私有静态
    //     bool TryFindRandomRequestedThing(Map map, float value, out ThingDef thingDef,
    //                                      out int count, IEnumerable<ThingDef> allowedThings)
    //   候选只有 Silver / MedicineHerbal / MedicineIndustrial / Penoxycyline / Beer 五种,
    //   用 (td.PlayerAcquirable && PlayerItemAccessibilityUtility.Accessible(td, num, map)) 过滤。
    //   HSK 的经济 + 科研门禁下,这五种此刻全都"不可获取"时该方法返回 false。
    //   但 RunInt 只是 `if (TryFindRandomRequestedThing(...)) { slate.Set(requestedThing/...) }`,
    //   返回 false 时【跳过 set requestedThing 却照样生成任务】。
    //   于是描述模板 questDescription 里依赖 [requestedThing_label](该标签缺失→无规则→硬失败)
    //   整段解析失败,GrammarRequest 走兜底输出 "ERR: " + 规则表第 0 条(中文=groupLabelPlural->旅行者们)。
    //   同时 QuestPart_BegForItems 也没有可乞之物,任务功能一起坏。
    //
    // 本补丁: postfix 兜底 —— 当原方法返回 false 时,强制以 Silver 作为索要物(数量按请求价值折算),
    //   使 requestedThing/requestedThingDefName/requestedThingCount 一定被设。描述与任务功能同时恢复。
    //   Silver 恒在候选内、恒 PlayerAcquirable、且 claimInfo 有对应分支,是最稳的兜底。
    [StaticConstructorOnStartup]
    public static class BeggarRequestFallbackFix
    {
        static BeggarRequestFallbackFix()
        {
            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(QuestNode_Root_Beggars),
                    "TryFindRandomRequestedThing",
                    new Type[]
                    {
                        typeof(Map),
                        typeof(float),
                        typeof(ThingDef).MakeByRefType(),
                        typeof(int).MakeByRefType(),
                        typeof(IEnumerable<ThingDef>)
                    });
                if (target == null)
                {
                    Log.Error("[HSKFixPack] 未找到 QuestNode_Root_Beggars.TryFindRandomRequestedThing,跳过行乞者兜底补丁");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.beggarrequestfallback");
                harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(
                        typeof(BeggarRequestFallbackFix).GetMethod(
                            "Postfix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFixPack] 行乞者索要物兜底补丁已挂载");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFixPack] 行乞者索要物兜底补丁应用失败: " + e);
            }
        }

        // __1 = value(请求总价值); thingDef/count 为原方法 out 参数,postfix 里以 ref 改写。
        private static void Postfix(float __1, ref ThingDef thingDef, ref int count, ref bool __result)
        {
            if (__result)
            {
                return; // 原方法已选到可获取物,不干预
            }
            ThingDef silver = ThingDefOf.Silver;
            if (silver == null)
            {
                return; // 极端情况:Silver 缺失,保持原 false(任务不会生成,不产生 ERR 描述)
            }
            thingDef = silver;
            float mv = silver.BaseMarketValue;
            if (mv <= 0f)
            {
                mv = 1f;
            }
            count = Mathf.Max(1, Mathf.RoundToInt(__1 / mv));
            __result = true;
        }
    }
}

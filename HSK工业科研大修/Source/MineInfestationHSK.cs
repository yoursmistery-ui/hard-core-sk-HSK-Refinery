using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BlueprintUnlockHSK
{
    // ============================================================================
    //  矿井闲置也爆"深度虫害"的修复
    // ----------------------------------------------------------------------------
    //  根因: RK_MineShaft 等账单式矿井挂原版 CompCreatesInfestations。
    //        原版 CanCreateInfestationNow 唯一的"是否在工作"闸门是
    //            CompDeepDrill comp = parent.GetComp<CompDeepDrill>();
    //            if (comp != null && !comp.UsedLastTick()) return false;
    //        深钻(Deep drill)自带 CompDeepDrill 所以不钻不爆; 但矿井是
    //        Building_WorkTable(靠账单出料), 没有 CompDeepDrill → comp 为 null
    //        → 闸门被整段跳过 → 只要过了 7 天冷却, 闲置也照爆。
    //  修法: ①在配方产物生成出口给带 CompCreatesInfestations 的账单建筑打"上次真实
    //        出料"时间戳; ②虫灾触发前要求"最近确实产出过", 闲置即永不触发。
    //        带 CompDeepDrill 的原版深钻不干预(沿用其 UsedLastTick 判定)。
    //  性能(AGENTS §9): 打点只在配方产物生成事件点(无 tick / 无轮询 / 无反射);
    //        读取只在事件评估(GetUsableDeepDrills)时发生, 频率极低。
    // ============================================================================
    internal static class MineInfestationHSK
    {
        // 距上次产出超过此 tick 数视为"没在工作"(1 游戏日 = 60000 tick)。
        // 活跃矿井每几小时就出一次料, 轻松满足; 暂停/无人采掘则很快过期。
        public const int ActiveWindowTicks = 60000;

        private static readonly ConditionalWeakTable<Thing, TickBox> _lastProduce =
            new ConditionalWeakTable<Thing, TickBox>();

        private class TickBox
        {
            public int tick = int.MinValue;
        }

        public static void NoteProduced(Thing t)
        {
            if (t == null)
            {
                return;
            }
            TickBox box;
            if (!_lastProduce.TryGetValue(t, out box))
            {
                box = new TickBox();
                _lastProduce.Add(t, box);
            }
            box.tick = Find.TickManager.TicksGame;
        }

        public static bool WorkedRecently(Thing t)
        {
            if (t == null)
            {
                return false;
            }
            TickBox box;
            if (!_lastProduce.TryGetValue(t, out box))
            {
                return false;
            }
            return Find.TickManager.TicksGame - box.tick <= ActiveWindowTicks;
        }
    }

    // 出料打点: 仅真实劳作(worker != null)时记录, 排除 UI 预览调用。
    // 注意: 1.6 该形参名为 worker(旧写 pawn 会致 Harmony 参数绑定失败→整个 PatchAll 静态构造崩)。
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts", new Type[]
    {
        typeof(RecipeDef), typeof(Pawn), typeof(List<Thing>), typeof(Thing), typeof(IBillGiver),
        typeof(Precept_ThingStyle), typeof(ThingStyleDef), typeof(int?)
    })]
    internal static class Patch_MineInfestation_Stamp
    {
        public static void Postfix(Pawn worker, IBillGiver billGiver)
        {
            if (worker == null)
            {
                return;
            }
            ThingWithComps twc = billGiver as ThingWithComps;
            if (twc == null || twc.GetComp<CompCreatesInfestations>() == null)
            {
                return;
            }
            MineInfestationHSK.NoteProduced(twc);
        }
    }

    // 触发闸门: 账单式矿井(无 CompDeepDrill)必须最近真的出过料才允许引爆虫灾。
    [HarmonyPatch(typeof(CompCreatesInfestations), "get_CanCreateInfestationNow")]
    internal static class Patch_MineInfestation_Gate
    {
        public static void Postfix(CompCreatesInfestations __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            ThingWithComps parent = __instance.parent;
            if (parent == null)
            {
                return;
            }
            // 原版深钻自带 UsedLastTick 判定, 不干预。
            if (parent.GetComp<CompDeepDrill>() != null)
            {
                return;
            }
            __result = MineInfestationHSK.WorkedRecently(parent);
        }
    }
}

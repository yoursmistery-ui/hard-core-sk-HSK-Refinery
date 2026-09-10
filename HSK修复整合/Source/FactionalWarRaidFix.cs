// Factional War 派系战争"无兵袭击"修复(2026-08-25 用户授权)
//
// 背景: Factional War Continued(SR.ModRimworld.FactionalWarContinued)的
// IncidentWorkerFactionWar.ResolvePawnList 用 SrFactionFirst 策略生成袭击部队。
// 该策略的 worker 继承原版 RaidStrategyWorker_StageThenAttack,其 CanUsePawnGenOption
// 硬性排除所有动物(RaceProps.Animal → false)。而 FactionUtil.GetHostileFactionPair
// 选参战派系时只做原版 IsFactionEffective 检查(不限动物),于是动物部落
// (如 TribeCivil,战斗兵种几乎全是动物)被选中后生成结果为 0 →
// 刷 "[SR.ModRimWorld.FactionalWar]Got no pawns spawning raid" 错误,
// 且空列表继续走 MakeLords,事件实际无任何部队。
//
// 方案: 给 GetHostileFactionPair 打 Prefix,把候选列表原地过滤为"能在
// SrFactionFirst 策略下生成至少 1 个非动物战斗兵种"的派系(用原版
// PawnGroupMaker.CanGenerateFrom + 策略内建动物过滤,与真实生成判定一致)。
// 动物派系不再被选为参战方;若地图上只剩动物派系,原方法自然找不到配对,
// 事件返回 false 静默取消,不再刷错误。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionalWarRaidFix
{
    [StaticConstructorOnStartup]
    public static class FactionalWarRaidFixInit
    {
        private static int lastLogTick = -99999;

        static FactionalWarRaidFixInit()
        {
            try
            {
                Type factionUtilType = AccessTools.TypeByName("SR.ModRimworld.FactionalWar.FactionUtil");
                if (factionUtilType == null)
                {
                    return; // Factional War 未装,跳过
                }
                MethodInfo getPair = AccessTools.Method(factionUtilType, "GetHostileFactionPair");
                if (getPair == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.factionalwarraidfix");
                harmony.Patch(getPair, prefix: new HarmonyMethod(
                    typeof(FactionalWarRaidFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[FactionalWarRaidFix] patched FactionUtil.GetHostileFactionPair");
            }
            catch (Exception e)
            {
                Log.Error("[FactionalWarRaidFix] patch failed: " + e);
            }
        }

        // 候选派系原地过滤: 只保留能在 SrFactionFirst 下生成非动物战斗兵种的派系。
        // 参数按名字匹配原方法 GetHostileFactionPair(out Faction,out Faction,float,
        // PawnGroupKindDef,List<Faction>,Predicate<Faction>) 的实参;List 原地清空重填,
        // 原方法(含随后的 Shuffle)直接操作过滤后的列表。
        private static void Prefix(float points, PawnGroupKindDef pawnGroupKindDef, List<Faction> candidateFactionList)
        {
            try
            {
                if (candidateFactionList == null || candidateFactionList.Count == 0)
                {
                    return;
                }
                if (pawnGroupKindDef == null)
                {
                    return;
                }
                RaidStrategyDef warStrategy = DefDatabase<RaidStrategyDef>.GetNamedSilentFail("SrFactionFirst");
                if (warStrategy == null)
                {
                    return; // 策略 def 未加载(Factional War 异常时),不做过滤
                }
                int originalCount = candidateFactionList.Count;
                List<Faction> kept = new List<Faction>();
                for (int i = 0; i < candidateFactionList.Count; i++)
                {
                    Faction f = candidateFactionList[i];
                    if (f != null && CanSpawnForWar(f, points, pawnGroupKindDef, warStrategy))
                    {
                        kept.Add(f);
                    }
                }
                if (kept.Count == originalCount)
                {
                    return; // 全部可生成,原逻辑不变
                }
                candidateFactionList.Clear();
                candidateFactionList.AddRange(kept);
                // 节流日志(每 5 秒最多一条,防刷屏)
                if (Find.TickManager != null && Find.TickManager.TicksGame - lastLogTick > 300)
                {
                    lastLogTick = Find.TickManager.TicksGame;
                    Log.Message("[FactionalWarRaidFix] excluded " + (originalCount - kept.Count)
                        + " faction(s) that cannot spawn non-animal combat pawns under SrFactionFirst");
                }
            }
            catch (Exception e)
            {
                // 过滤失败保持原列表,不拦截原逻辑
                Log.Warning("[FactionalWarRaidFix] filter failed: " + e);
            }
        }

        // 与真实生成同判据: 存在一个 Combat 组,该组在 SrFactionFirst 策略下有可用选项
        // (CanGenerateFrom → PawnGroupKindWorker_Normal.CanGenerateFrom → AnyOptions
        //  → PawnGenOptionValid → 策略 CanUsePawnGenOption,动物被排除)。
        private static bool CanSpawnForWar(Faction f, float points, PawnGroupKindDef groupKind, RaidStrategyDef strategy)
        {
            if (f.def == null || f.def.pawnGroupMakers == null)
            {
                return false;
            }
            PawnGroupMakerParms parms = new PawnGroupMakerParms();
            parms.faction = f;
            parms.points = points;
            parms.groupKind = groupKind;
            parms.raidStrategy = strategy;
            for (int i = 0; i < f.def.pawnGroupMakers.Count; i++)
            {
                PawnGroupMaker gm = f.def.pawnGroupMakers[i];
                if (gm.kindDef != groupKind)
                {
                    continue;
                }
                if (gm.CanGenerateFrom(parms))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

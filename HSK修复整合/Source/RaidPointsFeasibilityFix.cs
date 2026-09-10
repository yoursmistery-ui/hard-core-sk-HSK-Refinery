// RaidPointsFeasibilityFix.cs — 袭击点数不可行兜底(2026-09-04)
//
// 症状: 日志反复刷
//   "Faction 太阳帝国 of def Empire has no usable PawnGroupMakers for parms ... points=257.25 ..."
//   栈: QuestNode_Raid.RunInt -> PawnGroupMakerUtility.GeneratePawnKindsExample -> Enumerable.Any
//   (任务剧本 GiveQuest 生成对帝国袭击的预览, 循环最多 50 次 => 每次一条同类报错)
//
// 根因: 原版 IncidentWorker_Raid.AdjustedRaidPoints 末尾用
//   points = Max(points, raidStrategy.Worker.MinimumPoints(faction, groupKind) * 1.05f)
//   兜底, 而 MinimumPoints 走 FactionDef.MinPointsToGeneratePawnGroup(parms=null),
//   该估计忽略了 raidStrategy/ageRestriction 等真实生成判据; HSK + CE 抬高士兵基础点数后,
//   兜底值(257.25)仍低于"真实能生成的最便宜战斗兵种"成本 => 所有 Combat maker 的
//   CanGenerateFrom 全 false => TryGetRandomPawnGroupMaker 选不出 => 报"no usable"且真袭击会生成 0 兵。
//
// 方案: postfix AdjustedRaidPoints(任务预览与真实袭击共用同一入口, 一处修两处)。
//   用与真实生成完全同判据的 PawnGroupMakerUtility.TryGetRandomPawnGroupMaker(带 raidStrategy/
//   ageRestriction)从当前点数逐级抬高(×1.5+1, 封顶原值×8 或至少 2000, ≤40 步), 直到至少能选出
//   一个可用组为止 => 任何被选中的战斗袭击保证 ≥1 兵, 消除刷屏报错与空袭击。
//   仅"抬点数", 且只在当前点数确实生成不出兵时生效; 常规足够点数的袭击首探测即可行 -> 原值不变, 零影响。
//
// 声明式补丁, 由 FacilityCrashFix 的 assembly-wide Harmony.PatchAll() 自动应用(勿再手动 Patch)。
// 编译: 并入 HSKFixPack.dll(系统 csc, C#5)。仅需 Assembly-CSharp(RimWorld/Verse)+ Harmony。
using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RaidPointsFeasibilityFix
{
    [HarmonyPatch(typeof(IncidentWorker_Raid), "AdjustedRaidPoints")]
    public static class AdjustedRaidPoints_FeasibleFloor
    {
        static void Postfix(
            ref float __result,
            Faction faction,
            PawnGroupKindDef groupKind,
            RaidStrategyDef raidStrategy,
            RaidAgeRestrictionDef ageRestriction)
        {
            try
            {
                if (faction == null || faction.def == null)
                {
                    return;
                }
                // 仅处理战斗组袭击(商人/和平/定居/游客组的可行性由各自逻辑保证)
                if (groupKind == null || groupKind != PawnGroupKindDefOf.Combat)
                {
                    return;
                }
                if (faction.def.pawnGroupMakers == null || faction.def.pawnGroupMakers.Count == 0)
                {
                    return;
                }
                if (__result <= 0f)
                {
                    return;
                }

                PawnGroupMakerParms probe = new PawnGroupMakerParms();
                probe.faction = faction;
                probe.groupKind = groupKind;
                probe.raidStrategy = raidStrategy;
                probe.raidAgeRestriction = ageRestriction;
                probe.points = __result;

                float pts = __result;
                float cap = __result * 8f;
                if (cap < 2000f)
                {
                    cap = 2000f;
                }

                PawnGroupMaker found;
                bool feasible = PawnGroupMakerUtility.TryGetRandomPawnGroupMaker(probe, out found, false);
                int guard = 0;
                while (!feasible && guard < 40 && pts < cap)
                {
                    pts = pts * 1.5f + 1f;
                    probe.points = pts;
                    feasible = PawnGroupMakerUtility.TryGetRandomPawnGroupMaker(probe, out found, false);
                    guard++;
                }

                if (feasible && pts > __result)
                {
                    __result = pts;
                }
            }
            catch (Exception)
            {
                // 兜底失败保持原值, 绝不拦截/改写正常袭击流程
            }
        }
    }
}

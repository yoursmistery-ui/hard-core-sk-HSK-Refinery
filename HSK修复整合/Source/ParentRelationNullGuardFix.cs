// ParentRelationUtility.GetParent 悬空 parent 引用崩溃修复
//
// 症状: 读档 PostLoadInit(BackCompatibility.FactionManagerPostLoadInit)或世界生成派系首领
// (Faction.TryGenerateNewLeader)时抛
//   Error while generating pawn. NullReferenceException at RimWorld.ParentRelationUtility.GetParent
//   ... PawnRelationWorker_Child.GenerationChance -> other.GetMother()/GetFather()
//   -> Could not do PostLoadInit on RimWorld.FactionManager
//
// 根因: 派系内某个小人的 DirectRelations 里存在一条 def=Parent 的 DirectPawnRelation,
// 但其 otherPawn 为 null(该父/母小人已被销毁/丢弃或存档中未能解析)。原版 GetParent 遍历到这条
// 记录时直接读 directPawnRelation.otherPawn.gender -> NRE,中断首领小人关系生成,连锁报 PostLoadInit 失败。
// (HSK 生态里鼠族/姐妹/牧师等 C# mod 生成亲属关系时容易留下这种悬空 Parent 条目。)
//
// 方案: 给 ParentRelationUtility.GetParent(private static extension)加 Harmony 前缀,完整复刻原版逻辑,
// 唯一差别是遍历时跳过 otherPawn==null 的记录(有父则返回有效父,无有效父则返回 null,与"孤儿"正常路径一致,
// 下游 ChanceOfBecomingChildOf 本就处理 mother/father==null)。返回 false 接管原方法。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ParentRelationNullGuardFix
{
    [StaticConstructorOnStartup]
    public static class ParentRelationNullGuardInit
    {
        static ParentRelationNullGuardInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(ParentRelationUtility), "GetParent",
                    new Type[] { typeof(Pawn), typeof(Gender) });
                if (target == null)
                {
                    Log.Warning("[ParentRelationNullGuard] GetParent not found, skip.");
                    return;
                }
                Harmony harmony = new Harmony("local.hskparentrelationnullguard");
                harmony.Patch(target, prefix: new HarmonyMethod(
                    typeof(ParentRelationNullGuardInit).GetMethod(
                        "Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[ParentRelationNullGuard] patched ParentRelationUtility.GetParent");
            }
            catch (Exception e)
            {
                Log.Error("[ParentRelationNullGuard] patch failed: " + e);
            }
        }

        // pawn.GetParent(parentGender) —— 安全版:忽略 otherPawn 为空的 Parent 关系。
        private static bool Prefix(Pawn pawn, Gender parentGender, ref Pawn __result)
        {
            __result = null;
            if (pawn == null)
            {
                return false;
            }
            RaceProperties raceProps = pawn.RaceProps;
            if (raceProps == null || !raceProps.IsFlesh)
            {
                return false;
            }
            var relations = pawn.relations;
            if (relations == null)
            {
                return false;
            }
            List<DirectPawnRelation> directRelations = relations.DirectRelations;
            if (directRelations == null)
            {
                return false;
            }
            for (int i = 0; i < directRelations.Count; i++)
            {
                DirectPawnRelation rel = directRelations[i];
                if (rel == null || rel.def != PawnRelationDefOf.Parent)
                {
                    continue;
                }
                Pawn otherPawn = rel.otherPawn;
                if (otherPawn == null)
                {
                    continue; // 悬空 parent 引用,跳过而非崩溃
                }
                if (otherPawn.gender == parentGender)
                {
                    __result = otherPawn;
                    return false;
                }
            }
            return false;
        }
    }
}

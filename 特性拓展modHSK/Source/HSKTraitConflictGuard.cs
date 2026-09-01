// 特性拓展modHSK —— 冲突特性授予拦截器 (2026-08-24, v3)
//
// 根因(已反编译确认):
//   1. 原版 RimWorld 1.6 的 TraitSet.GainTrait 只查"是否已有同款", 完全不检查 conflictingTraits;
//   2. HAR(AlienRace.dll) 的 GenerateTraitsPostfix 授予 forcedTraitsChance(背景概率强制特性)
//      时也只查 HasTrait, 不查 conflictingTraits;
//   3. 鼠族背景(RatkinRaceHSK / RatkinBackStoryExpandedHSK)大量用 forcedTraitsChance 概率强制
//      Greedy(99)/Jealous(99)/Ascetic(40~80), 童年背景强制 Greedy/Jealous + 成年背景强制
//      Ascetic(或反向)交叉时, 两者同时被授予 → "贪心/嫉妒 + 苦行者" 共存。
//
// 方案(v2, 2026-08-24 用户要求"冲突时重新roll"): Harmony Prefix 拦截
//   TraitSet.GainTrait(suppressConflicts=false 时), 新特性与 pawn 已有特性冲突
//   (TraitDef.ConflictsWith 双向检查, 含 conflictingTraits + exclusionTags)时,
//   自动重新 roll 一个不冲突的替代特性授予(按 commonality 加权, 过滤背景 disallowed),
//   保证特性数量不损失; 无可用替代时才拒绝。
//
// v3 (2026-08-24 用户要求"只要生成人物那一刻校验"): 增加生成阶段门控 ——
//   仅当该小人正在 PawnGenerator 生成(PawnGenerator.IsBeingGenerated)时才执行冲突检查;
//   生成完成后的任何 GainTrait 授予(成长时刻/事件/其它 mod 加特性)一律放行,
//   不再重roll、不再拦截。存档读取仍不经 GainTrait, 天然不追溯。
//   ※ 1.6 实测: PawnGenerator 无 generatingFor 字段(编译不过), 门控改按小人查
//     IsBeingGenerated(pawn), 语义更精确(只拦正在生成的小人自身)。
//
// 边界:
//   - suppressConflicts=true 的调用(异种基因/Anomaly 等 suppression 体系)一律放行, 不干扰原版机制;
//   - 已存在的小人不受影响(存档读取不经 GainTrait, 符合"历史小人不动"原则);
//   - 被抑制(suppressedByTrait)的已有特性不视为生效, 不参与冲突判断;
//   - 异常时放行, 不破坏原逻辑。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    public static class HSKTraitConflictGuard
    {
        public static void Apply(Harmony harmony)
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(TraitSet), "GainTrait", new Type[] { typeof(Trait), typeof(bool) });
                if (target == null)
                {
                    Log.Warning("[HSKTraitExt] 未找到 TraitSet.GainTrait, 冲突拦截未生效");
                    return;
                }
                MethodInfo prefix = typeof(HSKTraitConflictGuard).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Log.Message("[HSKTraitExt] 已启用冲突特性授予拦截+重roll (TraitSet.GainTrait)");
            }
            catch (Exception ex)
            {
                Log.Error("[HSKTraitExt] 冲突拦截注册失败: " + ex);
            }
        }

        // 返回 false = 拒绝原授予(可能已改为授予替代特性); true = 放行
        private static bool Prefix(TraitSet __instance, Trait trait, bool suppressConflicts)
        {
            try
            {
                if (trait == null || trait.def == null || __instance == null || __instance.allTraits == null)
                {
                    return true;
                }
                if (suppressConflicts)
                {
                    return true; // 调用方显式要求不处理冲突(异种/基因授予等), 尊重原版
                }
                // v3 (2026-08-24): 只在 PawnGenerator 生成小人阶段执行冲突检查;
                // 生成完成后的任何 GainTrait 授予(成长时刻/事件/其它 mod)一律放行
                Pawn p = AccessTools.Field(typeof(TraitSet), "pawn").GetValue(__instance) as Pawn;
                if (p == null || !PawnGenerator.IsBeingGenerated(p))
                {
                    return true;
                }
                TraitDef conflicted = null;
                for (int i = 0; i < __instance.allTraits.Count; i++)
                {
                    Trait held = __instance.allTraits[i];
                    if (held == null || held.def == null)
                    {
                        continue;
                    }
                    if (held.def == trait.def)
                    {
                        return true; // 同款交给原版 HasTrait 逻辑处理
                    }
                    if (held.suppressedByTrait)
                    {
                        continue; // 被抑制特性不实际生效, 不拦
                    }
                    if (trait.def.ConflictsWith(held.def))
                    {
                        conflicted = held.def;
                        break;
                    }
                }
                if (conflicted == null)
                {
                    return true;
                }

                string pawnName = PawnNameOf(__instance);
                // 冲突: 重新 roll 一个不冲突的替代特性, 保证特性数量不损失
                Trait alt = RollAlternative(__instance, trait.def);
                if (alt != null)
                {
                    Log.Message("[HSKTraitExt] 特性冲突重roll: " + pawnName + " 的[" + trait.def.label + "]与已有[" + conflicted.label + "]互斥, 已改授[" + alt.def.label + "]");
                    __instance.GainTrait(alt, false); // 递归授予替代(alt 已确认不冲突, 会直接通过本拦截)
                }
                else
                {
                    Log.Message("[HSKTraitExt] 拦截冲突特性授予: " + pawnName + " 已有[" + conflicted.label + "], 与[" + trait.def.label + "]互斥, 无可用替代, 已拒绝");
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[HSKTraitExt] 冲突拦截异常(已放行): " + ex);
                return true;
            }
        }

        // 按 commonality 加权随机选一个不冲突、未被背景 disallow 的特性; 找不到返回 null
        private static Trait RollAlternative(TraitSet ts, TraitDef rejected)
        {
            Pawn pawn = null;
            try
            {
                pawn = AccessTools.Field(typeof(TraitSet), "pawn").GetValue(ts) as Pawn;
            }
            catch { }
            if (pawn == null || pawn.story == null)
            {
                return null;
            }

            List<TraitDef> all = DefDatabase<TraitDef>.AllDefsListForReading;
            for (int tries = 0; tries < 200; tries++)
            {
                TraitDef cand = all.RandomElementByWeight(delegate(TraitDef d) { return d.GetGenderSpecificCommonality(pawn.gender); });
                if (cand == null || cand == rejected)
                {
                    continue;
                }
                if (pawn.story.traits.HasTrait(cand))
                {
                    continue;
                }
                bool bad = false;
                for (int i = 0; i < ts.allTraits.Count; i++)
                {
                    Trait held = ts.allTraits[i];
                    if (held == null || held.def == null)
                    {
                        continue;
                    }
                    if (held.def == cand)
                    {
                        bad = true;
                        break;
                    }
                    if (held.suppressedByTrait)
                    {
                        continue;
                    }
                    if (cand.ConflictsWith(held.def))
                    {
                        bad = true;
                        break;
                    }
                }
                if (bad)
                {
                    continue;
                }
                int degree = PawnGenerator.RandomTraitDegree(cand);
                if (pawn.story.Childhood != null && pawn.story.Childhood.DisallowsTrait(cand, degree))
                {
                    continue;
                }
                if (pawn.story.Adulthood != null && pawn.story.Adulthood.DisallowsTrait(cand, degree))
                {
                    continue;
                }
                return new Trait(cand, degree);
            }
            return null;
        }

        private static string PawnNameOf(TraitSet ts)
        {
            try
            {
                Pawn p = AccessTools.Field(typeof(TraitSet), "pawn").GetValue(ts) as Pawn;
                if (p != null)
                {
                    return p.LabelShort;
                }
            }
            catch { }
            return "该小人";
        }
    }
}

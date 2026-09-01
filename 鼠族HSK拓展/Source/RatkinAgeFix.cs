// 鼠族HSK拓展 - 年龄生成修复
//
// 问题: Character Editor 在 Biotech 下生成角色时,请求 BiologicalAgeRange = 12.1~13,
// 而 PawnGenerator.GenerateRandomAge 是按年龄曲线(Rand.ByCurve)采样后再做范围/阶段校验,
// 鼠族曲线在该区间概率几乎为 0,导致 300 次重试全部失败("Tried 300 times to generate age")。
// 另外 CE 有 27% 概率只允许 Child 阶段,而鼠族 12.1~13 岁属于 Teenager 生命阶段,
// 其 developmentalStage 默认是 Adult,永远过不了 Child 校验 —— 光改曲线救不了这个分支。
//
// 方案: 在 PawnGenerator.GenerateNewPawnInternal(ref PawnGenerationRequest) 入口,
// 当请求带 BiologicalAgeRange 时,直接按区间取一个确定年龄放到 FixedBiologicalAge,
// 清空 BiologicalAgeRange,并把实际年龄所属的发育阶段并入 AllowedDevelopmentalStages。
// 这样 GenerateRandomAge 走固定年龄分支,不再采样曲线、不再 300 次重试。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RatkinAgeFix
{
    [StaticConstructorOnStartup]
    public static class RatkinAgeFixInit
    {
        static RatkinAgeFixInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.ratkin.clothesweapons.agefix");
                MethodInfo target = AccessTools.Method(typeof(PawnGenerator), "GenerateNewPawnInternal");
                if (target == null)
                {
                    Log.Warning("[RatkinAgeFix] PawnGenerator.GenerateNewPawnInternal not found");
                    return;
                }
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(RatkinAgeFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception e)
            {
                Log.Error("[RatkinAgeFix] patch failed: " + e);
            }
        }

        private static void Prefix(ref PawnGenerationRequest request)
        {
            if (!request.BiologicalAgeRange.HasValue)
            {
                return;
            }
            if (request.FixedBiologicalAge.HasValue)
            {
                return;
            }
            float age = request.BiologicalAgeRange.Value.RandomInRange;
            request.FixedBiologicalAge = age;
            request.BiologicalAgeRange = null;

            // 把该年龄实际落入的发育阶段并入允许列表,避免后续 ValidatePawn 因阶段不符整只重生成。
            if (request.KindDef != null && request.KindDef.race != null && request.KindDef.race.race != null && request.KindDef.race.race.lifeStageAges != null)
            {
                DevelopmentalStage stage = DevelopmentalStage.Adult;
                foreach (LifeStageAge lifeStageAge in request.KindDef.race.race.lifeStageAges)
                {
                    if (age < lifeStageAge.minAge)
                    {
                        break;
                    }
                    if (lifeStageAge.def != null)
                    {
                        stage = lifeStageAge.def.developmentalStage;
                    }
                }
                request.AllowedDevelopmentalStages |= stage;
            }
        }
    }
}

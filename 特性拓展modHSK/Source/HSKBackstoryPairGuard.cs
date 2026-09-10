// 特性拓展modHSK —— 背景配对技能一致性守卫 (2026-08-24)
//
// 需求(用户定义):
//   - 幼年(Childhood)背景生成不受限,想怎么选都行;
//   - 成年(Adulthood,含"老年向"成年背景)背景生成时,与已选幼年背景按"技能一致性"判定,
//     同一技能上:
//       幼年 +1   → 成年禁止 -2/-3(允许 -1);
//       幼年 +2/+3 → 成年禁止一切负数;
//     规则双向:成年背景也当作参考方判定(把成年当幼年一样判定);
//   - 背景若禁用了某技能(workDisables 使该技能失效),该技能视为 ---(最差档);
//   - 冲突就重新 roll,直到不冲突为止;
//   - 只在生成人物时检查,生成过的小人一律不动。
//
// 实现(2026-08-24 反编译 1.6.4871 确认):
//   - 原版背景配对唯一限制 = requiredWorkTags ↔ workDisables(禁工作),完全不查技能加减数值,
//     因此"幼年+2射击/成年-3射击"等矛盾组合原版照常允许;
//   - 本守卫全量替换 PawnBioAndNameGenerator.FillBackstorySlotShuffled(Harmony prefix 返回 false):
//       复刻原版逻辑 + 在成年槽候选过滤中注入本规则 → 选中的背景必然满足规则(等效于"冲突重roll");
//   - 生成顺序:原版 PawnGenerator 先生成背景(L807 GiveAppropriateBioAndNameTo)后生成特性
//     (L819 GenerateTraits),本守卫挂在背景生成环节,天然先于特性判定,无需额外处理。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    public static class HSKBackstoryPairGuard
    {
        private const int MaxRollAttempts = 60; // 每轮抽 20 个候选,60 轮足够覆盖全池

        public static void Apply(Harmony harmony)
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(PawnBioAndNameGenerator), "FillBackstorySlotShuffled",
                    new Type[]
                    {
                        typeof(Pawn), typeof(BackstorySlot), typeof(List<BackstoryCategoryFilter>),
                        typeof(FactionDef), typeof(BackstorySlot?)
                    });
                if (target == null)
                {
                    Log.Warning("[HSKTraitExt] 未找到 PawnBioAndNameGenerator.FillBackstorySlotShuffled, 背景配对守卫未生效");
                    return;
                }
                MethodInfo prefix = typeof(HSKBackstoryPairGuard).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                Log.Message("[HSKTraitExt] 已启用背景配对技能一致性守卫 (FillBackstorySlotShuffled)");
            }
            catch (Exception ex)
            {
                Log.Error("[HSKTraitExt] 背景配对守卫注册失败: " + ex);
            }
        }

        // 返回 false = 全量替换原方法(复刻原版逻辑 + 注入技能一致性规则)。
        // 返回 true  = 放行原版(设置关闭或异常时)。
        private static bool Prefix(Pawn pawn, BackstorySlot slot, List<BackstoryCategoryFilter> backstoryCategories,
            FactionDef factionType, BackstorySlot? mustBeCompatibleTo)
        {
            try
            {
                if (HSKTraitMod.settings == null || !HSKTraitMod.settings.enableBackstoryPairGuard)
                {
                    return true; // 设置关闭 → 走原版
                }
                if (pawn == null || pawn.story == null)
                {
                    return true;
                }

                // —— 复刻原版 FillBackstorySlotShuffled 主体 ——
                BackstoryCategoryFilter categoryFilter = null;
                if (backstoryCategories != null && backstoryCategories.Count > 0)
                {
                    categoryFilter = backstoryCategories.RandomElementByWeight(c => c.commonality);
                }
                if (categoryFilter == null)
                {
                    // 复刻原版 FallbackCategoryGroup
                    categoryFilter = new BackstoryCategoryFilter
                    {
                        categories = new List<string> { "Civil" },
                        commonality = 1f
                    };
                }

                IEnumerable<BackstoryDef> source = AllBackstories()
                    .Where(bs => bs.shuffleable && categoryFilter.Matches(bs));

                List<BackstoryDef> candidates = new List<BackstoryDef>();
                if (!mustBeCompatibleTo.HasValue)
                {
                    candidates.AddRange(source.Where(bs => bs.slot == slot));
                }
                else
                {
                    List<BackstoryDef> compatible = source.Where(bs => bs.slot == mustBeCompatibleTo.Value).ToList();
                    candidates.AddRange(source.Where(bs => bs.slot == slot
                        && compatible.Any(b => !b.requiredWorkTags.OverlapsWithOnAnyWorkType(bs.workDisables)
                                               && !ViolatesPairRule(bs, b))));
                }

                // —— 抽取:等效"冲突就重 roll,直到不冲突为止" ——
                BackstoryDef result = null;
                for (int attempt = 0; attempt < MaxRollAttempts && result == null; attempt++)
                {
                    IEnumerable<BackstoryDef> pool = candidates.TakeRandom(20).Where(bs =>
                        VanillaAllows(pawn, slot, bs)
                        && (slot != BackstorySlot.Adulthood || pawn.story.Childhood == null
                            || !ViolatesPairRule(pawn.story.Childhood, bs)));
                    if (pool.TryRandomElementByWeight(SelectionWeight, out result))
                    {
                        break;
                    }
                }

                // 兜底:技能一致性过滤后候选为空(如商队领袖成年背景与童年背景在
                // workDisables/技能上天然冲突, 见 2026-08-25 商队背景)时, 放宽到
                // 同一分类下"仅满足 vanilla 规则"的候选, 避免刷 No shuffled + 全库乱选。
                if (result == null)
                {
                    Log.Warning("[HSKTraitExt] 背景配对守卫: " + slot + " 未找到同时满足技能一致性规则的候选,已放宽到原版规则("
                        + pawn.ToStringSafe() + " / " + factionType.ToStringSafe() + ")。");
                    List<BackstoryDef> fallbackPool = candidates.Count > 0
                        ? candidates
                        : source.Where(bs => bs.slot == slot).ToList();
                    for (int attempt = 0; attempt < MaxRollAttempts && result == null; attempt++)
                    {
                        IEnumerable<BackstoryDef> pool = fallbackPool.TakeRandom(20).Where(bs => VanillaAllows(pawn, slot, bs));
                        if (pool.TryRandomElementByWeight(SelectionWeight, out result))
                        {
                            break;
                        }
                    }
                }
                if (result == null)
                {
                    Log.Error("No shuffled " + slot + " found for " + pawn.ToStringSafe() + " of "
                        + factionType.ToStringSafe() + " (HSKTraitExt 背景配对守卫兜底). Choosing random.");
                    result = AllBackstories().Where(bs => bs.slot == slot).RandomElement();
                }

                if (slot == BackstorySlot.Adulthood)
                {
                    pawn.story.Adulthood = result;
                }
                else
                {
                    pawn.story.Childhood = result;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[HSKTraitExt] 背景配对守卫异常(已放行原版): " + ex);
                return true;
            }
        }

        // 复刻原版最终过滤:成年背景的 requiredWorkTags 不得与幼年背景的 workDisables 冲突
        private static bool VanillaAllows(Pawn pawn, BackstorySlot slot, BackstoryDef bs)
        {
            if (slot != BackstorySlot.Adulthood)
            {
                return true;
            }
            if (bs.requiredWorkTags == WorkTags.None)
            {
                return true;
            }
            if (pawn.story.Childhood == null)
            {
                return true;
            }
            return !bs.requiredWorkTags.OverlapsWithOnAnyWorkType(pawn.story.Childhood.workDisables);
        }

        // —— 技能一致性规则(双向:成年也当参考方) ——
        // 对每个技能:参考方增益 gRef 对另一方增益 gOther 的限制:
        //   gRef == +1   → gOther 禁 -2/-3(允许 -1);
        //   gRef >= +2   → gOther 禁一切负数;
        //   gRef <= 0    → 不限。
        private static bool ViolatesPairRule(BackstoryDef a, BackstoryDef b)
        {
            List<SkillDef> skills = DefDatabase<SkillDef>.AllDefsListForReading;
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDef skill = skills[i];
                int ga = EffectiveGain(a, skill);
                int gb = EffectiveGain(b, skill);
                if (ga == 0 && gb == 0)
                {
                    continue; // 双方都没涉及该技能
                }
                if (ViolatesOneSide(ga, gb) || ViolatesOneSide(gb, ga))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool ViolatesOneSide(int gRef, int gOther)
        {
            if (gRef == 1)
            {
                return gOther <= -2; // 参考方 +1 → 对方禁 -2/-3
            }
            if (gRef >= 2)
            {
                return gOther <= -1; // 参考方 +2/+3 → 对方禁一切负数
            }
            return false;            // 参考方 0/负数 → 不限
        }

        // 背景在技能上的有效增益:背景禁用了该技能(workDisables 使技能失效) → 视为 ---(最差档)
        private static int EffectiveGain(BackstoryDef bs, SkillDef skill)
        {
            if (skill.IsDisabled(bs.workDisables, bs.DisabledWorkTypes))
            {
                return int.MinValue;
            }
            List<SkillGain> gains = bs.skillGains;
            for (int i = 0; i < gains.Count; i++)
            {
                SkillGain sg = gains[i];
                if (sg.skill == skill)
                {
                    return sg.amount;
                }
            }
            return 0;
        }

        // 复刻原版 BackstorySelectionWeight:按 workDisables 档位给权重
        private static float SelectionWeight(BackstoryDef bs)
        {
            WorkTags wt = bs.workDisables;
            float num = 1f;
            if ((wt & WorkTags.ManualDumb) != WorkTags.None) num *= 0.5f;
            if ((wt & WorkTags.ManualSkilled) != WorkTags.None) num *= 1f;
            if ((wt & WorkTags.Violent) != WorkTags.None) num *= 0.6f;
            if ((wt & WorkTags.Social) != WorkTags.None) num *= 0.7f;
            if ((wt & WorkTags.Intellectual) != WorkTags.None) num *= 0.4f;
            if ((wt & WorkTags.Firefighting) != WorkTags.None) num *= 0.8f;
            return num;
        }

        // 全量背景池:原版 BackstoryDef + HAR(AlienRace) 的 AlienBackstoryDef。
        // 铁律(2026-08-25 反编译确认): DirectXmlLoader.DefFromNode 按节点类型调用
        //   DefDatabase<T>.Add → HAR 鼠族背景注册在 DefDatabase<AlienBackstoryDef>,
        //   不在 DefDatabase<BackstoryDef>。守卫若只用后者, 商队/游民等纯 HAR 分类
        //   的背景永远匹配不到 → 候选池空 → 刷 "No shuffled ... Choosing random"。
        //   故这里合并两个数据库, 按 defName 去重(同一 def 只保留一个引用)。
        private static IEnumerable<BackstoryDef> AllBackstories()
        {
            HashSet<string> seen = new HashSet<string>();
            List<BackstoryDef> list = new List<BackstoryDef>();
            foreach (BackstoryDef bs in DefDatabase<BackstoryDef>.AllDefs)
            {
                if (bs != null && seen.Add(bs.defName))
                {
                    list.Add(bs);
                }
            }
            try
            {
                Type harType = GenTypes.GetTypeInAnyAssembly("AlienRace.AlienBackstoryDef");
                if (harType != null)
                {
                    Type defDb = typeof(DefDatabase<>).MakeGenericType(harType);
                    PropertyInfo allDefs = defDb.GetProperty("AllDefs",
                        BindingFlags.Public | BindingFlags.Static);
                    if (allDefs != null)
                    {
                        object value = allDefs.GetValue(null);
                        System.Collections.IEnumerable harDefs = value as System.Collections.IEnumerable;
                        if (harDefs != null)
                        {
                            foreach (object obj in harDefs)
                            {
                                BackstoryDef bs = obj as BackstoryDef;
                                if (bs != null && seen.Add(bs.defName))
                                {
                                    list.Add(bs);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // HAR 缺失/版本差异时静默降级为仅原版背景, 不阻断背景生成
                Log.WarningOnce("[HSKTraitExt] 读取 HAR 背景池失败, 仅使用原版背景: " + ex.Message, 1837001);
            }
            return list;
        }
    }
}

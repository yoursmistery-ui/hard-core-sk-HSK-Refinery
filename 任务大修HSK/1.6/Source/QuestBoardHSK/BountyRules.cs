using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 悬赏规则核心(2026-08-31 六期):
    /// 科技档门槛(只能悬赏与发布方相差一级以内)、接受侧科技惩罚(目标档低于本方×0.5)、
    /// 发出侧预付手续费(按目标档 500~5000,越级×2)、派系关系倍率(0最低×1.0,+100→×1.5,−100→×1.4)、
    /// 发单激怒(按职位+身价分级)。奖励公式 = 角色身价 × 科技惩罚 × 关系倍率。
    /// </summary>
    internal static class BountyRules
    {
        public const int MaxTechGap = 1;
        public const float BelowTechRewardFactor = 0.5f;
        public const float AboveTechCostFactor = 2f;

        private const float AngerBase = 12f;
        private const float AngerLeaderBonus = 40f;
        private const float AngerSeniorityDiv = 25f;
        private const float AngerValueDiv = 150f;
        private const int AngerMin = 12;
        private const int AngerMax = 110;

        // 预付手续费,按被通缉方派系科技档
        public static int PrepayForTech(TechLevel t)
        {
            switch (t)
            {
                case TechLevel.Medieval: return 1000;
                case TechLevel.Industrial: return 2000;
                case TechLevel.Spacer: return 3500;
                case TechLevel.Ultra:
                case TechLevel.Archotech: return 5000;
                default: return 500;
            }
        }

        // 关系倍率:关系0最低(×1.0),+100→×1.5,−100→×1.4,中间线性
        public static float RelationMultiplier(int goodwill)
        {
            float g = Mathf.Clamp(goodwill, -100, 100) / 100f;
            return 1f + (g >= 0f ? 0.5f * g : -0.4f * g);
        }

        // 接受侧科技惩罚:目标派系档低于本方 → 奖励×0.5
        public static float AcceptTechMultiplier(TechLevel targetTech)
        {
            return targetTech < PlayerTechLevel() ? BelowTechRewardFactor : 1f;
        }

        // 玩家科技档 = 派系基线与已完成研究的最高档取大(按游戏日缓存,ResearchManager 随存档)
        private static Game cacheGame;
        private static int cacheDay = -1;
        private static TechLevel cacheVal;

        public static TechLevel PlayerTechLevel()
        {
            Game game = Current.Game;
            int day = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) / 60000;
            if (cacheGame == game && cacheDay == day)
                return cacheVal;
            TechLevel lvl = Faction.OfPlayer.def.techLevel;
            foreach (ResearchProjectDef rp in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (rp.techLevel > lvl && rp.IsFinished)
                    lvl = rp.techLevel;
            }
            cacheGame = game;
            cacheDay = day;
            cacheVal = lvl;
            return lvl;
        }

        // 世界NPC派系(可见、未灭、非玩家、人类系)
        public static bool IsWorldNpcFaction(Faction f)
        {
            return f != null && f.def != null && f.def.humanlikeFaction
                && !f.defeated && !f.Hidden && !f.IsPlayer;
        }

        public static bool TechGapOk(Faction a, Faction b)
        {
            return a?.def != null && b?.def != null
                && Mathf.Abs((int)a.def.techLevel - (int)b.def.techLevel) <= MaxTechGap;
        }

        // 受雇完成成功率(玩家发布的悬赏由NPC派系执行):原始66% → 极致33% 线性,两端封顶
        public const float SuccessChanceHigh = 0.66f;
        public const float SuccessChanceLow = 0.33f;

        public static float WarrantSuccessChance(TechLevel t)
        {
            float perLevel = (SuccessChanceHigh - SuccessChanceLow)
                / ((int)TechLevel.Ultra - (int)TechLevel.Neolithic);
            float v = SuccessChanceHigh - perLevel * ((int)t - (int)TechLevel.Neolithic);
            return Mathf.Clamp(v, SuccessChanceLow, SuccessChanceHigh);
        }

        // 发单激怒:领袖+40、王室头衔 seniority/25、身价/150,基准12,clamp 12~110
        public static int AngerOnIssue(Pawn target, Faction targetFaction)
        {
            float rank = 0f;
            if (targetFaction.leader == target)
                rank += AngerLeaderBonus;
            if (target.royalty != null && target.royalty.AllTitlesForReading != null)
            {
                int seniority = -1;
                foreach (RoyalTitle t in target.royalty.AllTitlesForReading)
                {
                    if (t != null && t.def != null && t.faction == targetFaction && t.def.seniority > seniority)
                        seniority = t.def.seniority;
                }
                if (seniority > 0)
                    rank += seniority / AngerSeniorityDiv;
            }
            float hit = AngerBase + rank + Mathf.Max(0f, target.MarketValue) / AngerValueDiv;
            return Mathf.Clamp(Mathf.RoundToInt(hit), AngerMin, AngerMax);
        }

        public static string TechLevelLabel(TechLevel t)
        {
            return ("RK_Bounty.Tech_" + t).Translate().ToString();
        }

        // SW 的设置壳类 SimpleWarrantsMod 是 internal,反射读其静态 Settings.warrantRewardScaling,探明后缓存
        private static bool scalingProbed;
        private static bool scalingEnabled;

        public static bool RewardScalingEnabled()
        {
            if (!scalingProbed)
            {
                scalingProbed = true;
                System.Type modT = AccessTools.TypeByName("SimpleWarrants.SimpleWarrantsMod");
                object settings = modT != null ? AccessTools.Property(modT, "Settings")?.GetValue(null) : null;
                if (settings != null)
                {
                    object v = AccessTools.Field(settings.GetType(), "warrantRewardScaling")?.GetValue(settings);
                    scalingEnabled = v is bool b && b;
                }
            }
            return scalingEnabled;
        }
    }
}

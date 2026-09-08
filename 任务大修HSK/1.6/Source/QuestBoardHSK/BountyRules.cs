using System.Collections.Generic;
using System.Linq;
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
        public static float AboveTechCostFactor { get { RewardCurveDef c = RewardCurves.Current; return c != null ? c.aboveTechCostFactor : 2f; } }

        // 玩家发单报酬上下限(2026-09-02):下限=身价×15%,上限=身价×60%;目标科技高于我方 → 上限×2
        public const float RewardMinPct = 0.15f;
        public const float RewardMaxPct = 0.60f;
        public const float RewardMaxOverTechMult = 2f;

        private const float AngerBase = 12f;
        private const float AngerLeaderBonus = 40f;
        private const float AngerSeniorityDiv = 25f;
        private const float AngerValueDiv = 150f;
        private const int AngerMin = 12;
        private const int AngerMax = 110;

        // 预付手续费,按被通缉方派系科技档（读 RewardCurveDef，缺则回退）
        public static int PrepayForTech(TechLevel t)
        {
            RewardCurveDef c = RewardCurves.Current;
            switch (t)
            {
                case TechLevel.Medieval: return c != null ? c.prepayMedieval : 1000;
                case TechLevel.Industrial: return c != null ? c.prepayIndustrial : 2000;
                case TechLevel.Spacer: return c != null ? c.prepaySpacer : 3500;
                case TechLevel.Ultra: return c != null ? c.prepayUltra : 5000;
                case TechLevel.Archotech: return c != null ? c.prepayArchotech : 5000;
                default: return c != null ? c.prepayNeolithic : 500;
            }
        }

        // —— 通缉扩展(2026-09-02):跨档费用累进 + 发布统一校验 ——
        // 跨档不再硬拒,费用按每超 1 档 ×2 累进(超1档×2/超2档×4/超3档×8 封顶)
        public const float MaxOverTechMult = 8f;

        public static int PrepayCost(TechLevel targetTech)
        {
            int over = (int)targetTech - (int)PlayerTechLevel();
            float mult = over > 0 ? Mathf.Min(MaxOverTechMult, Mathf.Pow(AboveTechCostFactor, over)) : 1f;
            return Mathf.RoundToInt(PrepayForTech(targetTech) * mult);
        }

        // 发布统一校验+预付扣费:发单窗(TryPublishFromUi)与 SW 主窗旁路(MainTabWindow_TryAddWarrant_Patch)
        // 共用此入口,消除双份校验。返回 true=已扣预付费可发布;false=failMsg 已给出失败文案。
        public static bool TryChargePrepay(Warrant warrant, out TaggedString failMsg, out bool overTech)
        {
            overTech = false;
            failMsg = TaggedString.Empty;
            // 十期信鸽强绑定:发布通缉必须经信鸽柱寄出
            if (!BirdPostUtil.AnyPlayerBirdPost())
            {
                failMsg = "RK_Bounty.NeedBirdPost".Translate();
                return false;
            }
            if (!(warrant is Warrant_Pawn wp) || wp.Pawn == null || wp.Pawn.RaceProps.Animal)
                return true;
            Faction targetFac = wp.Pawn.Faction;
            if (targetFac == null || targetFac == Faction.OfPlayer)
                return true;
            overTech = targetFac.def.techLevel > PlayerTechLevel();
            int prepay = PrepayCost(targetFac.def.techLevel);
            List<Thing> silvers = Utils.AllPlayerSilver();
            int have = silvers.Sum(t => t.stackCount);
            if (have < prepay)
            {
                failMsg = "RK_Bounty.NeedPrepay".Translate(prepay, have);
                return false;
            }
            warrant.Pay(silvers, prepay);
            Messages.Message(overTech
                ? "RK_Bounty.PrepayPaidOver".Translate(prepay)
                : "RK_Bounty.PrepayPaid".Translate(prepay),
                MessageTypeDefOf.NeutralEvent, false);
            return true;
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

        // 玩家发单报酬上下限:目标身价百分比区间;目标科技高于我方 → 上限×2(2026-09-02)
        public static void RewardBounds(Pawn pawn, out int min, out int max, out bool overTech)
        {
            min = 0;
            max = 0;
            overTech = false;
            if (pawn == null)
                return;
            min = Mathf.RoundToInt(pawn.MarketValue * RewardMinPct);
            max = Mathf.RoundToInt(pawn.MarketValue * RewardMaxPct);
            if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer && pawn.Faction.def != null)
            {
                TechLevel t = pawn.Faction.def.techLevel;
                overTech = t > PlayerTechLevel();
                if (overTech)
                    max = Mathf.RoundToInt(max * RewardMaxOverTechMult);
            }
            min = Mathf.Max(1, min);
            max = Mathf.Max(min, max);
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

        // —— 通缉扩展(2026-09-02):NPC 悬赏接取量随财富成长(按日缓存财富) ——
        // 在榜+在途总量上限与目标科技档上限随殖民地财富解锁,低财富只有少量低档悬赏。
        public static void OfferCap(out int maxCount, out int maxTier)
        {
            int wealth = ColonyWealth();
            if (wealth < 50000) { maxCount = 3; maxTier = Tier1; }
            else if (wealth < 100000) { maxCount = 4; maxTier = Tier2; }
            else if (wealth < 250000) { maxCount = 6; maxTier = Tier2; }
            else if (wealth < 500000) { maxCount = 8; maxTier = Tier3; }
            else if (wealth < 1000000) { maxCount = 9; maxTier = Tier3; }
            else { maxCount = 10; maxTier = Tier4; }
        }

        private static Game wealthCacheGame;
        private static int wealthCacheDay = -1;
        private static int wealthCacheVal;

        // 殖民地财富(主图 WealthTotal,按游戏日缓存)
        public static int ColonyWealth()
        {
            Game game = Current.Game;
            int day = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) / 60000;
            if (wealthCacheGame == game && wealthCacheDay == day)
                return wealthCacheVal;
            Map map = Find.AnyPlayerHomeMap;
            int wealth = map != null && map.wealthWatcher != null
                ? Mathf.RoundToInt(map.wealthWatcher.WealthTotal) : 0;
            wealthCacheGame = game;
            wealthCacheDay = day;
            wealthCacheVal = wealth;
            return wealth;
        }

        // —— P3 任务分级：按目标科技档派生（一致性用于奖励/报价/合法性；不入库、无需 Warrant 加字段）——
        public const int Tier1 = 1, Tier2 = 2, Tier3 = 3, Tier4 = 4;
        public static int TierOf(TechLevel t)
        {
            int r = TaskTiers.RankForTech(t);        // 优先查 TaskTierDef 表
            if (r > 0) return r;
            if (t <= TechLevel.Medieval) return Tier1;
            if (t == TechLevel.Industrial) return Tier2;
            if (t == TechLevel.Spacer) return Tier3;
            return Tier4; // Ultra / Archotech
        }
        public static float TierRewardMult(int tier)
        {
            float m = TaskTiers.RewardMultOrSentinel(tier);   // 优先查表
            if (m >= 0f) return m;
            RewardCurveDef c = RewardCurves.Current;
            switch (tier)
            {
                case Tier2: return c != null ? c.tier2Mult : 1.3f;
                case Tier3: return c != null ? c.tier3Mult : 1.7f;
                case Tier4: return c != null ? c.tier4Mult : 2.2f;
                default: return c != null ? c.tier1Mult : 1f;
            }
        }
        // 隐藏刺客公会(改造自敌对支):暗杀委托的直供方 / 好感结算对象
        private static readonly HashSet<string> GuildDefNames = new HashSet<string> { "Kurin_Faction_Hostile", "Miho_Faction_Supremacist" };
        public static bool IsAssassinGuild(Faction f)
        {
            return f != null && f.def != null && GuildDefNames.Contains(f.def.defName);
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

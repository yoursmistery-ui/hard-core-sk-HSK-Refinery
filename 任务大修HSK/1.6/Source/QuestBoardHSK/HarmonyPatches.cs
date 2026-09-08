using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using SimpleWarrants;

namespace QuestBoardHSK
{
    public static class QB_JobDefOf
    {
        private static JobDef receiveRadio;
        private static ThingDef birdPost;

        public static JobDef RK_Bounty_ReceiveRadio
        {
            get
            {
                if (receiveRadio == null)
                    receiveRadio = DefDatabase<JobDef>.GetNamed("RK_Bounty_ReceiveRadio");
                return receiveRadio;
            }
        }

        public static ThingDef RK_Bounty_BirdPost
        {
            get
            {
                if (birdPost == null)
                    birdPost = DefDatabase<ThingDef>.GetNamedSilentFail("RK_Bounty_BirdPost");
                return birdPost;
            }
        }
    }

    internal static class RadioNotifier
    {
        /// <summary>扑翅特效冷却:两次「开播」之间最短间隔 2 秒(120 tick @60tps)。</summary>
        private const int FlapCooldownTicks = 120;

        /// <summary>上次扑翅特效开播的游戏 tick;int.MinValue 表示本局尚未播过。</summary>
        private static int lastFlapTick = int.MinValue;

        internal static void Send(string titleKey, TaggedString text)
        {
            TryPlayFlap();
            Find.LetterStack.ReceiveLetter(
                "RK_Bounty.LetterTag".Translate() + ": " + titleKey.Translate(),
                text,
                LetterDefOf.NeutralEvent);
        }

        // 信鸽柱纯演出动画(2026-09-06):本 mod 自己的无线电/悬赏/外交类信件发出时,
        // 若有带饲料的信鸽柱就播一次扑翅。只做动画,不拦截、不延迟任何原版/第三方信件
        // (曾全局拦截 LetterStack 漏斗,Positive/Neutral 标准信被无限延后、永不入历史)。
        private static void TryPlayFlap()
        {
            if (Find.TickManager == null)
                return;
            Building post = BirdPostUtil.FindFedPost(true);
            if (post == null)
                return;
            int now = Find.TickManager.TicksGame;
            if (lastFlapTick != int.MinValue && now - lastFlapTick < FlapCooldownTicks)
                return;
            EffecterDef flap = DefDatabase<EffecterDef>.GetNamedSilentFail("RK_BirdPost_Flap");
            if (flap == null)
                return;
            flap.Spawn(post, post.MapHeld);
            lastFlapTick = now;
        }
    }

    // —— 一周一条限流:冷却期内直接短路 SW 的生成(避免冷却期每 tick 空跑昂贵生成) ——
    // GenerateRandomWarrant 为 private 无重载方法。
    [HarmonyPatch(typeof(WarrantsManager), "GenerateRandomWarrant")]
    internal static class WarrantsManager_GenerateRandomWarrant_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(ref Warrant __result)
        {
            // 十期信鸽强绑定:没有信鸽柱(或缺饲料)就不生成新通缉(无线电事件静默不发生)。
            if (!BirdPostUtil.AnyPlayerBirdPost())
            {
                __result = null;
                return false;
            }
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio != null && radio.GenerationCooledDown())
            {
                __result = null;
                return false;
            }
            // 十二期财富挂钩:在榜+待接收+在途总量达上限就不再生成(接取量随财富成长)。
            WarrantsManager mgr = WarrantsManager.Instance;
            if (mgr != null && radio != null)
            {
                BountyRules.OfferCap(out int cap, out int _);
                if (mgr.availableWarrants.Count + radio.pending.Count + radio.inFlight.Count >= cap)
                {
                    __result = null;
                    return false;
                }
            }
            return true;
        }

        // 六期科技门槛:目标必须来自世界NPC派系(可见/未灭/非玩家/人类系),
        // 且目标派系与发布方科技档相差一级以内(封死"原始时代悬赏极致时代天网");
        // 十二期:目标科技档还须不超过财富解锁的 T 档上限。
        // 违规销毁临时生成的 pawn 并返回 null,由 SW 的重试/限流机制自然消化。
        [HarmonyPostfix]
        private static void Postfix(ref Warrant __result)
        {
            if (!(__result is Warrant_Pawn wp) || wp.Pawn == null)
                return;
            Pawn pawn = wp.Pawn;
            if (pawn.RaceProps.Animal || pawn.Faction == Faction.OfPlayer)
                return;
            bool ok = BountyRules.IsWorldNpcFaction(pawn.Faction)
                && (wp.issuer == null || wp.issuer == Faction.OfPlayer || BountyRules.TechGapOk(wp.issuer, pawn.Faction));
            if (ok)
            {
                BountyRules.OfferCap(out int _, out int maxTier);
                ok = BountyRules.TierOf(pawn.Faction.def.techLevel) <= maxTier;
            }
            if (!ok)
            {
                if (!pawn.Destroyed)
                    pawn.Destroy(DestroyMode.Vanish);
                __result = null;
            }
        }
    }

    // —— 周期生成捕获改由 BountyRadioManager 每 60 tick 轮询(不在 SW 每 tick 热路径上挂 Harmony) ——
    // 这里只保留低频路径:开局/读档的初始补充通缉,封顶 2 条并即时转入待接收(历史积压情报)。
    [HarmonyPatch(typeof(WarrantsManager), "PopulateWarrants")]
    internal static class WarrantsManager_PopulateWarrants_Patch
    {
        [HarmonyPrefix]
        private static void CapAmount(ref int amountToPopulate)
        {
            if (amountToPopulate > 2)
                amountToPopulate = 2;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            BountyRadioManager radio = BountyRadioManager.Get();
            WarrantsManager mgr = WarrantsManager.Instance;
            if (radio == null || mgr == null)
                return;
            int now = Find.TickManager.TicksGame;
            int captured = 0;
            for (int i = mgr.availableWarrants.Count - 1; i >= 0; i--)
            {
                Warrant w = mgr.availableWarrants[i];
                if (w.createdTick == now)
                {
                    mgr.availableWarrants.RemoveAt(i);
                    radio.pending.Add(w);
                    radio.MarkSeen(w);
                    captured++;
                }
            }
            if (captured > 0)
            {
                radio.NoteGeneration(now);
                RadioNotifier.Send("RK_Bounty.RadioIncomingTitle",
                    "RK_Bounty.RadioBacklogText".Translate(radio.pending.Count));
            }
        }
    }

    // —— 呼叫太空商人:通话后概率夹带一条悬赏(受一周一条全局限流,另有 1 天商人冷却) ——
    // TradeShip.TryOpenComms(Pawn) 无重载。
    [HarmonyPatch(typeof(TradeShip), "TryOpenComms")]
    internal static class TradeShip_TryOpenComms_Patch
    {
        private const int CooldownTicks = 60000;
        private const float Chance = 0.25f;

        [HarmonyPostfix]
        private static void Postfix(TradeShip __instance)
        {
            if (!__instance.CanTradeNow)
                return;
            BountyRadioManager radio = BountyRadioManager.Get();
            WarrantsManager mgr = WarrantsManager.Instance;
            if (radio == null || mgr == null)
                return;
            if (radio.GenerationCooledDown())
                return;
            int now = Find.TickManager.TicksGame;
            if (now - radio.lastTraderRelayTick < CooldownTicks)
                return;
            radio.lastTraderRelayTick = now;
            if (!Rand.Chance(Chance))
                return;
            List<Warrant> snapshot = mgr.availableWarrants.ToList();
            mgr.PopulateWarrants(1);
            if (radio.CaptureNewByDiff(snapshot) > 0)
                RadioNotifier.Send("RK_Bounty.TraderRelayTitle",
                    "RK_Bounty.TraderRelayText".Translate(__instance.name ?? "…"));
        }
    }

    // —— 接收入口(十期信鸽改造):通讯台/公告牌右键接收已移除,唯一入口是信鸽柱自身的
    //    Building_RK_BirdPost.GetFloatMenuOptions override(零 Harmony 开销)。 ——
    // —— 悬赏实物奖励(商队交付):GiveReward 的各子类实现(抓捕/夺回)都会先调 base,
    //    基类体只置状态,因此只在基类挂 postfix 即每次交付恰好触发一次。
    //    驯服委托上游 bug(基类不发放任何报酬)在此顺带补发银。
    [HarmonyPatch(typeof(Warrant), "GiveReward")]
    internal static class Warrant_GiveReward_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Warrant __instance, Caravan caravan)
        {
            BountyRewards.FixTameSilver(__instance, caravan);
            BountyRewards.DeliverBonus(__instance, caravan);
            // 暗杀委托交付成功 → 直接给委托公会 +好感（纯 vanilla，不依赖 edgepolitics 双线）；失败无此路径=不扣。
            if (__instance.issuer != null && __instance.issuer != Faction.OfPlayer
                && BountyRules.IsAssassinGuild(__instance.issuer))
                __instance.issuer.TryAffectGoodwillWith(Faction.OfPlayer, 8, true, false, null, null);
        }
    }

    // —— 悬赏实物奖励(运输舱交付):SW 的 Arrived 只按 MaxRewardValue 掉银堆,
    //    postfix 追加同一种子的实物包,掉落点与银一致(玩家主图交易点附近)。
    //    Arrived 在该类仅一份声明;warrant 为私有实例字段。
    [HarmonyPatch(typeof(TransportersArrivalAction_ReturnWarrant), "Arrived")]
    internal static class PodReturnWarrant_Arrived_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(TransportersArrivalAction_ReturnWarrant __instance)
        {
            Warrant w = Traverse.Create(__instance).Field("warrant").GetValue<Warrant>();
            if (w != null)
                BountyRewards.DropBonusNearPlayer(w);
        }
    }

    // —— 六期·奖励重写:赏金 = 角色身价 × 科技惩罚(目标档低于本方×0.5) × 派系关系倍率
    //    (0最低×1.0,+100→×1.5,−100→×1.4)。SW 原形状保留:30% 概率有尸体酬金(活捉价的30~70%),
    //    财富缩放沿用其设置(原 AssignRewards 内部的财富缩放已被覆写,这里按原规则重做)。
    //    AssignRewards 为 private static void(Warrant_Pawn),无重载。
    [HarmonyPatch(typeof(WarrantsManager), "AssignRewards")]
    internal static class WarrantsManager_AssignRewards_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Warrant_Pawn warrant)
        {
            if (warrant == null || warrant.Pawn == null)
                return;
            Pawn pawn = warrant.Pawn;
            Faction targetFac = pawn.Faction;
            bool npcTarget = targetFac != null && targetFac != Faction.OfPlayer;
            float techMult = npcTarget ? BountyRules.AcceptTechMultiplier(targetFac.def.techLevel) : 1f;
            float relMult = npcTarget
                ? BountyRules.RelationMultiplier(Faction.OfPlayer.GoodwillWith(targetFac))
                : 1f;
            int ov = BountyRadioManager.TierOverride(warrant.loadID);
            int tier = ov > 0 ? ov : BountyRules.TierOf(npcTarget ? targetFac.def.techLevel : TechLevel.Medieval);
            float tierMult = BountyRules.TierRewardMult(tier);
            int living = Mathf.Max(0, Mathf.RoundToInt(pawn.MarketValue * techMult * relMult * tierMult));
            warrant.rewardForLiving = living;
            warrant.rewardForDead = Rand.Chance(0.3f) ? Mathf.RoundToInt(living * Rand.Range(0.3f, 0.7f)) : 0;
            if (!BountyRules.RewardScalingEnabled())
                return;
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
                return;
            int add = (int)(map.wealthWatcher.WealthTotal * 0.025f);
            if (add <= 0)
                return;
            if (warrant.rewardForLiving > 0)
                warrant.rewardForLiving += add;
            if (warrant.rewardForDead > 0)
                warrant.rewardForDead += add;
        }
    }

    // —— 六期·发出端:玩家自建悬赏(含确认弹窗后的回调路径)统一过 TryAddWarrant。
    //    校验/预付全部收敛到 BountyRules.TryChargePrepay(2026-09-02 通缉扩展:
    //    跨档不再硬拒,费用按每超 1 档 ×2 累进,封顶 ×8;与发单窗共用同一入口)。
    //    ⚠ TryAddWarrant(Warrant, string) 是 MainTabWindow_Warrants 的 private 实例方法
    //    (悬赏主窗「发布通缉」按钮路径),不在 WarrantsManager 上——挂错类会炸掉整个 PatchAll。
    [HarmonyPatch(typeof(MainTabWindow_Warrants), "TryAddWarrant")]
    internal static class MainTabWindow_TryAddWarrant_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Warrant warrant, string failReason)
        {
            if (warrant == null || !failReason.NullOrEmpty())
                return true;
            if (!BountyRules.TryChargePrepay(warrant, out TaggedString failMsg, out bool _))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(failMsg));
                return false;
            }
            return true;
        }
    }

    // —— 六期·受雇成功率:玩家发布的悬赏由NPC派系执行,到期掷 SuccessChance 定成败;
    //    SW 原口径(赏金/身价)在新奖励公式下≈100%,改为 66%(原始)→33%(极致)随目标科技线性。
    //    AcceptChance(NPC 接单意愿/接单速度)保留赏金口径——出价越高越快有人接。
    [HarmonyPatch(typeof(Warrant_Pawn), nameof(Warrant_Pawn.SuccessChance))]
    internal static class Warrant_Pawn_SuccessChance_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Warrant_Pawn __instance, ref float __result)
        {
            Faction fac = __instance.Pawn != null ? __instance.Pawn.Faction : null;
            if (fac == null || fac == Faction.OfPlayer)
                return;
            __result = BountyRules.WarrantSuccessChance(fac.def.techLevel);
        }
    }

    // —— 六期·发单激怒:替换 SW 对非敌对目标派系的固定 -80 好感,改为按职位(领袖/王室头衔)
    //    与身价分级的激怒值(职位越高、身价越高,惩罚越重)。OnCreate 为 public virtual 单一覆写。
    //    十期信鸽:激怒不再即时生效,寄出信鸽(按世界距离 0.5~2 天航程)到站才扣好感并发信。
    [HarmonyPatch(typeof(Warrant_Pawn), nameof(Warrant_Pawn.OnCreate))]
    internal static class Warrant_Pawn_OnCreate_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Warrant_Pawn __instance)
        {
            Pawn pawn = __instance.Pawn;
            Faction fac = pawn != null ? pawn.Faction : null;
            if (fac == null || fac == Faction.OfPlayer || FactionUtility.HostileTo(fac, Faction.OfPlayer))
                return false;
            int hit = BountyRules.AngerOnIssue(pawn, fac);
            BountyRadioManager.Get()?.QueueOutgoingAnger(fac, hit);
            return false;
        }
    }

    // —— 刺客公会双阶接单(2026-09-03 双阶整合实装):玩家发布的悬赏一律由两个隐藏刺客公会执行——
    //    目标中世纪及以下档→魅狐影刃会(Kurin_Faction_Hostile),更高档/无派系目标→美狐机枢会
    //    (Miho_Faction_Supremacist)。SW 原版 GetValidWarrantIssuers 过滤 Hidden 派系,两公会 09-02
    //    隐藏化后永远接不到单(悬赏仍被普通派系接走);这里整体接管 HandleCreatedWarrants 的派系选择,
    //    AcceptChance 接单节奏/AcceptBy/入册/完成期限/播报复刻原流程;两公会都不在场(未装 mod/全灭)
    //    时放行 SW 原版随机派系逻辑兜底。
    [HarmonyPatch(typeof(WarrantsManager), nameof(WarrantsManager.HandleCreatedWarrants))]
    internal static class WarrantsManager_HandleCreatedWarrants_AssassinGuild_Patch
    {
        private const string MedievalGuildDefName = "Kurin_Faction_Hostile";
        private const string SpacerGuildDefName = "Miho_Faction_Supremacist";

        private static Faction Guild(string defName)
        {
            return Find.FactionManager.AllFactions.FirstOrDefault(f =>
                f != null && f.def != null && f.def.defName == defName && !f.defeated && !f.IsPlayer
                && !FactionUtility.HostileTo(f, Faction.OfPlayer));
        }

        private static Faction Pick(Warrant w, Faction medieval, Faction spacer)
        {
            bool low = false;
            if (w is Warrant_Pawn wp && wp.Pawn != null && wp.Pawn.Faction != null
                && wp.Pawn.Faction != Faction.OfPlayer)
                low = BountyRules.TierOf(wp.Pawn.Faction.def.techLevel) <= BountyRules.Tier1;
            Faction pick = low ? medieval : spacer;
            return pick != null ? pick : (low ? spacer : medieval);
        }

        [HarmonyPrefix]
        private static bool Prefix(WarrantsManager __instance)
        {
            if (__instance.createdWarrants.Count == 0)
                return false;
            Faction medieval = Guild(MedievalGuildDefName);
            Faction spacer = Guild(SpacerGuildDefName);
            if (medieval == null && spacer == null)
                return true;
            int now = Find.TickManager.TicksGame;
            for (int i = __instance.createdWarrants.Count - 1; i >= 0; i--)
            {
                Warrant w = __instance.createdWarrants[i];
                if (!Rand.Chance(w.AcceptChance() / 420000f))
                    continue;
                Faction guild = Pick(w, medieval, spacer);
                if (guild == null)
                    continue;
                w.AcceptBy(guild);
                __instance.createdWarrants.RemoveAt(i);
                __instance.takenWarrants.Add(w);
                w.tickToBeCompleted = now + 60000 * (int)Rand.Range(3f, 15f);
                Messages.Message("SW.FactionTookYourWarrant".Translate(
                    guild.Named("FACTION"), w.thing.LabelCap), MessageTypeDefOf.PositiveEvent, true);
            }
            return false;
        }
    }

    // —— 失败不扣好感(刺客结算规则):SW 玩家发单失败时对执行方扣 failedPlayerWarrantRelationshipDamage
    //    (默认30),与"暗杀成功→加好感、失败→不扣好感"定案冲突;执行方现恒为刺客公会,该惩罚一律归零。
    //    不影响玩家接 AI 单失败时的 issuer 怒气(failedAIWarrantRelationshipDamage)。
    [HarmonyPatch(typeof(WarrantsManager), "HandleFactionsTakenWarrants")]
    internal static class WarrantsManager_HandleFactionsTakenWarrants_NoFailPenalty_Patch
    {
        private static bool probed;
        private static object settings;
        private static System.Reflection.FieldInfo fiPenalty;

        [HarmonyPrefix]
        private static void Prefix()
        {
            if (!probed)
            {
                probed = true;
                System.Type modT = AccessTools.TypeByName("SimpleWarrants.SimpleWarrantsMod");
                settings = modT != null ? AccessTools.Property(modT, "Settings")?.GetValue(null) : null;
                fiPenalty = settings != null
                    ? AccessTools.Field(settings.GetType(), "failedPlayerWarrantRelationshipDamage") : null;
            }
            if (fiPenalty != null && (int)fiPenalty.GetValue(settings) != 0)
                fiPenalty.SetValue(settings, 0);
        }
    }

    // —— 敌对事件档案(2026-09-02 扩展):四类敌对事件成功触发时记入 RaidArchiveComp
    //    (袭击/猛兽袭人/单只狂暴/绑架赎金),均只记 Map 目标(殖民地/基地),行商队遇袭不进档案。
    //    TryExecuteWorker 为各 worker 唯一声明,无重载歧义;__result 门控仿现有写法。
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    internal static class IncidentWorker_RaidEnemy_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(bool __result, IncidentParms parms)
        {
            if (!__result || parms == null || parms.target is not Map)
                return;
            RaidArchiveComp comp = RaidArchiveComp.Get();
            if (comp != null)
                comp.NotifyHostileEvent(parms, RaidEventKind.Raid);
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_AggressiveAnimals), "TryExecuteWorker")]
    internal static class IncidentWorker_AggressiveAnimals_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(bool __result, IncidentParms parms)
        {
            if (!__result || parms == null || parms.target is not Map)
                return;
            RaidArchiveComp comp = RaidArchiveComp.Get();
            if (comp != null)
                comp.NotifyHostileEvent(parms, RaidEventKind.Manhunter);
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_AnimalInsanitySingle), "TryExecuteWorker")]
    internal static class IncidentWorker_AnimalInsanitySingle_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(bool __result, IncidentParms parms)
        {
            if (!__result || parms == null || parms.target is not Map)
                return;
            RaidArchiveComp comp = RaidArchiveComp.Get();
            if (comp != null)
                comp.NotifyHostileEvent(parms, RaidEventKind.Insanity);
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_RansomDemand), "TryExecuteWorker")]
    internal static class IncidentWorker_RansomDemand_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(bool __result, IncidentParms parms)
        {
            if (!__result || parms == null || parms.target is not Map)
                return;
            RaidArchiveComp comp = RaidArchiveComp.Get();
            if (comp != null)
                comp.NotifyHostileEvent(parms, RaidEventKind.Ransom);
        }
    }

}


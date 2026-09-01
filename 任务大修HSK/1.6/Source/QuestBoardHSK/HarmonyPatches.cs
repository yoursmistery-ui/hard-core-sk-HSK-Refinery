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

        public static JobDef RK_Bounty_ReceiveRadio
        {
            get
            {
                if (receiveRadio == null)
                    receiveRadio = DefDatabase<JobDef>.GetNamed("RK_Bounty_ReceiveRadio");
                return receiveRadio;
            }
        }
    }

    internal static class RadioNotifier
    {
        internal static void Send(string titleKey, TaggedString text)
        {
            Find.LetterStack.ReceiveLetter(
                "RK_Bounty.LetterTag".Translate() + ": " + titleKey.Translate(),
                text,
                LetterDefOf.NeutralEvent);
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
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio != null && radio.GenerationCooledDown())
            {
                __result = null;
                return false;
            }
            return true;
        }

        // 六期科技门槛:目标必须来自世界NPC派系(可见/未灭/非玩家/人类系),
        // 且目标派系与发布方科技档相差一级以内(封死"原始时代悬赏极致时代天网")。
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

    // —— 通讯台右键菜单:前置「接收悬赏通讯」——
    // Building_CommsConsole.GetFloatMenuOptions(Pawn) 单声明,无歧义。
    [HarmonyPatch(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetFloatMenuOptions))]
    internal static class CommsConsole_GetFloatMenuOptions_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Building_CommsConsole __instance, Pawn myPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio == null || radio.pending.Count == 0 || myPawn == null || !myPawn.RaceProps.Humanlike)
                return;
            __result = ReceiveOption.PrependReceiveOption(__instance, myPawn, radio, __result);
        }
    }

    // —— 悬赏公告牌也能接收(早期没有通讯台时的入口):Thing.GetFloatMenuOptions 是虚方法,
    //    公告牌未重写,基础实现被调用时 postfix 生效;非公告牌目标立即返回,开销可忽略。
    [HarmonyPatch(typeof(Thing), nameof(Thing.GetFloatMenuOptions))]
    internal static class Thing_GetFloatMenuOptions_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Thing __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
        {
            if (!(__instance is Building bld) || !bld.HasComp<CompBountyBoard>())
                return;
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio == null || radio.pending.Count == 0 || selPawn == null || !selPawn.RaceProps.Humanlike)
                return;
            __result = ReceiveOption.PrependReceiveOption(__instance, selPawn, radio, __result);
        }
    }

    internal static class ReceiveOption
    {
        internal static IEnumerable<FloatMenuOption> PrependReceiveOption(Thing target, Pawn pawn, BountyRadioManager radio, IEnumerable<FloatMenuOption> rest)
        {
            var opts = new List<FloatMenuOption>
            {
                new FloatMenuOption(
                    "RK_Bounty.ReceiveRadio".Translate(radio.pending.Count),
                    delegate
                    {
                        Job job = JobMaker.MakeJob(QB_JobDefOf.RK_Bounty_ReceiveRadio, target);
                        pawn.jobs.TryTakeOrderedJob(job);
                    },
                    MenuOptionPriority.High)
            };
            if (rest != null)
                opts.AddRange(rest);
            return opts;
        }
    }

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
            int living = Mathf.Max(0, Mathf.RoundToInt(pawn.MarketValue * techMult * relMult));
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
    //    ①目标档 > 我方档+1 → 拦截;②预付手续费按目标档 500~5000,越级(目标>我方档)×2;
    //    ③余额不足 → 拦截。预付为发布手续费,发出即扣除(交付酬劳仍由 SW 在完成时收取)。
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
            if (!(warrant is Warrant_Pawn wp) || wp.Pawn == null || wp.Pawn.RaceProps.Animal)
                return true;
            Faction targetFac = wp.Pawn.Faction;
            if (targetFac == null || targetFac == Faction.OfPlayer)
                return true;
            TechLevel targetTech = targetFac.def.techLevel;
            TechLevel myTech = BountyRules.PlayerTechLevel();
            if (targetTech > myTech + BountyRules.MaxTechGap)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "RK_Bounty.IssueTooHighTech".Translate(targetFac.Name, BountyRules.TechLevelLabel(targetTech))));
                return false;
            }
            bool overTech = targetTech > myTech;
            int prepay = BountyRules.PrepayForTech(targetTech);
            if (overTech)
                prepay = Mathf.RoundToInt(prepay * BountyRules.AboveTechCostFactor);
            List<Thing> silvers = Utils.AllPlayerSilver();
            int have = silvers.Sum(t => t.stackCount);
            if (have < prepay)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "RK_Bounty.NeedPrepay".Translate(prepay, have)));
                return false;
            }
            warrant.Pay(silvers, prepay);
            Messages.Message(overTech
                ? "RK_Bounty.PrepayPaidOver".Translate(prepay)
                : "RK_Bounty.PrepayPaid".Translate(prepay),
                MessageTypeDefOf.NeutralEvent, false);
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
            fac.TryAffectGoodwillWith(Faction.OfPlayer, -hit, true, true, null, null);
            return false;
        }
    }

}

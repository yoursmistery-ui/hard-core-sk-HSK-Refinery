using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 七期(2026-08-31):悬赏成败事件化+故事化。
    /// 发单方(玩家雇佣 NPC 猎手):
    ///   成功 → MakeWarrantDialog prefix-false,重写为故事版付款弹窗(支付/拒付/宽限语义照搬 SW);
    ///   失败 → HandleFactionsTakenWarrants 前置快照+后置 diff,补一封故事信(SW 原有的
    ///   弃单回池/好感结算原样保留,不替换整个方法)。
    /// 接单方(玩家交货/过期)的故事信在 QuestScriptDef 里改 TKey + 成功分支加 QuestNode_Signal。
    /// </summary>
    internal static class BountyTales
    {
        // —— 发单成功:故事版付款弹窗(替换 SW.MakeWarrantDialog) ——

        internal static void ShowIssueSuccessDialog(Warrant warrant)
        {
            int reward = 0;
            bool dead = false;
            if (warrant is Warrant_Pawn wp)
            {
                float aliveWeight = Mathf.Clamp01(
                    (float)wp.rewardForLiving / (float)(wp.rewardForLiving + wp.rewardForDead));
                if (wp.rewardForDead > 0 && !Rand.Chance(aliveWeight))
                    dead = true;
                reward = dead ? wp.rewardForDead : wp.rewardForLiving;
            }
            else if (warrant is Warrant_Artifact wa)
            {
                reward = wa.reward;
            }

            Map map = Find.AnyPlayerHomeMap;
            List<Thing> silvers = Utils.AllPlayerSilver();
            Faction hunter = warrant.accepteer;

            TaggedString title = "RK_Bounty.Tale_IssueSuccess_Title".Translate(
                warrant.thing.LabelCap.Named("THING"));
            DiaNode node = new DiaNode(IssueSuccessStory(warrant, dead, reward));

            DiaOption pay = new DiaOption("RK_Bounty.DialogPay".Translate(reward.ToString()));
            pay.action = delegate
            {
                for (; reward > 0;)
                {
                    Thing silver = silvers.RandomElement();
                    silvers.Remove(silver);
                    if (silver == null)
                        break;
                    int take = Mathf.Min(reward, silver.stackCount);
                    silver.SplitOff(take).Destroy();
                    reward -= take;
                }
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(SW_DefOf.FactionArrival, map);
                parms.faction = hunter;
                Thing deliver = warrant.thing;
                if (dead)
                {
                    Pawn p = (Pawn)warrant.thing;
                    p.Kill(null, null);
                    deliver = p.Corpse;
                }
                else
                {
                    Pawn p = warrant.thing as Pawn;
                    if (p != null)
                    {
                        HealthUtility.DamageLegsUntilIncapableOfMoving(p, false);
                        HealthUtility.TryAnesthetize(p);
                    }
                }
                if (Utils.PlayerHomeIsOrbital())
                {
                    Map home = Find.AnyPlayerHomeMap;
                    DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(home), home,
                        new List<Thing> { deliver }, 110, false, false, true, true, true, null);
                    Messages.Message("SW.WarrantDeliveredByPods".Translate(),
                        MessageTypeDefOf.PositiveEvent, false);
                }
                else
                {
                    ((IncidentWorker_Visitors)SW_DefOf.SW_Visitors.Worker).SpawnVisitors(deliver, parms);
                }
            };
            pay.resolveTree = true;
            if (silvers.Sum(t => t.stackCount) < reward)
                pay.Disable("RK_Bounty.DialogNotEnoughSilver".Translate());
            node.options.Add(pay);

            DiaOption refuse = new DiaOption("RK_Bounty.DialogRefuse".Translate());
            refuse.action = delegate
            {
                hunter.TryAffectGoodwillWith(Faction.OfPlayer, -100, true, true, null, null);
                IncidentParms raid = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                raid.faction = hunter;
                IncidentDefOf.RaidEnemy.Worker.TryExecute(raid);
            };
            refuse.resolveTree = true;
            node.options.Add(refuse);

            if (!warrant.paymentPostponed)
            {
                DiaOption delay = new DiaOption("RK_Bounty.DialogDelay".Translate());
                delay.action = delegate
                {
                    WarrantsManager.Instance.postponedWarrants.Add(warrant);
                    warrant.postponedUntilTicks = Find.TickManager.TicksGame + 60000;
                    warrant.paymentPostponed = true;
                };
                delay.resolveTree = true;
                node.options.Add(delay);
            }

            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(node, hunter, true, false, title));
            Find.Archive.Add(new ArchivedDialog(node.text, title, hunter));
        }

        private static TaggedString IssueSuccessStory(Warrant warrant, bool dead, int reward)
        {
            var wp = warrant as Warrant_Pawn;
            Faction targetFac = wp != null && wp.Pawn != null && !wp.Pawn.RaceProps.Animal
                ? wp.Pawn.Faction : null;
            TaggedString story = ("RK_Bounty.Tale_IssueSuccess_" + Rand.RangeInclusive(0, 4)).Translate(
                warrant.thing.LabelCap.Named("THING"),
                warrant.accepteer.Name.Named("FACTION"),
                (targetFac != null ? targetFac.Name
                    : "RK_Bounty.PosterHostile".Translate().RawText).Named("TARGETFAC"),
                (wp != null && !wp.reason.NullOrEmpty() ? wp.reason : "—").Named("CRIME"),
                reward.ToString().Named("REWARD"));
            TaggedString outcome = (dead ? "RK_Bounty.Tale_IssueOutDead" : "RK_Bounty.Tale_IssueOutLiving")
                .Translate(warrant.thing.LabelCap.Named("THING"), reward.ToString().Named("REWARD"));
            return story + "\n\n" + outcome;
        }

        // —— 发单失败:SW 只发一条短 toast,这里补一封故事信 ——

        internal static void SendIssueFailLetter(Warrant warrant)
        {
            if (warrant == null || warrant.thing == null || warrant.accepteer == null)
                return;
            var wp = warrant as Warrant_Pawn;
            Faction targetFac = wp != null && wp.Pawn != null && !wp.Pawn.RaceProps.Animal
                ? wp.Pawn.Faction : null;
            TaggedString title = "RK_Bounty.Tale_IssueFail_Title".Translate(
                warrant.thing.LabelCap.Named("THING"));
            TaggedString text = ("RK_Bounty.Tale_IssueFail_" + Rand.RangeInclusive(0, 4)).Translate(
                warrant.thing.LabelCap.Named("THING"),
                warrant.accepteer.Name.Named("FACTION"),
                (targetFac != null ? targetFac.Name
                    : "RK_Bounty.PosterHostile".Translate().RawText).Named("TARGETFAC"),
                (wp != null && !wp.reason.NullOrEmpty() ? wp.reason : "—").Named("CRIME"));
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent,
                warrant.thing, warrant.accepteer);
        }

        // —— 八期:执行进度故事信(接单/追踪/接火) ——

        internal static void SendAcceptLetter(Warrant warrant)
        {
            if (warrant == null || warrant.thing == null || warrant.accepteer == null)
                return;
            TaggedString title = "RK_Bounty.Tale_Accept_Title".Translate(ProgressArgs(warrant));
            TaggedString text = ("RK_Bounty.Tale_Accept_" + Rand.RangeInclusive(0, 1))
                .Translate(ProgressArgs(warrant));
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent,
                warrant.thing, warrant.accepteer);
        }

        /// <param name="stage">1=追踪,2=接火。</param>
        internal static void SendProgressLetter(Warrant warrant, int stage)
        {
            if (warrant == null || warrant.thing == null || warrant.accepteer == null)
                return;
            string kind = stage >= 2 ? "Clash" : "Track";
            TaggedString title = ("RK_Bounty.Tale_" + kind + "_Title").Translate(ProgressArgs(warrant));
            TaggedString text = ("RK_Bounty.Tale_" + kind + "_" + Rand.RangeInclusive(0, 1))
                .Translate(ProgressArgs(warrant));
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NeutralEvent,
                warrant.thing, warrant.accepteer);
        }

        private static NamedArgument[] ProgressArgs(Warrant warrant)
        {
            var wp = warrant as Warrant_Pawn;
            Faction targetFac = wp != null && wp.Pawn != null && !wp.Pawn.RaceProps.Animal
                ? wp.Pawn.Faction : null;
            return new NamedArgument[]
            {
                warrant.thing.LabelCap.Named("THING"),
                warrant.accepteer.Name.Named("FACTION"),
                (targetFac != null ? targetFac.Name
                    : "RK_Bounty.PosterHostile".Translate().RawText).Named("TARGETFAC"),
            };
        }
    }

    // —— 发单成功弹窗替换:记录成功标记,供失败 diff 排除 ——

    [HarmonyPatch(typeof(WarrantsManager), "MakeWarrantDialog")]
    internal static class WarrantsManager_MakeWarrantDialog_StoryPatch
    {
        internal static readonly List<Warrant> dialogedThisTick = new List<Warrant>();

        [HarmonyPrefix]
        private static bool Prefix(Warrant warrant)
        {
            if (warrant == null)
                return false;
            if (warrant.accepteer == null)
                return true;
            dialogedThisTick.Add(warrant);
            BountyTales.ShowIssueSuccessDialog(warrant);
            return false;
        }
    }

    // —— 发单失败检测:快照+diff,不替换 SW 方法(弃单/延期/好感结算原样保留) ——

    [HarmonyPatch(typeof(WarrantsManager), "HandleFactionsTakenWarrants")]
    internal static class WarrantsManager_TakenWarrants_FailStoryPatch
    {
        private static readonly List<Warrant> before = new List<Warrant>();

        [HarmonyPrefix]
        private static void Prefix(WarrantsManager __instance)
        {
            before.Clear();
            for (int i = 0; i < __instance.takenWarrants.Count; i++)
                before.Add(__instance.takenWarrants[i]);
        }

        [HarmonyPostfix]
        private static void Postfix(WarrantsManager __instance)
        {
            try
            {
                for (int i = 0; i < before.Count; i++)
                {
                    Warrant w = before[i];
                    if (__instance.takenWarrants.Contains(w))
                        continue;
                    if (__instance.createdWarrants.Contains(w))
                        continue;
                    if (__instance.postponedWarrants.Contains(w))
                        continue;
                    if (WarrantsManager_MakeWarrantDialog_StoryPatch.dialogedThisTick.Contains(w))
                        continue;
                    BountyTales.SendIssueFailLetter(w);
                }
            }
            finally
            {
                WarrantsManager_MakeWarrantDialog_StoryPatch.dialogedThisTick.Clear();
                before.Clear();
            }
        }
    }
}

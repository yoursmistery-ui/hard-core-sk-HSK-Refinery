using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 刺客公会送人上门(2026-09-03 双阶整合):玩家发布的悬赏由公会执行成功后,取代 SW 固定
    /// "付钱→访问队送人"弹窗,改为阶梯交付(货到付款):
    ///   徒步押送(原价,0.5~2 天航程,目标放队伍旁) / 商队押送(×1.1,同航程,目标直接送进家区) /
    ///   飞船空投(×1.25,~1 小时空投到交易点;仅美狐机枢会+Royalty)。
    /// 拒收=好感-10(服务公会,不袭扰);延后24小时沿用 SW postponed 机制。
    /// </summary>
    internal static class GuildDelivery
    {
        public const float MultWalk = 1.00f;
        public const float MultCaravan = 1.10f;
        public const float MultShip = 1.25f;

        private const int ShipDelayTicks = 2500;      // 飞船空投 ~1 小时
        private const int RefuseGoodwillHit = 10;
        private const int NoSilverGoodwillHit = 5;

        public static bool Available(BountyPickupMethod method, Faction guild)
        {
            switch (method)
            {
                case BountyPickupMethod.Walk:
                case BountyPickupMethod.Caravan:
                    return true;
                case BountyPickupMethod.Ship:
                    // 派系级 techLevel 两公会都是 Spacer(种族决定,不可改),飞船档按"太空公会"语义单给机枢会
                    return ModsConfig.RoyaltyActive && guild != null && guild.def != null
                        && guild.def.defName == "Miho_Faction_Supremacist";
            }
            return false;
        }

        public static float PriceMult(BountyPickupMethod method)
        {
            switch (method)
            {
                case BountyPickupMethod.Caravan: return MultCaravan;
                case BountyPickupMethod.Ship: return MultShip;
                default: return MultWalk;
            }
        }

        public static void ShowDialog(Warrant warrant)
        {
            Faction guild = warrant.accepteer;

            // 酬金/生死判定复刻 SW MakeWarrantDialog
            bool dead = false;
            int baseReward;
            if (warrant is Warrant_Pawn wp && wp.Pawn != null)
            {
                dead = wp.Pawn.Dead;
                if (!dead && wp.rewardForDead > 0 && wp.rewardForLiving + wp.rewardForDead > 0)
                {
                    float aliveFrac = Mathf.Clamp01((float)wp.rewardForLiving / (wp.rewardForLiving + wp.rewardForDead));
                    dead = !Rand.Chance(aliveFrac);
                }
                baseReward = dead ? wp.rewardForDead : wp.rewardForLiving;
            }
            else if (warrant is Warrant_Artifact wa)
                baseReward = wa.reward;
            else
                baseReward = 0;

            string targetLabel = warrant.thing != null ? warrant.thing.LabelCap : "…";
            TaggedString title = "RK_Bounty.DeliverTitle".Translate();
            DiaNode node = new DiaNode("RK_Bounty.DeliverText".Translate(
                guild.Name, targetLabel,
                (dead ? "RK_Bounty.DeliverStateDead" : "RK_Bounty.DeliverStateAlive").Translate()));

            foreach (BountyPickupMethod m in System.Enum.GetValues(typeof(BountyPickupMethod)))
            {
                if (!Available(m, guild))
                    continue;
                BountyPickupMethod mm = m;
                int price = Mathf.Max(0, Mathf.RoundToInt(baseReward * PriceMult(mm)));
                DiaOption opt = new DiaOption("RK_Bounty.DeliverOpt".Translate(
                    ("RK_Bounty.PickupM_" + mm).Translate(), price));
                int have = Utils.AllPlayerSilver().Sum(t => t.stackCount);
                if (have < price)
                    opt.Disable("RK_Bounty.DialogNotEnoughSilver".Translate());
                opt.action = delegate
                {
                    BountyRadioManager radio = BountyRadioManager.Get();
                    if (radio == null)
                        return;
                    int now = Find.TickManager.TicksGame;
                    var item = new GuildDeliveryItem
                    {
                        warrant = warrant,
                        guild = guild,
                        method = mm,
                        price = price,
                        dead = dead,
                        arriveAtTick = now + (mm == BountyPickupMethod.Ship
                            ? ShipDelayTicks
                            : radio.MailRouteTicksFrom(guild))
                    };
                    radio.guildDeliveries.Add(item);
                    RadioNotifier.Send("RK_Bounty.DeliverOrderedTitle",
                        "RK_Bounty.DeliverOrderedText".Translate(guild.Name,
                            ("RK_Bounty.PickupM_" + mm).Translate(),
                            GenDate.TicksToDays(item.arriveAtTick - now).ToString("0.0"),
                            targetLabel, price));
                };
                node.options.Add(opt);
            }

            DiaOption refuse = new DiaOption("RK_Bounty.DeliverRefuse".Translate());
            refuse.action = delegate
            {
                guild.TryAffectGoodwillWith(Faction.OfPlayer, -RefuseGoodwillHit, true, true, null, null);
                Messages.Message("RK_Bounty.DeliverRefused".Translate(guild.Name, RefuseGoodwillHit),
                    MessageTypeDefOf.NeutralEvent, false);
            };
            node.options.Add(refuse);

            if (!warrant.paymentPostponed)
            {
                DiaOption later = new DiaOption("SW.Delay24Hours".Translate());
                later.action = delegate
                {
                    WarrantsManager.Instance?.postponedWarrants.Add(warrant);
                    warrant.postponedUntilTicks = Find.TickManager.TicksGame + 60000;
                    warrant.paymentPostponed = true;
                };
                node.options.Add(later);
            }

            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(node, guild, true, false, title));
            Find.Archive.Add(new ArchivedDialog(node.text, title, guild));
        }

        // ===================== 每帧扫描 =====================

        public static void TickAll(int now)
        {
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio == null || radio.guildDeliveries.Count == 0)
                return;
            for (int i = radio.guildDeliveries.Count - 1; i >= 0; i--)
            {
                GuildDeliveryItem item = radio.guildDeliveries[i];
                if (item?.warrant == null || item.guild == null)
                {
                    radio.guildDeliveries.RemoveAt(i);
                    continue;
                }
                if (now < item.arriveAtTick)
                    continue;
                radio.guildDeliveries.RemoveAt(i);
                try
                {
                    Execute(item);
                }
                catch (System.Exception e)
                {
                    Log.Error("[QuestBoardHSK] 公会送人上门交付异常: " + e);
                }
            }
        }

        private static void Execute(GuildDeliveryItem item)
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
                return;
            Thing deliver = ResolveTarget(item);
            if (deliver == null)
            {
                Messages.Message("RK_Bounty.DeliverGone".Translate(
                    item.warrant.thing?.LabelCap ?? "…", item.guild.Name),
                    MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            List<Thing> silvers = Utils.AllPlayerSilver();
            if (item.price > 0 && silvers.Sum(t => t.stackCount) < item.price)
            {
                item.guild.TryAffectGoodwillWith(Faction.OfPlayer, -NoSilverGoodwillHit, true, true, null, null);
                Messages.Message("RK_Bounty.DeliverNoSilver".Translate(item.price, item.guild.Name, NoSilverGoodwillHit),
                    MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            if (item.price > 0)
                item.warrant.Pay(silvers, item.price);
            item.warrant.status = WarrantStatus.Completed;

            if (item.method == BountyPickupMethod.Ship)
            {
                DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map,
                    new List<Thing> { deliver }, 110, false, false, true, true, true, null);
                Messages.Message("RK_Bounty.DeliverDropped".Translate(deliver.LabelCap, item.price),
                    MessageTypeDefOf.PositiveEvent, false);
                return;
            }

            bool caravan = item.method == BountyPickupMethod.Caravan;
            IntVec3 edge = CellFinder.RandomEdgeCell(map);
            List<Pawn> party = SpawnEscort(item.guild, map, edge, caravan);
            if (caravan)
            {
                // 商队押送:目标直接送进家区
                IntVec3 home = RandomHomeCell(map);
                GenSpawn.Spawn(deliver, CellFinder.RandomClosewalkCellNear(home, map, 3),
                    map, Rot4.Random, WipeMode.Vanish, false);
            }
            else
            {
                IntVec3 at = party.Count > 0 ? party[0].Position : edge;
                GenSpawn.Spawn(deliver, CellFinder.RandomClosewalkCellNear(at, map, 2),
                    map, Rot4.Random, WipeMode.Vanish, false);
            }
            RadioNotifier.Send("RK_Bounty.DeliverArrivedTitle",
                "RK_Bounty.DeliverArrivedText".Translate(item.guild.Name, deliver.LabelCap, item.price));
        }

        /// <summary>目标终态:活体→(订单判死则击杀)返回交付物;尸体→corpse;遗物→原物;不可回收→null。</summary>
        private static Thing ResolveTarget(GuildDeliveryItem item)
        {
            Pawn p = item.warrant.thing as Pawn;
            if (p == null)
                return item.warrant.thing != null && !item.warrant.thing.Destroyed ? item.warrant.thing : null;
            if (p.Dead)
                return p.Corpse != null && !p.Corpse.Destroyed ? p.Corpse : null;
            if (item.dead)
            {
                p.Kill(null, null);
                return p.Corpse != null && !p.Corpse.Destroyed ? p.Corpse : null;
            }
            if (p.Destroyed)
                return null;
            if (!p.Downed)
                HealthUtility.TryAnesthetize(p);
            return p;
        }

        private static List<Pawn> SpawnEscort(Faction guild, Map map, IntVec3 edge, bool caravan)
        {
            var party = new List<Pawn>();
            PawnKindDef kind = guild.def.basicMemberKind;
            int guards = 3 + Rand.RangeInclusive(0, 2);
            for (int i = 0; i < guards && kind != null; i++)
            {
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind, guild, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0f,
                    allowAddictions: false, worldPawnFactionDoesntMatter: true));
                party.Add(p);
            }
            if (caravan)
            {
                List<PawnKindDef> packs = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .Where(k => k.RaceProps.packAnimal && !k.RaceProps.Humanlike).ToList();
                int animals = 1 + Rand.RangeInclusive(0, 1);
                for (int i = 0; i < animals && packs.Count > 0; i++)
                {
                    Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                        packs.RandomElement(), guild, PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                        colonistRelationChanceFactor: 0f, worldPawnFactionDoesntMatter: true));
                    party.Add(p);
                }
            }
            for (int i = 0; i < party.Count; i++)
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(edge, map, 5);
                GenSpawn.Spawn(party[i], cell, map, Rot4.Random, WipeMode.Vanish, false);
            }
            List<Pawn> humans = party.Where(p => !p.RaceProps.Animal).ToList();
            if (humans.Count > 0)
                LordMaker.MakeNewLord(guild, new LordJob_VisitColony(guild,
                    DropCellFinder.TradeDropSpot(map)), map, humans);
            return party;
        }

        private static IntVec3 RandomHomeCell(Map map)
        {
            IntVec3 cell = map.areaManager.Home.ActiveCells
                .Where(c => c.Walkable(map))
                .RandomElementWithFallback(IntVec3.Invalid);
            return cell.IsValid ? cell : DropCellFinder.TradeDropSpot(map);
        }
    }

    /// <summary>送人上门订单条目(warrant Deep / guild 引用)。</summary>
    public class GuildDeliveryItem : IExposable
    {
        public Warrant warrant;
        public Faction guild;
        public BountyPickupMethod method = BountyPickupMethod.Walk;
        public int price;
        public bool dead;
        public int arriveAtTick;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref warrant, "warrant");
            Scribe_References.Look(ref guild, "guild");
            Scribe_Values.Look(ref method, "method", BountyPickupMethod.Walk);
            Scribe_Values.Look(ref price, "price", 0);
            Scribe_Values.Look(ref dead, "dead", false);
            Scribe_Values.Look(ref arriveAtTick, "arriveAtTick", 0);
        }
    }

    // —— 送人上门阶梯化:公会接单成功后接管 SW 交付弹窗;非公会(兜底普通派系)走原版 ——
    //    MakeWarrantDialog 为 private 无重载方法。
    [HarmonyPatch(typeof(WarrantsManager), "MakeWarrantDialog")]
    internal static class WarrantsManager_MakeWarrantDialog_GuildDelivery_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(Warrant warrant)
        {
            if (warrant == null || warrant.accepteer == null
                || !BountyRules.IsAssassinGuild(warrant.accepteer))
                return true;
            GuildDelivery.ShowDialog(warrant);
            return false;
        }
    }
}

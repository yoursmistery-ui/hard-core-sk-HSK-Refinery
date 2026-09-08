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
    /// 十期·方向B:玩家接单后的交货方式扩展。
    /// 原 SW 只能"玩家自己开运输舱送回发单据点"(全额);本类新增"叫对方派队来取"(打折):
    ///   徒步队伍(全部派系,−20%) / 商队带驮兽(中世纪+,−15%) / 飞船(太空档+且 Royalty,−10%)。
    /// 流程:通缉中心海报点「叫他们来取」→ 0.5~2 天航程 →
    ///   徒步/商队:抵达地图边缘弹 ABE 式三选项(放行=进地图交割/打发走=整单作废+扣好感/稍后=6h 重来);
    ///   飞船:原版任务飞船(TransportShip)降落,玩家装载目标后发射,TryStart prefix 检出货即付款。
    /// 目标已死:收尸体按死体酬金。交割完成镜像 SW Pod 路径:status=Completed+移出列表+实物加成掉落。
    /// </summary>
    internal static class BountyPickup
    {
        public const float DiscountWalk = 0.80f;
        public const float DiscountCaravan = 0.85f;
        public const float DiscountShip = 0.90f;

        private const int TransferDelayTicks = 2500;      // 放行后 ~1 小时完成交割
        private const float PartyWaitDays = 1.5f;         // 队伍等待交涉上限
        private const float ShipWaitDays = 1f;            // 飞船等待装载上限
        private const int RepromptTicks = 15000;          // 「稍后再来」6 小时
        private const int SendAwayGoodwillHit = 8;        // 打发走好感惩罚

        // ===================== 查询/入口 =====================

        public static bool Available(BountyPickupMethod method, Faction fac)
        {
            if (fac == null || fac.def == null)
                return false;
            switch (method)
            {
                case BountyPickupMethod.Walk:
                    return true;
                case BountyPickupMethod.Caravan:
                    return fac.def.techLevel >= TechLevel.Medieval;
                case BountyPickupMethod.Ship:
                    return ModsConfig.RoyaltyActive && fac.def.techLevel >= TechLevel.Spacer;
            }
            return false;
        }

        public static float Discount(BountyPickupMethod method)
        {
            switch (method)
            {
                case BountyPickupMethod.Caravan: return DiscountCaravan;
                case BountyPickupMethod.Ship: return DiscountShip;
                default: return DiscountWalk;
            }
        }

        /// <summary>海报按钮「叫他们来取」:按可用方式弹浮动菜单(含折后酬金)。</summary>
        public static void ShowMethodMenu(Warrant_Pawn w)
        {
            List<FloatMenuOption> opts = new List<FloatMenuOption>();
            foreach (BountyPickupMethod m in System.Enum.GetValues(typeof(BountyPickupMethod)))
            {
                if (!Available(m, w.issuer))
                    continue;
                BountyPickupMethod mm = m;
                float disc = Discount(m);
                int pay = Mathf.RoundToInt(w.rewardForLiving * disc);
                string label = "RK_Bounty.PickupMethod".Translate(
                    ("RK_Bounty.PickupM_" + m).Translate(),
                    Mathf.RoundToInt((1f - disc) * 100f),
                    pay);
                opts.Add(new FloatMenuOption(label, delegate { Request(w, mm); }));
            }
            if (opts.Count > 0)
                Find.WindowStack.Add(new FloatMenu(opts));
        }

        /// <summary>发起取货请求:校验(信鸽柱/重复/敌对/酬金)后入在途队列,航程 0.5~2 天。</summary>
        public static void Request(Warrant_Pawn w, BountyPickupMethod method)
        {
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio == null || w == null || w.issuer == null)
                return;
            if (!Available(method, w.issuer))
                return;
            if (!BirdPostUtil.AnyPlayerBirdPost())
            {
                Find.WindowStack.Add(new Dialog_MessageBox("RK_Bounty.NeedBirdPost".Translate()));
                return;
            }
            if (radio.pickups.Any(p => p != null && p.warrant == w))
            {
                Messages.Message("RK_Bounty.PickupActive".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (w.issuer.IsPlayer || FactionUtility.HostileTo(w.issuer, Faction.OfPlayer))
            {
                Messages.Message("RK_Bounty.PickupHostile".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (w.rewardForLiving <= 0 && w.rewardForDead <= 0)
            {
                Messages.Message("RK_Bounty.PickupNoReward".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            int now = Find.TickManager.TicksGame;
            float disc = Discount(method);
            var req = new PickupRequest
            {
                warrant = w,
                issuer = w.issuer,
                method = method,
                rewardLiving = Mathf.RoundToInt(w.rewardForLiving * disc),
                rewardDead = Mathf.RoundToInt(w.rewardForDead * disc),
                arriveAtTick = now + radio.MailRouteTicksFrom(w.issuer)
            };
            radio.pickups.Add(req);
            RadioNotifier.Send("RK_Bounty.PickupSentTitle",
                "RK_Bounty.PickupSentText".Translate(
                    w.issuer.Name,
                    GenDate.TicksToDays(req.arriveAtTick - now).ToString("0.0"),
                    ("RK_Bounty.PickupM_" + method).Translate()));
        }

        // ===================== 每帧扫描 =====================

        public static void TickAll(int now)
        {
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio == null || radio.pickups.Count == 0)
                return;
            for (int i = radio.pickups.Count - 1; i >= 0; i--)
            {
                PickupRequest req = radio.pickups[i];
                if (req == null || req.warrant == null)
                {
                    radio.pickups.RemoveAt(i);
                    continue;
                }
                // 单子已被 SW 其它路径(超时/补偿)收走 → 请求作废
                WarrantsManager wm = WarrantsManager.Instance;
                if (wm == null
                    || (!wm.takenWarrants.Contains(req.warrant)
                        && !wm.acceptedWarrants.Contains(req.warrant)))
                {
                    CleanupParty(req);
                    radio.pickups.RemoveAt(i);
                    continue;
                }
                switch (req.stage)
                {
                    case PickupStage.InTransit:
                        if (now >= req.arriveAtTick)
                            Arrive(req);
                        break;
                    case PickupStage.Waiting:
                        if (now >= req.giveUpTick)
                            GiveUpWaiting(radio, req, i);
                        break;
                    case PickupStage.Transferring:
                        if (now >= req.transferAtTick)
                            ExecuteTransfer(radio, req, i);
                        break;
                    case PickupStage.ShipWaiting:
                        TickShip(radio, req, i, now);
                        break;
                    case PickupStage.Postponed:
                        if (now >= req.repromptAtTick)
                        {
                            if (SpawnParty(req))
                            {
                                req.stage = PickupStage.Waiting;
                                req.giveUpTick = now + (int)(GenDate.TicksPerDay * PartyWaitDays);
                                ShowPartyDialog(req);
                            }
                            else
                                radio.pickups.RemoveAt(i);
                        }
                        break;
                }
            }
        }

        // ===================== 抵达 =====================

        private static void Arrive(PickupRequest req)
        {
            if (req.method == BountyPickupMethod.Ship)
            {
                if (SpawnShip(req))
                {
                    req.stage = PickupStage.ShipWaiting;
                    req.shipDeadlineTick = Find.TickManager.TicksGame + (int)(GenDate.TicksPerDay * ShipWaitDays);
                    SendShipLetter(req);
                }
                else
                {
                    // 起降点被占:顺延 6 小时再试
                    req.arriveAtTick = Find.TickManager.TicksGame + RepromptTicks;
                }
                return;
            }
            if (SpawnParty(req))
            {
                req.stage = PickupStage.Waiting;
                req.giveUpTick = Find.TickManager.TicksGame + (int)(GenDate.TicksPerDay * PartyWaitDays);
                ShowPartyDialog(req);
            }
            else
                req.arriveAtTick = Find.TickManager.TicksGame + RepromptTicks;
        }

        private static bool SpawnParty(PickupRequest req)
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null || req.issuer == null)
                return false;
            IntVec3 edge = CellFinder.RandomEdgeCell(map);
            List<Pawn> party = new List<Pawn>();
            int guards = 3 + Rand.RangeInclusive(0, 2);
            PawnKindDef kind = req.issuer.def.basicMemberKind;
            for (int i = 0; i < guards && kind != null; i++)
            {
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    kind, req.issuer, PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0f,
                    allowAddictions: false, worldPawnFactionDoesntMatter: true));
                party.Add(p);
            }
            // 商队:附加 1~2 头驮兽(驮运酬金/货物的意象,装饰性)
            if (req.method == BountyPickupMethod.Caravan)
            {
                List<PawnKindDef> packs = DefDatabase<PawnKindDef>.AllDefsListForReading
                    .Where(k => k.RaceProps.packAnimal && !k.RaceProps.Humanlike).ToList();
                int animals = 1 + Rand.RangeInclusive(0, 1);
                for (int i = 0; i < animals && packs.Count > 0; i++)
                {
                    Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                        packs.RandomElement(), req.issuer, PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                        colonistRelationChanceFactor: 0f, worldPawnFactionDoesntMatter: true));
                    party.Add(p);
                }
            }
            if (party.Count == 0)
                return false;
            for (int i = 0; i < party.Count; i++)
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(edge, map, 5);
                GenSpawn.Spawn(party[i], cell, map, Rot4.Random, WipeMode.Vanish, false);
            }
            List<Pawn> humans = party.Where(p => !p.RaceProps.Animal).ToList();
            if (humans.Count > 0)
                LordMaker.MakeNewLord(req.issuer, new LordJob_VisitColony(req.issuer,
                    DropCellFinder.TradeDropSpot(map)), map, humans);
            req.party = party;
            return true;
        }

        private static void ShowPartyDialog(PickupRequest req)
        {
            Warrant w = req.warrant;
            Thing target = PickupTargetOf(w);
            string targetLabel = target != null ? target.LabelCap
                : (w.thing != null ? w.thing.LabelCap : "…");
            TaggedString title = "RK_Bounty.PickupTitle".Translate();
            DiaNode node = new DiaNode("RK_Bounty.PickupText".Translate(
                req.issuer.Name, targetLabel,
                ("RK_Bounty.PickupM_" + req.method).Translate(),
                req.rewardLiving, req.rewardDead));

            DiaOption enter = new DiaOption("ABE.Enter".Translate());
            enter.action = delegate
            {
                req.stage = PickupStage.Transferring;
                req.transferAtTick = Find.TickManager.TicksGame + TransferDelayTicks;
            };
            enter.resolveTree = true;
            node.options.Add(enter);

            DiaOption sendAway = new DiaOption("ABE.SendAway".Translate());
            sendAway.action = delegate
            {
                CleanupParty(req);
                CancelWarrant(w, SendAwayGoodwillHit);
                RemoveRequest(req);
                Messages.Message("RK_Bounty.PickupSendAwayDone".Translate(req.issuer.Name, SendAwayGoodwillHit),
                    MessageTypeDefOf.NeutralEvent, false);
            };
            sendAway.resolveTree = true;
            node.options.Add(sendAway);

            DiaOption later = new DiaOption("ABE.Later".Translate());
            later.action = delegate
            {
                CleanupParty(req);
                req.stage = PickupStage.Postponed;
                req.repromptAtTick = Find.TickManager.TicksGame + RepromptTicks;
                Messages.Message("RK_Bounty.PickupLaterMsg".Translate(),
                    MessageTypeDefOf.NeutralEvent, false);
            };
            later.resolveTree = true;
            node.options.Add(later);

            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(node, req.issuer, true, false, title));
            Find.Archive.Add(new ArchivedDialog(node.text, title, req.issuer));
        }

        /// <summary>海报「与取货队交涉」按钮:重开三选项弹窗(弹窗曾被 Esc 关闭时用)。</summary>
        public static void ReopenPartyDialog(PickupRequest req)
        {
            if (req != null && req.stage == PickupStage.Waiting)
                ShowPartyDialog(req);
        }

        private static void SendShipLetter(PickupRequest req)
        {
            Thing target = PickupTargetOf(req.warrant);
            Find.LetterStack.ReceiveLetter(
                "RK_Bounty.PickupShipTitle".Translate(req.issuer.Name),
                "RK_Bounty.PickupShipText".Translate(req.issuer.Name,
                    target != null ? target.LabelCap : (req.warrant.thing?.LabelCap ?? "…"),
                    req.rewardLiving, req.rewardDead),
                LetterDefOf.PositiveEvent,
                req.shipThing, req.issuer);
        }

        // ===================== 交割 =====================

        /// <summary>放行后定时交割:目标(活体/尸体)被队伍带走,酬金在队伍位置落地。</summary>
        private static void ExecuteTransfer(BountyRadioManager radio, PickupRequest req, int idx)
        {
            Thing target = PickupTargetOf(req.warrant);
            if (target == null || !target.Spawned || req.party.NullOrEmpty() || req.party[0] == null
                || !req.party[0].Spawned)
            {
                // 目标暂时拿不到:队伍继续等,回到交涉态
                req.stage = PickupStage.Waiting;
                req.giveUpTick = Find.TickManager.TicksGame
                    + (int)(GenDate.TicksPerDay * PartyWaitDays);
                Messages.Message("RK_Bounty.PickupTransferFail".Translate(),
                    MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            bool alive = target is Pawn p && !p.Dead;
            int pay = alive ? req.rewardLiving : req.rewardDead;
            // 目标随队离开(消失)
            target.Destroy(DestroyMode.Vanish);
            // 酬金:队伍带来的银,落在队伍旁
            Pawn payer = req.party[0];
            IntVec3 at = payer.Position;
            Map map = payer.Map;
            for (int left = pay; left > 0;)
            {
                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                int take = Mathf.Min(left, silver.def.stackLimit);
                silver.stackCount = take;
                GenPlace.TryPlaceThing(silver, at, map, ThingPlaceMode.Near);
                left -= take;
            }
            CompleteWarrant(req.warrant, req.issuer);
            if (pay > 0)
                Messages.Message("RK_Bounty.PickupDone".Translate(pay),
                    MessageTypeDefOf.PositiveEvent, false);
            RemoveRequest(req);
        }

        /// <summary>飞船路径:玩家装载目标并发射 → FlyAway.TryStart prefix 检出货 → 付款。</summary>
        internal static void OnShipDepart(TransportShip ship)
        {
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio == null || ship == null)
                return;
            PickupRequest req = radio.pickups.FirstOrDefault(r =>
                r != null && r.stage == PickupStage.ShipWaiting && r.shipThing == ship.shipThing);
            if (req == null)
                return;
            List<Thing> contents = ship.TransporterComp != null
                ? ship.TransporterComp.GetDirectlyHeldThings().ToList() : null;
            Thing target = contents?.FirstOrDefault(t => IsWarrantTarget(t, req.warrant));
            if (target == null)
            {
                // 空载离开
                CleanupShip(req);
                RemoveRequest(req);
                Messages.Message("RK_Bounty.PickupShipLeft".Translate(),
                    MessageTypeDefOf.NeutralEvent, false);
                return;
            }
            bool alive = target is Pawn p && !p.Dead;
            int pay = alive ? req.rewardLiving : req.rewardDead;
            CompleteWarrant(req.warrant, req.issuer);
            BountyRewards.DropBonusNearPlayer(req.warrant);
            Map home = Find.AnyPlayerHomeMap;
            if (pay > 0 && home != null)
            {
                List<Thing> silver = new List<Thing>();
                for (int left = pay; left > 0;)
                {
                    Thing s = ThingMaker.MakeThing(ThingDefOf.Silver);
                    int take = Mathf.Min(left, s.def.stackLimit);
                    s.stackCount = take;
                    silver.Add(s);
                    left -= take;
                }
                DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(home), home, silver,
                    110, false, false, true, true, true, null);
            }
            if (pay > 0)
                Messages.Message("RK_Bounty.PickupDone".Translate(pay),
                    MessageTypeDefOf.PositiveEvent, false);
            RemoveRequest(req);
        }

        // ===================== 飞船 =====================

        private static bool SpawnShip(PickupRequest req)
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null || !ModsConfig.RoyaltyActive)
                return false;
            IntVec3 cell = IntVec3.Invalid;
            for (int i = 0; i < 60 && !cell.IsValid; i++)
            {
                IntVec3 c = CellFinder.RandomEdgeCell(map);
                if (RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(c, map, ThingDefOf.Shuttle))
                    cell = c;
            }
            if (!cell.IsValid)
                cell = DropCellFinder.TradeDropSpot(map);
            Thing shuttle = ThingMaker.MakeThing(ThingDefOf.Shuttle);
            CompShuttle comp = shuttle.TryGetComp<CompShuttle>();
            comp.permitShuttle = true;
            comp.acceptChildren = true;
            TransportShip ship = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle);
            ship.ArriveAt(cell, map.Parent);
            ship.AddJobs(ShipJobDefOf.WaitForever);
            req.shipThing = shuttle;
            return true;
        }

        private static void TickShip(BountyRadioManager radio, PickupRequest req, int idx, int now)
        {
            // 飞船被毁/已消失且未付款 → 请求作废(单子保留,可再叫)
            if (req.shipThing == null || req.shipThing.Destroyed || !req.shipThing.Spawned)
            {
                CleanupShip(req);
                radio.pickups.RemoveAt(idx);
                return;
            }
            if (now >= req.shipDeadlineTick)
            {
                TransportShip ship = req.shipThing.TryGetComp<CompShuttle>()?.shipParent;
                if (ship != null && ship.Waiting)
                    ship.ForceJob(ShipJobDefOf.FlyAway); // TryStart prefix 会检货;空载自然离开
                req.shipDeadlineTick = now + (int)(GenDate.TicksPerDay * ShipWaitDays);
            }
        }

        // ===================== 工具 =====================

        private static void GiveUpWaiting(BountyRadioManager radio, PickupRequest req, int idx)
        {
            CleanupParty(req);
            radio.pickups.RemoveAt(idx);
            Messages.Message("RK_Bounty.PickupExpired".Translate(req.issuer?.Name ?? "?"),
                MessageTypeDefOf.NeutralEvent, false);
        }

        /// <summary>镜像 SW Pod 路径完成:status=Completed+移出列表+实物加成掉落。</summary>
        private static void CompleteWarrant(Warrant w, Faction issuer)
        {
            w.status = WarrantStatus.Completed;
            WarrantsManager mgr = WarrantsManager.Instance;
            mgr.acceptedWarrants.Remove(w);
            mgr.takenWarrants.Remove(w);
            BountyRewards.DropBonusNearPlayer(w);
        }

        /// <summary>打发走:整单作废+扣好感(SW 弃单语义收窄为移除,不做袭击)。</summary>
        private static void CancelWarrant(Warrant w, int goodwillHit)
        {
            w.status = WarrantStatus.Failed;
            WarrantsManager mgr = WarrantsManager.Instance;
            mgr.acceptedWarrants.Remove(w);
            mgr.takenWarrants.Remove(w);
            if (w.issuer != null && !w.issuer.IsPlayer)
                w.issuer.TryAffectGoodwillWith(Faction.OfPlayer, -goodwillHit, true, true, null, null);
        }

        private static void RemoveRequest(PickupRequest req)
        {
            BountyRadioManager.Get()?.pickups.Remove(req);
        }

        private static void CleanupParty(PickupRequest req)
        {
            if (req.party == null)
                return;
            for (int i = 0; i < req.party.Count; i++)
                if (req.party[i] != null && req.party[i].Spawned && !req.party[i].Destroyed)
                    req.party[i].Destroy(DestroyMode.Vanish);
            req.party = null;
        }

        private static void CleanupShip(PickupRequest req)
        {
            req.shipThing = null;
        }

        /// <summary>可交割目标:活体优先,已死收尸体(仅当有死体酬金);须 Spawned 于任一玩家地图。</summary>
        private static Thing PickupTargetOf(Warrant w)
        {
            if (w?.thing == null)
                return null;
            if (w.thing is Pawn p)
            {
                if (!p.Dead && p.Spawned)
                    return p;
                if (p.Corpse != null && p.Corpse.Spawned && w is Warrant_Pawn wp && wp.rewardForDead > 0)
                    return p.Corpse;
            }
            return null;
        }

        private static bool IsWarrantTarget(Thing t, Warrant w)
        {
            if (t == null || w?.thing == null)
                return false;
            if (t == w.thing)
                return true;
            return t is Corpse c && c.InnerPawn == w.thing;
        }
    }

    public enum BountyPickupMethod
    {
        Walk,
        Caravan,
        Ship
    }

    public static class PickupStage
    {
        public const int InTransit = 0;
        public const int Waiting = 1;       // 队伍在边缘等待交涉(弹窗可重开)
        public const int Transferring = 2;  // 已放行,交割进行中
        public const int ShipWaiting = 3;   // 飞船等待装载
        public const int Postponed = 4;     // 「稍后再来」,6h 后重新弹窗
    }

    /// <summary>取货请求条目(warrant Deep / issuer·shipThing·party 引用)。</summary>
    public class PickupRequest : IExposable
    {
        public Warrant warrant;
        public Faction issuer;
        public BountyPickupMethod method = BountyPickupMethod.Walk;
        public int rewardLiving;
        public int rewardDead;
        public int arriveAtTick;
        public int stage = PickupStage.InTransit;
        public int transferAtTick;
        public int giveUpTick;
        public int repromptAtTick;
        public int shipDeadlineTick;
        public Thing shipThing;
        public List<Pawn> party;

        public void ExposeData()
        {
            // 2026-09-03: 存档前剔除已死/已销毁的护卫与已销毁的飞船——这类对象(临时生成、Vanish 销毁)
            // 不在任何深保存节点(地图/世界)里,残留引用会触发存档告警
            // "Object with load ID Thing_X is referenced (xml node name: li) but is not deep-saved",读档后为 null。
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (party != null)
                    party.RemoveAll(p => p == null || p.Dead || p.Destroyed || !p.Spawned);
                if (shipThing != null && shipThing.Destroyed)
                    shipThing = null;
            }
            Scribe_Deep.Look(ref warrant, "warrant");
            Scribe_References.Look(ref issuer, "issuer");
            Scribe_Values.Look(ref method, "method", BountyPickupMethod.Walk);
            Scribe_Values.Look(ref rewardLiving, "rewardLiving", 0);
            Scribe_Values.Look(ref rewardDead, "rewardDead", 0);
            Scribe_Values.Look(ref arriveAtTick, "arriveAtTick", 0);
            Scribe_Values.Look(ref stage, "stage", PickupStage.InTransit);
            Scribe_Values.Look(ref transferAtTick, "transferAtTick", 0);
            Scribe_Values.Look(ref giveUpTick, "giveUpTick", 0);
            Scribe_Values.Look(ref repromptAtTick, "repromptAtTick", 0);
            Scribe_Values.Look(ref shipDeadlineTick, "shipDeadlineTick", 0);
            Scribe_References.Look(ref shipThing, "shipThing");
            Scribe_Collections.Look(ref party, "party", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.LoadingVars && party == null)
                party = new List<Pawn>();
        }

        /// <summary>海报按钮文案:在途/交涉/交割中/飞船等待/稍后再来。</summary>
        public string StatusLabel()
        {
            int now = Find.TickManager.TicksGame;
            switch (stage)
            {
                case PickupStage.InTransit:
                    return "RK_Bounty.PickupInTransit".Translate(
                        Mathf.CeilToInt(GenDate.TicksToDays(arriveAtTick - now)));
                case PickupStage.Waiting:
                    return "RK_Bounty.PickupArrivedBtn".Translate();
                case PickupStage.Transferring:
                    return "RK_Bounty.PickupTransferring".Translate();
                case PickupStage.ShipWaiting:
                    return "RK_Bounty.PickupShipStatus".Translate(
                        Mathf.CeilToInt(GenDate.TicksToDays(shipDeadlineTick - now)));
                case PickupStage.Postponed:
                    return "RK_Bounty.PickupLaterStatus".Translate(
                        Mathf.CeilToInt(GenDate.TicksToDays(repromptAtTick - now)));
                default:
                    return "…";
            }
        }
    }

    // —— 飞船起飞拦截:TryStart 时货物还在 Transporter 内容器里(方法体内才搬运) ——
    [HarmonyPatch(typeof(ShipJob_FlyAway), "TryStart")]
    internal static class ShipJob_FlyAway_TryStart_Pickup
    {
        [HarmonyPrefix]
        private static void Prefix(ShipJob_FlyAway __instance)
        {
            try
            {
                BountyPickup.OnShipDepart(__instance.transportShip);
            }
            catch
            {
                // 检货失败不阻断原版发射流程
            }
        }
    }
}

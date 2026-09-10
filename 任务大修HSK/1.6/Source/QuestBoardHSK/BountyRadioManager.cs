using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 信鸽传信管理:SW 每次新生成的通缉不直接进榜,先进"在途"队列(按来源派系→玩家家
    /// 的世界距离折算 0.5~2 天航程),信鸽到站后转入"待接收"队列,由殖民者在信鸽柱
    /// 操作「收取信鸽来件」后才转入 availableWarrants。开局积压与商人夹带保持即时入待接收。
    /// 发单激怒同样走信鸽:发布时仅入 outMail,航程结束后才对目标派系扣好感并发信。
    /// 限流:任何来源入队后,一周(7 天)内不再生成新悬赏(GenerateRandomWarrant prefix 短路)。
    /// 时间限制:待接收 7 天过期作废(在途也计入,以 createdTick 起算);榜上沿 SW 规则 15 天。
    /// 性能:本组件每 60 tick 轮询一次,用 seen 集合识别新入列项;航程按派系缓存 1 天;
    /// 无每 tick 遍历。
    /// </summary>
    public class BountyRadioManager : GameComponent
    {
        public const int WeekTicks = 7 * 60000;
        public const float PendingExpiryDays = 7f;
        public const float AvailableExpiryDays = 15f;

        /// <summary>新单扫描周期(60 tick = 1 秒)。</summary>
        public const int ScanIntervalTicks = 60;

        public List<Warrant> pending = new List<Warrant>();

        /// <summary>信鸽在途:已捕获但航程未结束,到站才转 pending。</summary>
        public List<BirdMailItem> inFlight = new List<BirdMailItem>();

        /// <summary>发单激怒信鸽:到站后才对目标派系扣好感。</summary>
        public List<OutgoingAngerMail> outMail = new List<OutgoingAngerMail>();

        /// <summary>交接弹窗选「稍后再来」的送货:到点重新弹 ABE 式询问(照搬 ABE 6h 语义)。</summary>
        public List<DelayedBountyDelivery> delayedDeliveries = new List<DelayedBountyDelivery>();

        /// <summary>十期·方向B:玩家叫发单方派队/飞船来取货的请求(徒步/商队/飞船,阶梯折扣)。</summary>
        public List<PickupRequest> pickups = new List<PickupRequest>();

        /// <summary>刺客公会送人上门订单(2026-09-03 双阶整合):公会执行玩家悬赏成功后按所选方式在途送达。</summary>
        public List<GuildDeliveryItem> guildDeliveries = new List<GuildDeliveryItem>();

        private const float FlightMinDays = 0.5f;
        private const float FlightMaxDays = 2f;
        private const float RouteDistNorm = 110f;
        private const int RouteCacheTicks = 60000;
        private const int RouteCacheMax = 64;

        /// <summary>派系→航程 tick 缓存(1 天失效;不序列化,读档重算)。</summary>
        private Dictionary<int, int> routeCache = new Dictionary<int, int>();
        private Dictionary<int, int> routeCacheAtTick = new Dictionary<int, int>();

        public int lastTraderRelayTick = -999999;

        // 十二期·悬赏猎人日掷锚点 + 定向事件 def 缓存(2026-09-02 通缉扩展)
        public int lastHunterTick = -999999;
        private static IncidentDef retaliationDef;
        private static IncidentDef hunterDef;

        private static IncidentDef RetaliationDef
        {
            get { return retaliationDef != null ? retaliationDef : retaliationDef = DefDatabase<IncidentDef>.GetNamedSilentFail("RK_Bounty_Retaliation"); }
        }

        private static IncidentDef HunterDef
        {
            get { return hunterDef != null ? hunterDef : hunterDef = DefDatabase<IncidentDef>.GetNamedSilentFail("RK_Bounty_Hunter"); }
        }

        /// <summary>P5 暗杀委托生成器:独立冷却,与周限流共享锚点。</summary>
        private const int AssassinInterval = 4 * 60000;
        private int lastAssassinTick = -999999;

        /// <summary>#1 显式分层：通缉/委托 loadID → 钉定的 TaskTierDef.rank（0=按目标科技档派生）。</summary>
        public Dictionary<string, int> warrantTier = new Dictionary<string, int>();
        public void SetWarrantTier(string loadID, int rank) { if (!loadID.NullOrEmpty() && rank > 0) warrantTier[loadID] = rank; }
        public int WarrantTierOf(string loadID) { int r; return loadID != null && warrantTier.TryGetValue(loadID, out r) ? r : 0; }
        public static int TierOverride(string loadID) { BountyRadioManager g = Get(); return g != null ? g.WarrantTierOf(loadID) : 0; }

        /// <summary>最近一次悬赏入队(生成)时刻;全局一周一条的锚点。</summary>
        public int lastBountyGenTick = -999999;

        /// <summary>八期:玩家发单的执行进度(通缉 loadID → 已发里程碑 0接单/1追踪/2接火)。</summary>
        public Dictionary<string, int> bountyProgress = new Dictionary<string, int>();

        private bool progressInitialized;

        private bool guildsHiddenApplied;

        private HashSet<string> seenIds = new HashSet<string>();
        private bool seenInitialized;

        public BountyRadioManager(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pending, "pendingRadioWarrants", LookMode.Deep);
            Scribe_Collections.Look(ref inFlight, "birdMailInFlight", LookMode.Deep);
            Scribe_Collections.Look(ref outMail, "outgoingAngerMail", LookMode.Deep);
            Scribe_Collections.Look(ref delayedDeliveries, "delayedBountyDeliveries", LookMode.Deep);
            Scribe_Collections.Look(ref pickups, "bountyPickupRequests", LookMode.Deep);
            Scribe_Collections.Look(ref guildDeliveries, "guildDeliveries", LookMode.Deep);
            Scribe_Values.Look(ref lastTraderRelayTick, "lastTraderRelayTick", -999999);
            Scribe_Values.Look(ref lastHunterTick, "lastHunterTick", -999999);
            Scribe_Values.Look(ref lastBountyGenTick, "lastBountyGenTick", -999999);
            Scribe_Values.Look(ref lastAssassinTick, "lastAssassinTick", -999999);
            Scribe_Values.Look(ref progressInitialized, "progressInitialized", false);
            Scribe_Collections.Look(ref bountyProgress, "bountyProgress", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref warrantTier, "warrantTier", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref seenInitialized, "seenInitialized", false);
            Scribe_Collections.Look(ref seenIds, "seenWarrantIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (pending == null)
                    pending = new List<Warrant>();
                if (inFlight == null)
                    inFlight = new List<BirdMailItem>();
                if (outMail == null)
                    outMail = new List<OutgoingAngerMail>();
                if (delayedDeliveries == null)
                    delayedDeliveries = new List<DelayedBountyDelivery>();
                if (pickups == null)
                    pickups = new List<PickupRequest>();
                if (guildDeliveries == null)
                    guildDeliveries = new List<GuildDeliveryItem>();
                if (bountyProgress == null)
                    bountyProgress = new Dictionary<string, int>();
                if (warrantTier == null)
                    warrantTier = new Dictionary<string, int>();
                if (seenIds == null)
                    seenIds = new HashSet<string>();
            }
        }

        public override void GameComponentTick()
        {
            // 刺客公会隐藏钉补(一次性/每会话,幂等不序列化):仅钉"本体设计上隐藏"的公会。
            // 1.6 实证 Faction.Hidden => hidden ?? def.hidden,且世界生成把实例 hidden 固化为 false
            // (FactionGeneratorParms.hidden 默认 false,不读 def.hidden),所以 def 写 hidden=true 对已生成世界
            // (含新开档)永远不生效 → 运行时把 def.hidden==true 的公会(美狐机枢会)实例钉 true。
            // ⚠ Kurin_Faction 合并后是"可见"的影刃会(定居+族色发单方,def 无 hidden),绝不能被误钉隐藏,
            //    故门控加 f.def.hidden:只有本体声明 hidden 的公会才被钉。
            if (!guildsHiddenApplied)
            {
                guildsHiddenApplied = true;
                try
                {
                    foreach (Faction f in Find.FactionManager.AllFactions)
                        if (BountyRules.IsAssassinGuild(f) && f.def != null && f.def.hidden && f.hidden != true)
                            f.hidden = true;
                }
                catch (Exception e)
                {
                    Log.Error("[QuestBoardHSK] 刺客公会隐藏钉补异常: " + e);
                }
            }
            int now = Find.TickManager.TicksGame;
            if (now % ScanIntervalTicks == 0)
            {
                ScanCapture();
                if (PromoteArrivedInFlight(now) > 0)
                    RadioNotifier.Send("RK_Bounty.RadioIncomingTitle",
                        "RK_Bounty.RadioIncomingText".Translate(pending.Count));
                DeliverDueAngerMail(now);
                ScanBountyProgress();
                DeliverDueDelayedDelivery(now);
                TryAssassinContract(now);
                TrySendBountyHunter(now);
                BountyPickup.TickAll(now);
                GuildDelivery.TickAll(now);
            }
            // 过期播报清理,低频(每 600 tick)
            if (now % 600 == 0 && pending.Count > 0)
            {
                int expired = PurgeExpiredPending();
                if (expired > 0)
                    RadioNotifier.Send("RK_Bounty.RadioExpiredTitle",
                        "RK_Bounty.RadioExpiredText".Translate(expired));
            }
        }

        public static BountyRadioManager Get()
        {
            return Current.Game == null ? null : Current.Game.GetComponent<BountyRadioManager>();
        }

        public bool GenerationCooledDown()
        {
            return Current.Game != null && Find.TickManager.TicksGame - lastBountyGenTick < WeekTicks;
        }

        public void MarkSeen(Warrant w)
        {
            if (w != null)
                seenIds.Add(w.GetUniqueLoadID());
        }

        public void NoteGeneration(int tick)
        {
            lastBountyGenTick = tick;
        }

        /// <summary>
        /// 扫描 availableWarrants,把未见过的项(=新生成)转入信鸽在途队列。
        /// 首扫只登记基线不捕获(防止读档后把已在榜上/在途的旧单误截)。
        /// </summary>
        private int ScanCapture()
        {
            var mgr = WarrantsManager.Instance;
            if (mgr == null)
                return 0;
            if (!seenInitialized)
            {
                seenInitialized = true;
                foreach (Warrant w in mgr.availableWarrants)
                    seenIds.Add(w.GetUniqueLoadID());
                foreach (Warrant w in pending)
                    seenIds.Add(w.GetUniqueLoadID());
                for (int i = 0; i < inFlight.Count; i++)
                    if (inFlight[i]?.warrant != null)
                        seenIds.Add(inFlight[i].warrant.GetUniqueLoadID());
                return 0;
            }
            int now = Find.TickManager.TicksGame;
            int captured = 0;
            for (int i = mgr.availableWarrants.Count - 1; i >= 0; i--)
            {
                Warrant w = mgr.availableWarrants[i];
                if (seenIds.Add(w.GetUniqueLoadID()))
                {
                    mgr.availableWarrants.RemoveAt(i);
                    inFlight.Add(new BirdMailItem
                    {
                        warrant = w,
                        readyAtTick = now + MailRouteTicksFrom(SourceFactionOf(w))
                    });
                    captured++;
                }
            }
            if (captured > 0)
                lastBountyGenTick = now;
            return captured;
        }

        /// <summary>
        /// P5 暗杀委托:某 NPC 派系(与玩家非敌)向玩家下"猎杀其敌对派系某人"的 kill-only 委托,
        /// 复用通缉管线(入在途信鸽→待接收→公告牌),交付成功经 #6 给刺客公会加好感(+);
        /// 委托方为玩家接单履约对象,SW 对"接单方失败/过期"本就不额外扣玩家与委托方好感,符合"失败不扣好感"。
        /// 全 try/catch + 独立冷却 + 共享周限流,任何前置不满足即 no-op,绝不抛。
        /// </summary>
        private void TryAssassinContract(int now)
        {
            try
            {
                if (now - lastAssassinTick < AssassinInterval)
                    return;
                WarrantsManager mgr = WarrantsManager.Instance;
                if (mgr == null || GenerationCooledDown())
                    return;
                // 优先由隐藏刺客公会直供;找不到对手再退回普通 NPC patron。
                Faction patron = Find.FactionManager.AllFactions.FirstOrDefault(
                    f => BountyRules.IsAssassinGuild(f) && !f.defeated && !f.IsPlayer);
                Faction enemy = patron != null ? EnemyOf(patron) : null;
                if (patron == null || enemy == null)
                {
                    patron = Find.FactionManager.AllFactions.FirstOrDefault(
                        f => BountyRules.IsWorldNpcFaction(f) && !FactionUtility.HostileTo(f, Faction.OfPlayer));
                    enemy = patron != null ? EnemyOf(patron) : null;
                }
                if (patron == null || enemy == null)
                    return;
                Pawn target = null;
                foreach (Map m in Find.Maps)
                {
                    foreach (Pawn p in m.mapPawns.AllPawns)
                    {
                        if (p != null && !p.Dead && p.Faction == enemy && !p.IsPrisoner && !p.RaceProps.Animal)
                        {
                            target = p;
                            break;
                        }
                    }
                    if (target != null)
                        break;
                }
                if (target == null)
                    return;
                var w = new Warrant_Pawn
                {
                    loadID = mgr.GetWarrantID(),
                    issuer = patron,
                    createdTick = now
                };
                w.thing = target;
                int tier = BountyRules.TierOf(enemy.def.techLevel);
                w.rewardForLiving = 0;
                w.rewardForDead = Mathf.Max(50, Mathf.RoundToInt(target.MarketValue * BountyRules.TierRewardMult(tier)));
                w.reason = "RK_Bounty.AssassinReason".Translate(enemy.Name);
                w.message = "RK_Bounty.AssassinMessage".Translate(patron.Name, enemy.Name);
                SetWarrantTier(w.loadID, BountyRules.Tier3);   // #1 显式钉为 T3 外政委托
                lastAssassinTick = now;
                lastBountyGenTick = now;
                seenIds.Add(w.GetUniqueLoadID());
                inFlight.Add(new BirdMailItem
                {
                    warrant = w,
                    readyAtTick = now + MailRouteTicksFrom(patron)
                });
            }
            catch (Exception e)
            {
                Log.Error("[QuestBoardHSK] 暗杀委托生成异常: " + e);
            }
        }

        /// <summary>patron 的可猎杀敌对世界 NPC 派系（同档 ±1）。</summary>
        private static Faction EnemyOf(Faction patron)
        {
            if (patron == null)
                return null;
            return Find.FactionManager.AllFactions.FirstOrDefault(
                f => f != patron && BountyRules.IsWorldNpcFaction(f)
                    && FactionUtility.HostileTo(f, patron) && BountyRules.TechGapOk(patron, f));
        }

        /// <summary>委托来源派系:issuer 优先,空则回退目标 pawn 的派系(都缺则 null→默认航程)。</summary>
        private static Faction SourceFactionOf(Warrant w)
        {
            if (w.issuer != null && !w.issuer.IsPlayer)
                return w.issuer;
            if (w is Warrant_Pawn wp && wp.Pawn != null)
            {
                Faction fac = wp.Pawn.Faction;
                if (fac != null && !fac.IsPlayer)
                    return fac;
            }
            return null;
        }

        /// <summary>航程 tick:来源派系基站位→玩家家基站位的世界距离映射 0.5~2 天(按派系缓存 1 天)。</summary>
        public int MailRouteTicksFrom(Faction fac)
        {
            if (fac == null)
                return (int)(GenDate.TicksPerDay * FlightMinDays * 2f);
            int now = Find.TickManager.TicksGame;
            int cached;
            if (routeCache.TryGetValue(fac.loadID, out cached) && now - routeCacheAtTick[fac.loadID] < RouteCacheTicks)
                return cached;
            int ticks = ComputeMailRouteTicks(fac);
            if (routeCache.Count >= RouteCacheMax)
            {
                routeCache.Clear();
                routeCacheAtTick.Clear();
            }
            routeCache[fac.loadID] = ticks;
            routeCacheAtTick[fac.loadID] = now;
            return ticks;
        }

        private static int ComputeMailRouteTicks(Faction fac)
        {
            PlanetTile a = PlayerHomeTile();
            PlanetTile b = FactionHomeTile(fac);
            if (!a.Valid || !b.Valid)
                return (int)GenDate.TicksPerDay;
            int dist = Find.WorldGrid.TraversalDistanceBetween(a, b);
            float frac = Mathf.Clamp01(dist / RouteDistNorm);
            return Mathf.RoundToInt(Mathf.Lerp(GenDate.TicksPerDay * FlightMinDays, GenDate.TicksPerDay * FlightMaxDays, frac));
        }

        private static PlanetTile PlayerHomeTile()
        {
            Map map = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            if (map != null)
                return map.Tile;
            foreach (Settlement s in Find.WorldObjects.SettlementBases)
                if (s.Faction == Faction.OfPlayer && s.Tile.Valid)
                    return s.Tile;
            return default;
        }

        private static PlanetTile FactionHomeTile(Faction fac)
        {
            if (fac == null || fac.IsPlayer)
                return default;
            foreach (Settlement s in Find.WorldObjects.SettlementBases)
                if (s.Faction == fac && s.Tile.Valid && s.Tile.LayerDef == PlanetLayerDefOf.Surface)
                    return s.Tile;
            return default;
        }

        /// <summary>在途信鸽到站:转 pending。顺带清掉反序列化失败的死条目。</summary>
        private int PromoteArrivedInFlight(int now)
        {
            if (inFlight.Count == 0)
                return 0;
            int arrived = 0;
            for (int i = inFlight.Count - 1; i >= 0; i--)
            {
                BirdMailItem mail = inFlight[i];
                if (mail == null || mail.warrant == null)
                {
                    inFlight.RemoveAt(i);
                    continue;
                }
                if (now >= mail.readyAtTick)
                {
                    inFlight.RemoveAt(i);
                    pending.Add(mail.warrant);
                    arrived++;
                }
            }
            return arrived;
        }

        /// <summary>发单激怒入队:发布即寄出,航程结束才真正扣好感。</summary>
        public void QueueOutgoingAnger(Faction fac, int hit)
        {
            if (fac == null || hit <= 0)
                return;
            outMail.Add(new OutgoingAngerMail
            {
                fac = fac,
                hit = hit,
                deliverAtTick = Find.TickManager.TicksGame + MailRouteTicksFrom(fac)
            });
        }

        /// <summary>激怒信鸽到站:对目标派系扣好感并发信;派系已灭亡则静默丢弃。</summary>
        private int DeliverDueAngerMail(int now)
        {
            if (outMail.Count == 0)
                return 0;
            int delivered = 0;
            for (int i = outMail.Count - 1; i >= 0; i--)
            {
                OutgoingAngerMail mail = outMail[i];
                if (mail == null)
                {
                    outMail.RemoveAt(i);
                    continue;
                }
                if (now < mail.deliverAtTick)
                    continue;
                outMail.RemoveAt(i);
                if (mail.fac == null || mail.fac.defeated)
                    continue;
                mail.fac.TryAffectGoodwillWith(Faction.OfPlayer, -mail.hit, true, true, null, null);
                RadioNotifier.Send("RK_Bounty.AngerMailTitle",
                    "RK_Bounty.AngerMailText".Translate(mail.fac.Name, mail.hit));
                // 十二期·派系报复:按激怒值概率排队 1.5~4 天后的报复事件(交涉/威胁/袭击)
                TryQueueRetaliation(mail.fac, mail.hit);
                delivered++;
            }
            return delivered;
        }

        /// <summary>按激怒值概率安排派系报复:hit≥12,概率=hit/200(6%~50%),延迟 1.5~4 天。</summary>
        private void TryQueueRetaliation(Faction fac, int hit)
        {
            try
            {
                if (fac == null || fac.defeated || hit <= 0)
                    return;
                if (!Rand.Chance(Mathf.Clamp01(hit / 200f)))
                    return;
                IncidentDef def = RetaliationDef;
                if (def == null)
                    return;
                IncidentParms parms = new IncidentParms
                {
                    target = Find.AnyPlayerHomeMap,
                    faction = fac,
                    points = hit,
                    forced = true
                };
                Find.Storyteller.incidentQueue.Add(def,
                    Mathf.RoundToInt(GenDate.TicksPerDay * Rand.Range(1.5f, 4f)), parms);
            }
            catch (Exception e)
            {
                Log.Error("[QuestBoardHSK] 派系报复排队异常: " + e);
            }
        }

        /// <summary>
        /// 十二期·悬赏猎人:玩家有已接单(玩家发布)的存活目标时,每日低频一掷(40%)
        /// 排队 0.5~1.5 天后的猎人事件;无接单则不掷。日掷锚点先置位,保证每天最多评估一次。
        /// </summary>
        private void TrySendBountyHunter(int now)
        {
            if (now - lastHunterTick < 60000)
                return;
            lastHunterTick = now;
            try
            {
                IncidentDef def = HunterDef;
                WarrantsManager mgr = WarrantsManager.Instance;
                if (def == null || mgr == null)
                    return;
                bool hasTarget = mgr.takenWarrants.Any(w =>
                {
                    if (!(w is Warrant_Pawn wp) || wp.issuer != Faction.OfPlayer || wp.Pawn == null)
                        return false;
                    return !wp.Pawn.Dead && !wp.Pawn.Destroyed && !wp.Pawn.RaceProps.Animal;
                });
                if (!hasTarget)
                    return;
                if (!Rand.Chance(0.4f))
                    return;
                IncidentParms parms = new IncidentParms
                {
                    target = Find.AnyPlayerHomeMap,
                    forced = true
                };
                Find.Storyteller.incidentQueue.Add(def,
                    Mathf.RoundToInt(GenDate.TicksPerDay * Rand.Range(0.5f, 1.5f)), parms);
            }
            catch (Exception e)
            {
                Log.Error("[QuestBoardHSK] 悬赏猎人排队异常: " + e);
            }
        }

        /// <summary>交接弹窗选「稍后再来」:送货队退回,6 小时后重新来询问。</summary>
        public void QueueDelayedDelivery(Warrant warrant)
        {
            if (warrant == null)
                return;
            // 同一时间只保留一条待送(付款交割是单发流程)
            delayedDeliveries.Clear();
            delayedDeliveries.Add(new DelayedBountyDelivery
            {
                warrant = warrant,
                deliverAtTick = Find.TickManager.TicksGame + 15000
            });
        }

        private void DeliverDueDelayedDelivery(int now)
        {
            if (delayedDeliveries.Count == 0)
                return;
            for (int i = delayedDeliveries.Count - 1; i >= 0; i--)
            {
                DelayedBountyDelivery d = delayedDeliveries[i];
                if (d == null || d.warrant == null)
                {
                    delayedDeliveries.RemoveAt(i);
                    continue;
                }
                if (now < d.deliverAtTick)
                    continue;
                delayedDeliveries.RemoveAt(i);
                BountyTales.ShowDeliveryDialog(d.warrant);
            }
        }

        /// <summary>
        /// 八期:玩家发单的执行进度事件。diff takenWarrants:
        /// 新接单 → 接单故事信;历时 35%/70% → 追踪/接火里程碑信;
        /// 离开 taken(成功/失败/弃单回池) → 清跟踪(回池后再接单重走一遍流程)。
        /// 读档首扫按当前进度静默登记基线,不补发过期信。
        /// </summary>
        private void ScanBountyProgress()
        {
            var mgr = WarrantsManager.Instance;
            if (mgr == null)
                return;
            int now = Find.TickManager.TicksGame;
            if (!progressInitialized)
            {
                progressInitialized = true;
                foreach (Warrant w in mgr.takenWarrants)
                    if (w != null && w.issuer == Faction.OfPlayer)
                        bountyProgress[w.GetUniqueLoadID()] = MilestoneOf(w, now);
                return;
            }
            var alive = new HashSet<string>();
            foreach (Warrant w in mgr.takenWarrants)
            {
                if (w == null || w.issuer != Faction.OfPlayer)
                    continue;
                string id = w.GetUniqueLoadID();
                alive.Add(id);
                int stage;
                if (!bountyProgress.TryGetValue(id, out stage))
                {
                    bountyProgress[id] = 0;
                    BountyTales.SendAcceptLetter(w);
                    continue;
                }
                int m = MilestoneOf(w, now);
                if (m > stage)
                {
                    bountyProgress[id] = m;
                    BountyTales.SendProgressLetter(w, m);
                }
            }
            if (bountyProgress.Count > 0)
            {
                List<string> dead = null;
                foreach (string id in bountyProgress.Keys)
                    if (!alive.Contains(id))
                    {
                        if (dead == null)
                            dead = new List<string>();
                        dead.Add(id);
                    }
                if (dead != null)
                    for (int i = 0; i < dead.Count; i++)
                        bountyProgress.Remove(dead[i]);
            }
        }

        /// <summary>按 acceptedTick→tickToBeCompleted 的历时比例判里程碑(≥35% 追踪,≥70% 接火)。</summary>
        private static int MilestoneOf(Warrant w, int now)
        {
            if (w.acceptedTick < 0 || w.tickToBeCompleted <= w.acceptedTick)
                return 0;
            float frac = (float)(now - w.acceptedTick) / (float)(w.tickToBeCompleted - w.acceptedTick);
            if (frac >= 0.7f)
                return 2;
            if (frac >= 0.35f)
                return 1;
            return 0;
        }

        /// <summary>捕获刚由 PopulateWarrants 追加的通缉(快照差集法,商队夹带路径用,即时生效)。</summary>
        public int CaptureNewByDiff(List<Warrant> snapshot)
        {
            var mgr = WarrantsManager.Instance;
            if (mgr == null)
                return 0;
            int captured = 0;
            foreach (Warrant w in mgr.availableWarrants.ToList())
            {
                if (!snapshot.Contains(w))
                {
                    mgr.availableWarrants.Remove(w);
                    pending.Add(w);
                    seenIds.Add(w.GetUniqueLoadID());
                    captured++;
                }
            }
            if (captured > 0)
                lastBountyGenTick = Find.TickManager.TicksGame;
            return captured;
        }

        /// <summary>殖民者接收:弹出队首一条,入榜(公告牌立即可见)。</summary>
        public Warrant TryCollectOne()
        {
            var mgr = WarrantsManager.Instance;
            if (mgr == null || pending.Count == 0)
                return null;
            Warrant w = pending[0];
            pending.RemoveAt(0);
            mgr.availableWarrants.Add(w);
            return w;
        }

        /// <summary>清掉超期未接收的播报(从生成/播报时刻起 PendingExpiryDays 天)。</summary>
        public int PurgeExpiredPending()
        {
            int now = Find.TickManager.TicksGame;
            int removed = 0;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (GenDate.TicksToDays(now - pending[i].createdTick) >= PendingExpiryDays)
                {
                    pending.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>榜上悬赏剩余有效天数(SW 内部 15 天自动下榜;算到 0 表示即将失效)。</summary>
        public static int AvailableDaysLeft(Warrant w)
        {
            if (w == null || w.createdTick < 0)
                return -1;
            float elapsed = GenDate.TicksToDays(Find.TickManager.TicksGame - w.createdTick);
            return Mathf.Max(0, Mathf.CeilToInt(AvailableExpiryDays - elapsed));
        }

        /// <summary>待接收播报剩余天数(7 天过期作废)。</summary>
        public static int PendingDaysLeft(Warrant w)
        {
            if (w == null || w.createdTick < 0)
                return -1;
            float elapsed = GenDate.TicksToDays(Find.TickManager.TicksGame - w.createdTick);
            return Mathf.Max(0, Mathf.CeilToInt(PendingExpiryDays - elapsed));
        }
    }

    /// <summary>在途信鸽邮件条目(warrant 走 Deep 序列化)。</summary>
    public class BirdMailItem : IExposable
    {
        public Warrant warrant;
        public int readyAtTick;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref warrant, "warrant");
            Scribe_Values.Look(ref readyAtTick, "readyAtTick", 0);
        }
    }

    /// <summary>发单激怒信鸽条目(派系是全局单例,必须 Scribe_References)。</summary>
    public class OutgoingAngerMail : IExposable
    {
        public Faction fac;
        public int hit;
        public int deliverAtTick;

        public void ExposeData()
        {
            Scribe_References.Look(ref fac, "fac");
            Scribe_Values.Look(ref hit, "hit", 0);
            Scribe_Values.Look(ref deliverAtTick, "deliverAtTick", 0);
        }
    }

    /// <summary>交接「稍后再来」:6 小时后重新弹出送货询问。</summary>
    public class DelayedBountyDelivery : IExposable
    {
        public Warrant warrant;
        public int deliverAtTick;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref warrant, "warrant");
            Scribe_Values.Look(ref deliverAtTick, "deliverAtTick", 0);
        }
    }
}

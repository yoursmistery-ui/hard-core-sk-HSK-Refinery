using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 无线电悬赏管理:SW 每次新生成的通缉不直接进榜,先进"待接收"队列,
    /// 由殖民者在通讯台/公告牌操作「接收悬赏通讯」后才转入 availableWarrants(公告牌可见)。
    /// 限流:任何来源入队后,一周(7 天)内不再生成新悬赏(GenerateRandomWarrant prefix 短路)。
    /// 时间限制:待接收播报 7 天过期作废;榜上悬赏沿 SW 规则 15 天,UI 显示倒计时。
    /// 性能:周期生成的捕获不走每 tick Harmony 补丁,本组件每 60 tick 轮询一次
    /// availableWarrants,用 seen 集合识别新入列项(读档首扫把现有项全部记 seen,防旧单误截);
    /// 开局积压走 PopulateWarrants 补丁即时捕获(低频调用,零热路径开销)。
    /// </summary>
    public class BountyRadioManager : GameComponent
    {
        public const int WeekTicks = 7 * 60000;
        public const float PendingExpiryDays = 7f;
        public const float AvailableExpiryDays = 15f;

        /// <summary>新单扫描周期(60 tick = 1 秒)。</summary>
        public const int ScanIntervalTicks = 60;

        public List<Warrant> pending = new List<Warrant>();

        public int lastTraderRelayTick = -999999;

        /// <summary>最近一次悬赏入队(生成)时刻;全局一周一条的锚点。</summary>
        public int lastBountyGenTick = -999999;

        /// <summary>八期:玩家发单的执行进度(通缉 loadID → 已发里程碑 0接单/1追踪/2接火)。</summary>
        public Dictionary<string, int> bountyProgress = new Dictionary<string, int>();

        private bool progressInitialized;

        private HashSet<string> seenIds = new HashSet<string>();
        private bool seenInitialized;

        public BountyRadioManager(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pending, "pendingRadioWarrants", LookMode.Deep);
            Scribe_Values.Look(ref lastTraderRelayTick, "lastTraderRelayTick", -999999);
            Scribe_Values.Look(ref lastBountyGenTick, "lastBountyGenTick", -999999);
            Scribe_Values.Look(ref progressInitialized, "progressInitialized", false);
            Scribe_Collections.Look(ref bountyProgress, "bountyProgress", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref seenInitialized, "seenInitialized", false);
            Scribe_Collections.Look(ref seenIds, "seenWarrantIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (pending == null)
                    pending = new List<Warrant>();
                if (bountyProgress == null)
                    bountyProgress = new Dictionary<string, int>();
                if (seenIds == null)
                    seenIds = new HashSet<string>();
            }
        }

        public override void GameComponentTick()
        {
            int now = Find.TickManager.TicksGame;
            if (now % ScanIntervalTicks == 0)
            {
                if (ScanCapture() > 0)
                    RadioNotifier.Send("RK_Bounty.RadioIncomingTitle",
                        "RK_Bounty.RadioIncomingText".Translate(pending.Count));
                ScanBountyProgress();
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
        /// 扫描 availableWarrants,把未见过的项(=新生成)转入待接收队列。
        /// 首扫只登记基线不捕获(防止读档后把已在榜上的旧单误截)。
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
                    pending.Add(w);
                    captured++;
                }
            }
            if (captured > 0)
                lastBountyGenTick = now;
            return captured;
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
}

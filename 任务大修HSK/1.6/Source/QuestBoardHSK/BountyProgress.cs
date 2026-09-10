using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>悬赏进度种类。</summary>
    public enum BountyProgressKind
    {
        /// <summary>无进度可显示(NPC 发给 NPC 等)。</summary>
        None,
        /// <summary>玩家发单后无人接单,等待中。</summary>
        Waiting,
        /// <summary>玩家发单已被 NPC 派系接走,对方执行中(接单→追踪→交火→收网)。</summary>
        Execution,
        /// <summary>玩家接下的 NPC 悬赏,自己的行动进度(目标在外→目标到手→可交付)。</summary>
        PlayerQuest,
        /// <summary>榜上可接悬赏的时效进度(15 天有效期)。</summary>
        Lifetime,
        /// <summary>待接收单的过期进度(7 天)。</summary>
        Pending,
    }

    /// <summary>悬赏进度快照:由 UI 层每帧调用,全部纯读,无副作用。</summary>
    public class BountyProgressInfo
    {
        public BountyProgressKind kind;
        /// <summary>进度条填充 0~1(PlayerQuest 用阶段近似,Waiting/None 无意义)。</summary>
        public float fraction;
        /// <summary>剩余天数(-1=无时限概念)。</summary>
        public int daysLeft = -1;
        /// <summary>Execution: 0接单/1追踪/2交火;PlayerQuest: 0在外/1到手/2可交付。</summary>
        public int stage;
        /// <summary>进度条左上角标题。</summary>
        public string header;
        /// <summary>进度条右上角短文本(阶段名/剩余天数)。</summary>
        public string rightText;
        /// <summary>进度条下方状态说明。</summary>
        public string statusText;
        /// <summary>进度条填充色。</summary>
        public Color fillColor = Color.white;
        /// <summary>临近截止/高进度,右侧与状态用警示红。</summary>
        public bool danger;

        public bool HasBar
        {
            get { return kind != BountyProgressKind.None && kind != BountyProgressKind.Waiting; }
        }
    }

    /// <summary>
    /// 十三期·悬赏进度(2026-09-02):把散在各处的单据时限统一折算成进度条数据。
    /// - 玩家发单被 NPC 接走: 按 acceptedTick→tickToBeCompleted 历时,里程碑与 bountyProgress
    ///   字典(八期信件系统)一致: 0接单/35%追踪/70%交火,读字典优先、本地复算兜底。
    /// - 玩家接的 NPC 悬赏: SW 不给时限,按目标客观状态分三步: 目标在外/目标到手(死亡或被
    ///   控制/取得)/可交付(在玩家地面、商队或随身容器中),取货请求在途时视为可交付。
    /// - 可接/待接收: createdTick 起算 15 天/7 天时效。
    /// 全部只读计算;不写任何管理器状态。
    /// </summary>
    internal static class BountyProgress
    {
        public const float TrackMilestoneFrac = 0.35f;
        public const float ClashMilestoneFrac = 0.70f;

        /// <summary>主入口。mgr/radio 任一为 null 时尽量降级返回。</summary>
        public static BountyProgressInfo GetInfo(Warrant w, WarrantsManager mgr, BountyRadioManager radio)
        {
            if (w == null)
                return null;
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            bool inPending = radio != null && radio.pending.Contains(w);
            bool inPublic = mgr != null && mgr.availableWarrants.Contains(w);

            if (inPending)
                return PendingInfo(w, radio, now);
            if (inPublic)
                return LifetimeInfo(w, now);
            if (w.issuer != null && w.issuer.IsPlayer)
                return w.accepteer == null ? WaitingInfo() : ExecutionInfo(w, radio, now);
            if (w.accepteer != null && w.accepteer.IsPlayer)
                return PlayerQuestInfo(w, radio);
            return null;
        }

        // ---------- 玩家发单·NPC 执行 ----------

        private static BountyProgressInfo ExecutionInfo(Warrant w, BountyRadioManager radio, int now)
        {
            var pi = new BountyProgressInfo { kind = BountyProgressKind.Execution, header = "RK_Bounty.Prog_Exec".Translate().RawText };
            string id = w.GetUniqueLoadID();
            int stage;
            if (radio != null && radio.bountyProgress != null && radio.bountyProgress.TryGetValue(id, out stage))
                pi.stage = Mathf.Clamp(stage, 0, 2);
            else
                pi.stage = LocalMilestone(w, now);

            float frac = 0f;
            if (w.acceptedTick >= 0 && w.tickToBeCompleted > w.acceptedTick)
                frac = (float)(now - w.acceptedTick) / (float)(w.tickToBeCompleted - w.acceptedTick);
            pi.fraction = Mathf.Clamp01(frac);
            if (pi.stage == 0 && pi.fraction >= TrackMilestoneFrac)
                pi.stage = 1;
            if (pi.stage <= 1 && pi.fraction >= ClashMilestoneFrac)
                pi.stage = 2;

            int daysLeft = w.tickToBeCompleted > now
                ? Mathf.CeilToInt((w.tickToBeCompleted - now) / 60000f) : 0;
            pi.daysLeft = daysLeft;
            pi.rightText = StageLabel(pi.kind, pi.stage);
            pi.fillColor = StampGreen();
            pi.danger = daysLeft <= 1;
            pi.statusText = daysLeft <= 0
                ? "RK_Bounty.Prog_ExpFinishing".Translate().RawText
                : "RK_Bounty.Prog_ExecHint".Translate(w.accepteer.Name, daysLeft).RawText;
            return pi;
        }

        /// <summary>与 BountyRadioManager.MilestoneOf 同口径的本地复算(字典缺失时兜底)。</summary>
        public static int LocalMilestone(Warrant w, int now)
        {
            if (w.acceptedTick < 0 || w.tickToBeCompleted <= w.acceptedTick)
                return 0;
            float frac = (float)(now - w.acceptedTick) / (float)(w.tickToBeCompleted - w.acceptedTick);
            if (frac >= ClashMilestoneFrac)
                return 2;
            if (frac >= TrackMilestoneFrac)
                return 1;
            return 0;
        }

        // ---------- 玩家接单·自己的行动 ----------

        private static BountyProgressInfo PlayerQuestInfo(Warrant w, BountyRadioManager radio)
        {
            var pi = new BountyProgressInfo { kind = BountyProgressKind.PlayerQuest, header = "RK_Bounty.Prog_Player".Translate().RawText };
            pi.stage = PlayerQuestStage(w, radio);
            pi.fraction = pi.stage <= 0 ? 0.04f : pi.stage == 1 ? 0.5f : 1f;
            pi.rightText = StageLabel(pi.kind, pi.stage);
            pi.fillColor = StampGreen();
            pi.statusText = ("RK_Bounty.Prog_PlayerHint" + pi.stage.ToString()).Translate().RawText;
            return pi;
        }

        /// <summary>0=目标在外 1=目标到手(死亡/被控/取得) 2=可交付(在玩家地面或随身)。</summary>
        public static int PlayerQuestStage(Warrant w, BountyRadioManager radio)
        {
            // 已进入取货/空投交接流程 → 直接视为可交付
            if (radio != null && radio.pickups != null)
            {
                for (int i = 0; i < radio.pickups.Count; i++)
                {
                    var pr = radio.pickups[i];
                    if (pr != null && pr.warrant == w)
                        return 2;
                }
            }
            if (w is Warrant_Pawn wp)
            {
                Pawn p = wp.Pawn;
                if (p == null)
                    return 0;
                if (p.Dead)
                    return OnPlayerGround(p.Corpse) ? 2 : 1;
                bool inHand = p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony
                    || (p.CarriedBy != null && p.CarriedBy.Faction == Faction.OfPlayer);
                if (!inHand)
                    return 0;
                return OnPlayerGround(p) ? 2 : 1;
            }
            if (w is Warrant_TameAnimal wt)
            {
                ThingDef race = wt.AnimalRace != null ? wt.AnimalRace.race : null;
                if (race == null)
                    return 0;
                Pawn tamed = FindAnyTamedPlayerAnimal(race);
                if (tamed == null)
                    return 0;
                return OnPlayerGround(tamed) ? 2 : 1;
            }
            // 器物/其余: 目标实体在玩家地面即"到手即交付"
            if (w.thing != null)
                return OnPlayerGround(w.thing) ? 2 : w.thing.Spawned || w.thing.holdingOwner != null ? 1 : 0;
            return 0;
        }

        private static Pawn FindAnyTamedPlayerAnimal(ThingDef race)
        {
            List<Pawn> all = PawnsFinder.AllMapsWorldAndTemporary_Alive;
            for (int i = 0; i < all.Count; i++)
            {
                Pawn p = all[i];
                if (p == null || p.def != race || p.Faction != Faction.OfPlayer)
                    continue;
                return p;
            }
            return null;
        }

        /// <summary>实体在玩家基地地图/玩家商队/玩家随身容器上(逐层上溯,深度防环)。</summary>
        public static bool OnPlayerGround(Thing t)
        {
            return OnPlayerGround(t, 0);
        }

        private static bool OnPlayerGround(Thing t, int depth)
        {
            if (t == null || depth > 4)
                return false;
            if (t.Spawned && t.MapHeld != null && t.MapHeld.IsPlayerHome)
                return true;
            Caravan cv = t.GetCaravan();
            if (cv != null)
                return cv.Faction == Faction.OfPlayer;
            IThingHolder h = t.ParentHolder;
            while (h != null && depth <= 4)
            {
                if (h is Thing ht)
                    return ht != t && OnPlayerGround(ht, depth + 1);
                if (h is Map)
                    return false;
                h = h.ParentHolder;
            }
            return false;
        }

        // ---------- 时效类 ----------

        private static BountyProgressInfo PendingInfo(Warrant w, BountyRadioManager radio, int now)
        {
            var pi = new BountyProgressInfo { kind = BountyProgressKind.Pending, header = "RK_Bounty.Prog_Pending".Translate().RawText };
            float total = BountyRadioManager.PendingExpiryDays * 60000f;
            float elapsed = Mathf.Max(0, now - w.createdTick);
            pi.fraction = total > 0f ? Mathf.Clamp01(elapsed / total) : 0f;
            pi.daysLeft = BountyRadioManager.PendingDaysLeft(w);
            pi.rightText = "RK_Bounty.DaysLeft".Translate(Mathf.Max(0, pi.daysLeft)).RawText;
            pi.fillColor = StampBlue();
            pi.danger = pi.daysLeft <= 2;
            pi.statusText = "RK_Bounty.Prog_PendingHint".Translate(Mathf.Max(0, pi.daysLeft)).RawText;
            return pi;
        }

        private static BountyProgressInfo LifetimeInfo(Warrant w, int now)
        {
            var pi = new BountyProgressInfo { kind = BountyProgressKind.Lifetime, header = "RK_Bounty.Prog_Lifetime".Translate().RawText };
            float total = BountyRadioManager.AvailableExpiryDays * 60000f;
            float elapsed = Mathf.Max(0, now - w.createdTick);
            pi.fraction = total > 0f ? Mathf.Clamp01(elapsed / total) : 0f;
            pi.daysLeft = BountyRadioManager.AvailableDaysLeft(w);
            pi.rightText = "RK_Bounty.DaysLeft".Translate(Mathf.Max(0, pi.daysLeft)).RawText;
            pi.fillColor = StampBlue();
            pi.danger = pi.daysLeft <= 2;
            pi.statusText = "RK_Bounty.Prog_LifetimeHint".Translate(Mathf.Max(0, pi.daysLeft)).RawText;
            return pi;
        }

        private static BountyProgressInfo WaitingInfo()
        {
            var pi = new BountyProgressInfo
            {
                kind = BountyProgressKind.Waiting,
                header = "RK_Bounty.Prog_Exec".Translate().RawText,
                statusText = "RK_Bounty.Prog_Waiting".Translate().RawText,
                fillColor = StampBlue(),
            };
            return pi;
        }

        // ---------- 文案/颜色 ----------

        public static string StageLabel(BountyProgressKind kind, int stage)
        {
            if (kind == BountyProgressKind.Execution)
                return ("RK_Bounty.Prog_ExecStage" + Mathf.Clamp(stage, 0, 2)).Translate().ToString();
            return ("RK_Bounty.Prog_PlayerStage" + Mathf.Clamp(stage, 0, 2)).Translate().ToString();
        }

        private static Color StampGreen()
        {
            return new Color(0.56f, 0.71f, 0.45f);
        }

        private static Color StampBlue()
        {
            return new Color(0.50f, 0.69f, 0.85f);
        }
    }
}

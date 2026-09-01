// 特性拓展modHSK —— 节流账本(游戏组件持久化) v2
// v2 (2026-08-27) 新增"殖民地级节奏门": 开局满 2 年才判定 / 年内按人口配额 / 个人 1~1.5 年随机冷却。
// 注: 本安装的 Assembly-CSharp 为 HSK 重编译版, Verse.WorldComponent 缺失,
//     改用 GameComponent 承载持久化(GetComponent API 一致)。
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    // 每个小人的动态特性账本
    public class HSKPawnEntry : IExposable
    {
        public string pawnId;
        public int firstSeenTick = -1;   // 首次登记入队时刻(近似"加入殖民地")
        public int lastGrantTick = int.MinValue;
        public int lastRevokeTick = int.MinValue;
        public int lastDrugUseTick = int.MinValue; // 最近一次摄入毒品的时间(禁毒X年移除成瘾特性用)
        public int dynamicCount;          // 已授予的动态特性数(容量判定)
        public int negativeGrants;        // 其中负面特性条数(统计/更严门控用)
        // v2: 个人下一次可判定时刻。授予成功后 = now + (365 + rand(0~183)) 天, 即 1~1.5 年随机。
        //     int.MinValue = 从未授予过(只受"入队满 N 年"门约束)。
        public int nextEligibleTick = int.MinValue;
        public List<string> grantedTraits = new List<string>(); // 授予轨迹(失效按最早移除)
        public Dictionary<string, int> counters = new Dictionary<string, int>(); // 行为维度 -> 计数
        public Dictionary<string, int> lastVar = new Dictionary<string, int>();  // 文案槽位 -> 上次变体号(v3, 防同人连着拿到同一套)

        // 临时缓冲: EndCurrentJob 前缀写入, 后置消费
        public string pendingJobDef = null;
        public string pendingWorkType = null;
        public string pendingJoyKind = null;
        public string pendingTargetDef = null; // job target thing defName(判毒)
        public bool   pendingSuccess = false;
        public int    pendingHour = 0;
        public float  pendingRoomTemp = 9999f; // v2: 收工时所处温度(判耐热/耐寒)

        public void ExposeData()
        {
            Scribe_Values.Look(ref pawnId, "pawnId");
            Scribe_Values.Look(ref firstSeenTick, "firstSeenTick", -1);
            Scribe_Values.Look(ref lastGrantTick, "lastGrantTick", int.MinValue);
            Scribe_Values.Look(ref lastRevokeTick, "lastRevokeTick", int.MinValue);
            Scribe_Values.Look(ref lastDrugUseTick, "lastDrugUseTick", int.MinValue);
            Scribe_Values.Look(ref dynamicCount, "dynamicCount", 0);
            Scribe_Values.Look(ref negativeGrants, "negativeGrantsV2", 0);
            Scribe_Values.Look(ref nextEligibleTick, "nextEligibleTickV2", int.MinValue);
            Scribe_Collections.Look(ref grantedTraits, "grantedTraits", LookMode.Value);
            Scribe_Collections.Look(ref counters, "counters", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref lastVar, "lastVarV3", LookMode.Value, LookMode.Value);
        }

        public int GetCount(string key)
        {
            int v; return counters.TryGetValue(key, out v) ? v : 0;
        }
        public void AddCount(string key, int n)
        {
            int v = GetCount(key) + n;
            if (v <= 0) counters.Remove(key); else counters[key] = v;
        }
    }

    public class HSKTraitLedger : GameComponent
    {
        public Dictionary<string, HSKPawnEntry> entries = new Dictionary<string, HSKPawnEntry>();

        // v2 殖民地级节奏状态
        public int quotaYearIndex = -1;   // 当前配额所属"游戏年"序号
        public int grantsThisYear = 0;    // 本年已被授予动态特性的人数
        public int nextDriftTick = int.MinValue; // 下一次"性格漂移"检查时刻(持久化, 防重启后节奏丢失)

        public const int TICKS_PER_DAY = 60000;
        public const int TICKS_PER_YEAR = 60000 * 365;

        // ⚠️ 实测结论(2026-08-21 二次修复,以 dnfile 反编译 IL 为准):
        // 本环境 HSK 重编译版 Assembly-CSharp 的 Verse.GameComponent 基类虽是无参 protected 构造,
        // 但 Verse.Game.FillComponents() 的 IL 把 new object[]{ game } 传给 Activator.CreateInstance,
        // 即游戏按 (Verse.Game) 签名查找构造函数 —— 必须提供 public HSKTraitLedger(Game game),
        // 否则抛 MissingMethodException: Constructor on type '...HSKTraitLedger' not found.
        public HSKTraitLedger(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref entries, "entries", LookMode.Value, LookMode.Deep);
            Scribe_Values.Look(ref quotaYearIndex, "quotaYearIndexV2", -1);
            Scribe_Values.Look(ref grantsThisYear, "grantsThisYearV2", 0);
            Scribe_Values.Look(ref nextDriftTick, "nextDriftTickV2", int.MinValue);
            if (Scribe.mode == LoadSaveMode.LoadingVars && entries == null)
                entries = new Dictionary<string, HSKPawnEntry>();
        }

        public override void GameComponentTick()
        {
            // 只在"每天一次"的低频节点做清理/年度翻篇, 绝不每 tick 遍历(AGENTS.md §9 性能铁律)
            if (Find.TickManager == null) return;
            int tg = Find.TickManager.TicksGame;
            if (tg % TICKS_PER_DAY != 0) return;
            RolloverYearIfNeeded(tg);
            CleanupDead();
            CheckDrugAbstinence();
            MaybeDriftCheck(tg);
        }

        private void RolloverYearIfNeeded(int tg)
        {
            int year = tg / TICKS_PER_YEAR;
            if (year != quotaYearIndex)
            {
                quotaYearIndex = year;
                grantsThisYear = 0;
            }
        }

        // 年度配额: <15 人 → 1 人/年, 15~24 → 2, 25~34 → 3, 35+ → 4 (每多 10 人 +1, 上限 6)
        public int ColonyYearQuota()
        {
            int pop = 0;
            try { pop = PawnsFinder.AllMaps_FreeColonists.Count; }
            catch { pop = 0; }
            int q = 1 + (pop - 5) / 10;
            if (q < 1) q = 1;
            if (q > 6) q = 6;
            return q;
        }

        public bool ColonyQuotaLeft()
        {
            if (Find.TickManager == null) return false;
            RolloverYearIfNeeded(Find.TickManager.TicksGame);
            return grantsThisYear < ColonyYearQuota();
        }

        public void NoteColonyGrant()
        {
            if (Find.TickManager == null) return;
            RolloverYearIfNeeded(Find.TickManager.TicksGame);
            grantsThisYear++;
        }

        // 性格漂移: 每 revokeIntervalDays(含随机追加)最多移除 1 条(全殖民地), 由低频日检驱动
        private void MaybeDriftCheck(int tg)
        {
            HSKTraitSetting s = HSKTraitMod.settings;
            if (s == null || !s.enableTraitDrift) return;
            if (tg < nextDriftTick) return;
            if (entries == null) return;
            foreach (var kv in entries)
            {
                HSKPawnEntry e = kv.Value;
                if (e == null || e.dynamicCount <= 0) continue;
                Pawn p = FindPawn(kv.Key);
                if (p == null || p.Dead || p.Destroyed || p.Discarded) continue;
                if (p.story == null || p.story.traits == null) continue;
                if (!HSKTraits.CanRevokeNow(e)) continue;
                if (HSKTraits.RevokeOne(p))
                {
                    nextDriftTick = tg + (s.revokeIntervalDays + Rand.Range(0, 183)) * TICKS_PER_DAY;
                    return; // 一次只漂移掉一条
                }
            }
            // 无人可漂移: 200 天后再看, 避免每天全表扫描
            nextDriftTick = tg + 200 * TICKS_PER_DAY;
        }

        // 禁毒 X 天: 自上次摄入毒品起满 drugAbstinenceDays 天, 移除本mod授予的成瘾类特性
        private void CheckDrugAbstinence()
        {
            if (entries == null) return;
            foreach (var kv in entries)
            {
                HSKPawnEntry e = kv.Value;
                if (e == null || e.lastDrugUseTick == int.MinValue) continue;
                Pawn p = FindPawn(kv.Key);
                if (p == null || p.Dead || p.Destroyed || p.Discarded) continue;
                if (p.story == null || p.story.traits == null) continue;
                HSKTraits.TryRevokeDrugTraitsForAbstinence(p, e);
            }
        }

        public HSKPawnEntry EntryFor(Pawn p)
        {
            if (p == null) return null;
            string id = p.GetUniqueLoadID();
            HSKPawnEntry e;
            if (entries.TryGetValue(id, out e)) { if (e.pawnId == null) e.pawnId = id; return e; }
            e = new HSKPawnEntry { pawnId = id, firstSeenTick = Find.TickManager.TicksGame };
            entries[id] = e;
            return e;
        }

        // 只查不改: 供门控使用, 避免为从未有过行为记录的外人凭空建条目
        public HSKPawnEntry PeekEntry(Pawn p)
        {
            if (p == null || entries == null) return null;
            HSKPawnEntry e;
            entries.TryGetValue(p.GetUniqueLoadID(), out e);
            return e;
        }

        public void CleanupDead()
        {
            if (entries == null) return;
            List<string> dead = null;
            foreach (var kv in entries)
            {
                Pawn p = FindPawn(kv.Key);
                if (p == null || p.Destroyed || p.Dead || p.Discarded) { if (dead == null) dead = new List<string>(); dead.Add(kv.Key); }
            }
            if (dead != null) foreach (var d in dead) entries.Remove(d);
        }

        private Pawn FindPawn(string loadId)
        {
            Game g = Current.Game;
            if (g == null || g.Maps == null) return null;
            foreach (Map m in g.Maps)
            {
                if (m == null || m.mapPawns == null) continue;
                foreach (Pawn p in m.mapPawns.AllPawnsSpawned)
                    if (p != null && p.GetUniqueLoadID() == loadId) return p;
            }
            return null;
        }
    }

    public static class HSKLedger
    {
        public static HSKTraitLedger Game
        {
            get { return (Current.Game != null) ? Current.Game.GetComponent<HSKTraitLedger>() : null; }
        }
    }
}

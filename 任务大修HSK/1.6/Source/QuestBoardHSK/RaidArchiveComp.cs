using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestBoardHSK
{
    /// <summary>敌对事件种类(2026-09-02 档案扩展):原档案只记袭击,现扩展猛兽/狂暴/赎金。</summary>
    public enum RaidEventKind
    {
        Raid = 0,       // 敌对袭击(含小偷/绑架——劫匪 AI 行为,无独立事件)
        Manhunter = 1,  // 猛兽袭人(群体)
        Insanity = 2,   // 单只动物狂暴
        Ransom = 3      // 绑架赎金(无在场成员,只记派系)
    }

    /// <summary>
    /// 敌对事件档案(2026-09-02):持久化最近 N 次敌对事件——波次序号/派系小队名/天数/成员(名字+身价+科技档)。
    /// 数据源:IncidentWorker_* TryExecuteWorker postfix(见 HarmonyPatches.cs):
    /// RaidEnemy(袭击)/AggressiveAnimals(猛兽群)/AnimalInsanitySingle(单只狂暴)/RansomDemand(绑架赎金)。
    /// GameComponent 由 1.6 自动反射注册(AllSubclassesNonAbstract + Activator.CreateInstance(type, game)),无需 Def。
    /// 成员名单采用"事件后 5 秒窗口内持续补全"——步行袭击(EdgeWalk)稍晚抵达,窗口期内扫描地图敌对 pawns;
    /// 动物事件(RaidEventKind.Manhunter/Insanity)补扫狂暴动物(无派系,按狂暴心态判定)。
    /// </summary>
    public class RaidArchiveComp : GameComponent
    {
        public const int MaxRaids = 20;
        public const int MaxMembersPerRaid = 50;
        private const int FetchWindowTicks = 300;   // 事件后 5 秒补全窗口
        private const int ScanInterval = 15;        // 每 15 tick 扫一次,防每帧遍历 pawns

        public int nextWave = 1;
        public List<RaidRecord> raids = new List<RaidRecord>();

        [Unsaved(false)]
        private readonly List<RaidFetchTask> fetchTasks = new List<RaidFetchTask>();

        public RaidArchiveComp() { }

        public RaidArchiveComp(Game game) : base() { }

        public static RaidArchiveComp Get()
        {
            return Current.Game != null ? Current.Game.GetComponent<RaidArchiveComp>() : null;
        }

        /// <summary>敌对事件成功触发时由 patch 调用。仅记录 Map 目标的事件(殖民地/基地),行商队遇袭不算。</summary>
        public void NotifyHostileEvent(IncidentParms parms, RaidEventKind kind)
        {
            if (parms == null || (kind == RaidEventKind.Raid && parms.faction == null))
                return;

            // 去重:同 tick 同派系同战术/同种事件的重复触发只记一次
            if (raids.Count > 0)
            {
                RaidRecord top = raids[0];
                if (top.tick == Find.TickManager.TicksGame && top.kind == kind && top.faction == parms.faction
                    && top.strategyLabel == (parms.raidStrategy != null ? parms.raidStrategy.label : ""))
                    return;
            }

            var rec = new RaidRecord
            {
                wave = nextWave++,
                kind = kind,
                tick = Find.TickManager.TicksGame,
                day = GenDate.DaysPassedSinceSettle,
                faction = parms.faction,
                factionName = parms.faction != null ? parms.faction.Name : "",
                strategyLabel = parms.raidStrategy != null ? parms.raidStrategy.label : "",
                points = parms.points
            };
            raids.Insert(0, rec);
            if (raids.Count > MaxRaids)
                raids.RemoveRange(MaxRaids, raids.Count - MaxRaids);

            // 赎金事件无在场成员,不入补全队列
            if (kind == RaidEventKind.Ransom)
                return;
            fetchTasks.Add(new RaidFetchTask(rec, parms));
        }

        public override void GameComponentTick()
        {
            if (fetchTasks.Count == 0)
                return;
            if (Find.TickManager.TicksGame % ScanInterval != 0)
                return;

            int now = Find.TickManager.TicksGame;
            for (int i = fetchTasks.Count - 1; i >= 0; i--)
            {
                RaidFetchTask task = fetchTasks[i];
                if (now - task.startTick >= FetchWindowTicks)
                {
                    fetchTasks.RemoveAt(i);
                    continue;
                }
                if (!(task.parms.target is Map map) || map.mapPawns == null)
                    continue;
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p == null || p.Destroyed)
                        continue;
                    if (task.record.kind == RaidEventKind.Raid)
                    {
                        if (p.Faction == null || !p.Faction.HostileTo(Faction.OfPlayer))
                            continue;
                    }
                    else
                    {
                        // 动物事件:狂暴动物无派系,按狂暴心态判定
                        if (!p.RaceProps.Animal || !p.InMentalState
                            || p.MentalStateDef != MentalStateDefOf.Manhunter)
                            continue;
                    }
                    if (task.seenPawns.Contains(p.thingIDNumber))
                        continue;
                    task.seenPawns.Add(p.thingIDNumber);
                    if (task.record.members.Count < MaxMembersPerRaid)
                        task.record.members.Add(new RaidMember(p));
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextWave, "nextWave", 1);
            Scribe_Collections.Look(ref raids, "raids", LookMode.Deep);
        }
    }

    /// <summary>一次敌对事件的记录(最新在前)。</summary>
    public class RaidRecord : IExposable
    {
        public int wave;                 // 波次序号(全局递增)
        public RaidEventKind kind = RaidEventKind.Raid;  // 事件种类
        public int tick;                 // 触发 tick
        public int day;                  // 第几天
        public Faction faction;          // 事件派系(动物事件为 null;已灭/失联后引用仍可序列化)
        public string factionName = "";  // 派系名快照(派系销毁后仍可显示)
        public string strategyLabel = ""; // 战术名快照(如"袭击/围攻/突袭")
        public float points;             // 事件点数
        public List<RaidMember> members = new List<RaidMember>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref wave, "wave", 0);
            Scribe_Values.Look(ref kind, "kind", RaidEventKind.Raid);
            Scribe_Values.Look(ref tick, "tick", 0);
            Scribe_Values.Look(ref day, "day", 0);
            Scribe_References.Look(ref faction, "faction", false);
            Scribe_Values.Look(ref factionName, "factionName", "");
            Scribe_Values.Look(ref strategyLabel, "strategyLabel", "");
            Scribe_Values.Look(ref points, "points", 0f);
            Scribe_Collections.Look(ref members, "members", LookMode.Deep);
        }

        public int MemberCount => members != null ? members.Count : 0;
    }

    /// <summary>袭击成员(pawn 引用 + 当时快照)。</summary>
    public class RaidMember : IExposable
    {
        public Pawn pawn;          // 可能已死/销毁,引用仍可序列化
        public string name = "";   // LabelShortCap 快照
        public int marketValue;    // 当时身价
        public int techLevel;      // 当时派系科技档(0=Neolithic)

        public RaidMember() { }

        public RaidMember(Pawn p)
        {
            pawn = p;
            name = p.LabelShortCap;
            marketValue = Mathf.RoundToInt(p.MarketValue);
            techLevel = p.Faction != null && p.Faction.def != null ? (int)p.Faction.def.techLevel : 0;
        }

        public void ExposeData()
        {
            // 2026-09-03: 只对存活 pawn 保存引用(存活者必被地图/世界深保存)。
            // 已死袭击者尸体清理后不被任何深保存节点保存,残留引用触发存档告警
            // "referenced but is not deep-saved";死亡展示走 name/marketValue 快照。
            if (Scribe.mode == LoadSaveMode.Saving && pawn != null && (pawn.Dead || pawn.Destroyed))
                pawn = null;
            Scribe_References.Look(ref pawn, "pawn", false);
            Scribe_Values.Look(ref name, "name", "");
            Scribe_Values.Look(ref marketValue, "marketValue", 0);
            Scribe_Values.Look(ref techLevel, "techLevel", 0);
        }
    }

    /// <summary>袭击成员补全任务(不序列化——fetchTasks 字段已标 [Unsaved],窗口仅 5 秒)。</summary>
    public class RaidFetchTask
    {
        public RaidRecord record;
        public IncidentParms parms;
        public int startTick;
        public HashSet<int> seenPawns = new HashSet<int>();

        public RaidFetchTask(RaidRecord record, IncidentParms parms)
        {
            this.record = record;
            this.parms = parms;
            startTick = Find.TickManager.TicksGame;
        }
    }
}

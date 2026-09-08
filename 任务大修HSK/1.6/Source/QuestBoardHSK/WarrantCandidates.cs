using System.Collections.Generic;
using RimWorld;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 通缉候选池(2026-09-02 通缉扩展):SW 原版 Dialog_SelectPawn 只列不在地图上的世界 pawn,
    /// 访客/商队成员/袭击者都选不到。这里把候选池扩成"世界 pawn ∪ 各玩家地图在场的外派系 pawn",
    /// 由 Dialog_IssueWarrantHSK 构造 Dialog_SelectPawn 后整体覆盖其 public allPawns 字段
    /// (SW 的搜索/派系/异种过滤与回写机制原样复用,无需自写选人窗)。
    /// 仅点「选择」按钮时调用一次,零常驻开销。
    /// </summary>
    internal static class WarrantCandidates
    {
        public static List<Pawn> HumanPawns()
        {
            var list = new List<Pawn>();
            var seen = new HashSet<int>();

            // 1) 世界 pawn:沿用 SW 原过滤(不在地图/有 story/有名字/未挂赏)
            foreach (Pawn p in Find.WorldPawns.AllPawnsAlive)
            {
                if (p.MapHeld != null || p.story == null || p.Name == null)
                    continue;
                if (HasActiveWarrant(p) || !seen.Add(p.thingIDNumber))
                    continue;
                list.Add(p);
            }

            // 2) 地图在场 pawn:任意派系(中立/友好/敌对),排除本方殖民者与已挂赏者
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p == null || p.Dead || p.RaceProps.Animal || p.story == null || p.Name == null)
                        continue;
                    if (p.Faction == Faction.OfPlayer || p.IsPrisoner)
                        continue;
                    if (HasActiveWarrant(p) || !seen.Add(p.thingIDNumber))
                        continue;
                    list.Add(p);
                }
            }
            return list;
        }

        private static bool HasActiveWarrant(Pawn pawn)
        {
            WarrantsManager mgr = WarrantsManager.Instance;
            if (mgr == null)
                return false;
            foreach (Warrant w in mgr.createdWarrants)
                if (w.thing == pawn) return true;
            foreach (Warrant w in mgr.availableWarrants)
                if (w.thing == pawn) return true;
            return false;
        }
    }
}

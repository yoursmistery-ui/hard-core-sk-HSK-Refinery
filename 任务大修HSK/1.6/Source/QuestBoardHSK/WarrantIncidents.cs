using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 派系报复事件(2026-09-02 通缉扩展):对非敌对派系发通缉、激怒信鸽到站后按激怒值
    /// 概率排队(1.5~4 天后经 Find.Storyteller.incidentQueue 投递,IncidentDef baseChance=0
    /// 不进自然池)。激怒值经 parms.points 传入:低=使节交涉信,中=威胁信,高=直接袭击
    /// (点数随激怒放大)。
    /// </summary>
    public class IncidentWorker_RK_Retaliation : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Faction fac = parms.faction;
            Map map = parms.target as Map ?? Find.AnyPlayerHomeMap;
            int anger = Mathf.RoundToInt(parms.points);
            if (fac == null || fac.defeated)
                return false;
            if (anger < 40)
            {
                RadioNotifier.Send("RK_Bounty.RetaliationEnvoyTitle",
                    "RK_Bounty.RetaliationEnvoyText".Translate(fac.Name));
                return true;
            }
            if (anger < 80)
            {
                // NegativeEvent 不在信鸽门控范围内,直发
                Find.LetterStack.ReceiveLetter(
                    "RK_Bounty.RetaliationThreatTitle".Translate(fac.Name),
                    "RK_Bounty.RetaliationThreatText".Translate(fac.Name),
                    LetterDefOf.ThreatBig, null, fac);
                return true;
            }
            if (map == null)
                return false;
            RadioNotifier.Send("RK_Bounty.RetaliationRaidTitle",
                "RK_Bounty.RetaliationRaidText".Translate(fac.Name));
            IncidentParms raid = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            raid.faction = fac;
            raid.forced = true;
            raid.points *= 1f + anger / 100f;
            return IncidentDefOf.RaidEnemy.Worker.TryExecute(raid);
        }
    }

    /// <summary>
    /// 悬赏猎人事件(2026-09-02 通缉扩展):玩家有已接单的悬赏时,每日低频一掷概率触发
    /// (BountyRadioManager.TrySendBountyHunter 排队 0.5~1.5 天后投递)。猎人=与玩家非敌、
    /// 非目标派系的世界 NPC 派系小型访客队;若被通缉目标恰在本图,猎人会按原版敌我判定
    /// 主动与其派系接火。
    /// </summary>
    public class IncidentWorker_RK_BountyHunter : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            WarrantsManager mgr = WarrantsManager.Instance;
            Map map = parms.target as Map ?? Find.AnyPlayerHomeMap;
            if (mgr == null || map == null)
                return false;
            var taken = mgr.takenWarrants.OfType<Warrant_Pawn>()
                .Where(w => w.issuer == Faction.OfPlayer && w.Pawn != null
                    && !w.Pawn.Dead && !w.Pawn.Destroyed && !w.Pawn.RaceProps.Animal)
                .ToList();
            if (taken.Count == 0)
                return false;
            Warrant_Pawn target = taken.RandomElement();
            Faction targetFac = target.Pawn.Faction;
            Faction hunter = Find.FactionManager.AllFactions.Where(f =>
                    BountyRules.IsWorldNpcFaction(f) && f != targetFac
                    && !FactionUtility.HostileTo(f, Faction.OfPlayer))
                .RandomElementWithFallback();
            if (hunter != null)
            {
                IncidentParms vp = StorytellerUtility.DefaultParmsNow(SW_DefOf.FactionArrival, map);
                vp.faction = hunter;
                vp.forced = true;
                SW_DefOf.SW_Visitors.Worker.TryExecute(vp);
            }
            RadioNotifier.Send("RK_Bounty.HunterDispatchTitle",
                "RK_Bounty.HunterDispatchText".Translate(
                    target.thing.LabelCap,
                    hunter != null ? hunter.Name : "RK_Bounty.HunterUnknown".Translate()));
            return true;
        }
    }
}

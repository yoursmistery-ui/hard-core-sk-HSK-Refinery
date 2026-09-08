using System.Collections.Generic;
using RimWorld;
using SimpleWarrants;
using Verse;

namespace QuestBoardHSK
{
    /// <summary>
    /// 缺少通信设施提示(2026-09-03 合并):信鸽柱与通信台任一即可满足外联需求,
    /// 只要玩家家园图上建了其中一种(或已排蓝图/框架)就不再提示;两者都缺才报。
    /// 触发条件同旧两条:有玩家家园图且有已生成殖民者、轨道殖民地(自带通信)除外。
    /// 解释文案:若两者的解锁研究都还没做完,提示先研究对应科技;否则提示直接建造。
    /// </summary>
    public class Alert_MissingCommsFacility : Alert
    {
        public Alert_MissingCommsFacility()
        {
            defaultPriority = AlertPriority.Medium;
            defaultLabel = "RK_Bounty.Alert_NoCommsFacilityLabel".Translate();
            defaultExplanation = "RK_Bounty.Alert_NoCommsFacilityText".Translate();
        }

        public override TaggedString GetExplanation()
        {
            List<string> gates = new List<string>();
            string bird = BirdPostUtil.MissingBirdPostResearchLabel();
            if (!bird.NullOrEmpty())
                gates.Add(bird);
            string comms = BirdPostUtil.MissingCommsResearchLabel();
            if (!comms.NullOrEmpty() && !gates.Contains(comms))
                gates.Add(comms);
            if (gates.Count > 0)
                return "RK_Bounty.Alert_NoCommsFacilityText_Research".Translate(string.Join("、", gates));
            return defaultExplanation;
        }

        public override AlertReport GetReport()
        {
            if (Find.AnyPlayerHomeMap == null)
                return false;
            if (Utils.PlayerHomeIsOrbital())   // 轨道殖民地视作自带通信
                return false;

            bool anyColonist = false;
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome && map.mapPawns.AnyColonistSpawned)
                {
                    anyColonist = true;
                    break;
                }
            }
            if (!anyColonist)
                return false;

            // 任一设施已建/在建即视为"通信设施已就绪",不再打扰。
            if (BirdPostUtil.HasAnyCommsConsole())
                return false;
            if (BirdPostUtil.HasAnyBirdPost())
                return false;
            return true;
        }
    }
}

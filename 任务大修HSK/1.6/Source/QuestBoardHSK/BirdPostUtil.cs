using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QuestBoardHSK
{
    internal static class BirdPostUtil
    {
        /// <summary>任意玩家家地图上存在未摧毁且有饲料的信鸽柱(蓝图/框架是独立 def,天然排除)。</summary>
        public static bool AnyPlayerBirdPost()
        {
            return FindFedPost(false) != null;
        }

        /// <summary>找一根有饲料的信鸽柱;preferCurrentMap 时优先当前地图。</summary>
        public static Building FindFedPost(bool preferCurrentMap)
        {
            ThingDef def = QB_JobDefOf.RK_Bounty_BirdPost;
            if (def == null)
                return null;
            List<Map> maps = Current.Game?.Maps;
            if (maps == null)
                return null;
            Map currentMap = Find.CurrentMap;
            Building fallback = null;
            for (int i = 0; i < maps.Count; i++)
            {
                if (!maps[i].IsPlayerHome)
                    continue;
                List<Thing> things = maps[i].listerThings.ThingsOfDef(def);
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j].Destroyed)
                        continue;
                    CompRefuelable fuel = ((Building)things[j]).GetComp<CompRefuelable>();
                    if (fuel != null && !fuel.HasFuel)
                        continue;
                    if (preferCurrentMap && maps[i] == currentMap)
                        return (Building)things[j];
                    if (fallback == null)
                        fallback = (Building)things[j];
                }
            }
            return fallback;
        }

        // —— 通信台(CommsConsole)相关辅助(2026-09-03) ——
        private static ThingDef commsDef;

        /// <summary>原版通信台 def;轨道殖民地(Utils.PlayerHomeIsOrbital)视作自带通信,调用方自行短路。</summary>
        public static ThingDef CommsConsoleDef
        {
            get
            {
                if (commsDef == null)
                    commsDef = DefDatabase<ThingDef>.GetNamedSilentFail("CommsConsole");
                return commsDef;
            }
        }

        /// <summary>任意玩家家地图上已有通信台(含蓝图/框架;不看通电,电力是瞬时状态)。</summary>
        public static bool HasAnyCommsConsole()
        {
            ThingDef def = CommsConsoleDef;
            List<Map> maps = Current.Game?.Maps;
            if (def == null || maps == null)
                return false;
            for (int i = 0; i < maps.Count; i++)
            {
                if (!maps[i].IsPlayerHome)
                    continue;
                if (maps[i].listerThings.ThingsOfDef(def).Count > 0)
                    return true;
                if (HasPendingConstruction(maps[i], def))
                    return true;
            }
            return false;
        }

        /// <summary>任意玩家家地图上已有信鸽柱(含蓝图/框架;不看饲料,用于与通信台合并的"缺通信设施"提示)。</summary>
        public static bool HasAnyBirdPost()
        {
            ThingDef def = QB_JobDefOf.RK_Bounty_BirdPost;
            List<Map> maps = Current.Game?.Maps;
            if (def == null || maps == null)
                return false;
            for (int i = 0; i < maps.Count; i++)
            {
                if (!maps[i].IsPlayerHome)
                    continue;
                if (maps[i].listerThings.ThingsOfDef(def).Count > 0)
                    return true;
                if (HasPendingConstruction(maps[i], def))
                    return true;
            }
            return false;
        }

        /// <summary>通信台是否已解锁:返回首个未完成的前置研究名,全部完成(或无前置)返回 null。</summary>
        public static string MissingCommsResearchLabel()
        {
            return MissingResearchLabel(CommsConsoleDef);
        }

        /// <summary>信鸽柱是否已解锁:返回首个未完成的前置研究名,全部完成(或无前置)返回 null。</summary>
        public static string MissingBirdPostResearchLabel()
        {
            return MissingResearchLabel(QB_JobDefOf.RK_Bounty_BirdPost);
        }

        /// <summary>某建筑 def 的首个未完成前置研究名;无前置或全部已完成返回 null。</summary>
        private static string MissingResearchLabel(ThingDef def)
        {
            if (def == null || def.researchPrerequisites == null)
                return null;
            for (int i = 0; i < def.researchPrerequisites.Count; i++)
            {
                ResearchProjectDef proj = def.researchPrerequisites[i];
                if (proj != null && !proj.IsFinished)
                    return proj.LabelCap;
            }
            return null;
        }

        private static bool HasPendingConstruction(Map map, ThingDef def)
        {
            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint))
                if (t.def.entityDefToBuild == def)
                    return true;
            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame))
                if (t.def.entityDefToBuild == def)
                    return true;
            return false;
        }

        /// <summary>任意玩家家地图上存在通电可用的通讯台(空投放货的资格线)。</summary>
        public static bool HasPoweredCommsConsole()
        {
            List<Map> maps = Current.Game?.Maps;
            if (maps == null)
                return false;
            for (int i = 0; i < maps.Count; i++)
            {
                if (!maps[i].IsPlayerHome)
                    continue;
                List<Thing> things = maps[i].listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j] is Building_CommsConsole console && console.CanUseCommsNow)
                        return true;
                }
            }
            return false;
        }
    }
}

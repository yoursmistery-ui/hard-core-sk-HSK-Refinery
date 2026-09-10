using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestBoardHSK
{
    /// <summary>
    /// #2 天网(Skynet)战令：三段叙事链(侦察→入侵→终章)全部改为**事件驱动**，
    /// 骑在原生 Skynet mod 的调度上，不再自算 财富/科技/日历 门槛(方案A, 2026-09-08)。
    ///   Stage1 侦察信号 = 玩家主地图首次出现 SkynetHumanlike 单位(原生袭击/渗透落地)。
    ///   Stage2 入侵升级 = 天网单位再次现身(新批次袭击)且距上一阶≥1年。
    ///   Stage3 终章简报 = 玩家主地图出现原生 TTcounter(Salvation 倒计时启动)——直接复用原生演出。
    /// 奖励：击毁 T-X 终结者(race/kind 含 PrototypeTX) → 反抗军好感 + 天网科技残料(一次性)。
    /// 全 try/catch；未装 Skynet(派系 def 不存在) → 静默不触发。所有世界写留主线程(AGENTS 线程模型)。
    /// </summary>
    public class SkyNetCampaign : GameComponent
    {
        private const int MinStageGapDays = 80;   // 相邻叙事阶至少间隔 1 年(仅用于 1→2)

        private int stage;                 // 0未启 1侦察 2入侵 3终章
        private int lastStageDay;          // 上一阶发信的游戏日
        private int skynetSightings;       // 天网单位在玩家主地图的"现身批次"计数(false→true 跳变累加)
        private int txKills;
        private bool rewarded;
        private int lastCheckedDay = -1;   // 每天只判一次(transient，不入库)
        private bool pawnPresentLastCheck; // 上轮是否检测到天网单位(transient，用于跳变计数)

        public SkyNetCampaign(Game game) { }

        public static SkyNetCampaign Get()
        {
            return Current.Game == null ? null : Current.Game.GetComponent<SkyNetCampaign>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stage, "skynetStage", 0);
            Scribe_Values.Look(ref lastStageDay, "skynetLastStageDay", 0);
            Scribe_Values.Look(ref skynetSightings, "skynetSightings", 0);
            Scribe_Values.Look(ref txKills, "skynetTXKills", 0);
            Scribe_Values.Look(ref rewarded, "skynetRewarded", false);
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null)
                return;
            int day = Find.TickManager.TicksGame / 60000;
            if (day == lastCheckedDay)   // 每天只判一次
                return;
            lastCheckedDay = day;
            if (stage >= 3)              // 叙事链已走完，奖励由 OnTXKilled 负责
                return;
            try
            {
                TickOnce(day);
            }
            catch (Exception e) { Log.Error("[SkyNetCampaign] tick: " + e); }
        }

        private void TickOnce(int day)
        {
            Faction skynet = SkynetFaction();
            if (skynet == null)          // 未装 Skynet → 静默
                return;

            // 观测：天网单位是否现身玩家主地图；false→true 跳变记一次"批次"。
            bool present = AnyPlayerHomeMapHasPawnOf(skynet);
            if (present && !pawnPresentLastCheck)
                skynetSightings++;
            pawnPresentLastCheck = present;

            if (stage == 0)
            {
                if (skynetSightings >= 1)
                {
                    stage = 1; lastStageDay = day; TrySendStageLetter(1);
                }
                return;
            }
            if (stage == 1)
            {
                // 升级信号：又一批天网单位现身(≥2 次)，且与侦察信至少隔 1 年，避免连着刷。
                if (skynetSightings >= 2 && day >= lastStageDay + MinStageGapDays)
                {
                    stage = 2; lastStageDay = day; TrySendStageLetter(2);
                }
                return;
            }
            // stage == 2 → 等原生 Salvation 倒计时(TTcounter)落地，再发终章简报，与原生演出精确同步。
            if (AnyPlayerHomeMapHasTTcounter())
            {
                stage = 3; TrySendStageLetter(3);
            }
        }

        // 玩家主地图是否有任何 SkynetHumanlike 阵营的存活单位。
        private static bool AnyPlayerHomeMapHasPawnOf(Faction skynet)
        {
            if (Find.Maps == null)
                return false;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m == null || !m.IsPlayerHome)
                    continue;
                if (m.mapPawns.SpawnedPawnsInFaction(skynet).Count > 0)
                    return true;
            }
            return false;
        }

        // 玩家主地图是否存在原生 TTcounter(Salvation 倒计时建筑)。
        private static bool AnyPlayerHomeMapHasTTcounter()
        {
            ThingDef tt = DefDatabase<ThingDef>.GetNamedSilentFail("TTcounter");
            if (tt == null || Find.Maps == null)
                return false;
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m == null || !m.IsPlayerHome)
                    continue;
                if (m.listerThings.ThingsOfDef(tt).Count > 0)
                    return true;
            }
            return false;
        }

        private static Faction SkynetFaction()
        {
            return Find.FactionManager == null ? null : Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("SkynetHumanlike"));
        }

        private void TrySendStageLetter(int st)
        {
            try
            {
                string title, text;
                switch (st)
                {
                    case 1: title = "RK_SkyNet.Stage1Title".Translate(); text = "RK_SkyNet.Stage1Text".Translate(); break;
                    case 2: title = "RK_SkyNet.Stage2Title".Translate(); text = "RK_SkyNet.Stage2Text".Translate(); break;
                    default: title = "RK_SkyNet.Stage3Title".Translate(); text = "RK_SkyNet.Stage3Text".Translate(); break;
                }
                Find.LetterStack.ReceiveLetter(title, text,
                    st >= 2 ? LetterDefOf.ThreatBig : LetterDefOf.NegativeEvent);
            }
            catch (Exception e) { Log.Error("[SkyNetCampaign] 阶段信失败: " + e); }
        }

        // 由 Pawn.Killed postfix 调用(主线程)。击毁 T-X → 一次性奖励(不再受本地 stage 门控)。
        public void OnTXKilled(Pawn victim)
        {
            try
            {
                txKills++;
                if (!rewarded)
                    DeliverReward();
            }
            catch (Exception e) { Log.Error("[SkyNetCampaign] OnTXKilled: " + e); }
        }

        private void DeliverReward()
        {
            rewarded = true;
            Faction resistance = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("HumanResistance"));
            if (resistance != null)
                resistance.TryAffectGoodwillWith(Faction.OfPlayer, 30, true, true, null, null);

            Map home = Find.AnyPlayerHomeMap ?? Find.CurrentMap;
            int dropped = DropLoot(home);
            string text = "RK_SkyNet.RewardText".Translate(dropped);
            Find.LetterStack.ReceiveLetter("RK_SkyNet.RewardTitle".Translate(), text,
                LetterDefOf.PositiveEvent);
        }

        private int DropLoot(Map home)
        {
            if (home == null)
                return 0;
            IntVec3 c = home.Center;
            int count = 0;
            string[] defs = { "AndroidRepairKit", "NitinolAlloy", "Plasteel" };
            for (int i = 0; i < defs.Length; i++)
            {
                ThingDef d = DefDatabase<ThingDef>.GetNamedSilentFail(defs[i]);
                if (d == null)
                    continue;
                Thing t = ThingMaker.MakeThing(d, null);
                t.stackCount = Mathf.Min(d.stackLimit, defs[i] == "Plasteel" ? 120 : defs[i] == "NitinolAlloy" ? 60 : 3);
                GenPlace.TryPlaceThing(t, c, home, ThingPlaceMode.Near);
                count++;
            }
            return count;
        }
    }

    /// <summary>天网 boss 单位被杀 → 驱动结算。1.6 起 Pawn.Killed 已改名 Kill(DamageInfo?, Hediff),逐类隔离注册。</summary>
    [HarmonyPatch(typeof(Pawn), "Kill")]
    internal static class Pawn_Killed_SkyNetPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Pawn __instance)
        {
            if (!IsTerminatorBoss(__instance))
                return;
            SkyNetCampaign.Get()?.OnTXKilled(__instance);
        }

        private static bool IsTerminatorBoss(Pawn p)
        {
            if (p == null)
                return false;
            string race = p.def != null ? p.def.defName : "";
            string kind = p.kindDef != null ? p.kindDef.defName : "";
            return race.Contains("PrototypeTX") || kind.Contains("PrototypeTX");
        }
    }
}

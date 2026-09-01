using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

// ============================================================================
//  P1 · M2 探秘桌扫描链(2026-08-28) —— 复刻 MO 的 QuestFinder 玩法, 零 MO DLL
// ----------------------------------------------------------------------------
//  MO: Building_QuestScanner + CompQuestFinder + WorkGiver_OperateQuest +
//      GameComponent_QuestFinder + QuestInformation(链接件 1/5/9 解锁 1~3 级)
//  我们(有意简化, 全事件驱动、零 tick, 符合 AGENTS §9):
//    · 桌子 = 原版 Building_WorkTable + CompProperties_AffectedByFacilities + 本 comp
//    · 门槛照抄 1/5/9 件链接件 → 1/2/3 级线索
//    · 代价 = 消耗地图上已有的"残页情报"(RK_TornNote) 3/4/6 张
//    · 产出 = QuestUtility.GenerateQuestAndMakeAvailable(指定任务脚本, 点数)
//      站点地图由 Sites_RK_Ruins.xml 的 KCSG 步骤生成(废墟 + 可搜刮容器 → 蓝图书)
//    · 冷却与"越扫越深只各一次"记在桌子自身(随地图存档), 不新增 WorldComponent
//  1.6 API 备忘(本机实测): Gizmo.disabled 是 protected → 只 yield 当前可用档位;
//    TickManager.AbsTicksGame 已移除 → 用 GenTicks.TicksAbs;Command 字段是 defaultDesc;
//    ThingComp 无 ExposeData 可重写 → 用 PostExposeData。
// ============================================================================
namespace BlueprintUnlockHSK
{
    public class QuestFinderProperties : DefModExtension
    {
        public List<int> tierThreshold = new List<int> { 1, 5, 9 };   // 各级所需链接件数
        public List<string> questScripts = new List<string>();        // 各级任务脚本 defName
        public List<int> tierPoints = new List<int> { 600, 1200, 2200 };
        public List<int> noteCost = new List<int> { 3, 4, 6 };
        public int minDaysBetweenScans = 3;
    }

    public class CompProperties_QuestFinderHSK : CompProperties
    {
        public CompProperties_QuestFinderHSK()
        {
            compClass = typeof(CompQuestFinderHSK);
        }
    }

    public class CompQuestFinderHSK : ThingComp
    {
        private const int TierCount = 3;
        private int lastScanAbsDay = -9999;
        private int deepestDone;   // 已拼合过的最高档 → 防止同一档反复刷任务

        private QuestFinderProperties Props
        {
            get
            {
                QuestFinderProperties p = parent.def.GetModExtension<QuestFinderProperties>();
                if (p == null)
                {
                    Log.Warning("[BlueprintUnlockHSK] " + parent.def.defName + " 缺少 QuestFinderProperties");
                }
                return p;
            }
        }

        private static int At(List<int> list, int index, int fallback)
        {
            if (list == null || index < 0 || index >= list.Count)
            {
                return fallback;
            }
            return list[index];
        }

        private static string At(List<string> list, int index, string fallback)
        {
            if (list == null || index < 0 || index >= list.Count)
            {
                return fallback;
            }
            return list[index];
        }

        private int LinkedCount()
        {
            CompAffectedByFacilities c = parent.TryGetComp<CompAffectedByFacilities>();
            if (c == null)
            {
                return 0;
            }
            List<Thing> linked = c.LinkedFacilitiesListForReading;
            return (linked == null) ? 0 : linked.Count;
        }

        private static ThingDef NoteDef
        {
            get { return DefDatabase<ThingDef>.GetNamedSilentFail("RK_TornNote"); }
        }

        private static int NotesOnMap(Map map)
        {
            ThingDef note = NoteDef;
            if (note == null || map == null)
            {
                return 0;
            }
            List<Thing> list = map.listerThings.ThingsOfDef(note);
            if (list == null)
            {
                return 0;
            }
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && !list[i].Destroyed)
                {
                    total += list[i].stackCount;
                }
            }
            return total;
        }

        private static void ConsumeNotes(Map map, int want)
        {
            ThingDef note = NoteDef;
            if (note == null || map == null)
            {
                return;
            }
            List<Thing> list = map.listerThings.ThingsOfDef(note);
            if (list == null)
            {
                return;
            }
            for (int i = list.Count - 1; i >= 0 && want > 0; i--)
            {
                Thing t = list[i];
                if (t == null || t.Destroyed || !t.Spawned)
                {
                    continue;
                }
                int take = Math.Min(want, t.stackCount);
                Thing cut = t.SplitOff(take);
                if (cut != null)
                {
                    cut.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    t.Destroy(DestroyMode.Vanish);
                }
                want -= take;
            }
        }

        private static float NowAbsDay
        {
            get { return GenTicks.TicksAbs / 60000f; }
        }

        // 0 = 不可拼合; 1..3 = 当前可拼合档位
        private int BestTier(QuestFinderProperties p, out string whyNot)
        {
            whyNot = null;
            int links = LinkedCount();
            int tier = 0;
            for (int t = TierCount - 1; t >= 0; t--)
            {
                if (links >= At(p.tierThreshold, t, 1))
                {
                    tier = t + 1;
                    break;
                }
            }
            if (tier == 0)
            {
                whyNot = "链接件不足: 至少需要 " + At(p.tierThreshold, 0, 1) + " 件, 当前 " + links + " 件";
                return 0;
            }
            float next = lastScanAbsDay + p.minDaysBetweenScans;
            if (NowAbsDay < next)
            {
                whyNot = "线报已用尽, " + ((int)Math.Ceiling(next - NowAbsDay)) + " 天后可再次拼合";
                return 0;
            }
            int cost = At(p.noteCost, tier - 1, 3);
            int have = NotesOnMap(parent.Map);
            if (have < cost)
            {
                whyNot = "残页情报不足: 本级需 " + cost + " 张, 当前 " + have + " 张";
                return 0;
            }
            if (deepestDone >= tier)
            {
                whyNot = "第 " + tier + " 级线索已拼合过, 想拿更深的需要摆更多链接件";
                return 0;
            }
            return tier;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (parent.Map == null || !parent.Map.IsPlayerHome)
            {
                yield break;
            }
            QuestFinderProperties p = Props;
            if (p == null || p.questScripts == null || p.questScripts.Count == 0)
            {
                yield break;
            }
            string whyNot;
            int tier = BestTier(p, out whyNot);
            if (tier == 0)
            {
                yield break;
            }
            Command_Action act = new Command_Action();
            act.defaultLabel = "拼合线索 (" + tier + " 级)";
            act.defaultDesc = "把残页情报在桌上摊开拼合, 定位一处第 " + tier + " 级目标。\n消耗残页情报 " +
                              At(p.noteCost, tier - 1, 3) + " 张, 产出威胁点数约 " + At(p.tierPoints, tier - 1, 600) +
                              " 的任务线索(见任务面板)。\n两次拼合之间需间隔 " + p.minDaysBetweenScans + " 天。";
            int captured = tier;
            act.action = delegate { TryScan(p, captured); };
            yield return act;
        }

        private void TryScan(QuestFinderProperties p, int tier)
        {
            string whyNot;
            if (BestTier(p, out whyNot) != tier)
            {
                Messages.Message("无法拼合线索: " + whyNot, parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            string script = At(p.questScripts, tier - 1, null);
            QuestScriptDef qdef = script.NullOrEmpty() ? null : DefDatabase<QuestScriptDef>.GetNamedSilentFail(script);
            if (qdef == null)
            {
                Log.Warning("[BlueprintUnlockHSK] 探秘桌找不到任务脚本: " + script);
                Messages.Message("拼合失败: 指定的任务脚本不可用。", parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            ConsumeNotes(parent.Map, At(p.noteCost, tier - 1, 3));
            lastScanAbsDay = (int)NowAbsDay;
            deepestDone = Math.Max(deepestDone, tier);
            QuestUtility.GenerateQuestAndMakeAvailable(qdef, At(p.tierPoints, tier - 1, 600));
            Find.LetterStack.ReceiveLetter("拼合出新线索",
                "探秘桌上的一张地图被彻底展开: 一处第 " + tier + " 级目标的位置被标了出来, 详情见任务面板。",
                LetterDefOf.PositiveEvent, parent, null, null);
        }

        public override string CompInspectStringExtra()
        {
            QuestFinderProperties p = Props;
            if (p == null)
            {
                return null;
            }
            string whyNot;
            int tier = BestTier(p, out whyNot);
            string text = "链接件: " + LinkedCount() + " / 门槛 " + At(p.tierThreshold, 0, 1) + "-" +
                          At(p.tierThreshold, 1, 5) + "-" + At(p.tierThreshold, 2, 9) +
                          ";残页情报: " + NotesOnMap(parent.Map);
            if (tier > 0)
            {
                text += ";可拼合 " + tier + " 级线索";
            }
            else if (!whyNot.NullOrEmpty())
            {
                text += ";暂不可拼合(" + whyNot + ")";
            }
            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastScanAbsDay, "lastScanAbsDay", -9999);
            Scribe_Values.Look(ref deepestDone, "deepestDone", 0);
        }
    }
}

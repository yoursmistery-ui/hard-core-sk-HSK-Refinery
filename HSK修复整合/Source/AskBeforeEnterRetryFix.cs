// Ask Before Enter「点放行后商队不出现」修复(并入 HSK 修复整合)
//
// 问题: Mlie.AskBeforeEnter 的 Main.AskDialog「允许他们进入」回调只做了一次
//   parms.forced = true; incident.TryExecute(parms);
//   若此刻地图边缘找不到合法可达的进入格(门正关着 / 边缘被雾遮蔽 / 有敌对领主暂堵 /
//   Change Map Edge Limit 收窄了可用边缘), IncidentWorker_NeutralGroup.TryResolveParms →
//   RCellFinder.TryFindRandomPawnEntryCell 返回 false → TryExecuteWorker 直接 false。
//   说书人正常路径(MTB)会每个 interval 重新掷点隐式重试, 而 ABE 只有一次机会 →
//   商队被静默丢弃、既不报错也不出现, 表现为"点了放进来还是没进来"、偶尔触发、与存档无关。
//
// 方案: Harmony 前缀整体替换 ABE 的 Main.AskDialog(保留原有 送走来晚 选项与
//   Achievement 计数), 仅把「进入」改为: 先正常执行一次; 若失败且确实没生成任何小人
//   (用 AllPawnsCount 前后差判定, 避免卡在"已生成小人但找不到殖民地外落脚点"时的重复生成),
//   则把该事件带重试窗口重新压入 incidentQueue(forced=true 不再二次弹窗), 由说书人在
//   门开/雾散/敌对领主离开后自动再放行, 复刻原版隐式重试。未装 ABE 时 TypeByName 返回 null 自动跳过。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AskBeforeEnterRetryFix
{
    [StaticConstructorOnStartup]
    public static class AskBeforeEnterRetryFixInit
    {
        static AskBeforeEnterRetryFixInit()
        {
            try
            {
                Type mainType = AccessTools.TypeByName("AskBeforeEnter.Main");
                if (mainType == null)
                {
                    return;
                }
                MethodInfo target = AccessTools.Method(mainType, "AskDialog");
                if (target == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.askbeforeenter");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(AskBeforeEnterRetryFixInit).GetMethod(
                        "Prefix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched AskBeforeEnter.Main.AskDialog (Enter 失败后入队带重试窗口)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] AskBeforeEnter fix failed: " + e);
            }
        }

        private static bool Prefix(IncidentParms parms, IncidentWorker incident)
        {
            TaggedString text = ("ABE." + incident.def.defName).Translate();
            if (text.RawText.StartsWith("ABE."))
            {
                text = "ABE.GenericGroup".Translate();
            }

            DiaNode node = new DiaNode(text);

            node.options.Add(new DiaOption("ABE.SendAway".Translate())
            {
                action = delegate { IncRefusal(); },
                resolveTree = true
            });

            if (incident.def.defName != "TravelerGroup" && incident.def.defName != "RaidFriendly")
            {
                node.options.Add(new DiaOption("ABE.Later".Translate())
                {
                    action = delegate
                    {
                        Find.Storyteller.incidentQueue.Add(incident.def, Find.TickManager.TicksGame + 15000, parms);
                    },
                    resolveTree = true
                });
            }

            node.options.Add(new DiaOption("ABE.Enter".Translate())
            {
                action = delegate
                {
                    Map map = parms.target as Map;
                    int before = map != null ? map.mapPawns.AllPawnsCount : 0;
                    parms.forced = true;
                    bool ok = incident.TryExecute(parms);
                    int after = map != null ? map.mapPawns.AllPawnsCount : 0;
                    // 执行失败且确实没有生成任何小人 => 是"找不到进入格"的早退, 安全地重新入队重试。
                    // (若已生成小人但仍返回 false, 是"殖民地外落脚点"晚退, 重试会重复生成, 不处理。)
                    if (!ok && after <= before)
                    {
                        parms.spawnCenter = IntVec3.Invalid;
                        Find.Storyteller.incidentQueue.Add(incident.def, Find.TickManager.TicksGame + 120, parms, 45000);
                    }
                },
                resolveTree = true
            });

            Find.WindowStack.Add(new Dialog_NodeTree(node, true, true, "ABE.Approching".Translate()));
            return false;
        }

        private static void IncRefusal()
        {
            try
            {
                Type tracker = AccessTools.TypeByName("AskBeforeEnter.GameComponent_RefusalTracker");
                if (tracker == null)
                {
                    return;
                }
                GameComponent gc = Current.Game.GetComponent(tracker);
                if (gc == null)
                {
                    return;
                }
                FieldInfo field = AccessTools.Field(tracker, "guestsRefused");
                if (field == null)
                {
                    return;
                }
                field.SetValue(gc, (int)field.GetValue(gc) + 1);
            }
            catch
            {
                // Achievement 计数非关键, 失败忽略
            }
        }
    }
}

// CE WorldStrengthTracker 幽灵派系记录清理修复
//
// 问题: 存档保存时出现
//   "Object with load ID Faction_XX is referenced (xml node name: faction) but is not deep-saved."
//   根源在 CombatExtended.WorldStrengthTracker(世界强度追踪器,List<FactionStrengthTracker> trackers):
//   - Rebuild() 只移除「已战败(defeated)」的派系记录,从不移除「已从 world 消失的临时派系」记录;
//   - 任务/商队/事件动态创建的临时派系会被 GetFactionTracker 建记录,派系随后从世界移除后记录残留;
//   - 保存时该记录以 LookMode.Reference 序列化 <faction>Faction_XX</faction>,但派系本身不在
//     deep-save 列表(已不存在) → 引用悬空 → DebugLoadIDsSavingErrorsChecker 警告,
//     读档时该条记录解析失败丢失。
//   长期游戏 + 动态派系多(HSK 环境)会积累成 Faction_81~96 一串幽灵条目,每次自动保存都警告。
//
// 方案: Harmony 补丁,在 Rebuild 之后与每次保存(ExposeData, Saving 模式)之前,
//   把 trackers 里「派系为 null / 已战败 / 不在 world.factionManager.AllFactions」的记录移除。
//   与原逻辑等价(这些派系本就该没记录),不改变任何强度计算行为。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CEWorldStrengthTrackerFix
{
    [StaticConstructorOnStartup]
    public static class CEWorldStrengthTrackerFixInit
    {
        private const string HarmonyId = "local.hskcewstfix";

        static CEWorldStrengthTrackerFixInit()
        {
            try
            {
                Type trackerType = AccessTools.TypeByName("CombatExtended.WorldStrengthTracker");
                if (trackerType == null)
                {
                    return; // 未装 CE,跳过
                }
                Harmony harmony = new Harmony(HarmonyId);

                MethodInfo rebuild = AccessTools.Method(trackerType, "Rebuild");
                if (rebuild != null)
                {
                    harmony.Patch(rebuild, postfix: new HarmonyMethod(
                        typeof(CEWorldStrengthTrackerFixInit).GetMethod(
                            "RebuildPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
                    Log.Message("[CEWorldStrengthTrackerFix] patched WorldStrengthTracker.Rebuild");
                }

                MethodInfo exposeData = AccessTools.Method(trackerType, "ExposeData");
                if (exposeData != null)
                {
                    harmony.Patch(exposeData, prefix: new HarmonyMethod(
                        typeof(CEWorldStrengthTrackerFixInit).GetMethod(
                            "ExposeDataPrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                    Log.Message("[CEWorldStrengthTrackerFix] patched WorldStrengthTracker.ExposeData");
                }
            }
            catch (Exception e)
            {
                Log.Error("[CEWorldStrengthTrackerFix] patch failed: " + e);
            }
        }

        // Rebuild 完成后清一遍,快速收敛残留记录
        private static void RebuildPostfix(object __instance)
        {
            Purge(__instance);
        }

        // 保存前兜底清理,保证存档里不再出现悬空 faction 引用
        private static bool ExposeDataPrefix(object __instance)
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Purge(__instance);
            }
            return true;
        }

        private static void Purge(object instance)
        {
            try
            {
                if (instance == null || Find.World == null || Find.World.factionManager == null)
                {
                    return;
                }
                FieldInfo field = AccessTools.Field(instance.GetType(), "trackers");
                if (field == null)
                {
                    return;
                }
                IList list = field.GetValue(instance) as IList;
                if (list == null)
                {
                    return;
                }
                List<Faction> allFactions = Find.World.factionManager.AllFactions.ToList();
                PropertyInfo factionProp = null;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    object t = list[i];
                    if (t == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                    if (factionProp == null)
                    {
                        factionProp = t.GetType().GetProperty("Faction");
                        if (factionProp == null)
                        {
                            return; // 结构异常,不做处理
                        }
                    }
                    Faction fac = factionProp.GetValue(t, null) as Faction;
                    if (fac == null || fac.defeated || !allFactions.Contains(fac))
                    {
                        list.RemoveAt(i);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[CEWorldStrengthTrackerFix] purge skipped: " + e.Message);
            }
        }
    }
}

// 存档期"失效小人引用"清扫(2026-09-10)
//
// 现象(每次自动存档 3 条 Warning,调试模式开启时才打印):
//   Object with load ID Thing_HumanXXXX is referenced (xml node name: pawn / li / Pawn)
//   but is not deep-saved. This will cause errors during loading.
// 核对 Autosave-3.rws 确认三处引用者:
//   taleManager/tales/li[Tale_SinglePawn]/pawnData/pawn  → 节点 pawn
//   components[WorkTab.PriorityManager]/Priorities/keys/li → 节点 li
//   .../Priorities/values/li/Pawn                          → 节点 Pawn
// 同一个人类小人(本档里是 Villager,带 VSIE_WasPreviouslyOurEnemy 编年史)。
//
// 根因: 小人被"从世界小人池摘掉",但既没销毁也没丢弃,所以任何"只查 Destroyed"的清理都放过它。
//   1.6 里 Thing.Destroyed => mapIndexOrState == -2 || -3,而 -3(Discarded)已被它涵盖;
//   真正漏网的是 mapIndexOrState == -1(从未生成到地图)且 ParentHolder 为空、又不在 WorldPawns 三个列表里的对象
//   —— 原版 PawnGenerator 换装复用世界小人(PawnGenerator.GenerateOrRedressPawnInternal → Find.WorldPawns.RemovePawn)
//   以及各类"精简世界小人"路径都只 RemovePawn 不 Discard。
//   这种对象既不在任何地图/商队,也不在世界小人池,∴ 本轮存档不会被深存档,但引用它的地方照样把 loadID 写进档,
//   读档时解析为 null(Work Tab 读字典时还会报一次异常)。
//   Work Tab(arof.fluffy.worktab.continued)自己的 PriorityManager.ExposeData 只清 `item == null || Destroyed`,
//   ∴ 这类条目每次存档都残留。
//   编年史这边: RimWorld.TaleData_Pawn.ExposeData 写的是 `Scribe_References.Look(ref pawn, "pawn", saveDestroyedThings: true)`,
//   但文本生成(TaleData_Pawn.GetRules)只用快照字段(kind/name/faction/age...),不碰 pawn 引用,
//   ∴ 存盘前把失效引用置空是安全的,既不丢历史条目,也不影响显示文字。
//
// 方案: 两个 ExposeData 前缀,仅在 Scribe.mode == Saving 时清理失效引用(不影响读档/游戏逻辑,非热路径)。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKFixStalePawnRefs
{
    [StaticConstructorOnStartup]
    public static class StalePawnRefSaveFix
    {
        private static FieldInfo workTabPrioritiesField;

        static StalePawnRefSaveFix()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.stalepawnrefs");

                MethodInfo taleExpose = AccessTools.Method(typeof(TaleData_Pawn), "ExposeData");
                if (taleExpose != null)
                {
                    harmony.Patch(taleExpose, prefix: new HarmonyMethod(
                        typeof(StalePawnRefSaveFix).GetMethod("TalePrefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                }

                Type manager = AccessTools.TypeByName("WorkTab.PriorityManager");
                if (manager != null)
                {
                    workTabPrioritiesField = manager.GetField("priorities",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    MethodInfo managerExpose = manager.GetMethod("ExposeData");
                    if (workTabPrioritiesField != null && managerExpose != null)
                    {
                        harmony.Patch(managerExpose, prefix: new HarmonyMethod(
                            typeof(StalePawnRefSaveFix).GetMethod("WorkTabPrefix",
                                BindingFlags.Static | BindingFlags.NonPublic)));
                        // 挂载确认不打日志(用户口径:正式 DLL 零启动噪声),失败仍报 Error
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] StalePawnRefSaveFix 挂载失败: " + e);
            }
        }

        // 本轮存档会不会被深存档?不会 = 失效引用。
        // 只在存档路径调用(非热路径),所以允许读 Thing.Spawned。
        private static bool Stale(Pawn p)
        {
            if (p == null)
            {
                return true;
            }
            if (p.Destroyed) // 1.6: 含 Destroyed(-2) 与 Discarded(-3)
            {
                return true;
            }
            if (p.Spawned)
            {
                return false; // 在某张地图的 thingList 里
            }
            if (p.ParentHolder != null)
            {
                return false; // 商队/运输舱/载具/物品栏等 ThingOwner 内,随持有者深存档
            }
            return Find.WorldPawns == null || !Find.WorldPawns.Contains(p);
        }

        private static void TalePrefix(TaleData_Pawn __instance)
        {
            if (Scribe.mode != LoadSaveMode.Saving || __instance == null)
            {
                return;
            }
            if (Stale(__instance.pawn))
            {
                __instance.pawn = null;
            }
        }

        private static void WorkTabPrefix(object __instance)
        {
            if (Scribe.mode != LoadSaveMode.Saving || workTabPrioritiesField == null)
            {
                return;
            }
            try
            {
                object raw = workTabPrioritiesField.IsStatic
                    ? workTabPrioritiesField.GetValue(null)
                    : workTabPrioritiesField.GetValue(__instance);
                IDictionary dict = raw as IDictionary;
                if (dict == null)
                {
                    return;
                }
                List<object> stale = new List<object>();
                foreach (object key in dict.Keys)
                {
                    if (Stale(key as Pawn))
                    {
                        stale.Add(key);
                    }
                }
                for (int i = 0; i < stale.Count; i++)
                {
                    if (stale[i] != null)
                    {
                        dict.Remove(stale[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] Work Tab 失效小人清理失败: " + e);
            }
        }
    }
}

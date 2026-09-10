// VTE TraitsManager 失效小人引用修复
//
// 问题: Vanilla Traits Expanded 的 TraitsManager 用 Dictionary<Pawn,...>/HashSet<Pawn> 记录
// 特殊特质小人(疯狂外科医生/懦夫/大骨架/势利眼/走神等)。当小人被丢弃或销毁(开局小人重掷、
// Character Editor 重建、BetterGC 清理等)时,若 VTE 的 RemoveDestroyedPawn 没被触发,
// 字典里就残留指向已不存在小人的引用。存档时 Scribe_Collections 仍会把它们的 load ID 写入,
// 读档时这些引用解析为 null,触发:
//   Could not resolve reference to object with loadID Thing_XXX of type Verse.Pawn...
//   Null key while loading dictionary of Verse.Pawn and System.Int32. label=madSurgeonsWithLastHarvestedTick
// 纯日志报错,不影响读档,但每次读档刷屏,且字典丢失对应条目。
//
// 方案: 给 TraitsManager.ExposeData 加 Harmony 前缀,仅在 Saving 模式下把已销毁(Destroyed)
// /已丢弃(Discarded)/null 的小人从全部字典与集合中清除,防止再写入无效引用。已在旧存档中的
// 失效条目会在下次读档后(字典加载时自动跳过空键)再存档时自然清除。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace VTEStalePawnFix
{
    [StaticConstructorOnStartup]
    public static class VTEStalePawnFixInit
    {
        static VTEStalePawnFixInit()
        {
            try
            {
                Type manager = AccessTools.TypeByName("VanillaTraitsExpanded.TraitsManager");
                if (manager == null)
                {
                    return;
                }
                MethodInfo exposeData = AccessTools.Method(manager, "ExposeData");
                if (exposeData == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskvtestalereffix");
                harmony.Patch(exposeData, prefix: new HarmonyMethod(
                    typeof(VTEStalePawnFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                // 挂载确认不打日志(用户口径:正式 DLL 零启动噪声),失败仍报 Error
            }
            catch (Exception e)
            {
                Log.Error("[VTEStalePawnFix] patch failed: " + e);
            }
        }

        private static bool Prefix(object __instance)
        {
            try
            {
                if (Scribe.mode != LoadSaveMode.Saving || __instance == null)
                {
                    return true;
                }
                Type t = __instance.GetType();
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    object val = f.GetValue(__instance);
                    if (val == null)
                    {
                        continue;
                    }
                    if (val is IDictionary)
                    {
                        IDictionary dict = (IDictionary)val;
                        List<Pawn> stale = new List<Pawn>();
                        foreach (DictionaryEntry entry in dict)
                        {
                            Pawn p = entry.Key as Pawn;
                            if (p == null || p.Destroyed || p.Discarded)
                            {
                                stale.Add(p);
                            }
                        }
                        foreach (Pawn p in stale)
                        {
                            if (p != null)
                            {
                                dict.Remove(p);
                            }
                        }
                    }
                    else if (val is ISet<Pawn>)
                    {
                        ISet<Pawn> set = (ISet<Pawn>)val;
                        List<Pawn> stale = new List<Pawn>();
                        foreach (Pawn p in set)
                        {
                            if (p == null || p.Destroyed || p.Discarded)
                            {
                                stale.Add(p);
                            }
                        }
                        foreach (Pawn p in stale)
                        {
                            if (p != null)
                            {
                                set.Remove(p);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[VTEStalePawnFix] cleanup failed: " + e);
            }
            return true;
        }
    }
}

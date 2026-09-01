// NewRatkin 流浪商队修复(并入 HSK 修复整合)
//
// 一) GameComponent_WanderingCaravan.CleanupDeadPawns 空值修复
// 问题: RatkinRaceHSK 的 NewRatkin.dll 中
// NewRatkin.GameComponent_WanderingCaravan.CleanupDeadPawns 会遍历 settlerPool,
// 把"null 或已销毁或已死亡"的小人从三个集合里 Remove 掉;但清除谓词会把 null 也判为
// "要移除"(p == null || p.Destroyed || p.Dead),随后对两个 Dictionary
// (settlerRequirements / settlerAppearanceCount) 调用 Remove(null)
// → System.ArgumentNullException: Value cannot be null. Parameter name: key
// (Dictionary<K,V>.Remove 对 null 键抛异常),每次流浪商队事件触发都会刷一次错误。
//
// 方案: 前缀完全替换 CleanupDeadPawns,逻辑与原版一致,但:
//   - 对 Dictionary 用非泛型 IDictionary.Remove(object)(键非 null 时安全);
//   - 对 List 用 List.Remove(Pawn)(null 安全);
//   - 全部字段判空,避免其他异常。
// ⚠️ 2026-08-16 实测修复: settlerPool 里可能混入 null 元素(这正是原版要清理的脏数据),
//   对 null pawn 调 IDictionary.Remove(null) 同样抛 ArgumentNullException(IsCompatibleKey 检查,
//   非泛型 Remove 并非 null 键安全)——SafeRemove 对 pawn==null 直接跳过,字典键永不为 null。
// 纯防御性修复,不改任何游戏数值/玩法。
//
// 二) Lord.AddPawns 重复小人修复(2026-08-16 用户反馈
//     "Lord for 铁血鼠族 tried to add 曼珠沙华 whom it already controls.")
// 问题: NewRatkin.IncidentWorker_RatkinWanderingTrader.TryExecuteWorker 每次触发时把
//   GameComponent_WanderingCaravan 的四份持久化名单(rosterLeader / rosterGuards /
//   rosterSettlers / settlerPool,Scribe 跨存档)经 TakePawnsForSpawn 汇成 allPawns
//   传给 LordMaker.MakeNewLord → Lord.AddPawns。若同一 Pawn 实例同时出现在
//   两份名单(典型成因: 旧版 NewRatkin.dll 的 AddToRoster/AddLeader/AddGuard 写出的
//   跨名单脏数据在换版本后仍留在存档的 GameComponent 里;或读档瞬间名单与仍在场的
//   旧 Lord 交叉),allPawns 就含重复 → 新 Lord 第一次 AddPawns 时第二个重复项命中
//   原版 AddPawnInternal 的 ownedPawns.Contains(p) 分支 → 刷该错误(原版只是
//   Log.Error 后跳过,不崩溃,但事件每 45 天触发一次就刷一条)。
// 方案: Lord.AddPawns 前缀,仅当 LordJob 是 NewRatkin.LordJob_WanderingCaravan 时生效:
//   - 按引用去重(同一 Pawn 实例只保留第一个,后续重复项丢弃——正是本错误的直接成因);
//   - 跳过 null / 已销毁 / 已死亡的小人;
//   - 已在其它 Lord 名下的小人先 Notify_PawnLost(ForcedToJoinOtherLord) 释放再入列,
//     防止 "already a member of lord" 与旧 Lord 悬挂(释放失败则跳过该小人);
//   其余 Lord(原版 / 其它 mod)完全不受影响。纯防御性修复,零数值改动。

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI.Group;

namespace WanderingCaravanFix
{
    [StaticConstructorOnStartup]
    public static class WanderingCaravanFixInit
    {
        private static FieldInfo leaderField;
        private static FieldInfo guardsField;
        private static FieldInfo poolField;
        private static FieldInfo reqField;
        private static FieldInfo countField;
        private static Type lordJobType;

        static WanderingCaravanFixInit()
        {
            try
            {
                Type type = AccessTools.TypeByName("NewRatkin.GameComponent_WanderingCaravan");
                if (type == null)
                {
                    Log.Message("[HSKFix] NewRatkin.GameComponent_WanderingCaravan not found, skip");
                    return;
                }
                MethodInfo target = AccessTools.Method(type, "CleanupDeadPawns");
                if (target == null)
                {
                    Log.Message("[HSKFix] CleanupDeadPawns not found, skip");
                    return;
                }
                leaderField = AccessTools.Field(type, "rosterLeader");
                guardsField = AccessTools.Field(type, "rosterGuards");
                poolField = AccessTools.Field(type, "settlerPool");
                reqField = AccessTools.Field(type, "settlerRequirements");
                countField = AccessTools.Field(type, "settlerAppearanceCount");

                Harmony harmony = new Harmony("local.hskfixpack.wanderingcaravan");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(WanderingCaravanFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched NewRatkin.GameComponent_WanderingCaravan.CleanupDeadPawns (null-safe)");

                // 二) Lord.AddPawns 去重守卫(仅流浪商队 LordJob)
                lordJobType = AccessTools.TypeByName("NewRatkin.LordJob_WanderingCaravan");
                if (lordJobType != null)
                {
                    MethodInfo addPawns = AccessTools.Method(typeof(Lord), "AddPawns", new Type[] { typeof(IEnumerable<Pawn>), typeof(bool) });
                    if (addPawns != null)
                    {
                        harmony.Patch(addPawns,
                            prefix: new HarmonyMethod(typeof(WanderingCaravanFixInit).GetMethod("AddPawnsPrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                        Log.Message("[HSKFix] patched Verse.AI.Group.Lord.AddPawns (wandering caravan duplicate-pawn guard)");
                    }
                    else
                    {
                        Log.Warning("[HSKFix] Lord.AddPawns not found, skip duplicate-pawn guard");
                    }
                }
                else
                {
                    Log.Message("[HSKFix] NewRatkin.LordJob_WanderingCaravan not found, skip duplicate-pawn guard");
                }
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] wandering caravan fix failed: " + e);
            }
        }

        // 去重守卫: 同一 Pawn 实例只允许进新 Lord 一次;已在其它 Lord 名下的先释放。
        // 返回 true 走原方法(pawns 已被替换为清洗后的列表)。
        private static bool AddPawnsPrefix(Lord __instance, ref IEnumerable<Pawn> pawns)
        {
            try
            {
                LordJob lordJob = __instance.LordJob;
                if (lordJob == null || lordJobType == null || !lordJobType.IsInstanceOfType(lordJob))
                {
                    return true;
                }
                if (pawns == null)
                {
                    return true;
                }
                List<Pawn> result = new List<Pawn>();
                HashSet<Pawn> seen = new HashSet<Pawn>();
                int dropped = 0;
                int released = 0;
                List<string> samples = new List<string>();
                foreach (Pawn p in pawns)
                {
                    if (p == null || p.Destroyed || p.Dead)
                    {
                        dropped++;
                        continue;
                    }
                    if ((__instance.ownedPawns != null && __instance.ownedPawns.Contains(p)) || !seen.Add(p))
                    {
                        dropped++;
                        if (samples.Count < 5)
                        {
                            samples.Add(p.LabelShort);
                        }
                        continue;
                    }
                    Lord other = p.GetLord();
                    if (other != null && other != __instance)
                    {
                        try
                        {
                            other.Notify_PawnLost(p, PawnLostCondition.ForcedToJoinOtherLord, null);
                            released++;
                        }
                        catch (Exception e)
                        {
                            Log.Warning("[HSKFix] release pawn " + p.LabelShort + " from old lord failed: " + e.Message);
                            dropped++;
                            continue;
                        }
                    }
                    result.Add(p);
                }
                if (dropped > 0 || released > 0)
                {
                    pawns = result;
                    Log.Message("[HSKFix] Lord.AddPawns dedup: kept=" + result.Count + " dropped=" + dropped +
                        " released=" + released + FormatNames(samples));
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] Lord.AddPawns prefix error: " + e);
                return true;
            }
        }

        private static string FormatNames(List<string> names)
        {
            if (names == null || names.Count == 0)
            {
                return "";
            }
            return " (" + string.Join(", ", names.ToArray()) + ")";
        }

        private static bool Prefix(object __instance)
        {
            try
            {
                List<Pawn> rosterLeader = GetField<List<Pawn>>(__instance, leaderField);
                List<Pawn> rosterGuards = GetField<List<Pawn>>(__instance, guardsField);
                List<Pawn> settlerPool = GetField<List<Pawn>>(__instance, poolField);

                if (rosterLeader != null)
                {
                    rosterLeader.RemoveAll(IsDeadOrNull);
                }
                if (rosterGuards != null)
                {
                    rosterGuards.RemoveAll(IsDeadOrNull);
                }
                if (settlerPool != null)
                {
                    List<Pawn> toRemove = settlerPool.Where(IsDeadOrNull).ToList();
                    object requirements = reqField != null ? reqField.GetValue(__instance) : null;
                    object appearanceCount = countField != null ? countField.GetValue(__instance) : null;
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        Pawn pawn = toRemove[i];
                        settlerPool.Remove(pawn);
                        SafeRemove(requirements, pawn);
                        SafeRemove(appearanceCount, pawn);
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] wandering caravan cleanup prefix error: " + e);
                return true;
            }
        }

        private static bool IsDeadOrNull(Pawn pawn)
        {
            return pawn == null || pawn.Destroyed || pawn.Dead;
        }

        // Dictionary<K,V> 的泛型 Remove(null) 会抛 ArgumentNullException;非泛型
        // IDictionary.Remove(null) 同样抛(IsCompatibleKey 检查)。集合里的 null 元素只在
        // List 里有意义(前面 List.Remove(null) 已安全处理),字典键永不为 null,直接跳过。
        private static void SafeRemove(object collection, Pawn pawn)
        {
            if (collection == null || pawn == null)
            {
                return;
            }
            IDictionary dict = collection as IDictionary;
            if (dict != null)
            {
                dict.Remove(pawn);
                return;
            }
            IList list = collection as IList;
            if (list != null)
            {
                list.Remove(pawn);
            }
        }

        private static T GetField<T>(object instance, FieldInfo field)
        {
            if (field == null || instance == null)
            {
                return default(T);
            }
            return (T)field.GetValue(instance);
        }
    }
}

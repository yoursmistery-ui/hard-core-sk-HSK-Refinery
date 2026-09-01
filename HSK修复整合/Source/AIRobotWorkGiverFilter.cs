// AIRobotWorkGiverFilter.cs — 机器人工作扫描精简(2026-08-19)
//
// 背景: AIRobot(Misc Robots)的搬运/清洁机器人在 X2_JobGiver_Work 里对「全部
// WorkGiver」逐一遍历并全图扫描(反编译 AIRobot.dll 确认: TryIssueJobPackage
// 调 WorkGiver_Work.GetWorkGivers() 拿全量列表,对每个 WorkGiver 跑
// PawnCanUseWorkGiver —— 只查存活/能力/ShouldSkip,不按机器人工种过滤)。
// 原版 Core 里 Hauling 有 18 个 WorkGiver、Cleaning 有 2 个,加上 Processing/
// BiotechHauling 等,每个机器人每 tick 都扫一大堆与自己无关的 WorkGiver 和
// 对应物品 → 12 台机器人(8 搬运+4 清洁)就是 DPA 里 AIRobot.X2_JobGiver_Work
// 单次 1.66ms、累计大户的来源。
//
// 修复: 给 X2_JobGiver_Work.TryIssueJobPackage 打 Harmony 前缀,按机器人的
// workSettings.priorities(DefMap<WorkTypeDef,int>,启用工种优先级>0)过滤
// WorkGiver 列表 —— 搬运机器人只扫 Hauling/Processing/BiotechHauling 对应的
// WorkGiver,清洁机器人只扫 Cleaning 的 2 个。不改变任何工作行为(机器人本来
// 就只被允许做自己启用工种的工作),只跳过无关 WorkGiver 的扫描。
//
// 写法: 全部反射访问(DefMap 索引器、WorkGiver.def.workType),不编译期依赖
// AIRobot 或游戏内部类型名,未装 AIRobot 时 TypeByName 返回 null 自动跳过。
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AIRobotWorkGiverFilter
{
    [StaticConstructorOnStartup]
    public static class AIRobotWorkGiverFilterInit
    {
        static AIRobotWorkGiverFilterInit()
        {
            try
            {
                Type jobGiverType = AccessTools.TypeByName("AIRobot.X2_JobGiver_Work");
                if (jobGiverType == null)
                {
                    return;
                }
                MethodInfo method = AccessTools.Method(jobGiverType, "TryIssueJobPackage");
                if (method == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.airobotworkgiverfilter");
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(AIRobotWorkGiverFilterInit), "Prefix"));
                // 实际过滤点: 每个 WorkGiver 是否可用的判定
                MethodInfo canUse = AccessTools.Method(jobGiverType, "PawnCanUseWorkGiver");
                if (canUse != null)
                {
                    harmony.Patch(canUse,
                        prefix: new HarmonyMethod(typeof(AIRobotWorkGiverFilterInit), "PawnCanUseWorkGiverPrefix"));
                }
                Log.Message("[HSKFix] AIRobotWorkGiverFilter: 机器人工作扫描按启用工种过滤");
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] AIRobotWorkGiverFilter patch failed: " + e);
            }
        }

        // 返回 true 走原方法(不拦截),false 短路(给 __result 设值)。
        // 这里只做过滤准备: 把「启用工种集合」存到静态字段,原方法遍历 WorkGiver
        // 时每步都会走到 PawnCanUseWorkGiver —— 我们用前缀在 pawn 上标记,再在
        // PawnCanUseWorkGiver 前缀里做实际过滤(见下)。
        private static bool Prefix(Pawn pawn)
        {
            try
            {
                if (pawn == null || pawn.workSettings == null)
                {
                    return true;
                }
                // 读 workSettings.priorities(DefMap<WorkTypeDef,int>)
                FieldInfo prioField = AccessTools.Field(pawn.workSettings.GetType(), "priorities");
                if (prioField == null)
                {
                    return true;
                }
                object defMap = prioField.GetValue(pawn.workSettings);
                if (defMap == null)
                {
                    return true;
                }
                // 收集启用工种(defMap[wt] > 0)
                HashSet<WorkTypeDef> enabled = new HashSet<WorkTypeDef>();
                PropertyInfo itemProp = defMap.GetType().GetProperty("Item",
                    new Type[] { typeof(WorkTypeDef) });
                if (itemProp == null)
                {
                    return true;
                }
                List<WorkTypeDef> allTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading;
                for (int i = 0; i < allTypes.Count; i++)
                {
                    WorkTypeDef wt = allTypes[i];
                    if (wt == null)
                    {
                        continue;
                    }
                    try
                    {
                        object val = itemProp.GetValue(defMap, new object[] { wt });
                        if (val != null && Convert.ToInt32(val) > 0)
                        {
                            enabled.Add(wt);
                        }
                    }
                    catch
                    {
                        // 单个工作类型异常忽略
                    }
                }
                CurrentPawn = pawn;
                EnabledWorkTypes = enabled;
                return true;
            }
            catch
            {
                return true;
            }
        }

        internal static Pawn CurrentPawn;
        internal static HashSet<WorkTypeDef> EnabledWorkTypes;

        // 在 PawnCanUseWorkGiver 前缀里做实际过滤: 机器人只扫启用工种对应的 WorkGiver
        // ⚠️ 原方法签名是 (ref Pawn, WorkGiver),Harmony 前缀需用 ref 匹配
        public static bool PawnCanUseWorkGiverPrefix(ref Pawn pawn, WorkGiver giver, ref bool __result)
        {
            try
            {
                if (pawn == null || giver == null || CurrentPawn != pawn)
                {
                    return true;
                }
                if (EnabledWorkTypes == null || EnabledWorkTypes.Count == 0)
                {
                    return true;
                }
                WorkGiverDef giverDef = giver.def;
                if (giverDef == null || giverDef.workType == null)
                {
                    return true;
                }
                if (!EnabledWorkTypes.Contains(giverDef.workType))
                {
                    __result = false;
                    return false;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}

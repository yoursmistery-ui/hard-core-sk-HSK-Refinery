// STL 世界生成阶段玩家派系缺失报错修复
//
// 问题: SurvivalToolsLite 的 RightToolForJob.EquipTool 与 SurvivalToolUtility.CanUseSurvivalTools
// 都用 "pawn.Faction != Faction.OfPlayer" 判断是否为玩家派系。Faction.OfPlayer 在玩家派系
// 尚未建立时会 Log.Error("Could not find player faction.") 并返回 null。世界生成阶段
// (Page_CreateWorldParams -> WorldGenerator.GenerateWorld -> 生成派系领袖小人 ->
// 安装科技假体 -> 小人状态变化开始工作)玩家派系还没注册,于是每次生成新世界都会刷
// "Could not find player faction." 错误日志(每个带科技假体的派系领袖一次)。
//
// 方案: 给两处方法加 Harmony 前缀,当 pawn.Faction 为 null 或非玩家派系时直接跳过原方法
// (CanUseSurvivalTools 直接返回 false),完全不调用 Faction.OfPlayer。与原逻辑等价
// (原方法在该条件下本来就会 return / return false),但不触发报错,也不依赖 Find.World
// 是否存在(IsPlayer 只读 def.isPlayer,无副作用)。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace STLPlayerFactionGuard
{
    [StaticConstructorOnStartup]
    public static class STLPlayerFactionGuardInit
    {
        static STLPlayerFactionGuardInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskstlplayerfactionguard");

                Type rightToolForJob = AccessTools.TypeByName("SurvivalToolsLite.RightToolForJob");
                if (rightToolForJob != null)
                {
                    MethodInfo equipTool = AccessTools.Method(rightToolForJob, "EquipTool");
                    if (equipTool != null)
                    {
                        harmony.Patch(equipTool, prefix: new HarmonyMethod(
                            typeof(STLPlayerFactionGuardInit).GetMethod(
                                "EquipToolPrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                        Log.Message("[STLPlayerFactionGuard] patched RightToolForJob.EquipTool");
                    }
                }

                Type survivalToolUtility = AccessTools.TypeByName("SurvivalToolsLite.SurvivalToolUtility");
                if (survivalToolUtility != null)
                {
                    MethodInfo canUse = AccessTools.Method(survivalToolUtility, "CanUseSurvivalTools");
                    if (canUse != null)
                    {
                        harmony.Patch(canUse, prefix: new HarmonyMethod(
                            typeof(STLPlayerFactionGuardInit).GetMethod(
                                "CanUseSurvivalToolsPrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                        Log.Message("[STLPlayerFactionGuard] patched SurvivalToolUtility.CanUseSurvivalTools");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("[STLPlayerFactionGuard] patch failed: " + e);
            }
        }

        // EquipTool(Pawn): 非玩家派系/无派系小人原方法本来就会直接 return,这里提前跳过,避免调用 Faction.OfPlayer。
        private static bool EquipToolPrefix(Pawn pawn)
        {
            return pawn != null && pawn.Faction != null && pawn.Faction.IsPlayer;
        }

        // CanUseSurvivalTools(Pawn): 同上,非玩家派系/无派系小人直接判定为不可使用生存工具。
        private static bool CanUseSurvivalToolsPrefix(Pawn pawn, ref bool __result)
        {
            if (pawn == null || pawn.Faction == null || !pawn.Faction.IsPlayer)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}

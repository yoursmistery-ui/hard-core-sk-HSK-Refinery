// AIRobot 受损机器人可修理修复(2026-08-16;Misc Robots 自身设计缺陷)
//
// 症状: 机器人受伤但未死时完全无法修复 —— 不能医疗(机械族 pawn 无可治疗伤情)、
// 不能手术(def 无 recipes)、无自愈;右键充电站出现的"修理机器人"选项被
// AIRobot_Helper.GetFloatMenuOption4RepairStationRobot 第一道检查拒绝:
//   if (station.robot != null && !station.robotIsDestroyed)
//       return "CannotRepairRobotIsActive"("机器人活动中,不能修理")
// 即 DLL 只允许修理"已报废"的机器人,受损未死的只能带伤工作到死。
//
// 修复: Harmony prefix 整体替换该方法,删掉"机器人活动中"拦截,保留原有
// 可达性 / 手工技能(jobRepairRobotSkillMin=5) / 材料检查。材料按损伤比例结算
// (CalculateResourcesNeededForRepairingRobot: RobotParts/ComponentIndustrial × (1-生命%)),
// 修理完成走 Notify_RobotRepaired → Button_RepairDamagedRobot → 销毁旧机器人重生满血
// (保留名字/活动区)。与已报废机器人的修理路径完全同一条,零数值改动。
//
// 用法: 选中有手工≥5 的小人 → 右键充电站 → "修理机器人"。
// (损伤过轻时按比例折算材料不足 1 件,DLL 不生成选项,与原设计一致。)
//
// 门控: AccessTools.TypeByName 反射定位 AIRobot 类型,不编译期引用 AIRobot.dll;
// 未装 Misc Robots 时 TypeByName 返回 null,整体跳过零副作用。
// 日志前缀 [AIRobotRepairFix]。编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AIRobotRepairFix
{
    [StaticConstructorOnStartup]
    public static class AIRobotRepairFixInit
    {
        static AIRobotRepairFixInit()
        {
            try
            {
                Type helperType = AccessTools.TypeByName("AIRobot.AIRobot_Helper");
                if (helperType == null)
                {
                    Log.Message("[AIRobotRepairFix] AIRobot.AIRobot_Helper not found (Misc Robots absent) — skip");
                    return;
                }
                MethodInfo target = AccessTools.Method(helperType, "GetFloatMenuOption4RepairStationRobot");
                if (target == null)
                {
                    Log.Warning("[AIRobotRepairFix] GetFloatMenuOption4RepairStationRobot not found — skip");
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.airobotrepair");
                harmony.Patch(target, prefix: new HarmonyMethod(
                    AccessTools.Method(typeof(AIRobotRepairFixInit), "Prefix")));
                Log.Message("[AIRobotRepairFix] patched AIRobot_Helper.GetFloatMenuOption4RepairStationRobot (damaged-but-alive robots are now repairable at the station)");
            }
            catch (Exception e)
            {
                Log.Error("[AIRobotRepairFix] patch failed: " + e);
            }
        }

        // 返回 false = 跳过原方法,整体用本实现(原实现仅多一道"机器人活动中"拦截)
        private static bool Prefix(Pawn selPawn, object station, Dictionary<ThingDef, int> resources, ref FloatMenuOption __result)
        {
            try
            {
                if (station == null || selPawn == null || selPawn.skills == null)
                {
                    __result = null;
                    return false;
                }
                Thing stationThing = station as Thing;
                if (stationThing == null)
                {
                    __result = null;
                    return false;
                }

                // 可达性(与原方法同参数: ClosestTouch / Deadly)
                if (!ReachabilityUtility.CanReach(selPawn, stationThing, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    __result = new FloatMenuOption("CannotUseNoPath".Translate().CapitalizeFirst(), null);
                    return false;
                }

                // 手工技能门槛(读 AIRobot_Helper.jobRepairRobotSkillMin,默认 5)
                Type helperType = AccessTools.TypeByName("AIRobot.AIRobot_Helper");
                int skillMin = 5;
                FieldInfo minField = helperType == null ? null : AccessTools.Field(helperType, "jobRepairRobotSkillMin");
                if (minField != null)
                {
                    skillMin = (int)minField.GetValue(null);
                }
                if (selPawn.skills.GetSkill(SkillDefOf.Crafting).Level < skillMin)
                {
                    __result = new FloatMenuOption(
                        "SkillTooLowForConstruction".Translate(SkillDefOf.Crafting.label).Resolve()
                            + ": " + "MinSkill".Translate().Resolve() + " " + skillMin, null);
                    return false;
                }

                // 材料齐备检查(复用原静态方法,含资源统计)
                MethodInfo missingMethod = AccessTools.Method(helperType, "GetStationRepairJobMissingThingStrings");
                List<string> missing = missingMethod != null
                    ? (List<string>)missingMethod.Invoke(null, new object[] { resources, selPawn })
                    : null;
                if (missing == null || missing.Count == 0)
                {
                    Pawn pawn = selPawn;
                    object stat = station;
                    Dictionary<ThingDef, int> res = resources;
                    __result = new FloatMenuOption("AIRobot_RepairRobot".Translate().CapitalizeFirst(), delegate
                    {
                        MethodInfo start = AccessTools.Method(helperType, "StartStationRepairJob");
                        if (start != null)
                        {
                            start.Invoke(null, new object[] { pawn, stat, res });
                        }
                    });
                    return false;
                }
                string missText = "";
                foreach (string s in missing)
                {
                    missText = missText + "\n" + s;
                }
                __result = new FloatMenuOption(
                    "AIRobot_RepairRobot".Translate().CapitalizeFirst()
                        + ": " + "NotEnoughStoredLower".Translate() + missText, null);
                return false;
            }
            catch (Exception e)
            {
                Log.Error("[AIRobotRepairFix] prefix error, falling back to original: " + e);
                return true; // 出错时回退原方法行为
            }
        }
    }
}

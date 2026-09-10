using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace HSKFixAIRobot
{
    // Misc. Robots (AIRobot.dll) lets its recharge station force a GoRecharge job on a
    // robot without checking whether the robot is downed. Starting that job calls
    // Pawn_PathFollower.StartPath on a downed pawn and RimWorld logs:
    //   "<robot> tried to path while downed. This should never happen."
    // This prefix stops Button_CallBotForShutdown when the target robot is downed/dead.
    [StaticConstructorOnStartup]
    public static class AIRobotDownedRechargeFix
    {
        static AIRobotDownedRechargeFix()
        {
            try
            {
                Type stationType = AccessTools.TypeByName("AIRobot.X2_Building_AIRobotRechargeStation");
                if (stationType == null)
                {
                    return;
                }

                MethodInfo target = AccessTools.Method(stationType, "Button_CallBotForShutdown");
                if (target == null)
                {
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.airbotdownedrecharge");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(AIRobotDownedRechargeFix).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));

                Log.Message("[HSKFix] patched AIRobot recharge station: skip jobs for downed/dead robots");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] AIRobot downed recharge fix failed: " + e);
            }
        }

        private static bool Prefix(object __instance)
        {
            if (__instance == null)
            {
                return true;
            }

            Pawn robot = Traverse.Create(__instance).Field("robot").GetValue<Pawn>();
            if (robot != null && (robot.Downed || robot.Dead))
            {
                return false;
            }

            return true;
        }
    }
}

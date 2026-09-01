// AllowTool 派对狩猎空引用防护(2026-08-16,JobErrorRecover.log + Player.log 捕获)
//
// 背景: 日志尾部 "清洁工3 (Misc Robots 清洁机器人) started 10 jobs in one tick" 风暴 +
//   "JobDriver threw exception in toil MakeNewToils's initAction for pawn 清洁工3 driver=JobDriver_Wait"
//   + "An error occurred while starting an error recover job ... pawn is now jobless"。
//
// 根因(反编译 AllowTool.dll + HugsLib.dll 全链确认):
//   AllowTool.PartyHuntHandler.get_WorldSettings = AllowToolController.Instance.WorldSettings.PartyHunt,
//   WorldSettings 是 HugsLib UtilityWorldObjectManager 提供的 UtilityWorldObject,
//   只在 AllowToolController.WorldLoaded() 时才赋值(GetUtilityWorldObject<T> 会自动创建,
//   因此赋值后永不为 null)。若某个小人/机器人(清洁工3)的 JobDriver_Wait 在 WorldLoaded
//   完成之前(世界/地图载入过渡期)进入 initAction → CheckForAutoAttack →
//   AllowTool 的 DoPartyHunting postfix → DoBehaviorForPawn 首行访问
//   WorldSettings.PawnIsPartyHunting(pawn) → WorldSettings 为 null → NullReferenceException →
//   toil initAction 抛异常 → TryStartErrorRecoverJob 启动 Wait 恢复任务 → 恢复任务 initAction
//   又触发同一 NRE → 无限循环保护("started 10 jobs in one tick"),小人变无任务
//   ("pawn is now jobless")。修复前的 Player.log 因异常关闭丢栈,靠 JobErrorRecover.log 落盘抓到。
//
// 修复: Harmony 前缀挂 AllowTool.PartyHuntHandler.DoBehaviorForPawn(JobDriver_Wait),
//   AllowToolController.Instance 为 null 或 Instance.WorldSettings 为 null 时返回 false
//   跳过原方法(派对狩猎逻辑不执行,静默);WorldSettings 就绪后正常放行。
//   MayRequire=UnlimitedHugs.AllowTool 门控: 反射定位失败(未装/改名)自动跳过,零报错。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace AllowToolPartyHuntFix
{
    [StaticConstructorOnStartup]
    public static class AllowToolPartyHuntFixInit
    {
        private static PropertyInfo instanceProp;
        private static PropertyInfo worldSettingsProp;

        static AllowToolPartyHuntFixInit()
        {
            try
            {
                Type controller = AccessTools.TypeByName("AllowTool.AllowToolController");
                Type partyHunt = AccessTools.TypeByName("AllowTool.PartyHuntHandler");
                if (controller == null || partyHunt == null)
                {
                    return;
                }

                instanceProp = controller.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                worldSettingsProp = controller.GetProperty("WorldSettings", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo doBehavior = partyHunt.GetMethod("DoBehaviorForPawn", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp == null || worldSettingsProp == null || doBehavior == null)
                {
                    Log.Warning("[HSKFix] AllowTool.PartyHuntHandler.DoBehaviorForPawn / controller props not found — skip");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.allowtoolpartyhunt");
                harmony.Patch(
                    doBehavior,
                    prefix: new HarmonyMethod(
                        typeof(AllowToolPartyHuntFixInit).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));

                Log.Message("[HSKFix] patched AllowTool.PartyHuntHandler.DoBehaviorForPawn: null WorldSettings guarded (wait-job NRE storm)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] AllowTool party-hunt fix failed: " + e);
            }
        }

        private static bool Prefix()
        {
            if (instanceProp == null || worldSettingsProp == null)
            {
                return true;
            }
            try
            {
                object instance = instanceProp.GetValue(null, null);
                if (instance == null)
                {
                    return false;
                }
                return worldSettingsProp.GetValue(instance, null) != null;
            }
            catch (Exception)
            {
                // 反射异常时放行原方法,由 AllowTool 自身兜底。
                return true;
            }
        }
    }
}

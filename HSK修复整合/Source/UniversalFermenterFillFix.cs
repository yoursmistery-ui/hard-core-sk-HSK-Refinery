// Universal Fermenter 灌装任务空引用防护(UniversalFermenterFillFix, 并入 HSK 修复整合)
//
// 问题: JobErrorRecover.log 反复刷 "Exception in JobDriver tick for pawn ..."
//   System.NullReferenceException,栈在
//   UniversalFermenterSK.JobDriver_FillUF+<>c__DisplayClass7_0.<MakeNewToils>b__4()
//   的 JobDriver.TryActuallyStartNextToil → toil initAction。反编译部署版
//   UniversalFermenter_SK.dll(Core_SK 内置)确认 b__4 IL:
//
//     ldarg.0                                  // display class
//     ldfld  reservePlacedIngredient           // Toil
//     ldfld  actor                             // Toil.actor (Pawn)
//     stloc.0
//     ldloc.0
//     callvirt get_Map()                       // pawn.Map
//     ldfld  physicalInteractionReservationManager  // ← pawn.Map 为 null 时此处 NRE
//     ... Reserve(pawn, pawn.CurJob, GetTarget(...))
//
//   pawn.Map 为 null(小人不在任何地图: 跨地图传送/加载瞬间/despawn)时,
//   get_Map() 返回 null 后直接 ldfld 物理交互预留管理器 → NRE。
//   该异常让 toil 启动即崩 → Job 失败 → JobGiver 立刻重发同一任务 →
//   "started 10 jobs in one tick" 循环风暴(FillUniversalFermenter)。
//
// 方案: 前缀挂 <MakeNewToils>b__4()(display class 实例方法,无参),当
//   reservePlacedIngredient.actor 为 null 或 actor.Map 为 null 时跳过原方法
//   (Reserve 幂等,跳过无副作用,下帧 actor 有地图后正常预留)。
//   其余情况放行原方法。不改变任何正常行为。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
//   纯反射,无需引用 UniversalFermenter_SK.dll。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace UniversalFermenterFillFix
{
    [StaticConstructorOnStartup]
    public static class UniversalFermenterFillFixInit
    {
        private static FieldInfo _reservePlacedIngredientField;
        private static FieldInfo _toilActorField;

        static UniversalFermenterFillFixInit()
        {
            try
            {
                Assembly uf = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "UniversalFermenter_SK")
                    {
                        uf = asm;
                        break;
                    }
                }
                if (uf == null)
                {
                    Log.Warning("[HSKFix] UniversalFermenter_SK.dll not loaded, fill fix skipped");
                    return;
                }
                Type driverType = uf.GetType("UniversalFermenterSK.JobDriver_FillUF");
                if (driverType == null)
                {
                    Log.Warning("[HSKFix] JobDriver_FillUF not found, fill fix skipped");
                    return;
                }

                // 目标: 嵌套 display class 里的 <MakeNewToils>b__4()(Void 实例方法)
                MethodInfo target = null;
                Type displayClass = null;
                foreach (Type nested in driverType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    MethodInfo m = nested.GetMethod(
                        "<MakeNewToils>b__4",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (m != null && m.ReturnType == typeof(void))
                    {
                        target = m;
                        displayClass = nested;
                        break;
                    }
                }
                if (target == null)
                {
                    Log.Warning("[HSKFix] JobDriver_FillUF <MakeNewToils>b__4 not found, fill fix skipped");
                    return;
                }

                _reservePlacedIngredientField = AccessTools.Field(displayClass, "reservePlacedIngredient");
                if (_reservePlacedIngredientField == null)
                {
                    Log.Warning("[HSKFix] display class reservePlacedIngredient field not found, fill fix skipped");
                    return;
                }
                _toilActorField = AccessTools.Field(typeof(Toil), "actor");
                if (_toilActorField == null)
                {
                    Log.Warning("[HSKFix] Toil.actor field not found, fill fix skipped");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.universalfermenterfill");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(UniversalFermenterFillFixInit).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched JobDriver_FillUF <MakeNewToils>b__4 (null Map reserve guard)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] Universal Fermenter fill fix failed: " + e);
            }
        }

        // 前缀: actor 不在任何地图时跳过 Reserve(幂等),防止 pawn.Map 为 null
        // 时读 physicalInteractionReservationManager 空引用。
        // 返回 false = 跳过原方法; true = 放行。
        private static bool Prefix(object __instance)
        {
            try
            {
                object toil = _reservePlacedIngredientField.GetValue(__instance);
                if (toil == null)
                {
                    return true;
                }
                Pawn actor = (Pawn)_toilActorField.GetValue(toil);
                if (actor == null || actor.Map == null)
                {
                    // 小人不在任何地图: Reserve 无法执行,跳过(幂等,下帧重试)
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] Universal Fermenter fill guard failed: " + e);
            }
            return true;
        }
    }
}

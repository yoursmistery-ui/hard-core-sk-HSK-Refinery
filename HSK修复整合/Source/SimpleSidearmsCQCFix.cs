// Simple Sidearms doCQC 空引用防护(SimpleSidearmsCQCFix, 并入 HSK 修复整合)
//
// 问题: JobErrorRecover.log 反复刷 "Exception in JobDriver tick for pawn ..."
//   System.NullReferenceException,栈在
//   PeteTimesSix.SimpleSidearms.Utilities.WeaponAssingment.doCQC(Pawn, Pawn)
//   的 Intercepts.Verb_MeleeAttackCE_TryCastShot_PostFix.TryCastShot →
//   CombatExtended.Verb_MeleeAttackCE.TryCastShot。已反编译部署版
//   SimpleSidearms.dll 并对照官方源码(WeaponAssingment.cs:483):
//
//     if (Settings.CQCTargetOnly == true && attacker != pawn.mindState.lastAttackedTarget.Thing)
//
//   pawn.mindState.lastAttackedTarget 是 LocalTargetInfo 值类型(非 nullable),
//   当小人没有任何攻击目标历史时其 IsValid == false、Thing 属性为 null——
//   原版此场景等价于「没有目标」,应跳过而非解引用。官方代码直接 .Thing,
//   若此时 attacker 非 null(被 CE 近战反打、目标已死/丢失等)即 NRE。
//   CE + 鼠族工具(近战)混战中稳定触发。
//
// 方案: 前缀补丁挂 WeaponAssingment.doCQC,当
//   CQCTargetOnly==true 且 lastAttackedTarget 无效(Thing==null)时,
//   CQCTargetOnly 分支本应跳过(attacker != null 恒成立 → 与「无目标」等义
//   return),直接返回 false 跳过原方法——等效于原代码想表达的「没记住目标就
//   不强行换目标」;其余情况放行原方法。不改变任何正常行为。
//
//   ⚠️ doCQC 是 internal static,签名 Void doCQC(Verse.Pawn, Verse.Pawn),
//   参数名 pawn/attacker 必须与原方法一致(Harmony 按名匹配)。
//   前缀返回 bool 跳过原方法即可,无需触碰 __result。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
//   需引用 SimpleSidearms.dll(Mods/Simple Sidearms SK/Assemblies)。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleSidearmsCQCFix
{
    [StaticConstructorOnStartup]
    public static class SimpleSidearmsCQCFixInit
    {
        static SimpleSidearmsCQCFixInit()
        {
            try
            {
                Assembly ss = FindSimpleSidearmsAssembly();
                if (ss == null)
                {
                    Log.Warning("[HSKFix] SimpleSidearms.dll not loaded, CQC fix skipped");
                    return;
                }
                Type type = ss.GetType("PeteTimesSix.SimpleSidearms.Utilities.WeaponAssingment");
                if (type == null)
                {
                    Log.Warning("[HSKFix] WeaponAssingment type not found, CQC fix skipped");
                    return;
                }
                MethodInfo target = type.GetMethod(
                    "doCQC",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (target == null)
                {
                    Log.Warning("[HSKFix] WeaponAssingment.doCQC not found, CQC fix skipped");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.simplesidearmscqc");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(SimpleSidearmsCQCFixInit).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched SimpleSidearms WeaponAssingment.doCQC (null lastAttackedTarget guard)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] SimpleSidearms CQC fix failed: " + e);
            }
        }

        // 前缀: pawn 状态异常(死亡/despawn 时 mindState/jobs 可能被清理)时
        // 跳过 doCQC,防止 pawn.mindState.lastAttackedTarget.Thing /
        // pawn.mindState.enemyTarget / pawn.CurJobDef 在 null 上读字段 → NRE。
        // 返回 false = 跳过原方法; true = 放行。
        private static bool Prefix(Pawn pawn, Pawn attacker)
        {
            try
            {
                if (pawn == null || attacker == null)
                {
                    return true; // 原方法自带 null 检查,放行
                }
                if (pawn.mindState == null || pawn.jobs == null)
                {
                    // 小人状态异常: doCQC 内部多处读 mindState/jobs 字段会 NRE,跳过
                    return false;
                }
                // CQCTargetOnly 开启且无攻击目标历史时,原代码
                // `attacker != pawn.mindState.lastAttackedTarget.Thing` 中
                // .Thing 为 null(无目标),语义是「无目标不换向」,提前跳过等效
                if (SimpleSidearms_Settings_CQCTargetOnly() && pawn.mindState.lastAttackedTarget.Thing == null)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] SimpleSidearms CQC guard failed: " + e);
            }
            return true;
        }

        // 反射读 SimpleSidearms_Settings.CQCTargetOnly(仅当 CQCTargetOnly 开启时
        // 该分支才有空引用风险;未开启时放行,避免无谓开销)
        private static bool SimpleSidearms_Settings_CQCTargetOnly()
        {
            try
            {
                Assembly ss = FindSimpleSidearmsAssembly();
                if (ss == null)
                {
                    return true; // 未知时保守放行(防护逻辑本身无害)
                }
                object inst = GetSettingsInstance(ss);
                if (inst == null)
                {
                    return true;
                }
                // ⚠️ 2026-08-19 dnfile 确认: CQCTargetOnly 在本 SK 版是【实例字段】
                // (字段表有、方法表无 get_CQCTargetOnly),属性/字段双查兼容旧版属性写法
                PropertyInfo prop = AccessTools.Property(inst.GetType(), "CQCTargetOnly");
                if (prop != null)
                {
                    object val = prop.GetValue(inst, null);
                    return val is bool && (bool)val;
                }
                FieldInfo field = AccessTools.Field(inst.GetType(), "CQCTargetOnly");
                if (field != null)
                {
                    object val = field.GetValue(inst);
                    return val is bool && (bool)val;
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] SimpleSidearms settings read failed: " + e);
                return true;
            }
        }

        // 设置实例 = 主类 SimpleSidearms.Settings 静态属性。
        // ⚠️ 2026-08-19 dnfile 确认(元数据铁证): 主类
        //   PeteTimesSix.SimpleSidearms.SimpleSidearms 有 get_Settings/set_Settings +
        //   <Settings>k__BackingField;SimpleSidearms_Settings 类自身【没有】Settings 属性。
        //   此前代码查 SimpleSidearms_Settings.Settings → AccessTools.Property 返回 null
        //   → null.GetValue → NRE(日志 "[HSKFix] SimpleSidearms settings read failed: NRE")。
        private static object GetSettingsInstance(Assembly ss)
        {
            Type modClass = ss.GetType("PeteTimesSix.SimpleSidearms.SimpleSidearms");
            if (modClass != null)
            {
                PropertyInfo prop = AccessTools.Property(modClass, "Settings");
                if (prop != null)
                {
                    return prop.GetValue(null, null);
                }
                // 主类字段回退(含自动属性后备字段)
                FieldInfo field = AccessTools.Field(modClass, "Settings");
                if (field == null)
                {
                    field = AccessTools.Field(modClass, "<Settings>k__BackingField");
                }
                if (field != null)
                {
                    return field.GetValue(null);
                }
            }
            // 兼容旧版: 设置类自身静态 Settings
            Type settingsType = ss.GetType("PeteTimesSix.SimpleSidearms.SimpleSidearms_Settings");
            if (settingsType != null)
            {
                PropertyInfo prop2 = AccessTools.Property(settingsType, "Settings");
                if (prop2 != null)
                {
                    return prop2.GetValue(null, null);
                }
            }
            return null;
        }

        private static Assembly FindSimpleSidearmsAssembly()
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "SimpleSidearms")
                {
                    return asm;
                }
            }
            return null;
        }
    }
}

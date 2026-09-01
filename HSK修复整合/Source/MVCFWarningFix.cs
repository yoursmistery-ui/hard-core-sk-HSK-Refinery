// MVCF 警告修复 (HSK / CE 兼容)
//
// 问题: MVCF(Multi Verb Combat Framework,随 Vanilla Expanded Framework 加载)在装备转移时
// 会遍历 CompEquippable 的全部 verb 并调用 VerbManager.RemoveVerb。CE 的近战/射击 verb
// (Verb_MeleeAttackCE / Verb_ShootCE)从未被 MVCF 注册,导致反复刷日志警告:
//   [MVCF] Not found: CombatExtended.Verb_MeleeAttackCE(鹤嘴锄/CompEquippable_..._Poke)
// SurvivalToolsLite 自动换工具(Pawn_EquipmentTracker.TryTransferEquipmentToContainer)时频繁触发。
//
// 方案: 给 MVCF.VerbManager.RemoveVerb 加前缀补丁。verb 确实在 MVCF 注册表时走原逻辑;
// 未注册的 verb(CE 普通装备 verb)直接跳过原方法 —— 与原方法"警告后直接返回"行为一致,
// 但不再刷警告。纯日志修复,不影响任何 MVCF/CE 功能。
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MVCFWarningFix
{
    [StaticConstructorOnStartup]
    public static class MVCFWarningFixInit
    {
        static MVCFWarningFixInit()
        {
            try
            {
                Type verbManager = AccessTools.TypeByName("MVCF.VerbManager");
                if (verbManager == null)
                {
                    return;
                }
                MethodInfo removeVerb = AccessTools.Method(verbManager, "RemoveVerb");
                if (removeVerb == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskmvcfverbfix");
                harmony.Patch(
                    removeVerb,
                    prefix: new HarmonyMethod(
                        typeof(MVCFWarningFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[MVCFWarningFix] patched MVCF.VerbManager.RemoveVerb");
            }
            catch (Exception e)
            {
                Log.Error("[MVCFWarningFix] patch failed: " + e);
            }
        }

        private static bool Prefix(object __instance, Verb verb)
        {
            if (verb == null || __instance == null)
            {
                return true;
            }
            FieldInfo verbsField = AccessTools.Field(__instance.GetType(), "verbs");
            if (verbsField == null)
            {
                return true;
            }
            IEnumerable list = verbsField.GetValue(__instance) as IEnumerable;
            if (list == null)
            {
                return true;
            }
            foreach (object mv in list)
            {
                if (mv == null)
                {
                    continue;
                }
                FieldInfo fi = AccessTools.Field(mv.GetType(), "Verb");
                PropertyInfo pi = fi == null ? AccessTools.Property(mv.GetType(), "Verb") : null;
                if (fi == null && pi == null)
                {
                    continue;
                }
                object v = fi != null ? fi.GetValue(mv) : pi.GetValue(mv, null);
                if (ReferenceEquals(v, verb))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

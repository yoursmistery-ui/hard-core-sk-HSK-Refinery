using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FacilityCrashFix
{
    // 仅禁止生育/怀孕(仅 Biotech DLC 启用时生效):
    // 怀孕相关概率全部为 0(自然怀孕 / 伴侣怀孕 / IVF 胚胎植入)。
    // 儿童相关内容(允许儿童、儿童生成、年龄成长)保持原版,不做任何改动。
    [StaticConstructorOnStartup]
    public static class NoChildrenInit
    {
        static NoChildrenInit()
        {
            try
            {
                if (!ModsConfig.BiotechActive)
                {
                    return;
                }

                Harmony harmony = new Harmony("local.facilitycrashfix.nochildren");

                string[] pregnancyMethods =
                {
                    "PregnancyChanceForPawn",
                    "PregnancyChanceForPartners",
                    "PregnancyChanceForWoman",
                    "PregnancyChanceImplantEmbryo"
                };
                foreach (string methodName in pregnancyMethods)
                {
                    MethodInfo method = AccessTools.Method(typeof(PregnancyUtility), methodName);
                    if (method != null)
                    {
                        harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(typeof(NoChildrenInit).GetMethod(
                                "ZeroPregnancyChancePrefix",
                                BindingFlags.Static | BindingFlags.NonPublic)));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("[FacilityCrashFix] 禁生育补丁应用失败: " + e);
            }
        }

        private static bool ZeroPregnancyChancePrefix(ref float __result)
        {
            __result = 0f;
            return false;
        }
    }
}

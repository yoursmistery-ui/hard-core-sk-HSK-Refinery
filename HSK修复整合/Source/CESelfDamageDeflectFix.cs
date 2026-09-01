// CE 无武器自我伤害偏转警告静默修复
//
// 问题: 日志反复出现(每触发一次自杀/自残伤害打一条)
//   "[CE] Deflection for Instigator:XXX Target: DamageDef:Scratch Weapon: has null verb, overriding AP."
//   触发链: 第三方疾病类 mod(Cybranian - Diseases+ 的 DimonSever000_Suicide 工作)用
//   pawn.TakeDamage(new DamageInfo(DamageDefOf.Scratch, ..., pawn, null, ...)) 造成无武器自我伤害;
//   CombatExtended.ArmorUtilityCE.GetAfterArmorDamage 的官方跳过条件是
//   「Weapon==null && Instigator==null」才直接返回原伤害,而自杀伤害 Instigator==自己,
//   不满足跳过 → 进入护甲偏转流程 → Scratch(默认 AP=0)遇任意护甲>0 即判偏转 →
//   GetDeflectDamageInfo 中 LastAttackVerb==null && def.defaultArmorPenetration==0 →
//   打上述 Warning 并硬编码钝击 AP=50(偏转后伤害被折算成 ~7.9 钝击再过甲,数值失控)。
//
// 方案: Harmony Prefix 补丁 GetAfterArmorDamage,把「无武器 + 自己打自己」的伤害
// 与 CE 对 Weapon==null && Instigator==null 伤害的处理对齐 —— 直接返回原伤害、跳过偏转。
// 行为等价且更合理: 自我伤害本就不该被自己的护甲偏转成钝击;同时消除日志噪音。
// 仅拦截 Instigator==pawn 且 Weapon==null 的场景,普通近战(LastAttackVerb)不受影响,
// 陷阱/环境伤害(Instigator==null)原本就命中 CE 官方跳过分支,也不受影响。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace CENullWeaponDeflectFix
{
    [StaticConstructorOnStartup]
    public static class CENullWeaponDeflectFixInit
    {
        private const string HarmonyId = "local.hskcendf";

        static CENullWeaponDeflectFixInit()
        {
            try
            {
                Type armorUtil = AccessTools.TypeByName("CombatExtended.ArmorUtilityCE");
                if (armorUtil == null)
                {
                    return; // 未装 CE,跳过
                }
                MethodInfo method = AccessTools.Method(armorUtil, "GetAfterArmorDamage");
                if (method == null)
                {
                    return;
                }
                Harmony harmony = new Harmony(HarmonyId);
                harmony.Patch(method, prefix: new HarmonyMethod(
                    typeof(CENullWeaponDeflectFixInit).GetMethod(
                        "GetAfterArmorDamagePrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[CENullWeaponDeflectFix] patched ArmorUtilityCE.GetAfterArmorDamage");
            }
            catch (Exception e)
            {
                Log.Error("[CENullWeaponDeflectFix] patch failed: " + e);
            }
        }

        private static bool GetAfterArmorDamagePrefix(
            ref DamageInfo __result,
            DamageInfo originalDinfo,
            Pawn pawn,
            BodyPartRecord hitPart,
            out bool armorDeflected,
            out bool armorReduced,
            out bool shieldAbsorbed)
        {
            armorDeflected = false;
            armorReduced = false;
            shieldAbsorbed = false;

            // 无武器的「自己打自己」伤害(Diseases 自杀、自残类 mod 等):
            // 直接返回原伤害,与 CE 官方对 Weapon==null && Instigator==null 伤害的跳过处理一致。
            if (originalDinfo.Weapon == null
                && originalDinfo.Instigator != null
                && originalDinfo.Instigator == pawn)
            {
                __result = originalDinfo;
                return false;
            }
            return true;
        }
    }
}

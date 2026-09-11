using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RatkinUnderground;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.GetGizmos))]
public static class EquipmentTracker_Patch
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_ApparelTracker __instance)
    {
        if (__result != null)
        {
            foreach (var gizmo in __result)
            {
                if (gizmo != null) yield return gizmo;
            }
        }

        if (__instance?.WornApparel != null)
        {
            foreach (var apparel in __instance.WornApparel)
            {
                if (apparel?.def?.defName != null && apparel.def.defName.StartsWith("RKU_"))
                {
                    if (apparel.AllComps != null)
                    {
                        foreach (var comp in apparel.AllComps)
                        {
                            if (comp != null && comp.GetType() != typeof(CompForbiddable))
                            {
                                var extraGizmos = comp.CompGetGizmosExtra();
                                if (extraGizmos != null)
                                {
                                    foreach (var extra in extraGizmos)
                                    {
                                        if (extra != null) yield return extra;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

// 这个是把武器上定义的gizmo返回到pawn身上的
[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos))]
public static class EquipmentTracker_WeaponGizmosPatch
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn_EquipmentTracker __instance)
    {
        if (__result != null)
        {
            foreach (var gizmo in __result)
            {
                if (gizmo != null) yield return gizmo;
            }
        }

        if (__instance?.AllEquipmentListForReading != null)
        {
            foreach (var equip in __instance.AllEquipmentListForReading)
            {
                if (equip?.def?.defName != null && equip.def.defName.StartsWith("RKU_"))
                {
                    if (equip.AllComps != null)
                    {
                        foreach (var comp in equip.AllComps)
                        {
                            if (comp != null && comp.GetType() != typeof(CompForbiddable))
                            {
                                var extraGizmos = comp.CompGetGizmosExtra();
                                if (extraGizmos != null)
                                {
                                    foreach (var extra in extraGizmos)
                                    {
                                        if (extra != null) yield return extra;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

// 禁用武器在过热期间的攻击，并在连续射击期间绕过冷却
[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Verb), nameof(Verb.Available))]
public static class Verb_Available_Patch
{
    public static void Postfix(Verb __instance, ref bool __result)
    {
        if (!(__instance is Verb_Shoot)) return;

        if (__instance.caster is Pawn pawn && pawn.equipment != null && pawn.equipment.Primary != null)
        {
            var compEquippable = pawn.equipment.Primary.TryGetComp<CompEquippable>();
            if (compEquippable != null && compEquippable.PrimaryVerb == __instance)
            {
                var burstFireComp = pawn.equipment.Primary.TryGetComp<Comp_RKU_BurstFire>();
                if (burstFireComp != null)
                {
                    // 优先检查：如果武器过热，强制禁用攻击（无论其他条件）
                    if (burstFireComp.IsWeaponDisabled)
                    {
                        __result = false;
                        return;
                    }
                    
                    // 如果正在连续射击，绕过冷却检查（允许连续射击）
                    // 但前提是武器没有过热
                    if (burstFireComp.IsBurstFiring && !__result)
                    {
                        __result = true;
                    }
                }
            }
        }
    }
}

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Verb), "WarmupTicksLeft", MethodType.Getter)]
public static class Verb_WarmupTicksLeft_Patch
{
    public static void Postfix(Verb __instance, ref int __result)
    {
        if (!(__instance is Verb_Shoot)) return;

        if (__instance.caster is Pawn pawn && pawn.equipment != null && pawn.equipment.Primary != null)
        {
            var compEquippable = pawn.equipment.Primary.TryGetComp<CompEquippable>();
            if (compEquippable != null && compEquippable.PrimaryVerb == __instance)
            {
                var burstFireComp = pawn.equipment.Primary.TryGetComp<Comp_RKU_BurstFire>();
                if (burstFireComp != null && burstFireComp.IsBurstFiring)
                {
                    __result = 0;
                }
            }
        }
    }
}

// 如果机械体装备的主武器是钢铁星机炮则不报缺少武器
[StaticConstructorOnStartup]
[HarmonyPatch(typeof(MechRepairUtility), nameof(MechRepairUtility.IsMissingWeapon))]
public static class MechRepairUtility_IsMissingWeapon_Patch
{
    public static bool Prefix(Pawn mech, ref bool __result)
    {
        if (mech.equipment != null && mech.equipment.Primary != null && 
            mech.equipment.Primary.def.defName == "RKU_IronStarCannon")
        {
            __result = false;
            return false; 
        }
        return true; 
    }
}


using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MihoHSK.TurretAimFix
{
    /// <summary>
    /// Prevents CE turrets from immediately restarting their entire warmup when
    /// TryStartCastOn would reject a target at the end of the aiming period.
    ///
    /// Building_TurretGunCE.BeginBurst ignores TryStartCastOn's return value.
    /// A target that becomes unhittable while the turret is aiming can therefore
    /// leave the turret repeatedly warming up without ever firing or cooling down.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class TurretAimLoopFix
    {
        private const int RetryCooldownTicks = 60;
        private const string HarmonyId = "MihoHSK.TurretAimLoopFix";

        private static readonly MethodInfo ResetCurrentTargetMethod;
        private static readonly FieldInfo BurstCooldownTicksLeftField;
        private static readonly FieldInfo IsAimingField;

        static TurretAimLoopFix()
        {
            try
            {
                Type turretType = AccessTools.TypeByName("CombatExtended.Building_TurretGunCE");
                Type shootVerbType = AccessTools.TypeByName("CombatExtended.Verb_ShootCE");
                if (turretType == null || shootVerbType == null)
                {
                    return;
                }

                MethodInfo beginBurst = AccessTools.Method(turretType, "BeginBurst");
                ResetCurrentTargetMethod = AccessTools.Method(turretType, "ResetCurrentTarget");
                BurstCooldownTicksLeftField = AccessTools.Field(turretType, "burstCooldownTicksLeft");
                IsAimingField = AccessTools.Field(shootVerbType, "_isAiming");

                if (beginBurst == null || ResetCurrentTargetMethod == null ||
                    BurstCooldownTicksLeftField == null || IsAimingField == null)
                {
                    Log.Error("[Miho HSK] Could not install the CE turret aiming-loop fix because the expected CE members were not found.");
                    return;
                }

                Harmony harmony = new Harmony(HarmonyId);
                harmony.Patch(
                    beginBurst,
                    prefix: new HarmonyMethod(typeof(TurretAimLoopFix), "BeginBurstPrefix"));
            }
            catch (Exception exception)
            {
                Log.Error("[Miho HSK] Failed to install the CE turret aiming-loop fix: " + exception);
            }
        }

        private static bool BeginBurstPrefix(object __instance)
        {
            Building_Turret turret = __instance as Building_Turret;
            if (turret == null || turret.def == null ||
                !turret.def.defName.StartsWith("Miho_", StringComparison.Ordinal))
            {
                return true;
            }

            Verb attackVerb = turret.AttackVerb;
            LocalTargetInfo target = turret.CurrentTarget;
            try
            {
                if (attackVerb != null && target.IsValid && attackVerb.CanHitTarget(target))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[Miho HSK] CE turret target validation failed; falling back to CE's original BeginBurst: " + exception,
                    1947310461);
                return true;
            }

            try
            {
                if (attackVerb != null && IsAimingField.DeclaringType.IsAssignableFrom(attackVerb.GetType()))
                {
                    IsAimingField.SetValue(attackVerb, false);
                }

                ResetCurrentTargetMethod.Invoke(__instance, null);

                int currentCooldown = (int)BurstCooldownTicksLeftField.GetValue(__instance);
                if (currentCooldown < RetryCooldownTicks)
                {
                    BurstCooldownTicksLeftField.SetValue(__instance, RetryCooldownTicks);
                }
            }
            catch (Exception exception)
            {
                Log.ErrorOnce(
                    "[Miho HSK] Failed to recover a CE turret from an invalid aim state: " + exception,
                    1947310462);
            }

            return false;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AM.CleanFeatures
{
    // 近战动画HSK 功能清理补丁(2026-09-07)。套索/处决/决斗功能已整体移除, 但它们的
    // 逻辑编译在 zAnimationMod.dll 里删不掉: (1) Mod 设置仍按 [Header] 反射出 Lasso /
    // Executions & Duels 两个 Tab 及其字段; (2) JobDriver_GrapplePawn 仍在。
    // 本补丁纯反射(不编译期依赖 AM 内部类型), 做两件事:
    //   A. SimpleSettings.GetHolder 后裁剪成员表 → 那两组 Tab 与字段从设置界面彻底消失;
    //   B. JobDriver_GrapplePawn.GiveJob 前缀 return false → 兜底杜绝任何套索抓取。
    [StaticConstructorOnStartup]
    public static class CleanFeaturesPatch
    {
        private static readonly HashSet<string> HiddenFields = new HashSet<string>(StringComparer.Ordinal)
        {
            // —— Lasso ——
            "AutoGrapple", "GrappleAttemptMTBSeconds", "LassoSpawnChance", "EnemiesCanGrapple",
            "GrappleAttemptMTBSecondsEnemy", "MinMeleeSkillToLasso", "MinManipulationToLasso",
            "MaxLassoMass", "MaxLassoBodySize", "GrappleSpeed", "MaxFillPctForLasso",
            // —— Executions & Duels ——
            "EnableExecutions", "AutoExecute", "EnemiesCanExecute", "ChanceToFailMinSkill",
            "ChanceToFailMaxSkill", "ExecuteAttemptMTBSeconds", "ExecuteAttemptMTBSecondsEnemy",
            "AnimalsCanBeExecuted", "ExecutionLethalityModifier", "ExecutionsOnFriendliesAreNotLethal",
            "ExecutionArmorCoefficient", "ExecutionsCanDestroyBodyParts", "MeleeSkillExecCooldownFactor",
            "FriendlyExecCooldownFactor", "EnemyExecCooldownFactor", "MinDuelDuration",
            "MaxDuelDuration", "FriendlyDuelCooldown",
            // —— 散落在 Visuals/Other 里的处决/决斗残留 ——
            "ShowExecutionMotes", "FriendlyPawnLethalityBonus", "FriendlyPawnDuelBonus",
            "WarnOfFriendlyExecution", "DuelVolumePct",
            // —— 特殊技能(Unique Skills, 功能已整体移除) ——
            "EnableUniqueSkills", "SkillCooldownFactor",
        };

        static CleanFeaturesPatch()
        {
            try
            {
                Harmony h = new Harmony("local.ratkin.am.cleanfeatures");

                // A. 裁剪设置成员表(连 Tab 一起消失)。GetHolder 是 private static。
                MethodInfo getHolder = AccessTools.Method("AM.AMSettings.SimpleSettings:GetHolder");
                if (getHolder != null)
                {
                    h.Patch(getHolder, postfix: new HarmonyMethod(
                        AccessTools.Method(typeof(CleanFeaturesPatch), "GetHolderPostfix")));
                    Log.Message("[AM.CleanFeatures] 设置裁剪已挂载 (GetHolder)");
                }
                else
                {
                    Log.Warning("[AM.CleanFeatures] 未找到 SimpleSettings.GetHolder, 设置裁剪跳过");
                }

                // B. 兜底禁套索抓取。
                MethodInfo giveJob = AccessTools.Method(
                    "AM.Grappling.JobDriver_GrapplePawn:GiveJob");
                if (giveJob != null)
                {
                    h.Patch(giveJob, prefix: new HarmonyMethod(
                        AccessTools.Method(typeof(CleanFeaturesPatch), "NoGrapple")));
                    Log.Message("[AM.CleanFeatures] 套索 GiveJob 已禁用");
                }
                else
                {
                    Log.Warning("[AM.CleanFeatures] 未找到 JobDriver_GrapplePawn.GiveJob");
                }

                // (2026-09-08) 原 C / C2 段是定位"待机双武器"的临时探针([CFP] smyh /
                // vanilla-aim / am-supp); 真凶=SYS 的背负/扛武器绘制(武器 def 上的 SYS comp),
                // 已通过 XML 全量摘除根治, 探针整段删除。

                // D. AM 正在动画该小人时让 SMYH 手部绘制让位(避免 SMYH 的手与 AM 手/武器动画叠摆)。
                //    注: 2026-09-08 实测 SMYH 只画手不画武器本体; "待机两把武器"真凶是 SYS 的
                //    背负/扛武器绘制(已 XML 摘除 SYS comp 根治), 与 SMYH 无关。此让位保留以兼容手部姿态。
                //    挂 SMYH HandDrawer.DrawHandsOnWeapon(Pawn)。
                ResolveAm();
                if (smthDrawHands != null)
                {
                    h.Patch(smthDrawHands, prefix: new HarmonyMethod(
                        AccessTools.Method(typeof(CleanFeaturesPatch), "SmyhYieldToAm")));
                    Log.Message("[AM.CleanFeatures] SMYH→AM 让位已挂载 (amReflect=" + amReady + ")");
                }
            }
            catch (Exception e)
            {
                Log.Error("[AM.CleanFeatures] 挂载失败: " + e);
            }
        }

        // ===== SMYH 让位 AM 动画 =====
        private static MethodInfo smthDrawHands;   // HandDrawer.DrawHandsOnWeapon(Pawn)
        private static Type amCompType;            // AM.Idle.IdleControllerComp
        private static FieldInfo amCurAnim;        // private currentAnimation
        private static FieldInfo amWantsVanilla;   // private wantsVanillaDrawThisFrame
        private static PropertyInfo amDestroyed;   // AnimRenderer.IsDestroyed
        private static bool amReady;

        private static void ResolveAm()
        {
            Type hd = AccessTools.TypeByName("ShowMeYourHands.HandDrawer");
            if (hd != null)
                foreach (MethodInfo m in AccessTools.GetDeclaredMethods(hd))
                    if (m.Name == "DrawHandsOnWeapon" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType.FullName == "Verse.Pawn")
                    { smthDrawHands = m; break; }

            amCompType = AccessTools.TypeByName("AM.Idle.IdleControllerComp");
            if (amCompType == null) return;
            amCurAnim = AccessTools.Field(amCompType, "currentAnimation");
            amWantsVanilla = AccessTools.Field(amCompType, "wantsVanillaDrawThisFrame");
            amDestroyed = AccessTools.Property(AccessTools.TypeByName("AM.AnimRenderer"), "IsDestroyed");
            amReady = amCurAnim != null && amWantsVanilla != null;
        }

        // AM 本帧是否在画该小人的武器网格: currentAnimation 存活 && !wantsVanillaDrawThisFrame。
        private static bool AmDrawing(Verse.Pawn pawn)
        {
            if (!amReady || pawn == null) return false;
            List<ThingComp> comps = pawn.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] == null || comps[i].GetType() != amCompType) continue;
                object anim = amCurAnim.GetValue(comps[i]);
                if (anim == null) return false;
                if (amDestroyed != null && (bool)amDestroyed.GetValue(anim, null)) return false;
                return !(bool)amWantsVanilla.GetValue(comps[i]);
            }
            return false;
        }

        // SMYH DrawHandsOnWeapon(Pawn) 前缀: AM 在画 → 跳过 SMYH 的摆手武器(消除重复)。
        private static bool SmyhYieldToAm(Verse.Pawn pawn)
        {
            return !AmDrawing(pawn);
        }

        // (2026-09-08) 临时探针整段删除(真凶=SYS 背负/扛武器绘制, 已 XML 摘除 SYS comp 根治)。

        // __result 是 FieldHolder(嵌套私有类), 其 public readonly Members 是
        // Dictionary<MemberInfo, MemberWrapper>。按字段名裁掉黑名单成员。幂等。
        private static void GetHolderPostfix(object __result)
        {
            try
            {
                if (__result == null) return;
                FieldInfo membersF = __result.GetType().GetField("Members",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (membersF == null) return;
                IDictionary members = membersF.GetValue(__result) as IDictionary;
                if (members == null || members.Count == 0) return;

                List<object> toRemove = null;
                foreach (DictionaryEntry kv in members)
                {
                    // Key 是 MemberInfo, Name 即字段/属性名。
                    MemberInfo mi = kv.Key as MemberInfo;
                    if (mi != null && HiddenFields.Contains(mi.Name))
                        (toRemove ?? (toRemove = new List<object>())).Add(kv.Key);
                }
                if (toRemove != null)
                    for (int i = 0; i < toRemove.Count; i++)
                        members.Remove(toRemove[i]);
            }
            catch { }
        }

        // GiveJob 返回 false → 不发起抓取。
        private static bool NoGrapple()
        {
            return false;
        }
    }
}

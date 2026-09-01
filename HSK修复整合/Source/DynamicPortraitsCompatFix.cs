// 修复 Dynamic Portraits(Nals.DynamicPortraits)与 CM Color Coded Mood Bar
// (CrashM.ColorCodedMoodBar.11)同时启用时的冲突(原独立 mod
// local.dynamicportraits.compatfix,2026-08-27 并入本补丁包,行为不变):
//
// 根因:CCMB 的前缀补丁跳过原版殖民者条绘制,导致 Dynamic Portraits 检测到
// "otherMoodBarModEnabled" 后放弃接管,其全部选项(心情条/休息食物条/工作图标/
// 武器/动态背景)配置后都不生效。
//
// 修复(两者都在场才打补丁,否则整体空转):
//   1) DP RenderColonist.CheckDrawTexture 转译:把 HarmonyPatches.otherMoodBarModEnabled
//      的 ldsfld 改 push 0,让 DP 无视其它心情条 mod;
//   2) DP RenderColonist.DrawPawnTextureV2 转译:去掉 GenUI.LeftHalf/RightHalf 裁切,
//      让立绘占满整条;
//   3) CCMB VanillaDrawColonist.Prefix 与 MoodPatch.CGColonistBarColonistDrawerPrefix /
//      CGColonistGroupPrefix 前缀短路(return true 走原版),让出绘制权由 DP 完整接管。
//
// 编译:并入 HSKFixPack.dll(系统 csc,C#5)。本 mod loadAfter 两目标 mod,
// StaticConstructorOnStartup 时其类型已可解析。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DynamicPortraitsCompatFix
{
    [StaticConstructorOnStartup]
    public static class DynamicPortraitsCompatFixInit
    {
        static DynamicPortraitsCompatFixInit()
        {
            try
            {
                bool hasDP = LoadedModManager.RunningMods.Any(x => x.PackageId.ToLowerInvariant().Contains("dynamicportrait"));
                bool hasCCMB = LoadedModManager.RunningMods.Any(x => x.PackageId.ToLowerInvariant().Contains("colorcodedmoodbar"));
                if (!hasDP || !hasCCMB) return;

                Harmony harmony = new Harmony("local.hskfixpack.dynamicportraitscompatfix");

                Type dpRender = AccessTools.TypeByName("DynamicPortrait.RenderColonist");
                if (dpRender != null)
                {
                    MethodInfo checkDraw = AccessTools.Method(dpRender, "CheckDrawTexture");
                    if (checkDraw != null)
                        harmony.Patch(checkDraw,
                            transpiler: new HarmonyMethod(typeof(DynamicPortraitsCompatFixInit), "IgnoreOtherMoodBarModTranspiler"));
                    MethodInfo drawV2 = AccessTools.Method(dpRender, "DrawPawnTextureV2");
                    if (drawV2 != null)
                        harmony.Patch(drawV2,
                            transpiler: new HarmonyMethod(typeof(DynamicPortraitsCompatFixInit), "ForceFullPortraitTranspiler"));
                }

                Type ccmbVanilla = AccessTools.TypeByName("ColoredMoodBar13.VanillaDrawColonist");
                if (ccmbVanilla != null)
                {
                    MethodInfo m = AccessTools.Method(ccmbVanilla, "Prefix");
                    if (m != null)
                        harmony.Patch(m, prefix: new HarmonyMethod(typeof(DynamicPortraitsCompatFixInit), "SkipCcmbPrefix"));
                }

                Type ccmbMood = AccessTools.TypeByName("ColoredMoodBar13.MoodPatch");
                if (ccmbMood != null)
                {
                    foreach (string name in new[] { "CGColonistBarColonistDrawerPrefix", "CGColonistGroupPrefix" })
                    {
                        MethodInfo m = AccessTools.Method(ccmbMood, name);
                        if (m != null)
                            harmony.Patch(m, prefix: new HarmonyMethod(typeof(DynamicPortraitsCompatFixInit), "SkipCcmbPrefix"));
                    }
                }
                Log.Message("[HSKFix] DynamicPortraitsCompatFix applied (Dynamic Portraits takes over colonist bar over CCMB)");
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] DynamicPortraitsCompatFix patch failed: " + e);
            }
        }

        private static IEnumerable<CodeInstruction> IgnoreOtherMoodBarModTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instr in instructions)
            {
                FieldInfo operandField = instr.operand as FieldInfo;
                if (instr.opcode == OpCodes.Ldsfld && operandField != null
                    && operandField.Name == "otherMoodBarModEnabled"
                    && operandField.DeclaringType != null
                    && operandField.DeclaringType.FullName == "DynamicPortrait.HarmonyPatches")
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                }
                else
                {
                    yield return instr;
                }
            }
        }

        private static IEnumerable<CodeInstruction> ForceFullPortraitTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo leftHalf = AccessTools.Method(typeof(GenUI), "LeftHalf", new Type[] { typeof(Rect) });
            MethodInfo rightHalf = AccessTools.Method(typeof(GenUI), "RightHalf", new Type[] { typeof(Rect) });
            foreach (CodeInstruction instr in instructions)
            {
                MethodInfo operandMethod = instr.operand as MethodInfo;
                if ((instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt)
                    && operandMethod != null
                    && (operandMethod == leftHalf || operandMethod == rightHalf))
                {
                    yield return new CodeInstruction(OpCodes.Nop);
                }
                else
                {
                    yield return instr;
                }
            }
        }

        // 短路 CCMB 各前缀:__state 不需要,直接 return true 让原版绘制继续
        private static bool SkipCcmbPrefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}

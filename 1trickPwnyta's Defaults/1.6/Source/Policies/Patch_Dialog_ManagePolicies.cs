using Defaults.Compatibility;
using Defaults.UI;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Defaults.Policies
{
    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(Dialog_ManagePolicies<Policy>))]
    [HarmonyPatch(nameof(Dialog_ManagePolicies<Policy>.DoWindowContents))]
    public static class Patch_Dialog_ManagePolicies_DoWindowContents
    {
        public static bool Prefix(Dialog_ManagePolicies<Policy> __instance, Rect inRect)
        {
            return ModCompatibilityUtility_AnimalControls.DoAnimalControlsDefaults(inRect, __instance);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            CodeInstruction instruction = instructionsList.First(i => i.operand as float? == 32f);
            instruction.opcode = OpCodes.Call;
            instruction.operand = typeof(PatchUtility_Dialog_ManagePolicies).Method(nameof(PatchUtility_Dialog_ManagePolicies.GetTitleHeight));
            instructionsList.Insert(instructionsList.IndexOf(instruction), new CodeInstruction(OpCodes.Ldarg_0));
            return instructionsList;
        }

        public static void Postfix(Dialog_ManagePolicies<Policy> __instance, Rect inRect, Policy ___policyInt)
        {
            float buttonOffset = 0f;
            bool isDefaultPolicy = ___policyInt == __instance.GetType().Method("GetDefaultPolicy").Invoke(__instance, new object[] { });

            if (___policyInt != null && __instance.IsGamePolicyDialog() && !Settings.GetValue<bool>(Settings.HIDE_SETASDEFAULT))
            {
                if (!isDefaultPolicy)
                {
                    buttonOffset += 42f;
                }
                Rect saveAsDefaultRect = new Rect(inRect.xMax - 158f - buttonOffset, inRect.y + 74f, 32f, 32f);
                if (Widgets.ButtonImage(saveAsDefaultRect, TexButton.Save))
                {
                    Policy policy = PolicyUtility.NewDefaultPolicy(___policyInt.GetType(), ___policyInt.label); ;
                    policy.CopyFrom(___policyInt);
                    DefaultsMod.SaveSettings();
                    Messages.Message("Defaults_PolicySavedAs".Translate(policy?.label ?? "?"), MessageTypeDefOf.PositiveEvent, false);
                }
                TooltipHandler.TipRegionByKey(saveAsDefaultRect, "Defaults_SaveNewDefaultPolicy");
                buttonOffset += 42f;
            }

            if (___policyInt != null && ___policyInt.IsLockable() && !__instance.IsGamePolicyDialog())
            {
                if (!isDefaultPolicy)
                {
                    buttonOffset += 42f;
                }
                Rect lockRect = new Rect(inRect.xMax - 158f - buttonOffset, inRect.y + 10f, 32f, 32f);
                bool locked = ___policyInt.IsLocked();
                UIUtility.DoLockButton(lockRect, ref locked);
                ___policyInt.SetLocked(locked);
                buttonOffset += 42f;
            }

            ModCompatibilityUtility_AnimalControls.DoAnimalControlsDefaultsButton(inRect, __instance);
        }
    }

    [HarmonyPatchCategory("Policies")]
    [HarmonyPatch(typeof(Dialog_ManagePolicies<Policy>))]
    [HarmonyPatch("DoPolicyListing")]
    public static class Patch_Dialog_ManagePolicies_DoPolicyListing
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundQuickSearch = false;
            CodeInstruction targetInstruction = null;

            foreach (CodeInstruction instruction in instructions.Reverse())
            {
                if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == typeof(QuickSearchWidget).Method(nameof(QuickSearchWidget.OnGUI)))
                {
                    foundQuickSearch = true;
                }
                if (foundQuickSearch && instruction.opcode == OpCodes.Ldc_R4)
                {
                    targetInstruction = instruction;
                    break;
                }
            }

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction == targetInstruction)
                {
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchUtility_Dialog_ManagePolicies).Method(nameof(PatchUtility_Dialog_ManagePolicies.GetNewPolicyButtonPaddingTop)));
                    continue;
                }

                yield return instruction;
            }
        }

        public static void Postfix(Window __instance, Rect leftRect)
        {
            if (!Settings.GetValue<bool>(Settings.HIDE_LOADDEFAULT))
            {
                Type type = __instance.GetType().Method("GetDefaultPolicy").Invoke(__instance, new object[] { }).GetType();
                Rect loadDefaultRect = new Rect(leftRect.x + 10f, leftRect.yMax - 24f - 10f - Window.CloseButSize.y * 2 - 10f, leftRect.width - 20f, Window.CloseButSize.y);
                if (Widgets.ButtonText(loadDefaultRect, "Defaults_LoadDefaultPolicy".Translate()))
                {
                    FloatMenu menu = __instance.IsGamePolicyDialog()
                        ? PatchUtility_Dialog_ManagePolicies.GetFloatMenu(type, __instance, PolicyUtility.GetVanillaPolicies(type), PolicyUtility.GetDefaultPolicies(type))
                        : PatchUtility_Dialog_ManagePolicies.GetFloatMenu(type, __instance, PolicyUtility.GetVanillaPolicies(type));
                    Find.WindowStack.Add(menu);
                }
            }
        }
    }

    public static class PatchUtility_Dialog_ManagePolicies
    {
        public static float GetTitleHeight(Dialog_ManagePolicies<Policy> window) => Find.WindowStack.Windows.Contains(window) ? 32f : 0f;

        public static float GetNewPolicyButtonPaddingTop() => !Settings.GetValue<bool>(Settings.HIDE_LOADDEFAULT) ? 10f + Window.CloseButSize.y : 10f;

        public static FloatMenu GetFloatMenu(Type type, Window dialog, IList vanillaChoices, IList defaultChoices = null)
        {
            List<FloatMenuOption> options(IList l)
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                if (l != null)
                {
                    foreach (Policy p in l)
                    {
                        list.Add(new FloatMenuOption(p.label, () =>
                        {
                            Policy policy = PolicyUtility.NewPolicy(type, dialog, p.label);
                            policy.CopyFrom(p);
                            dialog.GetType().Method("set_SelectedPolicy").Invoke(dialog, new[] { policy });
                        }));
                    }
                }
                return list;
            }

            List<FloatMenuOption> vanillaOptions = options(vanillaChoices);
            List<FloatMenuOption> defaultOptions = options(defaultChoices);

            return !defaultOptions.NullOrEmpty()
                ? new FloatMenu(new List<FloatMenuOption>()
                {
                    new FloatMenuOption("Defaults_Vanilla".Translate(), () => Find.WindowStack.Add(new FloatMenu(vanillaOptions, "Defaults_Vanilla".Translate()))),
                    new FloatMenuOption("Defaults_Defaults".Translate(), () => Find.WindowStack.Add(new FloatMenu(defaultOptions, "Defaults_Defaults".Translate())))
                })
                : new FloatMenu(vanillaOptions, "Defaults_Vanilla".Translate());
        }
    }
}

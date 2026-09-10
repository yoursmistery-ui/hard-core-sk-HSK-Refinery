using Defaults.Policies.ApparelPolicies;
using Defaults.Policies.DrugPolicies;
using Defaults.Policies.FoodPolicies;
using Defaults.Policies.ReadingPolicies;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Defaults.Policies
{
    [HarmonyPatch(typeof(Dialog_ManagePolicies<Policy>))]
    [HarmonyPatch(nameof(Dialog_ManagePolicies<Policy>.DoWindowContents))]
    public static class Patch_Dialog_ManagePolicies_DoWindowContents
    {
        public static void Postfix(Dialog_ManagePolicies<Policy> __instance, Rect inRect, Policy ___policyInt)
        {
            float buttonOffset = 0f;

            if (___policyInt != null && (__instance.GetType() == typeof(Dialog_ManageApparelPolicies) || __instance.GetType() == typeof(Dialog_ManageFoodPolicies) || __instance.GetType() == typeof(Dialog_ManageDrugPolicies) || __instance.GetType() == typeof(Dialog_ManageReadingPolicies)))
            {
                Rect saveAsDefaultRect = new Rect(inRect.xMax - 158f - buttonOffset, inRect.y + 74f, 32f, 32f);
                if (Widgets.ButtonImage(saveAsDefaultRect, TexButton.Save))
                {
                    string name = ___policyInt.label;

                    if (___policyInt is RimWorld.ApparelPolicy)
                    {
                        int i = DefaultsSettings.DefaultApparelPolicies.Count + 1;
                        while (DefaultsSettings.DefaultApparelPolicies.Any(p => p.label == name))
                        {
                            name = "ApparelPolicy".Translate() + " " + i++;
                        }
                        ApparelPolicies.ApparelPolicy policy = new ApparelPolicies.ApparelPolicy(0, name);
                        policy.filter.CopyAllowancesFrom(((RimWorld.ApparelPolicy)___policyInt).filter);
                        DefaultsSettings.DefaultApparelPolicies.Add(policy);
                    }

                    if (___policyInt is RimWorld.FoodPolicy)
                    {
                        int i = DefaultsSettings.DefaultFoodPolicies.Count + 1;
                        while (DefaultsSettings.DefaultFoodPolicies.Any(p => p.label == name))
                        {
                            name = "FoodPolicy".Translate() + " " + i++;
                        }
                        FoodPolicies.FoodPolicy policy = new FoodPolicies.FoodPolicy(0, name);
                        policy.filter.CopyAllowancesFrom(((RimWorld.FoodPolicy)___policyInt).filter);
                        DefaultsSettings.DefaultFoodPolicies.Add(policy);
                    }

                    if (___policyInt is DrugPolicy)
                    {
                        int i = DefaultsSettings.DefaultDrugPolicies.Count + 1;
                        while (DefaultsSettings.DefaultDrugPolicies.Any(p => p.label == name))
                        {
                            name = "DrugPolicy".Translate() + " " + i++;
                        }
                        DrugPolicy policy = new DrugPolicy(0, name);
                        policy.CopyFrom(___policyInt);
                        DefaultsSettings.DefaultDrugPolicies.Add(policy);
                    }

                    if (___policyInt is RimWorld.ReadingPolicy)
                    {
                        int i = DefaultsSettings.DefaultReadingPolicies.Count + 1;
                        while (DefaultsSettings.DefaultReadingPolicies.Any(p => p.label == name))
                        {
                            name = "ReadingPolicy".Translate() + " " + i++;
                        }
                        ReadingPolicies.ReadingPolicy policy = new ReadingPolicies.ReadingPolicy(0, name);
                        policy.CopyFrom(___policyInt);
                        DefaultsSettings.DefaultReadingPolicies.Add(policy);
                    }

                    LongEventHandler.ExecuteWhenFinished(DefaultsMod.Settings.Write);
                    Messages.Message("Defaults_PolicySavedAs".Translate(name), MessageTypeDefOf.PositiveEvent, false);
                }
                TooltipHandler.TipRegionByKey(saveAsDefaultRect, "Defaults_SaveNewDefaultPolicy");
                buttonOffset += 42f;
            }

            if (___policyInt != null && (__instance.GetType() == typeof(Dialog_ApparelPolicies) || __instance.GetType() == typeof(Dialog_FoodPolicies) || __instance.GetType() == typeof(Dialog_DrugPolicies) || __instance.GetType() == typeof(Dialog_ReadingPolicies)))
            {
                Rect lockRect = new Rect(inRect.xMax - 158f - buttonOffset, inRect.y + 74f, 32f, 32f);
                if (___policyInt is ApparelPolicies.ApparelPolicy)
                {
                    ApparelPolicies.ApparelPolicy policy = (ApparelPolicies.ApparelPolicy)___policyInt;
                    UIUtility.DrawCheckButton(lockRect, UIUtility.LockIcon, "Defaults_LockSetting".Translate(), ref policy.locked);
                    buttonOffset += 42f;
                }

                if (___policyInt is FoodPolicies.FoodPolicy)
                {
                    FoodPolicies.FoodPolicy policy = (FoodPolicies.FoodPolicy)___policyInt;
                    UIUtility.DrawCheckButton(lockRect, UIUtility.LockIcon, "Defaults_LockSetting".Translate(), ref policy.locked);
                    buttonOffset += 42f;
                }

                if (___policyInt is ReadingPolicies.ReadingPolicy)
                {
                    ReadingPolicies.ReadingPolicy policy = (ReadingPolicies.ReadingPolicy)___policyInt;
                    UIUtility.DrawCheckButton(lockRect, UIUtility.LockIcon, "Defaults_LockSetting".Translate(), ref policy.locked);
                    buttonOffset += 42f;
                }
            }

            object defaultPolicy = __instance.GetType().Method("GetDefaultPolicy").Invoke(__instance, new object[] { });
            if (___policyInt != null && ___policyInt != defaultPolicy)
            {
                Rect defaultRect = new Rect(inRect.xMax - 158f - buttonOffset, inRect.y + 74f, 32f, 32f);
                if (Widgets.ButtonImage(defaultRect, UIUtility.StarIcon, true, "Defaults_SetAsDefault".Translate()))
                {
                    if (___policyInt is RimWorld.ApparelPolicy)
                    {
                        List<RimWorld.ApparelPolicy> policies = Current.Game.outfitDatabase.AllOutfits;
                        int currentIndex = policies.IndexOf((RimWorld.ApparelPolicy)___policyInt);
                        policies[currentIndex] = policies[0];
                        policies[0] = (RimWorld.ApparelPolicy)___policyInt;
                    }

                    if (___policyInt is RimWorld.FoodPolicy)
                    {
                        List<RimWorld.FoodPolicy> policies = Current.Game.foodRestrictionDatabase.AllFoodRestrictions;
                        int currentIndex = policies.IndexOf((RimWorld.FoodPolicy)___policyInt);
                        policies[currentIndex] = policies[0];
                        policies[0] = (RimWorld.FoodPolicy)___policyInt;
                    }

                    if (___policyInt is DrugPolicy && __instance.GetType() == typeof(Dialog_ManageDrugPolicies))
                    {
                        List<DrugPolicy> policies = Current.Game.drugPolicyDatabase.AllPolicies;
                        int currentIndex = policies.IndexOf((DrugPolicy)___policyInt);
                        policies[currentIndex] = policies[0];
                        policies[0] = (DrugPolicy)___policyInt;
                    }

                    if (___policyInt is RimWorld.ReadingPolicy && __instance.GetType() == typeof(Dialog_ManageReadingPolicies))
                    {
                        List<RimWorld.ReadingPolicy> policies = Current.Game.readingPolicyDatabase.AllReadingPolicies;
                        int currentIndex = policies.IndexOf((RimWorld.ReadingPolicy)___policyInt);
                        policies[currentIndex] = policies[0];
                        policies[0] = (RimWorld.ReadingPolicy)___policyInt;
                    }

                    if (___policyInt is ApparelPolicies.ApparelPolicy)
                    {
                        List<ApparelPolicies.ApparelPolicy> policies = DefaultsSettings.DefaultApparelPolicies;
                        int currentIndex = policies.IndexOf((ApparelPolicies.ApparelPolicy)___policyInt);
                        policies[currentIndex] = policies[0];
                        policies[0] = (ApparelPolicies.ApparelPolicy)___policyInt;
                    }

                    if (___policyInt is FoodPolicies.FoodPolicy)
                    {
                        List<FoodPolicies.FoodPolicy> policies = DefaultsSettings.DefaultFoodPolicies;
                        int currentIndex = policies.IndexOf((FoodPolicies.FoodPolicy)___policyInt);
                        policies[currentIndex] = policies[0];
                        policies[0] = (FoodPolicies.FoodPolicy)___policyInt;
                    }

                    if (___policyInt is DrugPolicy && __instance.GetType() == typeof(Dialog_DrugPolicies))
                    {
                        List<DrugPolicy> policies = DefaultsSettings.DefaultDrugPolicies;
                        int currentIndex = policies.IndexOf((DrugPolicy)___policyInt);
                        policies[currentIndex] = policies[0];
                        policies[0] = (DrugPolicy)___policyInt;
                    }

                    if (___policyInt is ReadingPolicies.ReadingPolicy && __instance.GetType() == typeof(Dialog_ReadingPolicies))
                    {
                        List<ReadingPolicies.ReadingPolicy> policies = DefaultsSettings.DefaultReadingPolicies;
                        int currentIndex = policies.IndexOf((ReadingPolicies.ReadingPolicy)___policyInt);
                        policies[currentIndex] = policies[0];
                        policies[0] = (ReadingPolicies.ReadingPolicy)___policyInt;
                    }
                }
                buttonOffset += 42f;
            }
        }
    }

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
                if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == DefaultsRefs.m_QuickSearchWidget_OnGUI)
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
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_Dialog_ManagePolicies_GetDefaultPolicy);
                    yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_PolicyUtility_GetNewPolicyButtonPaddingTop);
                    continue;
                }

                yield return instruction;
            }
        }

        public static void Postfix(Dialog_ManagePolicies<Policy> __instance, Rect leftRect)
        {
            object policy = __instance.GetType().Method("GetDefaultPolicy").Invoke(__instance, new object[] { });
            if (policy != null && (__instance.GetType() == typeof(Dialog_ManageApparelPolicies) || __instance.GetType() == typeof(Dialog_ManageFoodPolicies) || __instance.GetType() == typeof(Dialog_ManageDrugPolicies) || __instance.GetType() == typeof(Dialog_ManageReadingPolicies)))
            {
                Rect loadDefaultRect = new Rect(leftRect.x + 10f, leftRect.yMax - 24f - 10f - Window.CloseButSize.y * 2 - 10f, leftRect.width - 20f, Window.CloseButSize.y);
                if (Widgets.ButtonText(loadDefaultRect, "Defaults_LoadDefaultPolicy".Translate()))
                {
                    if (policy is RimWorld.ApparelPolicy)
                    {
                        Find.WindowStack.Add(new FloatMenu(DefaultsSettings.DefaultApparelPolicies.Select(p => new FloatMenuOption(p.label, delegate
                        {
                            RimWorld.ApparelPolicy apparelPolicy = (RimWorld.ApparelPolicy)__instance.GetType().Method("CreateNewPolicy").Invoke(__instance, new object[] { });
                            apparelPolicy.label = p.label;
                            apparelPolicy.filter.CopyAllowancesFrom(p.filter);
                            __instance.GetType().Method("set_SelectedPolicy").Invoke(__instance, new[] { apparelPolicy });
                        })).ToList()));
                    }
                    if (policy is RimWorld.FoodPolicy)
                    {
                        Find.WindowStack.Add(new FloatMenu(DefaultsSettings.DefaultFoodPolicies.Select(p => new FloatMenuOption(p.label, delegate
                        {
                            RimWorld.FoodPolicy foodPolicy = (RimWorld.FoodPolicy)__instance.GetType().Method("CreateNewPolicy").Invoke(__instance, new object[] { });
                            foodPolicy.label = p.label;
                            foodPolicy.filter.CopyAllowancesFrom(p.filter);
                            __instance.GetType().Method("set_SelectedPolicy").Invoke(__instance, new[] { foodPolicy });
                        })).ToList()));
                    }
                    if (policy is DrugPolicy)
                    {
                        Find.WindowStack.Add(new FloatMenu(DefaultsSettings.DefaultDrugPolicies.Select(p => new FloatMenuOption(p.label, delegate
                        {
                            DrugPolicy drugPolicy = (DrugPolicy)__instance.GetType().Method("CreateNewPolicy").Invoke(__instance, new object[] { });
                            drugPolicy.label = p.label;
                            drugPolicy.CopyFrom(p);
                            __instance.GetType().Method("set_SelectedPolicy").Invoke(__instance, new[] { drugPolicy });
                        })).ToList()));
                    }
                    if (policy is RimWorld.ReadingPolicy)
                    {
                        Find.WindowStack.Add(new FloatMenu(DefaultsSettings.DefaultReadingPolicies.Select(p => new FloatMenuOption(p.label, delegate
                        {
                            RimWorld.ReadingPolicy readingPolicy = (RimWorld.ReadingPolicy)__instance.GetType().Method("CreateNewPolicy").Invoke(__instance, new object[] { });
                            readingPolicy.label = p.label;
                            readingPolicy.CopyFrom(p);
                            __instance.GetType().Method("set_SelectedPolicy").Invoke(__instance, new[] { readingPolicy });
                        })).ToList()));
                    }
                }
            }
        }
    }
}

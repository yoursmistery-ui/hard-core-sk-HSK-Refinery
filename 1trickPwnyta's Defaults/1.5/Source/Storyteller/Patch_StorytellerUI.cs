using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Defaults.Storyteller
{
    [HarmonyPatch(typeof(StorytellerUI))]
    [HarmonyPatch(nameof(StorytellerUI.DrawStorytellerSelectionInterface))]
    public static class Patch_StorytellerUI_DrawStorytellerSelectionInterface
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool foundGameInitData = false;
            bool foundAnomalyActive = false;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo && (MethodInfo)instruction.operand == DefaultsRefs.m_Current_get_ProgramState)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Ldind_Ref);
                    yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_Dialog_Storyteller_ShouldNotDoPermadeathSelection);
                    continue;
                }
                if (!foundGameInitData && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo && (MethodInfo)instruction.operand == DefaultsRefs.m_Find_get_GameInitData)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
                    yield return new CodeInstruction(OpCodes.Ldarg_3);
                    yield return new CodeInstruction(OpCodes.Ldind_Ref);
                    yield return new CodeInstruction(OpCodes.Call, DefaultsRefs.m_Dialog_Storyteller_DoPermadeathSelection);
                    foundGameInitData = true;
                    continue;
                }
                if (foundGameInitData && !foundAnomalyActive)
                {
                    if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == DefaultsRefs.m_ModsConfig_get_AnomalyActive)
                    {
                        yield return instruction;
                        foundAnomalyActive = true;
                    }
                    continue;
                }

                yield return instruction;
            }
        }

        public static void Postfix(Rect rect, StorytellerDef chosenStoryteller, DifficultyDef difficulty, Difficulty difficultyValues)
        {
            if (!(difficultyValues is DifficultySub))
            {
                Rect buttonRect = new Rect(rect.x + rect.width - 150f - 16f, rect.y - 40f, 150f, 40f);
                if (Widgets.ButtonText(buttonRect, "Defaults_SetAsDefault".Translate()))
                {
                    DefaultsSettings.DefaultStoryteller = chosenStoryteller.defName;
                    DefaultsSettings.DefaultDifficulty = difficulty.defName;
                    DefaultsSettings.DefaultDifficultyValues.SetDifficultyValues(difficultyValues);
                    DefaultsSettings.DefaultAnomalyPlaystyle = difficultyValues.AnomalyPlaystyleDef.defName;
                    DefaultsSettings.DefaultPermadeath = Find.GameInitData != null ? Find.GameInitData.permadeath : Current.Game.Info.permadeathMode;
                    LongEventHandler.ExecuteWhenFinished(DefaultsMod.Settings.Write);
                    Messages.Message("Defaults_SetAsDefaultConfirmed".Translate(), MessageTypeDefOf.PositiveEvent, false);
                }
            }
        }
    }

    [HarmonyPatch(typeof(StorytellerUI))]
    [HarmonyPatch("DrawCustomLeft")]
    public static class Patch_StorytellerUI_DrawCustomLeft
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Callvirt && (MethodInfo)instruction.operand == DefaultsRefs.m_Difficulty_get_AnomalyPlaystyleDef)
                {
                    instruction.operand = DefaultsRefs.m_Dialog_Storyteller_GetAnomalyPlaystyleDef;
                }

                yield return instruction;
            }
        }
    }
}

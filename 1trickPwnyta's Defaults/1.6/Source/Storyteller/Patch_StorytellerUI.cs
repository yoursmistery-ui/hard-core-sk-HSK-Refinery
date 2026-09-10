using Defaults.Compatibility;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace Defaults.Storyteller
{
    [HarmonyPatchCategory("Storyteller")]
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
                if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo info1 && info1 == typeof(Current).Method("get_ProgramState"))
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchUtility_StorytellerUI).Method(nameof(PatchUtility_StorytellerUI.ShouldSkipPermadeathSelection)));
                    continue;
                }
                if (!foundGameInitData && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo info2 && info2 == typeof(Find).Method("get_GameInitData"))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, typeof(PatchUtility_StorytellerUI).Method(nameof(PatchUtility_StorytellerUI.DoPermadeathSelection)));
                    foundGameInitData = true;
                    continue;
                }
                if (foundGameInitData && !foundAnomalyActive)
                {
                    if (instruction.opcode == OpCodes.Call && (MethodInfo)instruction.operand == typeof(ModsConfig).Method("get_AnomalyActive"))
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
            if (!Find.WindowStack.IsOpen<Dialog_Storyteller>() && !Settings.GetValue<bool>(Settings.HIDE_SETASDEFAULT))
            {
                Rect buttonRect = new Rect(rect.x + rect.width - 150f - 16f, rect.y - 40f, 150f, 40f);
                if (Widgets.ButtonText(buttonRect, "Defaults_SetAsDefault".Translate()))
                {

                    Settings.Set(Settings.STORYTELLER, chosenStoryteller);
                    Settings.Set(Settings.DIFFICULTY, difficulty);
                    Settings.Set(Settings.DIFFICULTY_VALUES, difficultyValues);
                    Settings.Set(Settings.PERMADEATH, Find.GameInitData != null ? Find.GameInitData.permadeath : Current.Game.Info.permadeathMode);
                    ModCompatibilityUtility_NoPause.SetNoPauseOptions();
                    DefaultsMod.SaveSettings();
                    Messages.Message("Defaults_SetAsDefaultConfirmed".Translate(), MessageTypeDefOf.PositiveEvent, false);
                }
            }
        }
    }

    public static class PatchUtility_StorytellerUI
    {
        public static bool ShouldSkipPermadeathSelection()
        {
            return !Find.WindowStack.IsOpen<Dialog_Storyteller>() && Current.ProgramState != ProgramState.Entry;
        }

        public static void DoPermadeathSelection(Listing_Standard infoListing)
        {
            bool settingsOpen = Find.WindowStack.IsOpen<Dialog_Storyteller>();
            if (settingsOpen || Find.GameInitData != null)
            {
                bool permadeath = Settings.GetValue<bool>(Settings.PERMADEATH);
                bool active = settingsOpen ? permadeath : Find.GameInitData.permadeathChosen && Find.GameInitData.permadeath;
                bool active2 = settingsOpen ? !permadeath : Find.GameInitData.permadeathChosen && !Find.GameInitData.permadeath;
                if (infoListing.RadioButton("ReloadAnytimeMode".Translate(), active2, 0f, "ReloadAnytimeModeInfo".Translate(), null))
                {
                    if (settingsOpen)
                    {
                        Settings.SetValue(Settings.PERMADEATH, false);
                    }
                    else
                    {
                        Find.GameInitData.permadeathChosen = true;
                        Find.GameInitData.permadeath = false;
                    }
                }
                infoListing.Gap(3f);
                if (infoListing.RadioButton("CommitmentMode".TranslateWithBackup("PermadeathMode"), active, 0f, "PermadeathModeInfo".Translate(), null))
                {
                    if (settingsOpen)
                    {
                        Settings.SetValue(Settings.PERMADEATH, true);
                    }
                    else
                    {
                        Find.GameInitData.permadeathChosen = true;
                        Find.GameInitData.permadeath = true;
                    }
                }
            }
        }
    }
}

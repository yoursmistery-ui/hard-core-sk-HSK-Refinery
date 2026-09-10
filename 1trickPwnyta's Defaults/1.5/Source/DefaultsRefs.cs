using Defaults.Policies;
using Defaults.Storyteller;
using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace Defaults
{
    public static class DefaultsRefs
    {
        public static readonly MethodInfo m_object_GetType = AccessTools.Method(typeof(object), nameof(object.GetType));
        public static readonly MethodInfo m_Type_GetTypeFromHandle = AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle));
        public static readonly MethodInfo m_Find_get_GameInitData = AccessTools.Method(typeof(Find), "get_GameInitData");
        public static readonly MethodInfo m_Dialog_Storyteller_ShouldNotDoPermadeathSelection = AccessTools.Method(typeof(Dialog_Storyteller), nameof(Dialog_Storyteller.ShouldNotDoPermadeathSelection));
        public static readonly MethodInfo m_Dialog_Storyteller_DoPermadeathSelection = AccessTools.Method(typeof(Dialog_Storyteller), nameof(Dialog_Storyteller.DoPermadeathSelection));
        public static readonly MethodInfo m_Dialog_Storyteller_NonStandardAnomalyPlaystylesAllowed = AccessTools.Method(typeof(Dialog_Storyteller), nameof(Dialog_Storyteller.NonStandardAnomalyPlaystylesAllowed));
        public static readonly MethodInfo m_Dialog_Storyteller_GetAnomalyPlaystyleDef = AccessTools.Method(typeof(Dialog_Storyteller), nameof(Dialog_Storyteller.GetAnomalyPlaystyleDef));
        public static readonly MethodInfo m_Dialog_Storyteller_SetAnomalyPlaystyleDef = AccessTools.Method(typeof(Dialog_Storyteller), nameof(Dialog_Storyteller.SetAnomalyPlaystyleDef));
        public static readonly MethodInfo m_ModsConfig_get_AnomalyActive = AccessTools.Method(typeof(ModsConfig), "get_AnomalyActive");
        public static readonly MethodInfo m_Find_get_Scenario = AccessTools.Method(typeof(Find), "get_Scenario");
        public static readonly MethodInfo m_Current_get_ProgramState = AccessTools.Method(typeof(Current), "get_ProgramState");
        public static readonly MethodInfo m_Difficulty_get_AnomalyPlaystyleDef = AccessTools.Method(typeof(Difficulty), "get_AnomalyPlaystyleDef");
        public static readonly MethodInfo m_Difficulty_set_AnomalyPlaystyleDef = AccessTools.Method(typeof(Difficulty), "set_AnomalyPlaystyleDef");
        public static readonly MethodInfo m_Find_get_HiddenItemsManager = AccessTools.Method(typeof(Find), "get_HiddenItemsManager");
        public static readonly MethodInfo m_QuickSearchWidget_OnGUI = AccessTools.Method(typeof(QuickSearchWidget), nameof(QuickSearchWidget.OnGUI));
        public static readonly MethodInfo m_PolicyUtility_GetNewPolicyButtonPaddingTop = AccessTools.Method(typeof(PolicyUtility), nameof(PolicyUtility.GetNewPolicyButtonPaddingTop));
        public static readonly MethodInfo m_Dialog_ManagePolicies_GetDefaultPolicy = AccessTools.Method(typeof(Dialog_ManagePolicies<Policy>), "GetDefaultPolicy");
        public static readonly MethodInfo m_StatUtility_IsGameStartedInClassicMode = AccessTools.Method(typeof(Policies.StatUtility), nameof(Policies.StatUtility.IsGameStartedInClassicMode));
        public static readonly MethodInfo m_Find_get_IdeoManager = AccessTools.Method(typeof(Find), "get_IdeoManager");
        public static readonly MethodInfo m_StatUtility_GetScenarioStatFactor = AccessTools.Method(typeof(Policies.StatUtility), nameof(Policies.StatUtility.GetScenarioStatFactor));
        public static readonly MethodInfo m_Scenario_GetStatFactor = AccessTools.Method(typeof(Scenario), nameof(Scenario.GetStatFactor));

        public static readonly FieldInfo f_Difficulty_anomalyPlaystyleDef = AccessTools.Field(typeof(Difficulty), "anomalyPlaystyleDef");
        public static readonly FieldInfo f_DefaultsSettings_DefaultPermadeath = AccessTools.Field(typeof(DefaultsSettings), nameof(DefaultsSettings.DefaultPermadeath));
        public static readonly FieldInfo f_AnomalyPlaystyleDefOf_Standard = AccessTools.Field(typeof(AnomalyPlaystyleDefOf), nameof(AnomalyPlaystyleDefOf.Standard));
        public static readonly FieldInfo f_Dialog_AnomalySettings_difficulty = AccessTools.Field(typeof(Dialog_AnomalySettings), "difficulty");
        public static readonly FieldInfo f_Scribe_mode = AccessTools.Field(typeof(Scribe), nameof(Scribe.mode));
    }
}

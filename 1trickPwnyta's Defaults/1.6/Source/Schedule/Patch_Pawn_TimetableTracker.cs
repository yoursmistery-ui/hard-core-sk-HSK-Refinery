using Defaults.Workers;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Schedule
{
    [HarmonyPatchCategory("Schedule")]
    [HarmonyPatch(typeof(Pawn_TimetableTracker))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(Pawn) })]
    public static class Patch_Pawn_TimetableTracker_ctor
    {
        public static void Postfix(Pawn_TimetableTracker __instance, Pawn pawn)
        {
            if (pawn.Faction == Faction.OfPlayer)
            {
                Schedule schedule = DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Schedule>().GetNextDefaultSchedule();
                schedule?.ApplyToPawnTimetable(__instance);
            }
        }
    }
}

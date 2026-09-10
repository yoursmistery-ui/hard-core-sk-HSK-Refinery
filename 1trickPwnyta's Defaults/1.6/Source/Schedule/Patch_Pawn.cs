using Defaults.Workers;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Schedule
{
    [HarmonyPatchCategory("Schedule")]
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.Faction == Faction.OfPlayer)
            {
                Schedule schedule = DefaultSettingsCategoryWorker.GetWorker<DefaultSettingsCategoryWorker_Schedule>().GetNextDefaultSchedule();
                schedule?.ApplyToPawnTimetable(__instance.timetable);
            }
        }
    }
}

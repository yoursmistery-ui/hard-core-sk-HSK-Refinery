using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.Schedule
{
    [HarmonyPatch(typeof(Pawn))]
    [HarmonyPatch(nameof(Pawn.SetFaction))]
    public static class Patch_Pawn_SetFaction
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.Faction == Faction.OfPlayer)
            {
                Schedule schedule = DefaultsSettings.GetNextDefaultSchedule();
                if (schedule != null)
                {
                    schedule.ApplyToPawnTimetable(__instance.timetable);
                }
            }
        }
    }
}

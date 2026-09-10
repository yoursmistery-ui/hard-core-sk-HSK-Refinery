using RimWorld;
using Verse;

namespace Defaults.Schedule
{
    // Patched manually in mod constructor
    public static class Patch_Pawn_TimetableTracker_ctor
    {
        public static void Postfix(Pawn_TimetableTracker __instance, Pawn pawn)
        {
            if (pawn.Faction == Faction.OfPlayer)
            {
                Schedule schedule = DefaultsSettings.GetNextDefaultSchedule();
                if (schedule != null)
                {
                    schedule.ApplyToPawnTimetable(__instance);
                }
            }
        }
    }
}

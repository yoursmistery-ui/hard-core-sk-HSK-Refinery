using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Schedule
{
    public class Schedule : IExposable
    {
        public string name = string.Empty;
        public bool use = true;
        private List<string> assignments;

        public Schedule()
        {
            SetToDefaultSchedule();
        }

        public Schedule(string name)
        {
            this.name = name;
            SetToDefaultSchedule();
        }

        public Schedule(string name, Schedule schedule)
        {
            this.name = name;
            assignments = schedule.assignments.ListFullCopy();
        }

        public Schedule(string name, Pawn pawn)
        {
            this.name = name;
            if (pawn != null && pawn.timetable != null && pawn.timetable.times != null)
            {
                assignments = pawn.timetable.times.Select(t => t.defName).ToList();
            }
            else
            {
                SetToDefaultSchedule();
            }
        }

        private void SetToDefaultSchedule()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (assignments == null)
                {
                    assignments = new[]
                    {
                        TimeAssignmentDefOf.Sleep.defName,
                        TimeAssignmentDefOf.Sleep.defName,
                        TimeAssignmentDefOf.Sleep.defName,
                        TimeAssignmentDefOf.Sleep.defName,
                        TimeAssignmentDefOf.Sleep.defName,
                        TimeAssignmentDefOf.Sleep.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Anything.defName,
                        TimeAssignmentDefOf.Sleep.defName,
                        TimeAssignmentDefOf.Sleep.defName
                    }.ToList();
                }
            });
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref use, "use");
            Scribe_Collections.Look(ref assignments, "Assignments");
            if (assignments == null)
            {
                SetToDefaultSchedule();
            }
        }

        public TimeAssignmentDef GetTimeAssignment(int hour)
        {
            return DefDatabase<TimeAssignmentDef>.GetNamedSilentFail(assignments[hour]) ?? TimeAssignmentDefOf.Anything;
        }

        public void SetTimeAssignment(int hour, TimeAssignmentDef assignment)
        {
            assignments[hour] = assignment.defName;
        }

        public void ApplyToPawnTimetable(Pawn_TimetableTracker timetable)
        {
            if (timetable != null && timetable.times != null)
            {
                timetable.times.Clear();
                for (int i = 0; i < 24; i++)
                {
                    timetable.times.Add(GetTimeAssignment(i));
                }
            }
        }
    }
}

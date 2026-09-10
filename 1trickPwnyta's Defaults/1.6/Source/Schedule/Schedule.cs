using Defaults.Defs;
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
        private List<TimeAssignmentDef> assignments;

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
                assignments = pawn.timetable.times.ListFullCopy();
            }
            else
            {
                SetToDefaultSchedule();
            }
        }

        private void SetToDefaultSchedule()
        {
            assignments = Enumerable.Repeat(TimeAssignmentDefOf.Sleep, 6)
                .Concat(Enumerable.Repeat(TimeAssignmentDefOf.Anything, 16))
                .Concat(Enumerable.Repeat(TimeAssignmentDefOf.Sleep, 2)).ToList();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref use, "use");
            Scribe_Collections_Silent.Look(ref assignments, "Assignments", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (assignments == null)
                {
                    SetToDefaultSchedule();
                }
                assignments.Replace(null, TimeAssignmentDefOf.Anything);
            }
        }

        public TimeAssignmentDef this[int hour]
        {
            get => assignments[hour];
            set => assignments[hour] = value;
        }

        public void ApplyToPawnTimetable(Pawn_TimetableTracker timetable)
        {
            if (timetable != null && timetable.times != null)
            {
                timetable.times.Clear();
                for (int i = 0; i < 24; i++)
                {
                    timetable.times.Add(assignments[i]);
                }
            }
        }
    }
}

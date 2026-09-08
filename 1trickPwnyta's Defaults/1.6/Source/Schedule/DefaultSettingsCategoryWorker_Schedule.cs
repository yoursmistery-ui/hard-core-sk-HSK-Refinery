using Defaults.Defs;
using Defaults.Workers;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.Schedule
{
    public class DefaultSettingsCategoryWorker_Schedule : DefaultSettingsCategoryWorker
    {
        private List<Schedule> defaultSchedules;
        private int nextScheduleIndex = Mathf.Abs(Rand.Int);

        public DefaultSettingsCategoryWorker_Schedule(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public Schedule GetNextDefaultSchedule()
        {
            if (defaultSchedules.Any(s => s.use))
            {
                if (nextScheduleIndex >= defaultSchedules.Count)
                {
                    nextScheduleIndex %= defaultSchedules.Count;
                }
                while (!defaultSchedules[nextScheduleIndex].use)
                {
                    nextScheduleIndex++;
                    if (nextScheduleIndex >= defaultSchedules.Count)
                    {
                        nextScheduleIndex %= defaultSchedules.Count;
                    }
                }
                Schedule nextSchedule = defaultSchedules[nextScheduleIndex++];
                // ExecuteWhenFinished is required here. Loading a saved game breaks without it. Maybe because this method gets called during 
                // file load and messes up Scribe.mode?
                LongEventHandler.ExecuteWhenFinished(() => DefaultsMod.SaveSettings(false));
                return nextSchedule;
            }
            else
            {
                return null;
            }
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_ScheduleSettings(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.SCHEDULES:
                    value = defaultSchedules;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.SCHEDULES:
                    defaultSchedules = value as List<Schedule>;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            if (forced || defaultSchedules == null)
            {
                defaultSchedules = new List<Schedule>()
                {
                    new Schedule("Defaults_ScheduleName".Translate(1))
                };
            }
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Collections.Look(ref defaultSchedules, Settings.SCHEDULES);
            Scribe_Values.Look(ref nextScheduleIndex, Settings.NEXT_SCHEDULE, Mathf.Abs(Rand.Int));
        }
    }
}

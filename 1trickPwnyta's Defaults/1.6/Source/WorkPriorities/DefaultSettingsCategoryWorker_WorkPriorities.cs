using Defaults.Defs;
using Defaults.Workers;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.WorkPriorities
{
    [StaticConstructorOnStartup]
    public class DefaultSettingsCategoryWorker_WorkPriorities : DefaultSettingsCategoryWorker
    {
        private static readonly Texture2D altIcon = ContentFinder<Texture2D>.Get("DefaultSettingsCategoryIcons/WorkPriorities-alt2");

        private bool advancedMode = false;
        private Dictionary<WorkTypeDef, int> basicDefaultWorkPriorities;
        private List<Rule> advancedGlobalWorkPriorityLogic;
        private Dictionary<WorkTypeDef, List<Rule>> advancedWorkPriorityLogic;

        public DefaultSettingsCategoryWorker_WorkPriorities(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override Texture2D Icon => Settings.GetValue<bool>(Settings.MANUAL_PRIORITIES) ? base.Icon : altIcon;

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_WorkPriorities(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.WORK_PRIORITIES_ADVANCED_MODE:
                    value = advancedMode;
                    return true;
                case Settings.WORK_PRIORITIES_BASIC:
                    value = basicDefaultWorkPriorities;
                    return true;
                case Settings.WORK_PRIORITIES_GLOBAL_LOGIC:
                    value = advancedGlobalWorkPriorityLogic;
                    return true;
                case Settings.WORK_PRIORITIES_LOGIC:
                    value = advancedWorkPriorityLogic;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.WORK_PRIORITIES_ADVANCED_MODE:
                    advancedMode = (bool)value;
                    return true;
                case Settings.WORK_PRIORITIES_BASIC:
                    basicDefaultWorkPriorities = value as Dictionary<WorkTypeDef, int>;
                    return true;
                case Settings.WORK_PRIORITIES_GLOBAL_LOGIC:
                    advancedGlobalWorkPriorityLogic = value as List<Rule>;
                    return true;
                case Settings.WORK_PRIORITIES_LOGIC:
                    advancedWorkPriorityLogic = value as Dictionary<WorkTypeDef, List<Rule>>;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            if (forced)
            {
                advancedMode = false;
            }
            if (forced || basicDefaultWorkPriorities == null)
            {
                basicDefaultWorkPriorities = new Dictionary<WorkTypeDef, int>();
            }
            if (forced || advancedGlobalWorkPriorityLogic == null)
            {
                advancedGlobalWorkPriorityLogic = new List<Rule>();
            }
            if (forced || advancedWorkPriorityLogic == null)
            {
                advancedWorkPriorityLogic = new Dictionary<WorkTypeDef, List<Rule>>();
            }

            foreach (WorkTypeDef def in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!basicDefaultWorkPriorities.ContainsKey(def))
                {
                    basicDefaultWorkPriorities[def] = WorkPriorityValue.TynansChoice;
                }

                if (!advancedWorkPriorityLogic.ContainsKey(def))
                {
                    advancedWorkPriorityLogic[def] = new List<Rule>();
                }
            }
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Values.Look(ref advancedMode, Settings.WORK_PRIORITIES_ADVANCED_MODE, false);
            Scribe_Collections_Silent.LookKeysDef(ref basicDefaultWorkPriorities, Settings.WORK_PRIORITIES_BASIC, LookMode.Value);
            Scribe_Collections.Look(ref advancedGlobalWorkPriorityLogic, Settings.WORK_PRIORITIES_GLOBAL_LOGIC, LookMode.Deep);
            Scribe_Collections_Silent.LookKeysDef(ref advancedWorkPriorityLogic, Settings.WORK_PRIORITIES_LOGIC, LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                advancedGlobalWorkPriorityLogic?.RemoveWhere(r => !r.IsValid);
                foreach (List<Rule> rules in advancedWorkPriorityLogic.Values)
                {
                    rules.RemoveWhere(r => !r.IsValid);
                }
            }
        }
    }
}

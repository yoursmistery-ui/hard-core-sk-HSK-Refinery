using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.Rewards
{
    public class DefaultSettingsCategoryWorker_Rewards : DefaultSettingsCategoryWorker
    {
        private Dictionary<FactionDef, RewardPreference> defaultRewardPreferences;

        public DefaultSettingsCategoryWorker_Rewards(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_RewardsSettings(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.REWARDS:
                    value = defaultRewardPreferences;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.REWARDS:
                    defaultRewardPreferences = value as Dictionary<FactionDef, RewardPreference>;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            if (forced || defaultRewardPreferences == null)
            {
                defaultRewardPreferences = new Dictionary<FactionDef, RewardPreference>();
                foreach (FactionDef def in DefDatabase<FactionDef>.AllDefsListForReading)
                {
                    defaultRewardPreferences.Add(def, new RewardPreference());
                }
            }
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Collections_Silent.LookKeysDef(ref defaultRewardPreferences, Settings.REWARDS);
        }
    }
}

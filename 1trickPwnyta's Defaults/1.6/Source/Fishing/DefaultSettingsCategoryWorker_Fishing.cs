using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.Fishing
{
    public class DefaultSettingsCategoryWorker_Fishing : DefaultSettingsCategoryWorker
    {
        private FishingZoneOptions defaultFishingZoneOptions;

        public DefaultSettingsCategoryWorker_Fishing(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_FishingZoneSettings(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.FISHING_ZONE_OPTIONS:
                    value = defaultFishingZoneOptions;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.FISHING_ZONE_OPTIONS:
                    defaultFishingZoneOptions = value as FishingZoneOptions;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            if (forced || defaultFishingZoneOptions == null)
            {
                defaultFishingZoneOptions = new FishingZoneOptions();
            }
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Deep.Look(ref defaultFishingZoneOptions, Settings.FISHING_ZONE_OPTIONS);
        }
    }
}

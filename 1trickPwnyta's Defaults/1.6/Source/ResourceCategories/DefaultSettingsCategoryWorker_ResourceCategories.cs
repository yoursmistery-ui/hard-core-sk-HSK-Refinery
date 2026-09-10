using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.ResourceCategories
{
    public class DefaultSettingsCategoryWorker_ResourceCategories : DefaultSettingsCategoryWorker
    {
        public List<ThingCategoryDef> defaultExpandedResourceCategories;

        public DefaultSettingsCategoryWorker_ResourceCategories(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_ResourceCategories(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.EXPANDED_RESOURCE_CATEGORIES:
                    value = defaultExpandedResourceCategories;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.EXPANDED_RESOURCE_CATEGORIES:
                    defaultExpandedResourceCategories = value as List<ThingCategoryDef>;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            if (forced || defaultExpandedResourceCategories == null)
            {
                defaultExpandedResourceCategories = new List<ThingCategoryDef>() { ThingCategoryDefOf.Foods };
            }
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Collections_Silent.Look(ref defaultExpandedResourceCategories, Settings.EXPANDED_RESOURCE_CATEGORIES);
        }
    }
}

using Defaults.Compatibility;
using Defaults.Defs;
using Defaults.Workers;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class DefaultSettingsCategoryWorker_WorkbenchBills : DefaultSettingsCategoryWorker
    {
        private List<WorkbenchBillStore> defaultWorkbenchBills;

        public DefaultSettingsCategoryWorker_WorkbenchBills(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_WorkbenchBills(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.WORKBENCH_BILLS:
                    value = defaultWorkbenchBills;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.WORKBENCH_BILLS:
                    defaultWorkbenchBills = value as List<WorkbenchBillStore>;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        public override void HandleNewDefs(IEnumerable<Def> defs)
        {
            foreach (BillTemplate bill in defaultWorkbenchBills.SelectMany(s => s.bills))
            {
                if (!bill.locked)
                {
                    foreach (ThingDef def in defs.OfType<ThingDef>())
                    {
                        if (bill.recipe.fixedIngredientFilter.Allows(def))
                        {
                            bill.ingredientFilter.SetAllow(def, true);
                        }
                    }
                    HashSet<SpecialThingFilterDef> allSpecialThingFilters = bill.recipe.GetAllSpecialThingFilterDefs().ToHashSet();
                    foreach (SpecialThingFilterDef def in defs.OfType<SpecialThingFilterDef>())
                    {
                        if (!def.allowedByDefault && allSpecialThingFilters.Contains(def))
                        {
                            bill.ingredientFilter.SetAllow(def, false);
                        }
                    }
                }
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            if (forced || defaultWorkbenchBills == null)
            {
                defaultWorkbenchBills = new List<WorkbenchBillStore>();
            }
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Collections.Look(ref defaultWorkbenchBills, Settings.WORKBENCH_BILLS);
        }

        protected override void PostExposeData()
        {
            BackwardCompatibilityUtility.MigrateGlobalBillOptions();
        }

        public override float AdditionalSettingsDialogWidth => 720f;
    }
}

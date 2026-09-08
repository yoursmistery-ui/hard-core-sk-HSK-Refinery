using Defaults.Workers;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Defaults.Defs
{
    public class DefaultSettingDef : Def
    {
        private IDefaultSettingWorker worker;

        public DefaultSettingsCategoryDef category;
        public int uiOrder;
        public Type workerClass;
        public bool showInQuickOptions;
        public bool hideInAdditionalSettings;
        public List<string> keywords = new List<string>();

        public IDefaultSettingWorker Worker
        {
            get
            {
                if (worker == null)
                {
                    worker = WorkerFactory.GetWorker(this);
                }
                return worker;
            }
        }

        public bool Matches(QuickSearchFilter filter) => category.ShowInSearch && (
            filter.Matches(label)
            || keywords.Any(k => filter.Matches(k)
        ));

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (showInQuickOptions && !typeof(IQuickOption).IsAssignableFrom(workerClass))
            {
                yield return "If showInQuickOptions is true, workerClass must implement IQuickOption.";
            }
        }
    }
}

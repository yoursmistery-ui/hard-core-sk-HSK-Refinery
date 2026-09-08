using Defaults.Workers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Defs
{
    public class DefaultSettingsCategoryDef : DefWithIcon
    {
        private DefaultSettingsCategoryWorker worker;

        public Type workerClass;
        public List<string> keywords = new List<string>();
        public bool canDisable = true;

        public DefaultSettingsCategoryWorker Worker
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

        public IEnumerable<DefaultSettingDef> DefaultSettings => DefDatabase<DefaultSettingDef>.AllDefsListForReading.Where(d => d.category == this);

        public IEnumerable<FloatMenuOption> QuickOptions => DefaultSettings.Where(s => s.showInQuickOptions).OrderBy(s => s.uiOrder).Select(s => s.Worker).OfType<IQuickOption>().Select(w => w.QuickOption);

        public bool Enabled
        {
            get => !Worker.disabled;
            set => Worker.disabled = !value;
        }

        public bool ShowInSearch => Enabled || Settings.GetValue<bool>(Settings.SHOW_DISABLED_IN_SEARCH);

        public bool Matches(QuickSearchFilter filter) => ShowInSearch && (
            filter.Matches(label)
            || keywords.Any(k => filter.Matches(k))
            || DefaultSettings.Any(s => s.Matches(filter)
        ));
    }
}

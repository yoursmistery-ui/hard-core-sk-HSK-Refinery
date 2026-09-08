using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class WorkbenchBillStore : IExposable
    {
        public HashSet<ThingDef> workbenchGroup;
        public List<BillTemplate> bills = new List<BillTemplate>();

        public static WorkbenchBillStore Get(HashSet<ThingDef> workbenchGroup)
        {
            WorkbenchBillStore store = DefaultsSettings.DefaultWorkbenchBills.FirstOrDefault(s => s.workbenchGroup.Any(w => workbenchGroup.Contains(w)));
            if (store == null)
            {
                store = new WorkbenchBillStore(workbenchGroup);
                DefaultsSettings.DefaultWorkbenchBills.Add(store);
            }
            store.workbenchGroup = workbenchGroup;
            return store;
        }

        private WorkbenchBillStore() { }

        public WorkbenchBillStore(HashSet<ThingDef> workbenchGroup)
        {
            this.workbenchGroup = workbenchGroup;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref workbenchGroup, "workbenchGroup");
            workbenchGroup.RemoveWhere(t => t == null);
            Scribe_Collections.Look(ref bills, "bills");
            bills.RemoveWhere(b => b.recipe == null);
        }
    }
}

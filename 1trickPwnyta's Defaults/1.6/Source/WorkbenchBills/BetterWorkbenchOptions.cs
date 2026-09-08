using Defaults.Defs;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class BetterWorkbenchOptions : IExposable
    {
        public HashSet<ThingDef> CountAdditionalItems = new HashSet<ThingDef>();
        public bool CountWhenAway = false;

        public BetterWorkbenchOptions Clone() => new BetterWorkbenchOptions()
        {
            CountAdditionalItems = CountAdditionalItems.ToList().ListFullCopy().ToHashSet(),
            CountWhenAway = CountWhenAway
        };

        public void ExposeData()
        {
            Scribe_Collections_Silent.Look(ref CountAdditionalItems, "CountAdditionalItems");
            Scribe_Values.Look(ref CountWhenAway, "CountWhenAway", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && CountAdditionalItems == null)
            {
                CountAdditionalItems = new HashSet<ThingDef>();
            }
        }
    }
}

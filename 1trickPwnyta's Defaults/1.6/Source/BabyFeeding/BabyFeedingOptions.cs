using Defaults.Defs;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.BabyFeeding
{
    public class BabyFeedingOptions : IExposable
    {
        public AutofeedMode BirtherParent = AutofeedMode.Urgent;
        public AutofeedMode ParentLactating = AutofeedMode.Childcare;
        public AutofeedMode ParentNonlactating = AutofeedMode.Childcare;
        public AutofeedMode NonparentLactating = AutofeedMode.Childcare;
        public AutofeedMode NonparentNonlactating = AutofeedMode.Childcare;
        public HashSet<ThingDef> AllowedConsumables = ITab_Pawn_Feeding.BabyConsumableFoods.ToHashSet();
        public bool locked = false;

        public void ExposeData()
        {
            Scribe_Values.Look(ref BirtherParent, "BirtherParent", AutofeedMode.Urgent);
            Scribe_Values.Look(ref ParentLactating, "ParentLactating", AutofeedMode.Childcare);
            Scribe_Values.Look(ref ParentNonlactating, "ParentNonlactating", AutofeedMode.Childcare);
            Scribe_Values.Look(ref NonparentLactating, "NonparentLactating", AutofeedMode.Childcare);
            Scribe_Values.Look(ref NonparentNonlactating, "NonparentNonlactating", AutofeedMode.Childcare);
            Scribe_Collections_Silent.Look(ref AllowedConsumables, "AllowedConsumables");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && AllowedConsumables == null)
            {
                AllowedConsumables = ITab_Pawn_Feeding.BabyConsumableFoods.ToHashSet();
            }
            Scribe_Values.Look(ref locked, "locked", false);
        }
    }
}

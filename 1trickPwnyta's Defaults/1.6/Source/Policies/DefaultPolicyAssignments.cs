using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Defaults.Policies
{
    public class DefaultPolicyAssignments : IExposable
    {
        private static readonly HashSet<PawnType> relevantPawnTypes = new HashSet<PawnType>()
        {
            PawnType.AdultColonist,
            PawnType.ChildColonist,
            PawnType.Slave,
            PawnType.Guest
        };
        public Dictionary<PawnType, PolicyAssignment> PolicyAssignments;

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Sanitize();
            }
            Scribe_Collections.Look(ref PolicyAssignments, "PolicyAssignments", LookMode.Value, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Sanitize();
            }
        }

        private void Sanitize()
        {
            if (PolicyAssignments == null)
            {
                PolicyAssignments = new Dictionary<PawnType, PolicyAssignment>();
            }
            foreach (PawnType pawnType in relevantPawnTypes)
            {
                if (!PolicyAssignments.ContainsKey(pawnType))
                {
                    PolicyAssignments[pawnType] = new PolicyAssignment();
                }
            }
            foreach (PawnType pawnType in Enum.GetValues(typeof(PawnType)))
            {
                if (!relevantPawnTypes.Contains(pawnType) && PolicyAssignments.ContainsKey(pawnType))
                {
                    PolicyAssignments.Remove(pawnType);
                }
            }
        }
    }

    public class PolicyAssignment : IExposable
    {
        public ApparelPolicy apparelPolicy;
        public FoodPolicy foodPolicy;
        public DrugPolicy drugPolicy;
        public ReadingPolicy readingPolicy;

        public T GetPolicy<T>() where T : Policy =>
            typeof(T) == typeof(ApparelPolicy) ? apparelPolicy as T
            : typeof(T) == typeof(FoodPolicy) ? foodPolicy as T
            : typeof(T) == typeof(DrugPolicy) ? drugPolicy as T
            : typeof(T) == typeof(ReadingPolicy) ? readingPolicy as T
            : throw new Exception("Invalid policy type: " + typeof(T));

        public void ExposeData()
        {
            Scribe_References.Look(ref apparelPolicy, "apparelPolicy");
            Scribe_References.Look(ref foodPolicy, "foodPolicy");
            Scribe_References.Look(ref drugPolicy, "drugPolicy");
            Scribe_References.Look(ref readingPolicy, "readingPolicy");
        }
    }
}

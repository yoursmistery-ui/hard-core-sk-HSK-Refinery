using RimWorld;
using Verse;

namespace Defaults.Policies
{
    public static class PolicyUtility
    {
        public static DrugPolicy NewDrugPolicy()
        {
            string name;
            int i = DefaultsSettings.DefaultDrugPolicies.Count + 1;
            do
            {
                name = "DrugPolicy".Translate() + " " + i++;
            } while (DefaultsSettings.DefaultDrugPolicies.Any(p => p.label == name));
            DrugPolicy policy = new DrugPolicy(0, name);
            DefaultsSettings.DefaultDrugPolicies.Add(policy);
            return policy;
        }

        public static DrugPolicy NewDrugPolicyFromDef(DrugPolicyDef def)
        {
            DrugPolicy drugPolicy = NewDrugPolicy();
            drugPolicy.label = def.LabelCap;
            drugPolicy.sourceDef = def;
            if (def.allowPleasureDrugs)
            {
                for (int i = 0; i < drugPolicy.Count; i++)
                {
                    if (drugPolicy[i].drug.IsPleasureDrug)
                    {
                        drugPolicy[i].allowedForJoy = true;
                    }
                }
            }
            if (def.entries != null)
            {
                for (int j = 0; j < def.entries.Count; j++)
                {
                    drugPolicy[def.entries[j].drug].CopyFrom(def.entries[j]);
                }
            }
            return drugPolicy;
        }

        public static ReadingPolicies.ReadingPolicy NewReadingPolicy()
        {
            string name;
            int i = DefaultsSettings.DefaultReadingPolicies.Count + 1;
            do
            {
                name = "ReadingPolicy".Translate() + " " + i++;
            } while (DefaultsSettings.DefaultReadingPolicies.Any(p => p.label == name));
            ReadingPolicies.ReadingPolicy policy = new ReadingPolicies.ReadingPolicy(0, name);
            DefaultsSettings.DefaultReadingPolicies.Add(policy);
            return policy;
        }

        public static float GetNewPolicyButtonPaddingTop(Policy policy)
        {
            if (policy != null && (policy is ApparelPolicy || policy is DrugPolicy))
            {
                return 10f + Window.CloseButSize.y;
            }
            else
            {
                return 10f;
            }
        }
    }
}

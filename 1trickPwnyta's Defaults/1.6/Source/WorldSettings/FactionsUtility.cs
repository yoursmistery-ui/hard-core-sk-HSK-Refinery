using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.WorldSettings
{
    public static class FactionsUtility
    {
        private static List<FactionDef> GetDefaultFactions(bool selectable)
        {
            List<FactionDef> factions = new List<FactionDef>();
            foreach (FactionDef factionDef in FactionGenerator.ConfigurableFactions)
            {
                if (factionDef.displayInFactionSelection == selectable && factionDef.startingCountAtWorldCreation > 0)
                {
                    for (int i = 0; i < factionDef.startingCountAtWorldCreation; i++)
                    {
                        factions.Add(factionDef);
                    }
                }
            }
            using (IEnumerator<FactionDef> enumerator = FactionGenerator.ConfigurableFactions.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    FactionDef faction = enumerator.Current;
                    if (faction.replacesFaction != null)
                    {
                        factions.RemoveAll(f => f == faction.replacesFaction);
                    }
                }
            }
            return factions;
        }

        public static void SetDefaultFactions(List<FactionDef> factions)
        {
            factions.Clear();
            factions.AddRange(Settings.Get<List<FactionDef>>(Settings.FACTIONS).Where(f => f != null && f.displayInFactionSelection).Concat(GetDefaultNonselectableFactions()));
            foreach (FactionDef faction in GetDefaultSelectableFactions())
            {
                if (!factions.Contains(faction) && Current.Game.Scenario.AllParts.Any(p => p.def.preventRemovalOfFaction == faction))
                {
                    factions.Add(faction);
                }
            }
        }

        public static List<FactionDef> GetDefaultSelectableFactions()
        {
            return GetDefaultFactions(true);
        }

        public static List<FactionDef> GetDefaultNonselectableFactions()
        {
            return GetDefaultFactions(false);
        }
    }
}

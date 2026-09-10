using Defaults.Defs;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Compatibility
{
    public static class ModCompatibilityUtility_FactionXenotypeRandomizer
    {
        private static readonly Type customFactionDefType = AccessTools.TypeByName("FactionXenotypeRandomizer.CustomFactionDef");
        private static readonly Type customFactionXenotypesType = AccessTools.TypeByName("FactionXenotypeRandomizer.CustomFactionXenotypes");
        private static readonly bool factionXenotypeRandomizerActive = customFactionDefType != null;

        public static void InitializeCustomFactions() => customFactionXenotypesType?.Method("Initialize").Invoke(null, new object[] { });

        public static void UninitializeCustomFactions() => customFactionXenotypesType?.Method("Uninitialize").Invoke(null, new object[] { });

        public static bool ScribeDefaultFactions(ref List<FactionDef> factions)
        {
            if (factionXenotypeRandomizerActive)
            {
                List<FactionDef> factionDefs = new List<FactionDef>();
                List<FactionDef> customFactionDefs = new List<FactionDef>();
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    foreach (FactionDef faction in factions)
                    {
                        if (faction.GetType() == customFactionDefType)
                        {
                            customFactionDefs.Add(faction);
                        }
                        else
                        {
                            factionDefs.Add(faction);
                        }
                    }
                }
                Scribe_Collections_Silent.Look(ref factionDefs, Settings.FACTIONS);
                Scribe_Collections.Look(ref customFactionDefs, Settings.FACTION_XENOTYPE_RANDOMIZER_MUTANT_FACTIONS, LookMode.Deep);
                if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    if (factionDefs == null)
                    {
                        factionDefs = new List<FactionDef>();
                    }
                    if (customFactionDefs == null)
                    {
                        customFactionDefs = new List<FactionDef>();
                    }
                    factions = factionDefs.Concat(customFactionDefs).ToList();
                }
                return true;
            }
            return false;
        }
    }
}

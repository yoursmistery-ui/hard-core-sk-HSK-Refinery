using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;

namespace Defaults.WorldSettings
{
    [HarmonyPatchCategory("World")]
    [HarmonyPatchMod("koth.realisticplanets1.6")]
    public static class CompatibilityPatch_Planets_CreateWorldParams
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.TypeByName("Planets_Code.Core.Planets_CreateWorldParams").Method("InitializeFactionCounts");
        }

        public static void Postfix(List<FactionDef> ___factions)
        {
            FactionsUtility.SetDefaultFactions(___factions);
        }
    }
}

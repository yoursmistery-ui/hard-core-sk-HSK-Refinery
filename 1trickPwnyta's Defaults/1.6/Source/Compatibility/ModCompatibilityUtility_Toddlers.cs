using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace Defaults.Compatibility
{
    public static class ModCompatibilityUtility_Toddlers
    {
        private static readonly bool toddlersModEnabled = AccessTools.TypeByName("Toddlers.ToddlerUtility") != null;

        public static void FixDisabledWorkTypes(Pawn pawn, ref List<WorkTypeDef> disabledWorkTypes)
        {
            if (toddlersModEnabled)
            {
                disabledWorkTypes = typeof(Pawn).Field("cachedDisabledWorkTypes").GetValue(pawn) as List<WorkTypeDef>;
            }
        }
    }
}

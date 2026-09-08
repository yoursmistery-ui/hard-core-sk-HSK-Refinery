using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.AllowedAreas
{
    public static class AllowedAreaUtility
    {
        public static void SetDefaultAllowedArea(this Pawn pawn, PawnType? previousPawnType = null, Pawn mother = null)
        {
            if (pawn.playerSettings != null && pawn.playerSettings.SupportsAllowedAreas)
            {
                Dictionary<PawnType, AllowedArea> allowedPawnAreas = Settings.Get<Dictionary<PawnType, AllowedArea>>(Settings.ALLOWED_AREAS_PAWN);
                Dictionary<Map, Area> allowedAreas = pawn.playerSettings.GetType().Field("allowedAreas").GetValue(pawn.playerSettings) as Dictionary<Map, Area>;

                PawnType? pawnType = PawnTypeUtility.GetPawnType(pawn);
                if (pawnType.HasValue)
                {
                    Dictionary<Map, Area> inheritedAreas = null;
                    if (mother?.playerSettings != null && mother.playerSettings.SupportsAllowedAreas && (pawnType == PawnType.ChildColonist || pawnType == PawnType.Animal) && (pawnType == PawnType.ChildColonist ? Settings.GetValue<bool>(Settings.CHILDREN_INHERIT_AREA_FROM_PARENT) : Settings.GetValue<bool>(Settings.ANIMALS_INHERIT_AREA_FROM_PARENT)))
                    {
                        inheritedAreas = mother.playerSettings.GetType().Field("allowedAreas").GetValue(mother.playerSettings) as Dictionary<Map, Area>;
                    }
                    foreach (Map map in Find.Maps)
                    {
                        if (previousPawnType.HasValue)
                        {
                            if (allowedAreas.TryGetValue(map)?.Label != allowedPawnAreas.TryGetValue(previousPawnType.Value)?.name)
                            {
                                continue;
                            }
                        }
                        allowedAreas[map] = inheritedAreas == null || !inheritedAreas.ContainsKey(map) || !map.areaManager.AllAreas.Contains(inheritedAreas[map])
                            ? map.areaManager.AllAreas.FirstOrDefault(a => a.Label == allowedPawnAreas.TryGetValue(pawnType.Value)?.name)
                            : inheritedAreas[map];
                    }
                }
            }
        }
    }
}

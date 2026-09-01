using RimWorld;
using Verse;

namespace SmartFarming;

public class Utilities
{
    public static bool GrothSeasonNowPrefix(IntVec3 c, Map map, ThingDef plantDef)
    {
        if (ModSettings_SmartFarming.oldPatchMethod) return true; // skip our logic on legacy patch mode
        
        Zone_Growing zone = map.zoneManager.zoneGrid[c.z * map.info.sizeInt.x + c.x] as Zone_Growing;
        if (zone != null && Mod_SmartFarming.compCache.TryGetValue(map.uniqueID, out MapComponent_SmartFarming comp) && comp.growZoneRegistry.TryGetValue(zone.ID, out ZoneData zoneData))
        {
            switch (zoneData.sowMode)
            {
                case ZoneData.SowMode.Smart:
                {
                    return zoneData.alwaysSow ? true : zoneData.minHarvestDayForNewlySown > -1;
                }
                case ZoneData.SowMode.Force:
                {
                    return true;
                }
                case ZoneData.SowMode.On:
                {
                    return true; //Vanilla handling
                }
                case ZoneData.SowMode.Off:
                {
                    return false;
                }
            }
        }
        return true;
    }
    
}

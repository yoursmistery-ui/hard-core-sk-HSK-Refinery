using Defaults.Defs;
using System.Collections.Generic;
using Verse;

namespace Defaults
{
    public class Comp_SpawnTracker : ThingComp
    {
        private bool spawnedAnywhereEver;
        private HashSet<Map> spawnedOnMapEver = new HashSet<Map>();

        public bool EverSpawnedOnMap(Map map) => spawnedOnMapEver.Contains(map);

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            foreach (DefaultSettingsCategoryDef category in DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading)
            {
                if (category.Enabled)
                {
                    if (!spawnedAnywhereEver)
                    {
                        category.Worker.Notify_FirstSpawnAnywhere(parent as Pawn);
                    }
                    if (!EverSpawnedOnMap(parent.Map))
                    {
                        category.Worker.Notify_FirstSpawnOnMap(parent as Pawn, parent.Map);
                    }
                }
            }
            spawnedAnywhereEver = true;
            spawnedOnMapEver.Add(parent.Map);
        }

        public override void PostExposeData()
        {
            if (DefaultSettingsCategoryDefOf.AllowedAreas.Enabled)
            {
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    spawnedOnMapEver.RemoveWhere(m => !Find.Maps.Contains(m));
                }
                Scribe_Values.Look(ref spawnedAnywhereEver, "spawnedAnywhereEver", true);
                Scribe_Collections.Look(ref spawnedOnMapEver, "spawnedOnMapEver", LookMode.Reference);
                if (Scribe.mode == LoadSaveMode.PostLoadInit && spawnedOnMapEver == null)
                {
                    spawnedOnMapEver = new HashSet<Map>();
                }
            }
        }
    }
}

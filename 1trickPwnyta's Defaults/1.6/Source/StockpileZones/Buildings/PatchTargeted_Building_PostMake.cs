using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Defaults.StockpileZones.Buildings
{
    [HarmonyPatchCategory("Storage")]
    public static class PatchTargeted_Building_PostMake
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            HashSet<Type> patchedTypes = new HashSet<Type>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.building?.defaultStorageSettings != null))
            {
                Type postMakeType = def.thingClass;
                while (postMakeType.GetMethod(nameof(Building.PostMake)).DeclaringType != postMakeType)
                {
                    postMakeType = postMakeType.BaseType;
                }
                if (!patchedTypes.Contains(postMakeType))
                {
                    yield return postMakeType.Method(nameof(Building.PostMake));
                    patchedTypes.Add(postMakeType);
                }
            }
        }

        public static void Postfix(Building __instance)
        {
            if (!(__instance is IStoreSettingsParent parent))
            {
                parent = __instance.AllComps.OfType<IStoreSettingsParent>().FirstOrDefault();
            }
            if (parent != null)
            {
                BuildingUtility.SetDefaultBuildingStorageSettings(__instance.def, parent);
            }
        }
    }
}

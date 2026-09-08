using RimWorld;
using System.Linq;
using Verse;

namespace Defaults.Policies.ReadingPolicies
{
    // Patched manually in mod constructor
    public static class Patch_ReadingPolicy
    {
        public static void Postfix(RimWorld.ReadingPolicy __instance)
        {
            __instance.defFilter.SetDisallowAll();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.HasComp<CompBook>()))
            {
                __instance.defFilter.SetAllow(def, true);
            }
        }
    }
}

using System;
using System.Linq;
using CombatExtended;
using Verse;

namespace MihoHSK.MeleeTargetingFix
{
    /// <summary>
    /// Ensures CE's melee-targeting component is present after all startup
    /// constructors have run. The HSK CE build conditionally injects this comp
    /// from its own static constructor, which can run before its settings are
    /// available and leave every humanlike race without the targeting gizmo.
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class MeleeTargetingComponentFix
    {
        static MeleeTargetingComponentFix()
        {
            LongEventHandler.ExecuteWhenFinished(InjectMissingComponents);
        }

        private static void InjectMissingComponents()
        {
            try
            {
                int added = 0;
                Type targetComp = typeof(CompMeleeTargettingGizmo);

                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(
                    candidate => candidate.race != null && candidate.race.Humanlike))
                {
                    if (def.comps == null)
                    {
                        def.comps = new System.Collections.Generic.List<CompProperties>();
                    }

                    if (def.comps.Any(properties => properties != null && properties.compClass == targetComp))
                    {
                        continue;
                    }

                    def.comps.Add(new CompProperties { compClass = targetComp });
                    added++;
                }

                Log.Message("[Miho HSK] Restored CE melee targeting component on " + added + " humanlike race definitions.");
            }
            catch (Exception exception)
            {
                Log.Error("[Miho HSK] Failed to restore CE melee targeting components: " + exception);
            }
        }
    }
}

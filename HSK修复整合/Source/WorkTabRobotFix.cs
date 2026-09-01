using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKFixAIRobot
{
    // Misc Robots (AIRobot.dll) robots vs Work Tab (arof.fluffy.worktab.continued):
    // AIRobot.X2_AIRobot_Pawn_WorkSettings.EnableAndInitialize() runs inside its constructor
    // (new X2_AIRobot_Pawn_WorkSettings(pawn)) — at that moment pawn.workSettings is still null
    // (AIRobot assigns the field only after the ctor returns). It then calls the base
    // Pawn_WorkSettings.SetPriority, which Work Tab's Harmony prefix redirects into
    // PriorityManager.Get[pawn].SetPriority(...) -> WorkPriority.set_Item ->
    // pawn.workSettings.Notify_UseWorkPrioritiesChanged() -> NullReferenceException, caught by
    // AIRobot and logged once per robot as "Thrown error while setting priority. This can be an
    // issue with another mod!" — on every spawn AND on load for every robot re-instantiated.
    // The same robot entries also broke Work Tab's save data ("Exception in LookDictionary
    // (label=Priorities): Tried to add different values for the same key" at load).
    //
    // Fix: prefix on WorkTab.PriorityManager.get_Item(Pawn) — robots (workSettings still null or
    // of the AIRobot subclass type) get a per-pawn neutral PriorityTracker (Pawn == null) and the
    // original is skipped, so Work Tab never touches robots and never scribes them. The neutral
    // tracker's set_Item skips the workSettings call when Pawn is null, so no NRE can occur.
    // A second prefix purges robot entries from Work Tab's dictionary on ExposeData, so an
    // existing save heals after one save cycle.
    [StaticConstructorOnStartup]
    public static class WorkTabRobotFix
    {
        private static readonly Dictionary<Pawn, object> NeutralTrackers = new Dictionary<Pawn, object>();
        private static Type _priorityTrackerType;
        private static FieldInfo _prioritiesField;

        static WorkTabRobotFix()
        {
            try
            {
                Type managerType = AccessTools.TypeByName("WorkTab.PriorityManager");
                if (managerType == null)
                {
                    return;
                }

                MethodInfo getItem = AccessTools.Method(managerType, "get_Item", new Type[] { typeof(Pawn) });
                if (getItem == null)
                {
                    return;
                }

                _priorityTrackerType = AccessTools.TypeByName("WorkTab.PriorityTracker");
                _prioritiesField = managerType.GetField("priorities", BindingFlags.Static | BindingFlags.NonPublic);

                Harmony harmony = new Harmony("local.hskfixpack.worktabrobot");
                harmony.Patch(
                    getItem,
                    prefix: new HarmonyMethod(
                        typeof(WorkTabRobotFix).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                MethodInfo exposeData = managerType.GetMethod("ExposeData");
                if (exposeData != null)
                {
                    harmony.Patch(
                        exposeData,
                        prefix: new HarmonyMethod(
                            typeof(WorkTabRobotFix).GetMethod(
                                "ExposePrefix",
                                BindingFlags.Static | BindingFlags.NonPublic)));
                }

                Log.Message("[HSKFix] patched Work Tab: robots excluded from work priority tracking");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] Work Tab robot fix failed: " + e);
            }
        }

        private static bool Prefix(object __instance, Pawn pawn, ref object __result)
        {
            if (pawn == null || !IsRobot(pawn))
            {
                return true;
            }

            object tracker;
            if (!NeutralTrackers.TryGetValue(pawn, out tracker))
            {
                tracker = Activator.CreateInstance(_priorityTrackerType);
                NeutralTrackers[pawn] = tracker;
            }
            __result = tracker;
            return false;
        }

        private static void ExposePrefix()
        {
            try
            {
                if (_prioritiesField != null)
                {
                    object value = _prioritiesField.GetValue(null);
                    IDictionary dict = value as IDictionary;
                    if (dict != null)
                    {
                        List<object> toRemove = new List<object>();
                        foreach (object key in dict.Keys)
                        {
                            Pawn pawn = key as Pawn;
                            if (pawn != null && IsRobot(pawn))
                            {
                                toRemove.Add(key);
                            }
                        }
                        for (int i = 0; i < toRemove.Count; i++)
                        {
                            dict.Remove(toRemove[i]);
                        }
                    }
                }

                List<Pawn> dead = new List<Pawn>();
                foreach (KeyValuePair<Pawn, object> pair in NeutralTrackers)
                {
                    if (pair.Key == null || pair.Key.Destroyed)
                    {
                        dead.Add(pair.Key);
                    }
                }
                for (int i = 0; i < dead.Count; i++)
                {
                    NeutralTrackers.Remove(dead[i]);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] Work Tab robot cleanup failed: " + e);
            }
        }

        private static bool IsRobot(Pawn pawn)
        {
            Pawn_WorkSettings workSettings = pawn.workSettings;
            if (workSettings == null)
            {
                // AIRobot builds its work settings before pawn.workSettings is assigned.
                return true;
            }
            return workSettings.GetType().FullName == "AIRobot.X2_AIRobot_Pawn_WorkSettings";
        }
    }
}

using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;

namespace Defaults.Compatibility
{
    public static class ModCompatibilityUtility_AnimalControls
    {
        private static readonly Type animalControlsType = AccessTools.TypeByName("AnimalControls.Patch.Dialog_ManageFoodRestrictions_DoWindowContents_AnimalControlsPatch");
        private static readonly bool animalControlsActive = animalControlsType != null;

        public static bool DoAnimalControlsDefaults(Rect inRect, Dialog_ManagePolicies<Policy> dialog)
        {
            return !animalControlsActive || dialog.GetType() != typeof(Dialog_ManageFoodPolicies) || (bool)animalControlsType.Method("Prefix").Invoke(null, new object[] { dialog, inRect });
        }

        public static void DoAnimalControlsDefaultsButton(Rect inRect, Dialog_ManagePolicies<Policy> dialog)
        {
            if (animalControlsActive && dialog.GetType() == typeof(Dialog_ManageFoodPolicies))
            {
                animalControlsType.Method("Postfix").Invoke(null, new object[] { inRect });
            }
        }
    }
}

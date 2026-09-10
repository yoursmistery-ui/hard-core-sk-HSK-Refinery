using HarmonyLib;
using System;
using Verse;

namespace Defaults
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = true)]
    public class HarmonyPatchMod : HarmonyAttribute
    {
        public HarmonyPatchMod(string packageId)
        {
            if (!ModsConfig.IsActive(packageId))
            {
                info.category = "DisabledByHarmonyPatchMod";
            }
        }
    }
}

using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Defaults.AllowedAreas
{
    [HarmonyPatchCategory("AllowedAreas")]
    [HarmonyPatch(typeof(AreaManager))]
    [HarmonyPatch(nameof(AreaManager.AddStartingAreas))]
    public static class Patch_AreaManager
    {
        public static void Postfix(AreaManager __instance)
        {
            __instance.AllAreas.RemoveWhere(a => a.Mutable);
            foreach (AllowedArea area in Settings.Get<List<AllowedArea>>(Settings.ALLOWED_AREAS))
            {
                if (__instance.TryMakeNewAllowed(out Area_Allowed gameArea))
                {
                    gameArea.RenamableLabel = area.name;
                    gameArea.SetColor(area.color);
                    if (area.full)
                    {
                        gameArea.Invert();
                    }
                }
            }
        }
    }
}

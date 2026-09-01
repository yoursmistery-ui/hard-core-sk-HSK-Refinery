using Verse;
using UnityEngine;
using RimWorld;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Reflection;
using RimWorld.Planet;

namespace cn.zhuzijun.WealthCorrector
{


    [StaticConstructorOnStartup,HarmonyPatch(typeof(WealthWatcher), nameof(WealthWatcher.ForceRecount))]
    public static class WealthCorrector_Patch
    {
        static WealthCorrector_Patch()
        {
            var harmony = new Harmony("cn.zhuzijun.WealthCorrector.Patch");
            harmony.PatchAll();
            Log.Message("WealthCorrector initialized.");
        }

        //// Token: 0x040037F5 RID: 14325
        //private float wealthItems;

        //// Token: 0x040037F6 RID: 14326
        //private float wealthBuildings;

        //// Token: 0x040037F7 RID: 14327
        //private float wealthPawns;
        public static void Postfix(bool allowDuringInit, WealthWatcher __instance)
        {
            var traverse = Traverse.Create(__instance);
            Traverse field;
            if (ZModSettings.wealthIgnorePocketMap)
            {
                //判断pocketmapparent,是否为子地图,不缩放子地图的财富,防止子地图财富被计算2次
                field = traverse.Field("map");
                var map = field.GetValue<Map>();
                if (map.info.parent is PocketMapParent)
                    return;
            }
            field = traverse.Field("wealthItems");
            field.SetValue(Math.Max(1, field.GetValue<float>() * (ZModSettings.wealthItemsRate / 100f)));
            //Log.Message("Items=" + ins.GetValue<float>());

            field = traverse.Field("wealthFloorsOnly");
            var floor_wealth = field.GetValue<float>();

            field = traverse.Field("wealthBuildings");
            field.SetValue(
                Math.Max(1,
                    (field.GetValue<float>()- floor_wealth * (100f - ZModSettings.wealthFloorsRate) / 100f) * ZModSettings.wealthBuildingsRate / 100f
                )
                );
            //Log.Message("Buildings=" + ins.GetValue<float>());

            field = traverse.Field("wealthPawns");
            field.SetValue(Math.Max(1, field.GetValue<float>() * (ZModSettings.wealthPawnsRate / 100f)));
            //Log.Message("Pawns=" + ins.GetValue<float>());
        }
    }

}

using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using AlienRace;
using System;

namespace RatkinUnderground
{
    [StaticConstructorOnStartup]
    public static class AlienRacePatchInit
    {
        static AlienRacePatchInit()
        {
        }
    }

    // 在BodyAddon的GraphicFor方法中替换Graphic
    [HarmonyPatch(typeof(AlienPawnRenderNode_BodyAddon), "GraphicFor")]
    public static class BodyAddon_GraphicFor_Patch
    {
        [HarmonyPostfix]
        public static void GraphicFor_Postfix(AlienPawnRenderNode_BodyAddon __instance, Pawn pawn, ref Graphic __result)
        {
            if (__result == null || pawn == null || pawn.def.defName != "Ratkin")
            {
                return;
            }

            // 检查pawn背景和头发
            bool hasBG = pawn.story.Adulthood == DefDatabase<AlienRace.AlienBackstoryDef>.GetNamed("RKU_GuerrillaAR");
            if (hasBG)
            {
                hasBG = pawn.story.hairDef == DefDatabase<HairDef>.GetNamed("RKU_CommanderHair");
            }
            if (!hasBG)
            {
                return;
            }

            // 修复头发颜色的透明度（存读档后可能会丢失透明度）
            if (pawn.story.HairColor.a < 1f)
            {
                Color currentHairColor = pawn.story.HairColor;
                pawn.story.HairColor = new Color(currentHairColor.r, currentHairColor.g, currentHairColor.b, 1f);
            }

            // 获取当前的graphic路径
            string currentPath = __result?.path;
            if (string.IsNullOrEmpty(currentPath))
            {
                return;
            }

            string replacementPath = null;
            if (currentPath.Contains("RK_Texture_EarLeft"))
            {
                replacementPath = "Things/Commander/RK_EarLeft";
            }
            else if (currentPath.Contains("RK_Texture_EarRight"))
            {
                replacementPath = "Things/Commander/RK_EarRight";
            }
            else if (currentPath.Contains("RK_Texture_Ear") || currentPath.Contains("Body/RK_Ear"))
            {
                replacementPath = "Things/Commander/RK_Ear";
            }


            if (currentPath.Contains("RK_Texture_Tail") || currentPath.Contains("Body/RK_Tail"))
            {
                replacementPath = "Things/Commander/Tail";
            }

            if (replacementPath != null && replacementPath != currentPath)
            {
                // 创建新的graphic
                try
                {
                    Graphic newGraphic = GraphicDatabase.Get<Graphic_Multi>(replacementPath, __result.Shader, __result.drawSize, __result.color, __result.colorTwo);
                    __result = newGraphic;
                }
                catch
                {
                    try
                    {
                        Graphic newGraphic = GraphicDatabase.Get<Graphic_Single>(replacementPath, __result.Shader, __result.drawSize, __result.color, __result.colorTwo);
                        __result = newGraphic;
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
    }
}

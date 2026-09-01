using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

// =====================================================================
//  石化管道连管渲染 (2026-08-31) — 并入 HSK工业科研大修
//  原生 Graphic_Linked 没有会构建 subGraphic 的 Init, 直接当 graphicClass 用会让
//  subGraphic 为空/自指 → Graphic_Linked.LinkedDrawMatFrom 抛 NullReferenceException。
//  正解照抄 Rimefeller.Building_Pipe: 自定义 thingClass 覆写 Graphic, 返回
//  GraphicDatabaseUtility.WrapLinked(Graphic_Single(atlas), Basic) 造出的真连管图。
//  连不连仍由 def.graphicData.linkFlags 与 map.linkGrid 决定(储罐/泵/阀/出料口各自登记 flag)。
//  C#5 兼容。§9: Graphic 只读缓存字典, 无分配。
//
//  2026-09-01 改造: 删掉 "放置时绿色高亮" 的低风险 Draw 覆盖实现 — 现在由 PetroPipeOverlayHSK.cs
//  里的 SectionLayer_GasPipeOverlay (3 个 SectionLayer 子类, 1.6 Section 构造时自动反射注册)
//  提供与 Rimefeller 化合燃料一致的连管轮廓高亮, 按气体类型过滤 + 三色区分, 覆盖范围含
//  阀门/泵/出料口/储罐. 这里只留 Graphic override 维持连管渲染。
// =====================================================================

namespace BlueprintUnlockHSK
{
    public class Building_RKPipe : Building
    {
        private static readonly Dictionary<string, Graphic> cache = new Dictionary<string, Graphic>();

        public override Graphic Graphic
        {
            get
            {
                GraphicData gd = def.graphicData;
                if (gd == null || gd.texPath.NullOrEmpty())
                {
                    return base.Graphic;
                }
                string path = gd.texPath;
                Graphic g;
                if (!cache.TryGetValue(path, out g))
                {
                    Graphic sub = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Cutout, gd.drawSize, Color.white, Color.white);
                    g = new Graphic_Linked(sub);
                    cache[path] = g;
                }
                return g;
            }
        }
    }
}

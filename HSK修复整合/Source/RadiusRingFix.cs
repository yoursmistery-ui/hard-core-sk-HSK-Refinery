// 射程环修复(RadiusRingFix,并入 HSK 修复整合)
//
// 问题: RimWorld 1.6 原版 GenRadial 只预计算了约 79.8 半径内的径向模式(20000 格)。
// Core SK 的 SkyAiRefined 围攻 AI 在放置远程火炮蓝图时会调用原版
// PlaceWorker_ShowTurretRadius.AllowsPlacing -> GenDraw.DrawRadiusRing,以火炮射程画环;
// Vile's Pre-Industrial 等 mod 有射程 220 的火炮,超过原版上限,
// 原版只 Log.Error 一次("Cannot draw radius ring...")并直接返回,射程环不显示。
//
// 方案: 给 GenDraw.DrawRadiusRing(4 参重载)加 Harmony 前缀:
// - 半径在原版上限内 -> 走原版逻辑,完全不变;
// - 半径超上限 -> 按原版算法(圆心周围 x^2+z^2 <= r^2 的格)计算圆盘格并按整数半径缓存,
//   再用与 DrawFieldEdges 相同的算法绘制。原版 DrawFieldEdges 单次
//   Graphics.DrawMeshInstanced 最多 1023 个实例,半径 220 需要约 1764 个,
//   因此这里分批(每批 900)绘制。纯绘制修复,不改任何数值/行为。

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace RadiusRingFix
{
    [StaticConstructorOnStartup]
    public static class RadiusRingFixInit
    {
        private const int MaxSupportedRadius = 512;

        private const int InstancingBatchSize = 900;

        private static readonly Dictionary<int, List<IntVec3>> diskCache = new Dictionary<int, List<IntVec3>>();

        static RadiusRingFixInit()
        {
            try
            {
                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("local.hskmvcfverbfix.radiusring");
                harmony.Patch(
                    HarmonyLib.AccessTools.Method(typeof(GenDraw), "DrawRadiusRing", new Type[] { typeof(IntVec3), typeof(float), typeof(Color), typeof(Func<IntVec3, bool>) }),
                    prefix: new HarmonyLib.HarmonyMethod(typeof(RadiusRingFixInit).GetMethod("DrawRadiusRingPrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched GenDraw.DrawRadiusRing");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] radius ring patch failed: " + e);
            }
        }

        private static bool DrawRadiusRingPrefix(IntVec3 center, float radius, Color color, Func<IntVec3, bool> predicate)
        {
            if (radius <= GenRadial.MaxRadialPatternRadius)
            {
                return true;
            }
            if (radius <= 0f || float.IsNaN(radius))
            {
                return false;
            }
            int key = (int)Math.Ceiling(radius);
            if (key > MaxSupportedRadius)
            {
                return false;
            }
            List<IntVec3> offsets;
            if (!diskCache.TryGetValue(key, out offsets))
            {
                offsets = BuildDiskOffsets(key);
                diskCache[key] = offsets;
            }
            List<IntVec3> cells = new List<IntVec3>(offsets.Count);
            for (int i = 0; i < offsets.Count; i++)
            {
                IntVec3 cell = center + offsets[i];
                if (predicate == null || predicate(cell))
                {
                    cells.Add(cell);
                }
            }
            DrawFieldEdgesBatched(cells, color);
            return false;
        }

        private static List<IntVec3> BuildDiskOffsets(int radius)
        {
            List<IntVec3> list = new List<IntVec3>();
            int radiusSq = radius * radius;
            for (int z = -radius; z <= radius; z++)
            {
                int zSq = z * z;
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + zSq <= radiusSq)
                    {
                        list.Add(new IntVec3(x, 0, z));
                    }
                }
            }
            return list;
        }

        private static void DrawFieldEdgesBatched(List<IntVec3> cells, Color color)
        {
            Map map = Find.CurrentMap;
            if (map == null || cells == null || cells.Count == 0)
            {
                return;
            }
            Material edgeBase = MatLoader.LoadMat("Misc/FieldEdge");
            Material material = MaterialPool.MatFrom((Texture2D)edgeBase.mainTexture, ShaderDatabase.Transparent, color, 2900);
            material.GetTexture(Shader.PropertyToID("_MainTex")).wrapMode = TextureWrapMode.Clamp;
            material.enableInstancing = true;
            BoolGrid grid = new BoolGrid(map);
            int sizeX = map.Size.x;
            int sizeZ = map.Size.z;
            int count = cells.Count;
            float yOffset = Rand.ValueSeeded(color.GetHashCode()) * 0.03658537f / 10f;
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = cells[i];
                if (cell.InBounds(map))
                {
                    grid[cell.x, cell.z] = true;
                }
            }
            List<Matrix4x4> matrices = new List<Matrix4x4>();
            for (int j = 0; j < count; j++)
            {
                IntVec3 cell = cells[j];
                if (!cell.InBounds(map))
                {
                    continue;
                }
                Vector3 pos = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays) + new Vector3(0f, yOffset, 0f);
                if (cell.z < sizeZ - 1 && !grid[cell.x, cell.z + 1])
                {
                    matrices.Add(Matrix4x4.TRS(pos, new Rot4(0).AsQuat, Vector3.one));
                }
                if (cell.x < sizeX - 1 && !grid[cell.x + 1, cell.z])
                {
                    matrices.Add(Matrix4x4.TRS(pos, new Rot4(1).AsQuat, Vector3.one));
                }
                if (cell.z > 0 && !grid[cell.x, cell.z - 1])
                {
                    matrices.Add(Matrix4x4.TRS(pos, new Rot4(2).AsQuat, Vector3.one));
                }
                if (cell.x > 0 && !grid[cell.x - 1, cell.z])
                {
                    matrices.Add(Matrix4x4.TRS(pos, new Rot4(3).AsQuat, Vector3.one));
                }
            }
            if (matrices.Count == 0)
            {
                return;
            }
            Matrix4x4[] all = matrices.ToArray();
            for (int start = 0; start < all.Length; start += InstancingBatchSize)
            {
                int length = Math.Min(InstancingBatchSize, all.Length - start);
                Matrix4x4[] chunk = new Matrix4x4[length];
                Array.Copy(all, start, chunk, 0, length);
                Graphics.DrawMeshInstanced(MeshPool.plane10, 0, material, chunk);
            }
        }
    }
}

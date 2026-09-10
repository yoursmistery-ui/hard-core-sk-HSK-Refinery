// 地形通行性变更追踪器(2026-08-21,与 ReloadRegistryFix 配套)
//
// 目的: 区域链接脏化的肇事者定位。ReloadRegistryFix 检测到「配方原料搜索失败但
// 所有候选都可达+可预留+可发现」= 区域链接/可达性脏化,但原版日志无法指认是谁
// 改坏了区域。本追踪器记录最近 N 次「通行性变化」的地形编辑(walkable ↔
// impassable,这类变化会改变区域网格),带调用栈直接指认肇事 mod;
// ReloadRegistryFix 检测到脏化时调用 Dump() 把缓冲拼进诊断日志。
//
// 原理(1.6.4871 反编译确认): TerrainGrid.SetTerrain 是原版唯一的地形写入入口,
// 原版会在其中把格子标脏交给 regionAndRoomUpdater 重建区域。若某 mod 改了地形
// 通行性而区域链接仍脏化,说明标脏/重建链路被绕过或中断 —— 缓冲里的调用栈
// 就是肇事者。只记录通行性变化的编辑(通行性不变不影响区域,过滤噪音)。
//
// 性能: SetTerrain 前缀只做一次 TerrainAt 查找 + 通行性比较(纳秒级);
// 调用栈只在缓冲前 MaxStacks 条捕获(有界);缓冲上限 MaxEntries,无增长风险。
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Verse;

namespace ReloadRegistryFix
{
    [StaticConstructorOnStartup]
    public static class TerrainChangeTracer
    {
        private const int MaxEntries = 60;
        private const int MaxStacks = 10;

        private struct Entry
        {
            public int tick;
            public IntVec3 cell;
            public string oldTerrain;
            public string newTerrain;
            public string stack;
        }

        private static readonly List<Entry> buffer = new List<Entry>();
        private static int totalChanges = 0;

        static TerrainChangeTracer()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.terraintracer");
                harmony.Patch(AccessTools.Method(typeof(TerrainGrid), "SetTerrain"),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(TerrainChangeTracer), "SetTerrainPrefix")));
                Log.Message("[TerrainChangeTracer] active: recording passability-changing terrain edits");
            }
            catch (Exception e)
            {
                Log.Error("[TerrainChangeTracer] patch failed: " + e);
            }
        }

        private static void SetTerrainPrefix(TerrainGrid __instance, IntVec3 c, TerrainDef newTerr)
        {
            try
            {
                TerrainDef oldTerr = __instance.TerrainAt(c);
                if (oldTerr == newTerr)
                {
                    return;
                }
                bool oldWalk = oldTerr != null && oldTerr.passability == Traversability.Standable;
                bool newWalk = newTerr != null && newTerr.passability == Traversability.Standable;
                if (oldWalk == newWalk)
                {
                    return;
                }
                Entry e = new Entry();
                e.tick = (Find.TickManager != null) ? Find.TickManager.TicksGame : -1;
                e.cell = c;
                e.oldTerrain = (oldTerr != null) ? oldTerr.defName : "null";
                e.newTerrain = (newTerr != null) ? newTerr.defName : "null";
                e.stack = (buffer.Count < MaxStacks) ? Environment.StackTrace : null;
                buffer.Add(e);
                if (buffer.Count > MaxEntries)
                {
                    buffer.RemoveAt(0);
                }
                totalChanges++;
            }
            catch (Exception ex)
            {
                Log.Warning("[TerrainChangeTracer] trace failed: " + ex.Message);
            }
        }

        public static string Dump()
        {
            if (buffer.Count == 0)
            {
                return "no passability-changing terrain edits recorded";
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("terrainEdits(total=" + totalChanges + ",last=" + buffer.Count + "):");
            for (int i = 0; i < buffer.Count; i++)
            {
                Entry e = buffer[i];
                sb.Append("\n  t=" + e.tick + " " + e.cell + " " + e.oldTerrain + "->" + e.newTerrain);
                if (e.stack != null)
                {
                    sb.Append("\n    " + e.stack.Replace("\n", "\n    "));
                }
            }
            return sb.ToString();
        }
    }
}

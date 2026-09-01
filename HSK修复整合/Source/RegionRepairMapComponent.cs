// 区域网格/可达性脏化自愈(2026-08-17)
//
// 背景: 配方原料搜索(WorkGiver_DoBill.TryFindBestBillIngredients →
// TryFindBestIngredientsHelper)第二路走 RegionTraverser.BreadthFirstTraverse(rootReg,...),
// 靠 region 链接(regionGrid / regionLinkDatabase)跨区域找原料。当 region 链接/网格脏化
// (某 region 不知晓相邻 region 之间其实有可通行格)时,BFS 到不了原料所在 region,
// 即使原料就在隔壁衣柜/地上、且全部未禁止+可预留,搜索仍失败 —— 即
// ReloadRegistryFix 诊断输出的 "likely region/reachability staleness (rootReg=ok)"。
//
// 修复: 诊断确认脏化后,请求一次「全量 region + 房间重建」
// (map.regionAndRoomUpdater.RebuildAllRegionsAndRooms(),原版地图加载/开发模式同款调用,
// 会重建所有 region 及其链接,顺带复用旧房间/区域)。用 MapComponent 延迟到普通地图 tick
// 执行 —— 避开配方搜索的调用栈(防重入/卡顿),并带每图冷却(MinRebuildGapTicks),
// 避免「原料真被封死在密闭房间」这种合法不可达场景下周期性反复重建。
//
// 机制(1.6.4871 反编译核实): Map.FillComponents() 会为所有非抽象 MapComponent 子类
// 用 Activator.CreateInstance(type, map) 自动实例化,因此本组件不需要 MapComponentDef,
// 只要 DLL 里有一个 public 构造 (Map) 的 MapComponent 子类即可挂到每张地图。
// RebuildAllRegionsAndRooms 是 public,直接调用即可(游戏内地图 FinalizeLoading 已把
// regionAndRoomUpdater.Enabled 置 true)。
using System;
using System.Collections.Generic;
using Verse;

namespace ReloadRegistryFix
{
    public class RegionRepairMapComponent : MapComponent
    {
        // 延迟几个 tick,离开当前配方搜索调用栈后再执行(防重入)。
        private const int RequestDelayTicks = 5;

        // 同一地图两次 region 重建的最小间隔(≈100 秒),避免合法不可达场景反复重建。
        private const int MinRebuildGapTicks = 6000;

        private static readonly Dictionary<Map, int> lastRebuildTick = new Dictionary<Map, int>();

        private int requestedTick = -1;
        private bool rebuilding;

        public RegionRepairMapComponent(Map map)
            : base(map)
        {
        }

        public static void RequestRebuild(Map map)
        {
            if (map == null || map.Disposed)
            {
                return;
            }
            RegionRepairMapComponent comp = map.GetComponent<RegionRepairMapComponent>();
            if (comp == null)
            {
                return;
            }
            comp.requestedTick = Find.TickManager.TicksGame + RequestDelayTicks;
        }

        public override void MapComponentTick()
        {
            if (requestedTick < 0)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (now < requestedTick)
            {
                return;
            }
            requestedTick = -1;
            if (rebuilding || map == null || map.Disposed)
            {
                return;
            }
            int last;
            if (lastRebuildTick.TryGetValue(map, out last) && now - last < MinRebuildGapTicks)
            {
                return;
            }
            lastRebuildTick[map] = now;
            if (lastRebuildTick.Count > 32)
            {
                List<Map> dead = new List<Map>();
                foreach (KeyValuePair<Map, int> kv in lastRebuildTick)
                {
                    if (kv.Key == null || kv.Key.Disposed)
                    {
                        dead.Add(kv.Key);
                    }
                }
                for (int i = 0; i < dead.Count; i++)
                {
                    lastRebuildTick.Remove(dead[i]);
                }
            }
            if (map.regionAndRoomUpdater == null || !map.regionAndRoomUpdater.Enabled)
            {
                return;
            }
            rebuilding = true;
            try
            {
                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                Log.Message("[ReloadRegistryFix] rebuilt regions for map " + map.Tile +
                    " (region/reachability staleness recovery; bill ingredient search should resume)");
            }
            catch (Exception e)
            {
                Log.Warning("[ReloadRegistryFix] region rebuild failed: " + e.Message);
            }
            finally
            {
                rebuilding = false;
            }
        }
    }
}

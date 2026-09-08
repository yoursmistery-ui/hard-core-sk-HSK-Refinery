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

        // 该图"上次重建至今"若已连续无事件超过此阈值,就把退避计数归零 → 恢复灵敏
        // (说明那次重建大概率真修好了, 之后偶发的新脏化应尽快再修)。≈ 2.5 游戏天。
        private const int BackoffResetTicks = 150000;

        // 固定间隔 6000t(100 秒)会在"配方原料真没货(非脏化误判)"时, 每 100 秒白烧一次
        // 65~70ms 全量重建、永不停歇。改为指数退避: 同一条失败链上连续重建若都没让配方恢复
        // (下一图仍诊断到脏化 → 说明多半不是脏化而是真没货), 允许间隔按 6000→30000→90000
        // →180000t 逐级拉长, 把无效重建压到几乎不出现; 一旦重建见效(账单不再失败、无新请求),
        // 静默超过 BackoffResetTicks 即在下次触发时归零、恢复灵敏。单次重建本体(约 65ms)不变。
        private static int AllowedGap(int attempts)
        {
            if (attempts <= 1) return 6000;
            if (attempts == 2) return 30000;
            if (attempts == 3) return 90000;
            return 180000;
        }

        private static readonly Dictionary<Map, int> lastRebuildTick = new Dictionary<Map, int>();
        private static readonly Dictionary<Map, int> rebuildAttempts = new Dictionary<Map, int>();

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
            int last, attempts;
            lastRebuildTick.TryGetValue(map, out last);
            rebuildAttempts.TryGetValue(map, out attempts);
            int gap = AllowedGap(attempts);
            if (last != 0 && now - last < gap)
            {
                return;
            }
            // 静默足够久(上次重建后一直没再被请求 → 那次多半真修好了)→ 退避归零, 恢复灵敏。
            if (last != 0 && now - last > BackoffResetTicks)
            {
                attempts = 0;
            }
            attempts++;
            lastRebuildTick[map] = now;
            rebuildAttempts[map] = attempts;
            CleanupDeadMaps();
            if (map.regionAndRoomUpdater == null || !map.regionAndRoomUpdater.Enabled)
            {
                return;
            }
            rebuilding = true;
            try
            {
                map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                Log.Message("[ReloadRegistryFix] rebuilt regions for map " + map.Tile +
                    " (attempt " + attempts + ", next gap " + AllowedGap(attempts) +
                    "t; region/reachability staleness recovery; bill ingredient search should resume)");
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

        private static void CleanupDeadMaps()
        {
            if (lastRebuildTick.Count <= 32)
            {
                return;
            }
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
                rebuildAttempts.Remove(dead[i]);
            }
        }
    }
}

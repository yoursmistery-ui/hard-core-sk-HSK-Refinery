// listerThings / haulSource / 幽灵预留 自愈修复(2026-08-10 读档版;2026-08-16 定稿事件驱动)
//
// 背景: 重复加载存档后,熔炼武器必定「原料不足/需要1×可用原料」,重启消失。
// 用户实测: 把武器拾起再放下即恢复 —— 即物品仍「已生成」(可见、可拾取,
// thingGrid/spawnedThings 正常),但 listerThings 登记丢失,而熔炼搜索
// (PotentiallyMissingIngredients / TryFindBestBillIngredients)只认
// listerThings 中的已生成物品 → 所有武器被判不可用。
// 2026-08-13 扩展: 容器/货架/储物建筑属于 IHaulSource,原料搜索先走
// map.haulDestinationManager.AllHaulSourcesListForReading,而不是 listerThings。
//
// 2026-08-16 定稿(用户反馈「玩着玩着某配方没法制造,衣物就在隔壁衣柜,
// 只保存-重新加载后才能用」;同日复审按用户要求去掉周期轮询,只保留事件驱动):
//   - 原料搜索(WorkGiver_DoBill.TryFindBestBillIngredients)依赖:
//     ① haulDestinationManager(衣柜等存储建筑,内容递归枚举);
//     ② 区域 ListerThings;③ 候选须 CanReserve;
//     而 ReservationManager.ExposeData 只在读档时删幽灵预留
//     (claimant 已死/不在图),这正是「读档后恢复」的来源之一。
//   - 本文件两层自愈(全幂等,健康状态零改动):
//     A) FinalizeLoading postfix: 读档完成即修一次(读档注册表重建的兜底);
//     B) TryFindBestBillIngredients postfix: 原料搜索失败且本图冷却(600 tick)
//        到期 → 立即自愈本图,下一评估 tick 配方恢复。
//   - 根因追踪见 RegistryLeakTracer.cs(异常 Remove 抓调用栈,指认肇事 mod);
//     幽灵预留清理日志附带 JobDef 名(预留里存着 Job,直接指认泄漏的 JobDriver)。
//
// 2026-08-16 深夜追加(用户反馈「玩久了配方报需要原料×1,自愈查不出问题,重载存档消失」):
//   - 日志实证: 失败发生时 things=0/haulSources=0/幽灵预留=0(自愈静默无修复),
//     读档静默重建、且无任何报错的可疑状态只剩两类:
//     a) 活着的小人持有的「死任务预留」(Job 被异常中断未释放;读档时该 Job 引用
//        无法解析 → ReservationManager.ExposeData 直接删除,故重载后消失);
//     b) 区域网格/可达性脏化。
//   - 新增 DiagnoseBlockedIngredients: 搜索失败且常规自愈无事可修时,逐个检查
//     满足配方过滤的候选原料,报告被拒原因(forbidden/被谁以什么任务预留/根 Region
//     为 null/全部可预留→疑似区域脏化);对「claimant 活着但 Job 已不是其当前或
//     队列任务」的泄漏预留,用原版 API Pawn.ClearReservationsForJob 精确释放
//     (只放该任务的预留,不动小人其它合法预留;与读档时的原版清理语义一致)。
//
// 2026-08-17 修正误报根源(用户反馈「腌制肉类/屠宰尸体/制作鹤嘴锄 反复报错,
// 每次带 5 tick 后 region 重建、重建无效且刷屏」):
//   - 旧逻辑用 BillWantsIngredient(任一原料匹配即算候选)统计,再以
//     「候选全未禁止+可预留 → 区域脏化」判定 —— 有系统性误报:
//     · 腌制肉类(SaltMeat,HSK 配方)= 25 生肉 + 5 盐。地图有生肉但没盐时,
//       搜索因缺盐失败;旧逻辑只统计到 3 个生肉候选、全部可预留 → 误判
//       「区域脏化」→ 每 600 tick 刷一条并请求 region 重建。日志实证:
//       prev 会话 15+ 次重建全部无效,且 RegistryLeakTracer 零异常 ——
//       区域网格根本没坏,纯属误报;
//     · 候选不检查可达性: 原料被墙封死时同样误判「区域脏化」。
//   - 新逻辑: 按【每条原料】独立统计候选(filter 匹配 + 数量,stackCount 求和,
//     stuff 配方天然正确),任一原料的「未禁止+可预留+可达」数量不足需求
//     → 真缺料/被占/不可达 → 静默(游戏内 bill 界面会显示 Missing materials),
//     不重建、不刷日志;仅当【每条原料】都有足够可达候选而搜索仍失败
//     (或根 Region 为 null)时才判定区域脏化。另加同 bill 6000 tick 日志去重,
//     根治刷屏。
//   - 同日二次修正(用户反馈「屠宰尸体仍触发重建」): 「可用」再叠加搜索
//     【可发现性】三检查,与 TryFindBestIngredientsHelper 的 BFS 判定完全对齐:
//     ① def.EverHaulable(HaulableEver 组,否则 region BFS 不会枚举);
//     ② 候选须在所在 region 的 ListerThings 有登记(否则 BFS 看不见);
//     ③ 须在 bill.ingredientSearchRadius 半径内(baseValidator 距离过滤)。
//     任一不满足 → 归入「不可见(unfindable)」,不判脏化;region lister 缺登记
//     时用 RegionListersUpdater.RegisterInRegions 幂等补上(自愈,下一评估 tick
//     搜索即恢复)。日志输出附候选详情(usableSample/不可见原因),便于定位。
//
// 2026-08-25 半径外候选误判脏化(用户实测「屠宰尸体/腌制肉类每 100 秒刷一条
// 'likely region/reachability staleness'+触发 region 重建」):
//   - 实例: 屠宰尸体(ButcherCorpseFlesh)bill 的 ingredientSearchRadius=9.79
//     (存档里存的每 bill 自设值),全图人类尸体都在 112-115 格外 → 半径外候选
//     被旧逻辑计为「可用」→ 「每条原料都有足够候选」恒成立 → 恒判脏化。
//   - 机制: 原版 TryFindBestIngredientsHelper 的 baseValidator 用
//     InHorDistOf(searchRadius) 直接拒绝半径外候选,搜索失败是合法缺料;
//     region 重建(RebuildAllRegionsAndRooms)只会重建区域网格,不会让半径外
//     尸体进入半径,重建必然无效 → 纯误报刷屏。
//   - 修复: 半径外(outOfRadius)候选与 notHaulableEver 一起归入「真缺料」,
//     不算可用 → 该原料判 short → 整体不判脏化、不重建、不刷日志(与腌制肉类
//     缺盐的既有处理一致);region lister 缺登记仍单独自愈补上。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ReloadRegistryFix
{
    [StaticConstructorOnStartup]
    public static class ReloadRegistryFixInit
    {
        private const int BillFailCooldownTicks = 600;

        private static bool busy = false;
        private static readonly Dictionary<Map, int> lastBillRepair = new Dictionary<Map, int>();
        private static FieldInfo pirmReservationsField;

        static ReloadRegistryFixInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.reloadregistry");

                MethodInfo finalizeLoading = AccessTools.Method(typeof(ScribeLoader), "FinalizeLoading");
                if (finalizeLoading != null)
                {
                    harmony.Patch(finalizeLoading,
                        postfix: new HarmonyMethod(AccessTools.Method(typeof(ReloadRegistryFixInit), "FinalizeLoadingPostfix")));
                    Log.Message("[ReloadRegistryFix] patched ScribeLoader.FinalizeLoading (load-time registry repair)");
                }
                else
                {
                    Log.Warning("[ReloadRegistryFix] ScribeLoader.FinalizeLoading not found");
                }

                MethodInfo tryFindIngredients = AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients");
                if (tryFindIngredients != null)
                {
                    harmony.Patch(tryFindIngredients,
                        postfix: new HarmonyMethod(AccessTools.Method(typeof(ReloadRegistryFixInit), "BillIngredientsPostfix")));
                    Log.Message("[ReloadRegistryFix] patched WorkGiver_DoBill.TryFindBestBillIngredients (on-fail registry self-heal, " + BillFailCooldownTicks + "t cooldown/map)");
                }
                else
                {
                    Log.Warning("[ReloadRegistryFix] WorkGiver_DoBill.TryFindBestBillIngredients not found");
                }
            }
            catch (Exception e)
            {
                Log.Error("[ReloadRegistryFix] patch failed: " + e);
            }
        }

        // A) 读档完成后立即修一次
        private static void FinalizeLoadingPostfix()
        {
            RepairAllMaps("load");
        }

        // B) 配方原料搜索失败 → 本图立即自愈(冷却防抖;正常缺料时也只是
        //    每 600 tick 多一次幂等扫描,健康状态零改动)
        private static void BillIngredientsPostfix(ref bool __result, Bill bill, Pawn pawn, Thing billGiver)
        {
            try
            {
                if (__result)
                {
                    return;
                }
                if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map == null)
                {
                    return;
                }
                Map map = pawn.Map;
                int now = Find.TickManager.TicksGame;
                int last;
                if (lastBillRepair.TryGetValue(map, out last) && now - last < BillFailCooldownTicks)
                {
                    return;
                }
                lastBillRepair[map] = now;
                if (lastBillRepair.Count > 16)
                {
                    // 清理已销毁地图的冷却记录(量小,简单遍历)
                    List<Map> dead = new List<Map>();
                    foreach (KeyValuePair<Map, int> kv in lastBillRepair)
                    {
                        if (kv.Key == null || kv.Key.Disposed)
                        {
                            dead.Add(kv.Key);
                        }
                    }
                    for (int i = 0; i < dead.Count; i++)
                    {
                        lastBillRepair.Remove(dead[i]);
                    }
                }
                int repaired = RepairMap(map, "bill");
                if (repaired == 0)
                {
                    // 常规自愈无事可修 → 深挖: 诊断候选原料被拒原因,
                    // 外科手术式释放「活人持有的死任务预留」;
                    // 若确认是 region/可达性脏化(rootReg=ok、候选全可预留却搜索失败),
                    // 请求一次延迟到普通地图 tick 的全量 region 重建。
                    bool regionStale = DiagnoseBlockedIngredients(bill, pawn, billGiver, map);
                    if (regionStale)
                    {
                        RegionRepairMapComponent.RequestRebuild(map);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("[ReloadRegistryFix] on-fail self-heal failed: " + e);
            }
        }

        private static void RepairAllMaps(string context)
        {
            if (Find.Maps == null)
            {
                return;
            }
            for (int m = 0; m < Find.Maps.Count; m++)
            {
                Map map = Find.Maps[m];
                if (map == null || map.spawnedThings == null || map.listerThings == null ||
                    map.haulDestinationManager == null)
                {
                    continue;
                }
                RepairMap(map, context);
            }
        }

        private static int RepairMap(Map map, string context)
        {
            if (busy)
            {
                return 0;
            }
            busy = true;
            int repairedTotal = 0;
            try
            {
                int repairedThings = 0;
                int repairedSources = 0;
                int releasedReservations = 0;
                int releasedPhysical = 0;
                List<string> samples = new List<string>();

                HashSet<Thing> listed = new HashSet<Thing>(map.listerThings.AllThings);
                List<IHaulSource> existingSources = map.haulDestinationManager.AllHaulSourcesListForReading;
                foreach (Thing thing in map.spawnedThings)
                {
                    if (thing == null || thing.Destroyed || !thing.Spawned)
                    {
                        continue;
                    }
                    // Mote(瞬态特效)不是配方原料: 原版 SpawnSetup 短暂注册后即销毁,
                    // 每次自愈扫描时总有一批正在退场的 Mote 不在 listerThings ——
                    // 补登记它们毫无意义,只会刷屏「repaired things=N (全是 Mote)」。
                    if (thing is Mote)
                    {
                        continue;
                    }
                    if (!listed.Contains(thing))
                    {
                        map.listerThings.Add(thing);
                        repairedThings++;
                        if (samples.Count < 20)
                        {
                            samples.Add("thing:" + Describe(thing));
                        }
                    }
                    RegionListersUpdater.RegisterInRegions(thing, map);

                    IHaulSource source = thing as IHaulSource;
                    if (source != null && !existingSources.Contains(source))
                    {
                        map.haulDestinationManager.AddHaulSource(source);
                        repairedSources++;
                        if (samples.Count < 20)
                        {
                            samples.Add("haulsrc:" + Describe(thing));
                        }
                    }
                }

                CleanGhostReservations(map, ref releasedReservations, ref releasedPhysical, samples);

                repairedTotal = repairedThings + repairedSources + releasedReservations + releasedPhysical;
                if (repairedTotal > 0)
                {
                    Log.Message("[ReloadRegistryFix/" + context + "] repaired map " + map.Tile + ": things=" + repairedThings +
                        " haulSources=" + repairedSources + " ghostReservations=" + releasedReservations +
                        " ghostPhysical=" + releasedPhysical + FormatNames(samples));
                }
                return repairedTotal;
            }
            catch (Exception e)
            {
                Log.Error("[ReloadRegistryFix] repair failed: " + e);
                return repairedTotal;
            }
            finally
            {
                busy = false;
            }
        }

        // ============ 失败诊断 + 死任务预留外科手术(2026-08-16;2026-08-17 修正误报根源) ============

        private static readonly List<Verse.AI.ReservationManager.Reservation> tmpResEntries = new List<Verse.AI.ReservationManager.Reservation>();

        // 同一条 bill 的诊断日志去重(6000 tick ≈ 100 秒): 真缺料等非脏化情况默认静默,
        // 脏化/动手释放也最多每 100 秒重报一次,根治刷屏。
        private static readonly Dictionary<Bill, int> lastDiagLog = new Dictionary<Bill, int>();
        private const int DiagLogCooldownTicks = 6000;

        // 预留的 Job 是否仍是 claimant 的当前/队列任务(任务结束时原版会同步释放
        // 预留 → 不匹配即泄漏;jobs==null 无法判定时保守视为合法)
        private static bool JobIsCurrentOrQueued(Pawn p, Job job)
        {
            if (p.jobs == null)
            {
                return true;
            }
            if (p.CurJob == job)
            {
                return true;
            }
            Verse.AI.JobQueue q = p.jobs.jobQueue;
            if (q != null)
            {
                int n = q.Count;
                for (int i = 0; i < n; i++)
                {
                    if (q[i] != null && q[i].job == job)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void AddNote(List<string> notes, string s)
        {
            if (notes.Count < 8)
            {
                notes.Add(s);
            }
        }

        // 释放目标上的「死任务预留」: 预留的 Job 已不是 claimant 的当前/队列任务 = 泄漏
        // (与读档时原版删除 null-job 预留的语义一致,但只针对本配方的原料,影响面最小)
        private static void ReleaseStaleReservationsOn(Thing t, Pawn pawn, Map map, ref int staleReleased, List<string> notes)
        {
            try
            {
                List<Verse.AI.ReservationManager.Reservation> resList = map.reservationManager.ReservationsReadOnly;
                tmpResEntries.Clear();
                for (int r = 0; r < resList.Count; r++)
                {
                    Verse.AI.ReservationManager.Reservation res = resList[r];
                    if (res != null && res.Target.HasThing && res.Target.Thing == t)
                    {
                        tmpResEntries.Add(res);
                    }
                }
                for (int r = 0; r < tmpResEntries.Count; r++)
                {
                    Verse.AI.ReservationManager.Reservation res = tmpResEntries[r];
                    Pawn c = res.Claimant;
                    if (c == null || IsGhostClaimant(c, map))
                    {
                        // 幽灵 claimant(已死/离图/null): 常规自愈 CleanGhostReservations 已处理
                        continue;
                    }
                    if (res.Job != null && JobIsCurrentOrQueued(c, res.Job))
                    {
                        // 另一小人当前任务正在用 → 合法预留,非泄漏
                        continue;
                    }
                    string jobName = (res.Job != null && res.Job.def != null) ? res.Job.def.defName : "null";
                    string curName = (c.CurJob != null && c.CurJob.def != null) ? c.CurJob.def.defName : "none";
                    int before = resList.Count;
                    c.ClearReservationsForJob(res.Job);
                    int delta = before - resList.Count;
                    if (delta > 0)
                    {
                        staleReleased += delta;
                        AddNote(notes, Describe(t) + "->releasedStale:" + c.LabelShort +
                            "(job=" + jobName + ",cur=" + curName + ") x" + delta);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[ReloadRegistryFix] release-stale failed: " + e.Message);
            }
        }

        // —— NoMix 口径辅助(2026-09-08): 全部只在诊断冷却命中时调用, 非热路径 ——
        private static void AddUsableByDef(Dictionary<ThingDef, float> byDef, ThingDef def, float qty)
        {
            if (def == null)
            {
                return;
            }
            float cur;
            byDef.TryGetValue(def, out cur);
            byDef[def] = cur + qty;
        }

        private static float MaxByDef(Dictionary<ThingDef, float> byDef)
        {
            float mx = 0f;
            foreach (KeyValuePair<ThingDef, float> kv in byDef)
            {
                if (kv.Value > mx)
                {
                    mx = kv.Value;
                }
            }
            return mx;
        }

        private static string BestDefTag(Dictionary<ThingDef, float> byDef)
        {
            ThingDef best = null;
            float mx = 0f;
            foreach (KeyValuePair<ThingDef, float> kv in byDef)
            {
                if (kv.Value > mx)
                {
                    mx = kv.Value;
                    best = kv.Key;
                }
            }
            return (best != null ? best.defName : "-") + "=" + mx.ToString("0.#");
        }

        private static bool EnoughUsable(bool allowMix, float total, Dictionary<ThingDef, float> byDef, float need)
        {
            if (total + 0.001f < need)
            {
                return false;
            }
            return allowMix || MaxByDef(byDef) + 0.001f >= need;
        }

        // 搜索失败且常规自愈无事可修 → 按【每条原料】统计候选并诊断;
        // 顺手释放「活着的小人持有的死任务预留」(与读档时原版清理语义一致)。
        // 返回 true 表示确认是 region/可达性脏化(根 Region 为 null,或每条原料都有
        // 足够「未禁止+可预留+可达」候选但搜索仍失败),调用方应触发一次延迟的
        // 全量 region 重建。
        private static bool DiagnoseBlockedIngredients(Bill bill, Pawn pawn, Thing billGiver, Map map)
        {
            if (bill == null || bill.recipe == null || bill.recipe.ingredients == null || bill.recipe.ingredients.Count == 0)
            {
                return false;
            }
            try
            {
                // 工作台根 Region(搜索起点;为 null = 区域网格没建的直接证据)
                IntVec3 rootCell = pawn.Position;
                Building building = billGiver as Building;
                if (building != null && building.def.hasInteractionCell)
                {
                    rootCell = building.InteractionCell;
                }
                Region rootReg = rootCell.GetRegion(map);
                string rootRegState = (rootReg != null) ? "ok" : "NULL";

                List<Verse.AI.ReservationManager.Reservation> resList = map.reservationManager.ReservationsReadOnly;
                List<Thing> allThings = map.listerThings.AllThings;

                // —— 逐条原料统计候选(2026-08-17: 修正「任一匹配」系统性误报;
                // 2026-08-25: outOfRadius 归入真缺料,不再判脏化)——
                // 旧逻辑: 候选=匹配任一原料的物品,只要候选全未禁止+可预留就判「区域脏化」。
                // 腌制肉类(SaltMeat)= 25 生肉 + 5 盐: 有生肉无盐时搜索必然失败,但旧逻辑
                // 只数到生肉 → 误判脏化 → 每 600 tick 刷一条 + 触发无效 region 重建。
                // 新逻辑: 每条原料独立统计,任一原料可用候选不足 → 真缺料,非脏化;
                // 「可用」= 未禁止 + 可预留 + 可达 + 搜索可发现(HaulableEver 组 +
                // region lister 有登记 + 在 ingredientSearchRadius 内)——与
                // WorkGiver_DoBill.TryFindBestIngredientsHelper 的 BFS 判定完全对齐,
                // 杜绝「候选在但搜索看不见」的又一类误报。
                // 2026-08-25 再修正: 半径外候选(outOfRadius)也必须是「真缺料」——
                // 原版搜索的 baseValidator 会直接拒绝半径外的候选,搜索失败是合法的,
                // 不是 region 脏化。若把半径外候选算「可用」,「每条原料都有足够候选」
                // 恒成立 → 永远误判脏化 → 每 6000 tick 刷「likely region/reachability
                // staleness」并触发无效的 RebuildAllRegionsAndRooms。
                // 实例(玩家实测,存档 2026-08-25 Autosave-4): 屠宰尸体 bill 半径 9.79,
                // 全图 4 具人类尸体都在 112-115 格外 → 每条约 100 秒刷一条 + 反复重建;
                // 重建后搜索依旧失败(尸体就是不在半径内),证明重建无效、纯属误报。
                List<IngredientCount> ings = bill.recipe.ingredients;
                float searchRadius = bill.ingredientSearchRadius;
                float radiusSq = searchRadius * searchRadius;
                bool anyIngredientShort = false;   // 任一原料可用候选不足(真缺料/被占/不可达/不可见)
                int staleReleased = 0;
                int reRegistered = 0;              // region lister 缺登记,已补(自愈)
                List<string> notes = new List<string>();

                for (int ii = 0; ii < ings.Count; ii++)
                {
                    IngredientCount ing = ings[ii];
                    if (ing.filter == null)
                    {
                        continue;
                    }
                    float need = ing.GetBaseCount();
                    // 2026-09-08 二修: 真实搜索在 allowMixingIngredients=false 时走 NoMix 选择器
                    // (TryFindBestIngredientsInSet_NoMixHelper): 按 def 聚合可用量, 槽内【禁止跨
                    // def 混料】——单一 def 聚合不够数即失败。旧诊断把所有 def 混着累加,
                    // "皮60+布80 需皮类120" 会误判候选充足 → 又一轮无效 region 重建。
                    // 现按 def 分桶: allowMix 配方用总量判定, 否则用单 def 最大量判定。
                    bool allowMix = bill.recipe.allowMixingIngredients;
                    Dictionary<ThingDef, float> usableByDef = new Dictionary<ThingDef, float>();
                    float usable = 0f;
                    float reservedQty = 0f;
                    float forbiddenQty = 0f;
                    float unreachableQty = 0f;
                    float unfindableQty = 0f;
                    float outOfRadiusQty = 0f;
                    int scanned = 0;
                    string usableSample = null;
                    for (int i = 0; i < allThings.Count && scanned < 48 &&
                        !EnoughUsable(allowMix, usable, usableByDef, need); i++)
                    {
                        Thing t = allThings[i];
                        if (t == null || t.Destroyed || !t.Spawned || t is Mote)
                        {
                            continue;
                        }
                        if (!ing.filter.Allows(t) || !bill.IsFixedOrAllowedIngredient(t))
                        {
                            continue;
                        }
                        scanned++;
                        // 2026-09-08: 口径对齐真实搜索(TryFindBestBillIngredientsInSet_AllowMix):
                        // need=GetBaseCount() 与可用量都必须按 recipe.ingredientValueGetter 的
                        // 单位计(营养值配方按营养, 件数配方按件)。旧写法 max(1,stackCount) 用
                        // 件数去比营养需求, 高估 ~20 倍 → 真缺料被误判区域脏化 → 反复无效
                        // region 全量重建(百毫秒级卡顿)+ 诊断行每冷却期刷一条。
                        float qty = bill.recipe.IngredientValueGetter.ValuePerUnitOf(t.def) * (float)t.stackCount;
                        if (qty <= 0.0001f)
                        {
                            continue;
                        }
                        if (t.IsForbidden(pawn))
                        {
                            forbiddenQty += qty;
                            continue;
                        }
                        if (!map.reservationManager.CanReserve(pawn, t))
                        {
                            reservedQty += qty;
                            // 顺手释放「活人持有的死任务预留」,下一评估 tick 配方即恢复
                            ReleaseStaleReservationsOn(t, pawn, map, ref staleReleased, notes);
                            continue;
                        }
                        if (!pawn.CanReach(t, PathEndMode.Touch, pawn.NormalMaxDanger()))
                        {
                            unreachableQty += qty;
                            continue;
                        }
                        // 搜索「可发现性」三检查(镜像 TryFindBestIngredientsHelper 的 BFS 路径):
                        // ① HaulableEver 组(r.ListerThings.ThingsMatching(HaulableEver));
                        // ② 候选在所在 region 的 ListerThings 中有登记(否则 BFS 看不见);
                        // ③ 在 bill.ingredientSearchRadius 范围内(baseValidator 距离过滤)。
                        Region reg = t.Position.GetRegion(map);
                        bool inRegionLister = reg != null && reg.valid && reg.ListerThings != null &&
                            reg.ListerThings.Contains(t);
                        bool inRadius = (t.Position - billGiver.Position).LengthHorizontalSquared < radiusSq;
                        if (!t.def.EverHaulable || !inRadius)
                        {
                            // 不可搬运/半径外 → 原版搜索 baseValidator 直接拒绝,搜索失败合法,
                            // 归入「不可见(真缺料)」,不算可用,不判脏化(2026-08-25)。
                            // 半径外候选大量存在时,这一条原料必然判 short → 整体不判脏化,
                            // 不再误报「likely region/reachability staleness」。
                            unfindableQty += qty;
                            if (!t.def.EverHaulable)
                            {
                                AddNote(notes, Describe(t) + "->notHaulableEver");
                            }
                            else
                            {
                                outOfRadiusQty += qty;
                                AddNote(notes, Describe(t) + "->outOfRadius(need<=" + searchRadius +
                                    ",dist=" + (int)Math.Sqrt((double)(t.Position - billGiver.Position).LengthHorizontalSquared) + ")");
                            }
                            continue;
                        }
                        if (!inRegionLister)
                        {
                            // region lister 缺登记: 补上(幂等),下一评估 tick 搜索即恢复
                            RegionListersUpdater.RegisterInRegions(t, map);
                            reRegistered++;
                            AddNote(notes, Describe(t) + "->reRegisteredRegionLister");
                            usable += qty;
                            AddUsableByDef(usableByDef, t.def, qty);
                            if (usableSample == null)
                            {
                                usableSample = Describe(t) + "(lister=" + inRegionLister + ",dist=" +
                                    (int)Math.Sqrt((double)(t.Position - billGiver.Position).LengthHorizontalSquared) + ")";
                            }
                            continue;
                        }
                        usable += qty;
                        AddUsableByDef(usableByDef, t.def, qty);
                        if (usableSample == null)
                        {
                            usableSample = Describe(t) + "(lister=" + inRegionLister + ",dist=" +
                                (int)Math.Sqrt((double)(t.Position - billGiver.Position).LengthHorizontalSquared) + ")";
                        }
                    }
                    float effective = allowMix ? usable : MaxByDef(usableByDef);
                    if (effective + 0.001f < need)
                    {
                        anyIngredientShort = true;
                        string effTag = allowMix ? "" : (" 单def最大=" + BestDefTag(usableByDef));
                        AddNote(notes, ing.Summary + " need=" + need + " usable=" + effective + effTag +
                            " (总量=" + usable + ")" +
                            " forbidden=" + forbiddenQty + " reserved=" + reservedQty +
                            " unreachable=" + unreachableQty + " unfindable=" + unfindableQty +
                            " outOfRadius=" + outOfRadiusQty);
                    }
                    else if (usableSample != null)
                    {
                        AddNote(notes, "usableSample:" + usableSample);
                    }
                }

                // region 脏化判定: 根 Region 缺失(rootReg==null = 区域网格没建),
                // 或【每条原料】都有足够「未禁止+可预留+可达+搜索可发现」候选但搜索仍失败
                // (region 链接/BFS 到不了原料所在 region)。
                bool staleness = rootReg == null || !anyIngredientShort;

                // 日志去重: 同一条 bill 6000 tick 内只输出一次(真缺料/被占/不可达默认静默)
                int now = Find.TickManager.TicksGame;
                bool alreadyLogged = false;
                int lastLog;
                if (lastDiagLog.TryGetValue(bill, out lastLog) && now - lastLog < DiagLogCooldownTicks)
                {
                    alreadyLogged = true;
                }
                else
                {
                    lastDiagLog[bill] = now;
                }
                if (lastDiagLog.Count > 48)
                {
                    // 清理已冷却完毕的旧记录(量小,简单遍历)
                    List<Bill> dead = new List<Bill>();
                    foreach (KeyValuePair<Bill, int> kv in lastDiagLog)
                    {
                        if (now - kv.Value >= DiagLogCooldownTicks)
                        {
                            dead.Add(kv.Key);
                        }
                    }
                    for (int i = 0; i < dead.Count; i++)
                    {
                        lastDiagLog.Remove(dead[i]);
                    }
                }

                if (staleness)
                {
                    RegionRepairMapComponent.RequestRebuild(map);
                }
                if (!alreadyLogged)
                {
                    if (staleReleased > 0 || reRegistered > 0)
                    {
                        // 实际动手(释放泄漏预留 / 补 region lister 登记)后,下次搜索应即恢复
                        Log.Warning("[ReloadRegistryFix/bill] bill '" + bill.Label + "' (pawn " + pawn.LabelShort +
                            "): released " + staleReleased + " stale reservations, re-registered " + reRegistered +
                            " region-lister entries (rootReg=" + rootRegState + ")" + FormatNames(notes));
                    }
                    else if (staleness)
                    {
                        Log.Warning("[ReloadRegistryFix/diag] bill '" + bill.Label + "' (pawn " + pawn.LabelShort +
                            ") failed, but every ingredient has search-findable+reachable+unforbidden+reservable candidates" +
                            " → likely region/reachability staleness (rootReg=" + rootRegState + "), requesting region rebuild" +
                            FormatNames(notes) + "\n" + TerrainChangeTracer.Dump());
                    }
                    // else: 真缺料/被占/不可达/不可见 → 静默(游戏内 bill 界面会显示 Missing materials)
                }
                return staleness;
            }
            catch (Exception e)
            {
                Log.Warning("[ReloadRegistryFix] diagnose failed: " + e.Message);
            }
            return false;
        }

        private static bool IsGhostClaimant(Pawn p, Map map)
        {
            if (p == null || p.Destroyed || !p.Spawned)
            {
                return true;
            }
            return p.Map != map;
        }

        private static void CleanGhostReservations(Map map, ref int releasedReservations, ref int releasedPhysical, List<string> samples)
        {
            // 1) ReservationManager(配方/搬运预留;ReservationsReadOnly 即内部列表)
            try
            {
                List<Verse.AI.ReservationManager.Reservation> list = map.reservationManager.ReservationsReadOnly;
                if (list != null && list.Count > 0)
                {
                    // 先收集幽灵 claimant(与被删预留的 JobDef —— 预留里存着 Job,
                    // 可直接指认是哪个 JobDriver 泄漏的预留)
                    HashSet<Pawn> badPawns = null;
                    List<string> badJobs = new List<string>();
                    bool hasNull = false;
                    for (int i = 0; i < list.Count; i++)
                    {
                        Pawn c = list[i].Claimant;
                        if (c == null)
                        {
                            hasNull = true;
                            AddJobName(badJobs, list[i]);
                        }
                        else if (IsGhostClaimant(c, map))
                        {
                            if (badPawns == null)
                            {
                                badPawns = new HashSet<Pawn>();
                            }
                            badPawns.Add(c);
                            AddJobName(badJobs, list[i]);
                        }
                    }
                    if (hasNull)
                    {
                        for (int i = list.Count - 1; i >= 0; i--)
                        {
                            if (list[i].Claimant == null)
                            {
                                if (samples.Count < 20)
                                {
                                    samples.Add("res:null->" + LocalTargetDesc(list[i].Target));
                                }
                                list.RemoveAt(i);
                                releasedReservations++;
                            }
                        }
                    }
                    if (badPawns != null)
                    {
                        foreach (Pawn p in badPawns)
                        {
                            int before = list.Count;
                            map.reservationManager.ReleaseAllClaimedBy(p);
                            int n = before - list.Count;
                            if (n > 0)
                            {
                                releasedReservations += n;
                                if (samples.Count < 20)
                                {
                                    samples.Add("res:" + p.LabelShort + " x" + n);
                                }
                            }
                        }
                    }
                    if (badJobs.Count > 0)
                    {
                        samples.Add("resJobs:" + string.Join(",", badJobs.ToArray()));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[ReloadRegistryFix] reservation clean failed: " + e.Message);
            }

            // 2) PhysicalInteractionReservationManager(CanReserve 同样检查它;
            //    内部 reservations 列表反射读取,元素字段 claimant/target 为 public)
            try
            {
                PhysicalInteractionReservationManager mgr = map.physicalInteractionReservationManager;
                if (mgr != null)
                {
                    if (pirmReservationsField == null)
                    {
                        pirmReservationsField = AccessTools.Field(typeof(PhysicalInteractionReservationManager), "reservations");
                    }
                    if (pirmReservationsField != null)
                    {
                        List<PhysicalInteractionReservationManager.PhysicalInteractionReservation> plist =
                            pirmReservationsField.GetValue(mgr) as List<PhysicalInteractionReservationManager.PhysicalInteractionReservation>;
                        if (plist != null && plist.Count > 0)
                        {
                            HashSet<Pawn> badPawns = null;
                            List<string> badJobs = new List<string>();
                            bool hasNull = false;
                            for (int i = 0; i < plist.Count; i++)
                            {
                                Pawn c = plist[i].claimant;
                                if (c == null)
                                {
                                    hasNull = true;
                                }
                                else if (IsGhostClaimant(c, map))
                                {
                                    if (badPawns == null)
                                    {
                                        badPawns = new HashSet<Pawn>();
                                    }
                                    badPawns.Add(c);
                                    AddJobName(badJobs, plist[i].job, c.LabelShort);
                                }
                            }
                            if (hasNull)
                            {
                                for (int i = plist.Count - 1; i >= 0; i--)
                                {
                                    if (plist[i].claimant == null)
                                    {
                                        if (samples.Count < 20)
                                        {
                                            samples.Add("phys:null->" + LocalTargetDesc(plist[i].target));
                                        }
                                        plist.RemoveAt(i);
                                        releasedPhysical++;
                                    }
                                }
                            }
                            if (badPawns != null)
                            {
                                foreach (Pawn p in badPawns)
                                {
                                    int before = plist.Count;
                                    mgr.ReleaseAllClaimedBy(p);
                                    int n = before - plist.Count;
                                    if (n > 0)
                                    {
                                        releasedPhysical += n;
                                        if (samples.Count < 20)
                                        {
                                            samples.Add("phys:" + p.LabelShort + " x" + n);
                                        }
                                    }
                                }
                            }
                            if (badJobs.Count > 0)
                            {
                                samples.Add("physJobs:" + string.Join(",", badJobs.ToArray()));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[ReloadRegistryFix] physical reservation clean failed: " + e.Message);
            }
        }

        private static void AddJobName(List<string> jobs, Verse.AI.ReservationManager.Reservation r)
        {
            if (r == null || jobs.Count >= 8)
            {
                return;
            }
            string name = (r.Job != null && r.Job.def != null) ? r.Job.def.defName : "null";
            if (!jobs.Contains(name))
            {
                jobs.Add(name);
            }
        }

        private static void AddJobName(List<string> jobs, Job job, string pawnName)
        {
            if (jobs.Count >= 8)
            {
                return;
            }
            string name = ((job != null && job.def != null) ? job.def.defName : "null") + "@" + pawnName;
            if (!jobs.Contains(name))
            {
                jobs.Add(name);
            }
        }

        private static string Describe(Thing thing)
        {
            if (thing == null)
            {
                return "null";
            }
            string defName = (thing.def != null) ? thing.def.defName : "null";
            return defName + "@" + thing.Position;
        }

        private static string LocalTargetDesc(LocalTargetInfo t)
        {
            if (t.IsValid && t.HasThing && t.Thing != null)
            {
                return Describe(t.Thing);
            }
            return t.ToStringSafe();
        }

        private static string FormatNames(List<string> names)
        {
            if (names == null || names.Count == 0)
            {
                return "";
            }
            string result = " (" + string.Join(", ", names.ToArray());
            return result + ")";
        }
    }
}

// ============================================================================
//  BlueprintUnlockHSK — 科技蓝图「书籍化门控」机制 (HSK 本地整合, v4.0)
// ============================================================================
//  功能:
//    1) 每个门控科技对应「一套蓝图书」 (BlueprintTargetExtension + BookOutcomeProperties)
//       - 中世纪 = 图纸书 1 本/系列; 工业后 = 蓝图 N 本/系列 (I/II/III/IV)
//    2) 书 = 原版 Book 体系 (thingClass=Book): 可放书架、阅读、读后保留、带「已读」标记
//    3) 阅读 = 科研工作 (JobDriver_StudyBlueprint, workType=Research):
//       - 非休闲阅读 (不吃 joy), 加智力经验, 受 ResearchSpeed/智力水平加成
//    4) 门禁: 系列所有书都被「研读」过 → 科技解锁 → CanStartNow 才为 true
//       (不做中间科研值, 解锁后研究从 0 开始; 每本只需 1 人读 1 次, 第二次读无效果)
//    5) 科研树界面 (ResearchTreeSK) 节点角标: 已读 x/N, 集齐变绿
//  历史:
//    v3.x (2026-08-24): 存储池 + 使用即得 (CompUseEffect_Blueprint), 科研值银行
//    v4.0 (2026-08-27): 用户决策 D1~D11 → 全部书籍化, 删除存储池/CompUsable/进度条 UI
//  配置: 全部通过蓝图书 ThingDef 的 BlueprintTargetExtension (Defs 里改, 无需重编译)
//  编译: 用同目录 build.ps1 (csc.exe, .NET Framework 4.0)
//  注意: 本文件为 C#5 兼容写法 (无 $"" 插值 / 无 ?. / 无局部函数 / 无 out var 内联)
// ============================================================================
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BlueprintUnlockHSK
{
    // ---- Harmony 注册 ----
    [StaticConstructorOnStartup]
    public static class BlueprintUnlockInit
    {
        static BlueprintUnlockInit()
        {
            Harmony harmony = new Harmony("local.ratkin.blueprintunlockhsk");
            harmony.PatchAll();

            // 科研树节点徽标: 在每个门控科技节点上直接绘制 "已读X/N" 角标
            // (挂在 ResearchTreeSK.Node.Draw, 未装 ResearchTreeSK 时自动跳过)
            System.Type nodeType = AccessTools.TypeByName("ResearchTreeSK.Node");
            if (nodeType != null)
            {
                try
                {
                    harmony.Patch(AccessTools.Method(nodeType, "Draw"),
                        postfix: new HarmonyMethod(typeof(Patch_ResearchTreeNodeBadge).GetMethod("Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] 科研树节点徽标补丁失败(已跳过): " + e.Message);
                }

                // 门控科技未集齐蓝图 → 强制节点 Available=false:
                // ResearchTreeSK 的置灰/锁图标/禁止点击全部只看 Node.Available, 不看 CanStartNow,
                // 所以仅靠 Patch_CanStartNow 无法让科研树节点变灰不可点。这里补上科研树侧的门禁。
                try
                {
                    harmony.Patch(AccessTools.Method(nodeType, "UpdateCaches"),
                        postfix: new HarmonyMethod(typeof(Patch_NodeUpdateCaches).GetMethod("Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] 科研树节点门禁补丁失败(已跳过): " + e.Message);
                }
            }

            // ---- 事件/任务蓝图投放 (全部按类型存在性门控, 未装对应 mod 自动跳过) ----

            // ScatterAt 均为 protected (IntVec3, Map, GenStepParams, int) 重载, 继承链同名,
            // 必须显式参数类型查找, 避免 name-only 歧义
            System.Type[] scatterAtParams = new System.Type[]
            {
                typeof(IntVec3), typeof(Map), typeof(GenStepParams), typeof(int)
            };

            // 物品贮藏任务 (RimQuest 用原版 QuestScriptDef): 藏宝处放一张蓝图书
            try
            {
                harmony.Patch(AccessTools.Method(typeof(GenStep_ItemStash), "ScatterAt", scatterAtParams),
                    postfix: new HarmonyMethod(typeof(Patch_ItemStash).GetMethod("ScatterAt_Postfix")));
            }
            catch (System.Exception e)
            {
                Log.Warning("[BlueprintUnlockHSK] 物品贮藏补丁失败(已跳过): " + e.Message);
            }

            // Go Explore: 战利品生成后注入蓝图书 (失落之城/监狱营/拦截消息等)
            System.Type goEx = AccessTools.TypeByName("LetsGoExplore.RewardGeneratorUtilityLGE");
            if (goEx != null)
            {
                try
                {
                    harmony.Patch(AccessTools.Method(goEx, "GenerateStockpileReward"),
                        postfix: new HarmonyMethod(typeof(Patch_GoExploreRewards).GetMethod("Stockpile_Postfix")));
                    harmony.Patch(AccessTools.Method(goEx, "GenerateStorageBoxReward"),
                        postfix: new HarmonyMethod(typeof(Patch_GoExploreRewards).GetMethod("StorageBox_Postfix")));
                    harmony.Patch(AccessTools.Method(goEx, "GenerateInterceptedMessageReward"),
                        postfix: new HarmonyMethod(typeof(Patch_GoExploreRewards).GetMethod("Intercepted_Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] GoExplore 奖励补丁失败(已跳过): " + e.Message);
                }
            }

            // Cybranian Events: 陨石坠落处放蓝图书 + 老人随身带蓝图书
            System.Type meteor = AccessTools.TypeByName("EventsCore.GenSteps.GenStep_WorldMeteorite");
            if (meteor != null)
            {
                try
                {
                    harmony.Patch(AccessTools.Method(meteor, "ScatterAt", scatterAtParams),
                        postfix: new HarmonyMethod(typeof(Patch_CybranianEvents).GetMethod("Meteorite_Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] Cybranian 陨石补丁失败(已跳过): " + e.Message);
                }
            }
            // Cybranian Events: 老人随身带蓝图书。
            // AddSpawnPawnQuestParts(Quest, Map, Pawn) 是虚拟方法, 声明在基类 QuestNode_Root_WandererJoin_WalkIn (原版),
            // OldMan 只继承不重写 → Harmony 拒绝 patch 继承方法 (报 "Patch the declared method ... instead"),
            // 必须改打声明它的基类方法本身。打基类后会对所有 walk-in 流浪者任务触发,
            // 故在 OldMan_Postfix 内按 __instance 真实类型名门控, 仅处理 Cybranian 的 OldMan 任务。
            System.Type oldMan = AccessTools.TypeByName("EventsCore.Quests.QuestNode_Root_WandererJoin_OldMan");
            if (oldMan != null)
            {
                try
                {
                    harmony.Patch(
                        AccessTools.Method(typeof(RimWorld.QuestGen.QuestNode_Root_WandererJoin_WalkIn), "AddSpawnPawnQuestParts"),
                        postfix: new HarmonyMethod(typeof(Patch_CybranianEvents).GetMethod("OldMan_Postfix")));
                }
                catch (System.Exception e)
                {
                    Log.Warning("[BlueprintUnlockHSK] Cybranian 老人补丁失败(已跳过): " + e.Message);
                }
            }
        }
    }

    // ============================================================================
    //  蓝图书侧配置: 目标科技 + 分级 + 系列参数 (书籍化 v4)
    // ============================================================================
    //  系列推导: 同一 targetTech 的全部蓝图书 def 组成一个系列 (中世纪 1 本,
    //  太空 2 本, 极致 3 本, 超凡 4 本)。seriesTotal = 同 targetTech 的 def 数;
    //  seriesIndex = XML 可显式给 bookIndex, 缺省按 defName 排序 (I/II/III 自然有序)。
    public class BlueprintTargetExtension : DefModExtension
    {
        public string targetTech = "";
        public int tier = 0;                 // 1=中世纪 2=工业 3=太空 4=极致 5=超凡 (0=按 targetTech 时代推断)
        public float researchPoints = 500f;  // 保留字段 (供商人定价/投放权重参考, 不再直接加科研值)
        public int bookIndex = 0;            // 系列序号 (1..N), 0 = 按 defName 排序自动推导

        public static BlueprintTargetExtension Get(ThingDef def)
        {
            if (def == null)
            {
                return null;
            }
            return def.GetModExtension<BlueprintTargetExtension>();
        }

        // 分级: Defs 未显式给 tier 时, 按目标科技时代推断 (HSK 部分节点未声明 techLevel 则回退 3)
        public static int GetTier(BlueprintTargetExtension ext)
        {
            if (ext != null && ext.tier > 0)
            {
                return ext.tier;
            }
            if (ext != null && !string.IsNullOrEmpty(ext.targetTech))
            {
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
                if (p != null)
                {
                    return TechLevelToTier(p.techLevel);
                }
            }
            return 3;
        }

        private static int TechLevelToTier(TechLevel tl)
        {
            switch (tl)
            {
                case TechLevel.Industrial: return 2;
                case TechLevel.Spacer: return 3;
                case TechLevel.Ultra: return 4;
                case TechLevel.Archotech: return 5;
                default: return 1; // Undefined / Animal / Neolithic / Medieval
            }
        }
    }

    // ============================================================================
    //  门控数据库: 扫描全部蓝图书 ThingDef, 建立 tech → 蓝图书系列 映射
    // ============================================================================
    public static class BlueprintGateDatabase
    {
        private static Dictionary<string, List<ThingDef>> _map;
        private static bool _built;

        public static void Rebuild()
        {
            _map = new Dictionary<string, List<ThingDef>>();
            List<ThingDef> all = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                if (ext != null && !string.IsNullOrEmpty(ext.targetTech))
                {
                    List<ThingDef> list;
                    if (!_map.TryGetValue(ext.targetTech, out list))
                    {
                        list = new List<ThingDef>();
                        _map.Add(ext.targetTech, list);
                    }
                    if (!list.Contains(all[i]))
                    {
                        list.Add(all[i]);
                    }
                }
            }
            // 按系列序号/defName 排序, 保证 seriesIndex 稳定 (I/II/III/IV)
            foreach (KeyValuePair<string, List<ThingDef>> kv in _map)
            {
                kv.Value.Sort(delegate (ThingDef a, ThingDef b)
                {
                    int ia = GetSeriesIndex(a);
                    int ib = GetSeriesIndex(b);
                    if (ia != ib)
                    {
                        return ia.CompareTo(ib);
                    }
                    return string.CompareOrdinal(a.defName, b.defName);
                });
            }
            _built = true;
        }

        public static BlueprintTargetExtension GetExtensionForTech(string techDefName)
        {
            if (!_built)
            {
                Rebuild();
            }
            List<ThingDef> list;
            if (_map != null && _map.TryGetValue(techDefName, out list) && list != null && list.Count > 0)
            {
                return BlueprintTargetExtension.Get(list[0]);
            }
            return null;
        }

        // 该科技的全部蓝图书 def (按系列序号排序); 无则空表
        public static List<ThingDef> GetSeries(string techDefName)
        {
            if (!_built)
            {
                Rebuild();
            }
            List<ThingDef> list;
            if (_map != null && _map.TryGetValue(techDefName, out list) && list != null)
            {
                return new List<ThingDef>(list);
            }
            return new List<ThingDef>();
        }

        // 某本蓝图书在系列中的序号 (1..N); XML 未显式给 bookIndex 时按 defName 排序推导
        public static int GetSeriesIndex(ThingDef def)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
            if (ext != null && ext.bookIndex > 0)
            {
                return ext.bookIndex;
            }
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return 1;
            }
            List<ThingDef> series = GetSeries(ext.targetTech);
            for (int i = 0; i < series.Count; i++)
            {
                if (series[i] == def)
                {
                    return i + 1;
                }
            }
            return 1;
        }

        // 系列总本数 (某科技需读几本才解锁)
        public static int GetSeriesTotal(string techDefName)
        {
            return GetSeries(techDefName).Count;
        }

        public static List<KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>> AllGated()
        {
            if (!_built)
            {
                Rebuild();
            }
            List<KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>> result =
                new List<KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>>();
            if (_map == null)
            {
                return result;
            }
            foreach (KeyValuePair<string, List<ThingDef>> kv in _map)
            {
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(kv.Key);
                if (p != null)
                {
                    result.Add(new KeyValuePair<ResearchProjectDef, BlueprintTargetExtension>(
                        p, BlueprintTargetExtension.Get(kv.Value[0])));
                }
            }
            return result;
        }

        // 全部蓝图书物品 (供奖励生成器/调试使用)
        public static List<ThingDef> AllBlueprintDefs()
        {
            if (!_built)
            {
                Rebuild();
            }
            List<ThingDef> result = new List<ThingDef>();
            if (_map == null)
            {
                return result;
            }
            foreach (KeyValuePair<string, List<ThingDef>> kv in _map)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    if (!result.Contains(kv.Value[i]))
                    {
                        result.Add(kv.Value[i]);
                    }
                }
            }
            return result;
        }

        // 某本蓝图书的档位 (显式 tier>0, 否则按目标科技时代推断)
        public static int TierOfBook(ThingDef def)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
            return BlueprintTargetExtension.GetTier(ext);
        }

        // 兼容旧调用: 不限档, 卡节点权重 0.8
        public static ThingDef PickRandomBlueprint()
        {
            return PickRandomBlueprint(0, false, 0.8f);
        }

        // tierPref: 0=不限档, >0 只在该档内选。
        // forceStuck: true 时只要有卡点就必定从卡点里选 (委托保底); 否则按 stuckWeight 概率优先卡点。
        // stuckWeight: 非强制时, 有多大概率优先「当前被卡住」的节点。
        public static ThingDef PickRandomBlueprint(int tierPref, bool forceStuck, float stuckWeight)
        {
            List<ThingDef> all = AllBlueprintDefs();
            if (tierPref > 0)
            {
                List<ThingDef> filtered = new List<ThingDef>();
                for (int i = 0; i < all.Count; i++)
                {
                    if (TierOfBook(all[i]) == tierPref)
                    {
                        filtered.Add(all[i]);
                    }
                }
                if (filtered.Count > 0)
                {
                    all = filtered;
                }
            }
            if (all.Count == 0)
            {
                return null;
            }
            List<ThingDef> stuck = BlueprintStuckMonitor.GetStuck();
            if (tierPref > 0)
            {
                List<ThingDef> stuckT = new List<ThingDef>();
                for (int i = 0; i < stuck.Count; i++)
                {
                    if (TierOfBook(stuck[i]) == tierPref)
                    {
                        stuckT.Add(stuck[i]);
                    }
                }
                stuck = stuckT;
            }
            float bias = forceStuck ? 1f : stuckWeight;
            if (stuck.Count > 0 && Rand.Value < bias)
            {
                return stuck[Rand.RangeInclusive(0, stuck.Count - 1)];
            }
            List<ThingDef> usable = new List<ThingDef>();
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
                if (p != null && PrereqsFinished(p))
                {
                    usable.Add(all[i]);
                }
            }
            List<ThingDef> pool = (Rand.Value < 0.7f && usable.Count > 0) ? usable : all;
            return pool[Rand.RangeInclusive(0, pool.Count - 1)];
        }

        // 前置科技是否全部已研究 (不含蓝图自身, 与 CanStartNow 类似)
        public static bool PrereqsFinished(ResearchProjectDef project)
        {
            if (project.prerequisites != null)
            {
                for (int i = 0; i < project.prerequisites.Count; i++)
                {
                    if (project.prerequisites[i] != null && !project.prerequisites[i].IsFinished)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    // ---- 卡节点检测 (动态概率依据, 低频刷新, 性能铁律) ----
    // 「卡住」= 某门控科技前置已研究 (可研究) 但蓝图系列未集齐, 玩家被卡进度。
    // 刷新时机: 研读完成/事件生成时经 GetStuck() 惰性重建, 且 600 ticks 节流一次, 绝不每 tick 全量遍历。
    public static class BlueprintStuckMonitor
    {
        private static List<ThingDef> _stuck = new List<ThingDef>();
        private static int _asOf = -100000;
        private static bool _dirty = true;

        public static void MarkDirty()
        {
            _dirty = true;
        }

        public static bool HasStuck()
        {
            return GetStuck().Count > 0;
        }

        // 玩家最「卡」的档位: 统计各档未读卡点书数量, 取最多者 (并列取低档)。无卡点返回 0。
        public static int MostStuckTier()
        {
            List<ThingDef> stuck = GetStuck();
            if (stuck.Count == 0)
            {
                return 0;
            }
            Dictionary<int, int> counts = new Dictionary<int, int>();
            for (int i = 0; i < stuck.Count; i++)
            {
                int tier = BlueprintGateDatabase.TierOfBook(stuck[i]);
                int c;
                counts.TryGetValue(tier, out c);
                counts[tier] = c + 1;
            }
            int bestTier = 0;
            int bestCount = -1;
            foreach (KeyValuePair<int, int> kv in counts)
            {
                if (kv.Value > bestCount || (kv.Value == bestCount && kv.Key < bestTier))
                {
                    bestCount = kv.Value;
                    bestTier = kv.Key;
                }
            }
            return bestTier;
        }

        public static List<ThingDef> GetStuck()
        {
            int tick = GenTicks.TicksGame;
            if (_dirty || tick - _asOf >= 600)   // 最多每 600 ticks(10s) 重建一次
            {
                Rebuild();
                _asOf = tick;
                _dirty = false;
            }
            return _stuck;
        }

        private static void Rebuild()
        {
            _stuck = new List<ThingDef>();
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return;
            }
            List<ThingDef> all = BlueprintGateDatabase.AllBlueprintDefs();
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                if (ext == null || string.IsNullOrEmpty(ext.targetTech))
                {
                    continue;
                }
                ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
                if (p == null || p.IsFinished)
                {
                    continue;
                }
                if (tracker.IsTechUnlocked(p.defName))
                {
                    continue;
                }
                if (!BlueprintGateDatabase.PrereqsFinished(p))
                {
                    continue; // 前置未研究, 尚未到"卡住"阶段
                }
                // 只把「尚未读过」的书列为卡住候选 (已读的不用再投放)
                if (tracker.IsRead(all[i].defName))
                {
                    continue;
                }
                _stuck.Add(all[i]);
            }
        }
    }

    // ============================================================================
    //  存档状态 (WorldComponent): 已读书籍 + 已解锁科技
    // ============================================================================
    public class BlueprintUnlockTracker : WorldComponent
    {
        public List<string> readBooks = new List<string>();
        public List<string> unlocked = new List<string>();

        // 蓝图委托: 玩家在委托台发布后, 下一次生成的藏宝处(ItemStash)必须保底投放一本
        // 「优先卡节点」的蓝图书; pendingCommissionTier = 玩家最卡的档位 (0=不限档)。
        public bool pendingCommission = false;
        public int pendingCommissionTier = 0;

        public BlueprintUnlockTracker(World world) : base(world) { }

        public bool IsRead(string bookDefName)
        {
            return readBooks.Contains(bookDefName);
        }

        // 读一本书: 首次生效 (标记已读 + 检查集齐解锁), 重复读无效果
        public void MarkRead(string bookDefName)
        {
            if (readBooks.Contains(bookDefName))
            {
                return; // 第二次读同一本: 无任何效果
            }
            readBooks.Add(bookDefName);
            BlueprintStuckMonitor.MarkDirty();

            BlueprintTargetExtension ext = GetExtensionForBook(bookDefName);
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return;
            }
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
            if (p == null)
            {
                return;
            }
            int total = BlueprintGateDatabase.GetSeriesTotal(ext.targetTech);
            int have = GetReadCount(ext.targetTech);
            Messages.Message("RK_StudyReadMessage".Translate(GetBookTitle(bookDefName), have, total),
                MessageTypeDefOf.NeutralEvent, true);
            if (have >= total && !IsTechUnlocked(ext.targetTech))
            {
                Unlock(ext.targetTech);
                Messages.Message("RK_StudyUnlockMessage".Translate(p.label, total),
                    MessageTypeDefOf.PositiveEvent, true);
            }
        }

        public int GetReadCount(string techDefName)
        {
            int count = 0;
            List<ThingDef> series = BlueprintGateDatabase.GetSeries(techDefName);
            for (int i = 0; i < series.Count; i++)
            {
                if (readBooks.Contains(series[i].defName))
                {
                    count++;
                }
            }
            return count;
        }

        public bool IsTechUnlocked(string defName)
        {
            return unlocked.Contains(defName);
        }

        public void Unlock(string defName)
        {
            if (!unlocked.Contains(defName))
            {
                unlocked.Add(defName);
            }
        }

        public static BlueprintUnlockTracker Get()
        {
            return Find.World.GetComponent<BlueprintUnlockTracker>();
        }

        private static BlueprintTargetExtension GetExtensionForBook(string bookDefName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(bookDefName);
            if (def == null)
            {
                return null;
            }
            return BlueprintTargetExtension.Get(def);
        }

        private static string GetBookTitle(string bookDefName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(bookDefName);
            if (def != null)
            {
                return def.label;
            }
            return bookDefName;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<string>(ref readBooks, "readBooks", LookMode.Value);
            Scribe_Collections.Look<string>(ref unlocked, "unlocked", LookMode.Value);
            Scribe_Values.Look<bool>(ref pendingCommission, "pendingCommission", false);
            Scribe_Values.Look<int>(ref pendingCommissionTier, "pendingCommissionTier", 0);
            if (readBooks == null)
            {
                readBooks = new List<string>();
            }
            if (unlocked == null)
            {
                unlocked = new List<string>();
            }
        }
    }

    // ============================================================================
    //  蓝图书本体 (Book 子类) — 固定书名/描述, 不做 grammar 随机
    // ============================================================================
    //  原版 Book.LabelNoCount 返回 grammar 生成的 title (忽略 def.label), 需要
    //  nameMaker; 蓝图书要求每本固定 label (系列 I/II/III 等), 故子类直接以
    //  def.label 为书名, GenerateBook 置空 (不做随机生成, 也不崩 title=null)。
    //  右键追加「研读」选项 (强制指派走科研工作链路)。
    public class BlueprintBook : Book
    {
        public override string LabelNoCount
        {
            get
            {
                return def.label + StateMarker + GenLabel.LabelExtras(this, true, true);
            }
        }

        // 蓝图经济 v4.1 (2026-08-27): 标签状态标记 — 〔已读/未读〕按卷, 〔已解锁/未解锁〕按目标科技系列集齐状态
        private string StateMarker
        {
            get
            {
                BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
                if (tracker == null)
                {
                    return "";
                }
                string s = tracker.IsRead(def.defName) ? "\u3014已读\u3015" : "\u3014未读\u3015";
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
                if (ext != null && !string.IsNullOrEmpty(ext.targetTech))
                {
                    s += tracker.IsTechUnlocked(ext.targetTech) ? "\u3014已解锁\u3015" : "\u3014未解锁\u3015";
                }
                return s;
            }
        }

        public override string LabelNoParenthesis
        {
            get
            {
                return def.label;
            }
        }

        public override string DescriptionDetailed
        {
            get
            {
                return def.description;
            }
        }

        public override void GenerateBook(Pawn author = null, long? fixedDate = null)
        {
            // 固定书名/描述: 不做任何 grammar 随机 (title 保持 null, LabelNoCount 不依赖它)
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            // 先给原版选项 (阅读/拾取等), 再追加「研读」强制指派
            foreach (FloatMenuOption opt in base.GetFloatMenuOptions(selPawn))
            {
                yield return opt;
            }
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(def);
            if (ext != null && !string.IsNullOrEmpty(ext.targetTech))
            {
                BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
                bool read = tracker != null && tracker.IsRead(def.defName);
                string text = read ? "RK_StudyAlreadyRead".Translate() : "RK_StudyOption".Translate();
                yield return new FloatMenuOption(text, delegate
                {
                    Job job = JobMaker.MakeJob(JobDefOf_RK.StudyBlueprint, this);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }
    }

    // ============================================================================
    //  蓝图书 doer (BookOutcomeDoer 子类) — 阅读效果判定
    // ============================================================================
    //  原版 Book 阅读链路: JobDriver_Reading.tickIntervalAction 每 tick 调
    //  Book.OnBookReadTick(pawn, delta, readingBonus) → 派发给全部 doer.OnReadingTick。
    //  本项目不用原版 JobDriver_Reading (joy 活动, 与「科研工作」冲突),
    //  改用自写 JobDriver_StudyBlueprint (见下)。本 doer 只负责:
    //    - DoesProvidesOutcome: 未读过的书才提供效果 (第二次读无效果, 阅读列表不再排队)
    //    - GetBenefitsString:  信息卡显示研读效果
    //    - GetTopicRulePacks:  返回目标科技的 generalRules, 让书名 grammar 可用
    public class BlueprintBookProperties : BookOutcomeProperties
    {
        public override System.Type DoerClass
        {
            get
            {
                return typeof(BlueprintBookDoer);
            }
        }
    }

    public class BlueprintBookDoer : BookOutcomeDoer
    {
        public override bool DoesProvidesOutcome(Pawn reader)
        {
            if (reader == null)
            {
                return true;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return true;
            }
            return !tracker.IsRead(Parent.def.defName);
        }

        public override void OnBookGenerated(Pawn author = null)
        {
            // 系列语义固定 (defName 即系列), 不做随机生成
        }

        public override string GetBenefitsString(Pawn reader = null)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(Parent.def);
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return "";
            }
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
            if (p == null)
            {
                return "";
            }
            int idx = BlueprintGateDatabase.GetSeriesIndex(Parent.def);
            int total = BlueprintGateDatabase.GetSeriesTotal(ext.targetTech);
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker != null && tracker.IsTechUnlocked(ext.targetTech))
            {
                return string.Format("已解锁 {0} (系列 {1}/{1} 集齐)", p.label, total);
            }
            return string.Format("研读后计入 {0} 进度 ({1}/{2})", p.label, idx, total);
        }

        public override System.Collections.Generic.IEnumerable<Verse.Grammar.RulePack> GetTopicRulePacks()
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(Parent.def);
            if (ext == null)
            {
                return null;
            }
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
            if (p != null && p.generalRules != null)
            {
                List<Verse.Grammar.RulePack> list = new List<Verse.Grammar.RulePack>();
                list.Add(p.generalRules);
                return list;
            }
            return null;
        }
    }

    // ============================================================================
    //  研读工作 (JobDriver_StudyBlueprint) — D6 科研工作 / D7 耗时公式
    // ============================================================================
    //  与原版 JobDriver_Reading 的区别:
    //    - workType=Research (科研工作): 不吃 joy, 由 WorkGiver_StudyBlueprint 发起
    //    - 耗时 = tier 档位基础分钟 (tier1=2min ... tier5=6min) ÷ 速度因子
    //      (速度因子 = 0.5 + 智力等级×0.02 + ResearchSpeed stat)
    //    - 完成时 tracker.MarkRead (首次生效/集齐解锁)
    public class JobDriver_StudyBlueprint : JobDriver
    {
        public const TargetIndex BookIndex = TargetIndex.A;

        private bool carrying;
        private bool hasInInventory;

        public Book Book
        {
            get
            {
                return job.GetTarget(BookIndex).Thing as Book;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Book == null)
            {
                return false;
            }
            return pawn.Reserve(Book, job, 1, 1, null, errorOnFailed);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            job.count = 1;
            hasInInventory = pawn.inventory != null && pawn.inventory.Contains(Book);
            carrying = pawn != null && pawn.carryTracker != null && pawn.carryTracker.CarriedThing == Book;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(BookIndex);
            if (!carrying)
            {
                if (hasInInventory)
                {
                    yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, BookIndex);
                }
                else
                {
                    yield return Toils_Goto.GotoCell(Book.PositionHeld, PathEndMode.ClosestTouch)
                        .FailOnDestroyedNullOrForbidden(BookIndex)
                        .FailOnSomeonePhysicallyInteracting(BookIndex);
                    yield return Toils_Haul.StartCarryThing(BookIndex, false, false, false, true, true);
                }
            }
            yield return Toils_General.Wait(1); // 稳定一帧, 确保 carriedThing 更新

            int ticks = ComputeStudyTicks(pawn, Book);
            Toil study = Toils_General.Wait(ticks);
            study.debugName = "StudyBlueprint";
            study.handlingFacing = true;
            study.initAction = delegate
            {
                if (Book != null)
                {
                    Book.IsOpen = true;
                }
                pawn.pather.StopDead();
                job.showCarryingInspectLine = false;
            };
            study.tickIntervalAction = delegate (int delta)
            {
                // 面向书本 + 智力经验 (D6: 阅读性质 = 科研类工作, 加智力经验)
                if (Book != null)
                {
                    if (Book.Spawned)
                    {
                        pawn.rotationTracker.FaceCell(Book.Position);
                    }
                    else
                    {
                        pawn.rotationTracker.FaceCell(pawn.Position);
                    }
                }
                if (pawn.skills != null)
                {
                    pawn.skills.Learn(SkillDefOf.Intellectual, 0.02f * (float)delta);
                }
            };
            study.AddFinishAction(delegate
            {
                if (Book != null)
                {
                    Book.IsOpen = false;
                }
                BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
                if (tracker != null && Book != null)
                {
                    tracker.MarkRead(Book.def.defName);
                }
                // 书保留 (读后不消耗, D3); 若正携带则放回原处 (书架由 hauling 收尾)
                if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing == Book)
                {
                    Thing dropped = null;
                    pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out dropped);
                }
            });
            yield return study;
        }

        // D7 耗时公式: baseMinutes = 1 + tier (tier1=2min ... tier5=6min)
        //   baseTicks = baseMinutes * 60 * (60000f/3600f) ≈ baseMinutes * 41.67f
        //   speed = 0.5 + 智力等级*0.02 + ResearchSpeed; 实际 ticks = baseTicks / speed, 下限 30
        public static int ComputeStudyTicks(Pawn pawn, Thing book)
        {
            BlueprintTargetExtension ext = book != null ? BlueprintTargetExtension.Get(book.def) : null;
            int tier = (ext != null) ? BlueprintTargetExtension.GetTier(ext) : 1;
            if (tier < 1)
            {
                tier = 1;
            }
            if (tier > 5)
            {
                tier = 5;
            }
            int baseMinutes = 1 + tier;
            float baseTicks = (float)baseMinutes * 41.67f;

            float speed = 0.5f;
            if (pawn != null && pawn.skills != null)
            {
                SkillRecord intellectual = pawn.skills.GetSkill(SkillDefOf.Intellectual);
                if (intellectual != null)
                {
                    speed += (float)intellectual.Level * 0.02f;
                }
                speed += pawn.GetStatValue(StatDefOf.ResearchSpeed);
            }
            if (speed < 0.1f)
            {
                speed = 0.1f;
            }
            int ticks = Mathf.RoundToInt(baseTicks / speed);
            if (ticks < 30)
            {
                ticks = 30;
            }
            return ticks;
        }
    }

    // ============================================================================
    //  研读 WorkGiver — Research 工作类型下自动执行
    // ============================================================================
    public class WorkGiver_StudyBlueprint : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.Book);
            }
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return t is Book && IsEligible(t) && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Book))
            {
                return null;
            }
            if (!IsEligible(t))
            {
                return null;
            }
            if (!pawn.CanReserve(t))
            {
                return null;
            }
            return JobMaker.MakeJob(JobDefOf_RK.StudyBlueprint, t);
        }

        // 只研读「尚未读过」且「目标科技未解锁/未完成」的蓝图书
        private bool IsEligible(Thing t)
        {
            BlueprintTargetExtension ext = BlueprintTargetExtension.Get(t.def);
            if (ext == null || string.IsNullOrEmpty(ext.targetTech))
            {
                return false;
            }
            ResearchProjectDef p = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(ext.targetTech);
            if (p == null || p.IsFinished)
            {
                return false;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return false;
            }
            if (tracker.IsRead(t.def.defName))
            {
                return false; // 已读, 第二次读无效果 → 不再排队
            }
            return true;
        }
    }

    // ---- JobDef/WorkGiverDef 静态引用 (Defs/JobDefs_RK.xml + Defs/WorkGiverDefs_RK.xml) ----
    public static class JobDefOf_RK
    {
        public static JobDef StudyBlueprint;

        static JobDefOf_RK()
        {
            StudyBlueprint = DefDatabase<JobDef>.GetNamedSilentFail("RK_StudyBlueprint");
        }
    }

    // ---- 门控科技: 未解锁不可研究, 已解锁可研究 (D10) ----
    [HarmonyPatch(typeof(ResearchProjectDef), "CanStartNow", MethodType.Getter)]
    public static class Patch_CanStartNow
    {
        static bool Prefix(ResearchProjectDef __instance, ref bool __result)
        {
            BlueprintTargetExtension target = BlueprintGateDatabase.GetExtensionForTech(__instance.defName);
            if (target == null)
            {
                return true; // 非门控, 走原逻辑
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return true;
            }
            // 已解锁 且 前置全部已研究 (与研读判定一致)
            __result = tracker.IsTechUnlocked(__instance.defName)
                && BlueprintGateDatabase.PrereqsFinished(__instance);
            return false;
        }
    }

    // ---- 科研树门禁: 未集齐蓝图时强制节点不可用 (置灰/锁/禁止点击) ----
    // ResearchTreeSK 的节点可用态是 public bool Node.Available 字段, 在 UpdateCaches() 里按
    // 原版条件重算 (每帧 PreOpen 遍历全部节点刷新)。科研树的灰色背景 + 锁图标 + Tree.cs 里
    // 点击入队判定, 全都只看这个 Available 字段, 完全不读 CanStartNow —— 所以仅在 CanStartNow
    // 上门禁不足以让节点变灰不可点。这里在 UpdateCaches 之后追加一道: 命中"蓝图门控且未集齐"
    // 的科技就把 Available 压成 false。已集齐/非门控科技一律不动。
    public static class Patch_NodeUpdateCaches
    {
        private static FieldInfo fResearch;
        private static FieldInfo fAvailable;
        private static bool inited;

        private static void EnsureInit()
        {
            if (inited)
            {
                return;
            }
            System.Type nodeType = AccessTools.TypeByName("ResearchTreeSK.Node");
            if (nodeType != null)
            {
                fResearch = AccessTools.Field(nodeType, "Research");
                fAvailable = AccessTools.Field(nodeType, "Available");
            }
            inited = true;
        }

        public static void Postfix(object __instance)
        {
            // 本补丁跑在科研树每帧刷新流程里, 绝不能抛异常: 一律吞掉, 失败时最坏是本帧不置灰。
            try
            {
                EnsureInit();
                if (fResearch == null || fAvailable == null || __instance == null)
                {
                    return;
                }
                if (!((bool)fAvailable.GetValue(__instance)))
                {
                    return; // 本已不可用, 无需处理
                }
                ResearchProjectDef p = fResearch.GetValue(__instance) as ResearchProjectDef;
                if (p == null || p.IsFinished)
                {
                    return;
                }
                BlueprintTargetExtension target = BlueprintGateDatabase.GetExtensionForTech(p.defName);
                if (target == null)
                {
                    return; // 非蓝图门控科技
                }
                BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
                if (tracker == null)
                {
                    return;
                }
                if (!tracker.IsTechUnlocked(p.defName))
                {
                    fAvailable.SetValue(__instance, false); // 蓝图未集齐 → 置灰 + 锁 + 不可点
                }
            }
            catch
            {
            }
        }
    }

    // ---- 科研树节点徽标: 在门控科技节点右上角绘制 "已读X/N" 角标 (v4: 已读/集齐) ----
    // 挂在 ResearchTreeSK.Node.Draw 末尾; 未读显示 0/N, 读了几本变 x/N, 集齐变绿。
    public static class Patch_ResearchTreeNodeBadge
    {
        private static FieldInfo fResearch;
        private static FieldInfo fRect;
        private static bool inited;

        private static void EnsureInit()
        {
            if (inited)
            {
                return;
            }
            System.Type nodeType = AccessTools.TypeByName("ResearchTreeSK.Node");
            if (nodeType != null)
            {
                fResearch = AccessTools.Field(nodeType, "Research");
                fRect = AccessTools.Field(nodeType, "Rect");
            }
            inited = true;
        }

        public static void Postfix(object __instance, bool isDragged, bool drawInQueue)
        {
            if (isDragged || drawInQueue)
            {
                return; // 队列/拖拽视图不绘制徽标
            }
            // 整个 postfix 包 try/catch: 本补丁运行在 ResearchTreeSK.Node.Draw 内部,
            // 而 Node.Draw 位于 BeginScrollView 与 EndScrollView 之间——
            // 一旦异常逃逸会跳过 EndScrollView, 产生"2 GUIClip + 1 mousePosition"同款泄漏。
            // 必须吞掉并恢复 GUI 状态, 绝不能让异常逃逸到科研树绘制流程。
            try
            {
                EnsureInit();
                if (fResearch == null || fRect == null)
                {
                    return;
                }
                ResearchProjectDef p = fResearch.GetValue(__instance) as ResearchProjectDef;
                if (p == null || p.IsFinished)
                {
                    return;
                }
                BlueprintTargetExtension target = BlueprintGateDatabase.GetExtensionForTech(p.defName);
                if (target == null)
                {
                    return;
                }
                BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
                if (tracker == null)
                {
                    return;
                }

                Rect nodeRect;
                try
                {
                    nodeRect = (Rect)fRect.GetValue(__instance);
                }
                catch
                {
                    return;
                }
                if (nodeRect.width < 20f || nodeRect.height < 10f)
                {
                    return; // 节点极小/未布局时跳过
                }

                int have = tracker.GetReadCount(p.defName);
                int total = BlueprintGateDatabase.GetSeriesTotal(p.defName);
                if (total <= 0)
                {
                    total = 1;
                }
                bool unlocked = tracker.IsTechUnlocked(p.defName);
                string text;
                Color color;
                if (unlocked)
                {
                    text = string.Format("{0}/{1}", have, total);
                    color = new Color(0.4f, 1f, 0.4f, 1f);
                }
                else if (have > 0)
                {
                    text = string.Format("{0}/{1}", have, total);
                    color = new Color(1f, 0.85f, 0.3f, 1f);
                }
                else
                {
                    text = string.Format("0/{0}", total);
                    color = new Color(1f, 1f, 1f, 0.7f);
                }

                float bw = 44f;
                float bh = 15f;
                Rect badge = new Rect(nodeRect.xMax - bw - 3f, nodeRect.y + 2f, bw, bh);
                // 挂 Tooltip 显示完整说明
                TooltipHandler.TipRegion(badge, string.Format("{0}: 已研读蓝图 {1}/{2}",
                    p.label, have, total));

                Widgets.DrawBoxSolid(badge, new Color(0f, 0f, 0f, 0.62f));

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = color;
                Widgets.Label(badge, text);

                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
            catch (System.Exception e)
            {
                // 恢复 GUI 状态后吞掉, 确保 EndScrollView 仍会被执行, 不产生 clip 泄漏
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                Log.Warning("[BlueprintUnlockHSK] 科研树节点角标绘制失败(已捕获, 不影响科研树): " + e.Message);
            }
        }
    }

    // ---- 蓝图商人: 按派系科技等级卖对应分级蓝图书 (v4: 物件变书, 逻辑不变) ----
    // 规则: 中世纪/工业商人可"卖全套但稀有"(低概率+1-2本/次, 防一口气刷齐);
    //       太空/极致商人约半数卖半数不卖(50/50 实例投掷, 只在高档时卖 t4/t5)。数值集中在下方常量便于调整。
    public class StockGenerator_BlueprintTrader : StockGenerator
    {
        // 太空/极致档 的高档(t4/t5)蓝图携带概率 (0.5 = 约半数商卖)
        private const float SpacerCarryHighGate = 0.5f;

        // 蓝图经济 v4.1: 单次补货携带蓝图的概率 (蓝图学者商队=0.2; 既有注入实例默认 1, 行为不变)
        public float sellChance = 1f;

        public override bool HandlesThingDef(ThingDef td)
        {
            return td != null && BlueprintTargetExtension.Get(td) != null;
        }

        public override IEnumerable<Thing> GenerateThings(PlanetTile tile, Faction faction)
        {
            if (sellChance < 1f && Rand.Value > sellChance)
            {
                yield break;
            }
            TechLevel tierOfMerchant = faction != null ? faction.def.techLevel : TechLevel.Spacer;
            List<ThingDef> pool = BlueprintGateDatabase.AllBlueprintDefs();
            if (pool.Count == 0)
            {
                yield break;
            }
            // 太空商对高端(t4/t5)的实例子门控: 一半卖一半不卖
            bool allowHigh = (tierOfMerchant >= TechLevel.Spacer) && (Rand.Value < SpacerCarryHighGate);

            List<ThingDef> candidates = new List<ThingDef>();
            List<int> weights = new List<int>();
            for (int i = 0; i < pool.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(pool[i]);
                if (ext == null)
                {
                    continue;
                }
                int tier = BlueprintTargetExtension.GetTier(ext);
                if (!allowHigh && tier >= 4)   // 商人未投中高档时跳过极致/超凡
                {
                    continue;
                }
                int w = WeightFor(tierOfMerchant, tier);
                if (w <= 0)
                {
                    continue;
                }
                candidates.Add(pool[i]);
                weights.Add(w);
            }
            if (candidates.Count == 0)
            {
                yield break;
            }
            int picked = WeightedPick(weights);
            yield return ThingMaker.MakeThing(candidates[picked]);
        }

        // 派系档位 → 各 tier 权重表 (便于后续调整; 中世纪/工业 "卖全套但稀有")
        private int WeightFor(TechLevel merchant, int tier)
        {
            switch (merchant)
            {
                case TechLevel.Neolithic:
                    return 0;                                   // 石器不卖 (需求#4)
                case TechLevel.Medieval:
                    return SmallWeight(tier, 1, 15, 8, 6, 4);   // 本档最易, 其它稀有
                case TechLevel.Industrial:
                    return SmallWeight(tier, 15, 1, 12, 8, 5);  // 本档为工业(暂无门控则回落), 高四档稀有
                case TechLevel.Ultra:
                case TechLevel.Archotech:
                case TechLevel.Spacer:
                default:
                    return SmallWeight(tier, 4, 8, 35, 70, 90); // 太空: 越高级越常见, 低档稀有
            }
        }

        private int SmallWeight(int tier, int t1, int t2, int t3, int t4, int t5)
        {
            switch (tier)
            {
                case 1: return t1;
                case 2: return t2;
                case 3: return t3;
                case 4: return t4;
                case 5: return t5;
                default: return 0;
            }
        }

        private int WeightedPick(List<int> weights)
        {
            int total = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                total += weights[i];
            }
            int r = Rand.RangeInclusive(0, total - 1);
            int acc = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                acc += weights[i];
                if (r < acc)
                {
                    return i;
                }
            }
            return weights.Count - 1;
        }
    }

    // ---- 蓝图奖励生成器: 从门控清单随机选一张蓝图书 (用于任务/事件奖励池) ----
    public class ThingSetMaker_BlueprintReward : ThingSetMaker
    {
        // 0=不限档; >0 只在该档蓝图书里选 (XML 里 <tier>3</tier> 等)
        public int tier = 0;

        protected override bool CanGenerateSub(ThingSetMakerParams parms)
        {
            return BlueprintGateDatabase.AllBlueprintDefs().Count > 0;
        }

        protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            ThingDef def = BlueprintGateDatabase.PickRandomBlueprint(tier, false, 0.8f);
            if (def != null)
            {
                outThings.Add(ThingMaker.MakeThing(def));
            }
        }

        protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
        {
            return BlueprintGateDatabase.AllBlueprintDefs();
        }
    }

    // ---- 物品贮藏任务 (RimQuest 用原版 QuestScriptDef): 藏宝处放一张蓝图书 ----
    public static class Patch_ItemStash
    {
        public static void ScatterAt_Postfix(IntVec3 loc, Map map)
        {
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker != null && tracker.pendingCommission)
            {
                // 委托保底: 本藏宝处必定投放一本「优先卡节点」的蓝图书, 且限委托档位。
                tracker.pendingCommission = false;
                ThingDef need = BlueprintGateDatabase.PickRandomBlueprint(tracker.pendingCommissionTier, true, 1f);
                if (need == null)
                {
                    need = BlueprintGateDatabase.PickRandomBlueprint(tracker.pendingCommissionTier, false, 1f);
                }
                if (need != null)
                {
                    GenSpawn.Spawn(ThingMaker.MakeThing(need), loc, map);
                    BlueprintStuckMonitor.MarkDirty();
                }
                return;
            }
            // 按格调用, 概率门控避免每格都刷蓝图书 (4-8 格 → 期望 1-2 本);
            // 卡节点时提权 (0.25 → 0.45), 事件驱动、无 per-tick 开销
            float prob = BlueprintStuckMonitor.HasStuck() ? 0.45f : 0.25f;
            if (Rand.Value >= prob)
            {
                return;
            }
            ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
            if (def != null)
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(def), loc, map);
            }
        }
    }

    // ---- Go Explore: 战利品生成后注入蓝图书 (卡节点时提权) ----
    public static class Patch_GoExploreRewards
    {
        public static void Stockpile_Postfix(ref List<Thing> __result)
        {
            float prob = BlueprintStuckMonitor.HasStuck() ? 0.55f : 0.3f;
            if (__result != null && Rand.Value < prob)
            {
                ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
                if (def != null)
                {
                    __result.Add(ThingMaker.MakeThing(def));
                }
            }
        }

        public static void StorageBox_Postfix(ref List<Thing> __result)
        {
            float prob = BlueprintStuckMonitor.HasStuck() ? 0.65f : 0.5f;
            if (__result != null && Rand.Value < prob)
            {
                ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
                if (def != null)
                {
                    __result.Add(ThingMaker.MakeThing(def));
                }
            }
        }

        public static void Intercepted_Postfix(ref List<Thing> __result)
        {
            if (__result != null)
            {
                ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
                if (def != null)
                {
                    __result.Add(ThingMaker.MakeThing(def));
                }
            }
        }
    }

    // ---- Cybranian Events: 陨石坠落处放蓝图书 + 老人随身带蓝图书 ----
    public static class Patch_CybranianEvents
    {
        public static void Meteorite_Postfix(IntVec3 c, Map map)
        {
            // 按格调用, 概率门控避免每格都刷蓝图书; 卡节点时提权 (0.4 → 0.6)
            float prob = BlueprintStuckMonitor.HasStuck() ? 0.6f : 0.4f;
            if (Rand.Value >= prob)
            {
                return;
            }
            ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
            if (def != null)
            {
                GenSpawn.Spawn(ThingMaker.MakeThing(def), c, map);
            }
        }

        public static void OldMan_Postfix(object __instance, Pawn pawn)
        {
            // 打的是基类方法, 对全部 walk-in 流浪者任务触发, 必须按真实类型门控:
            // 只有 Cybranian 的 OldMan 任务才投放蓝图书, 原版其它 walk-in (WandererJoin/Abasia) 不处理。
            if (__instance == null || __instance.GetType().FullName != "EventsCore.Quests.QuestNode_Root_WandererJoin_OldMan")
            {
                return;
            }
            if (pawn == null || pawn.inventory == null)
            {
                return;
            }
            ThingDef def = BlueprintGateDatabase.PickRandomBlueprint();
            if (def != null)
            {
                pawn.inventory.TryAddItemNotForSale(ThingMaker.MakeThing(def));
            }
        }
    }

    // ============================================================================
    //  蓝图经济 v4.1 (2026-08-27) — MarketValue 折价 StatPart
    //  挂在原版 MarketValue stat 上(Patches/68), 仅对"已研读"的蓝图书实例 ×0.5。
    //  商人新生成的蓝图书 stack 未读 → 买价不受影响; 玩家出售已读副本折价。
    // ============================================================================
    public class StatPart_BlueprintReadDiscount : StatPart
    {
        public float discount = 0.5f;

        public override void TransformValue(StatRequest req, ref float val)
        {
            Thing t = req.Thing;
            if (t == null || t.def == null || BlueprintTargetExtension.Get(t.def) == null)
            {
                return;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker != null && tracker.IsRead(t.def.defName))
            {
                val *= discount;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            Thing t = req.Thing;
            if (t == null || t.def == null || BlueprintTargetExtension.Get(t.def) == null)
            {
                return null;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker != null && tracker.IsRead(t.def.defName))
            {
                return "已研读蓝图: 价值 \u00d7" + (int)(discount * 100f) + "%";
            }
            return null;
        }
    }

    // ============================================================================
    //  事件迁移 (MO Storyteller 子系统 → 无 MO 依赖版)
    // ============================================================================
    //  1) IncidentWorker_BlueprintGift — "游学者遗落的蓝图": 按已研究数推算时代档位,
    //     掉 1 本未解锁的对应档蓝图书 + 信。对应 MO "图纸=商人/探索获取" 的事件腿。
    public class IncidentWorker_BlueprintGift : IncidentWorker
    {
        private static int CountFinished()
        {
            int n = 0;
            List<ResearchProjectDef> all = DefDatabase<ResearchProjectDef>.AllDefsListForReading;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].IsFinished)
                {
                    n++;
                }
            }
            return n;
        }

        private static int ExpectedTier()
        {
            int f = CountFinished();
            if (f < 25) return 1;
            if (f < 60) return 2;
            if (f < 120) return 3;
            if (f < 220) return 4;
            return 5;
        }

        // 目标档未解锁且未读全的书池
        private static List<ThingDef> UsefulPool()
        {
            List<ThingDef> result = new List<ThingDef>();
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            if (tracker == null)
            {
                return result;
            }
            int want = ExpectedTier();
            List<ThingDef> all = BlueprintGateDatabase.AllBlueprintDefs();
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension ext = BlueprintTargetExtension.Get(all[i]);
                if (ext == null || string.IsNullOrEmpty(ext.targetTech))
                {
                    continue;
                }
                int tier = BlueprintTargetExtension.GetTier(ext);
                if (tier < want - 1 || tier > want)
                {
                    continue;
                }
                if (tracker.IsTechUnlocked(ext.targetTech))
                {
                    continue; // 已解锁的科技不再掉书
                }
                result.Add(all[i]);
            }
            return result;
        }

        private static int CountUseful()
        {
            return UsefulPool().Count;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }
            if (CountFinished() < 8)
            {
                return false;
            }
            List<ThingDef> pool = UsefulPool();
            if (pool.Count == 0)
            {
                return false;
            }
            ThingDef pick = pool.RandomElement();
            Thing book = ThingMaker.MakeThing(pick);
            if (book == null)
            {
                return false;
            }
            GenPlace.TryPlaceThing(book, map.Center, map, ThingPlaceMode.Near);
            string label = def.letterLabel ?? "游学者的馈赠";
            string text = (def.letterText ?? "一位游学者匆匆路过, 遗落了一份文献: {0}。").Replace("{0}", book.LabelCap);
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new GlobalTargetInfo(book), null, null);
            return true;
        }
    }

    // 2) IncidentWorker_RareBeastWandersIn — MO "稀有巨兽路过" 无自定义生物版:
    //    kindDef 指向任意原版/HSK 动物, 生成 1~max 只野生群 + 信。不做离开计时
    //    (对齐原版 AnimalsWanderIn 语义; MO 的 leaveMapAfterTime 需要其计时设施)。
    public class RareBeastProperties : DefModExtension
    {
        public string kindDef;
        public int min = 1;
        public int max = 2;
    }

    public class IncidentWorker_RareBeastWandersIn : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }
            RareBeastProperties props = def.GetModExtension<RareBeastProperties>();
            if (props == null || string.IsNullOrEmpty(props.kindDef))
            {
                return false;
            }
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(props.kindDef);
            if (kind == null)
            {
                Log.Warning("[BlueprintUnlockHSK] RareBeast kindDef 不存在: " + props.kindDef);
                return false;
            }
            int n = Rand.RangeInclusive(Mathf.Max(1, props.min), Mathf.Max(props.min, props.max));
            List<Pawn> spawned = new List<Pawn>();
            for (int i = 0; i < n; i++)
            {
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, null, PawnGenerationContext.NonPlayer, map.Tile, true));
                if (p == null)
                {
                    continue;
                }
                IntVec3 c;
                if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.GetEdifice(map) == null && x.Standable(map), map, 20, out c))
                {
                    c = map.Center;
                }
                GenSpawn.Spawn(p, c, map);
                spawned.Add(p);
            }
            if (spawned.Count == 0)
            {
                return false;
            }
            string label = (def.letterLabel ?? "稀有巨兽").Replace("{0}", kind.GetLabelPlural()).Replace("{1}", spawned.Count.ToString());
            string text = (def.letterText ?? "一群{0}闯入了地区。它们危险, 但皮毛/材料价值不菲。")
                .Replace("{1}", spawned.Count.ToString()).Replace("{0}", kind.GetLabelPlural());
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new GlobalTargetInfo(spawned[0]), null, null);
            return true;
        }
    }

    // ============================================================================
    //  蓝图委托台 (2026-08-28 v4.2) — 定向获取蓝图的专用任务入口
    // ============================================================================
    //  复刻 探秘桌 的事件驱动范式 (玩家点击 + 消耗残页情报 + 天数冷却, 零 tick):
    //    · 点击「发布蓝图委托」→ 记下玩家当前最卡的档位到 WorldComponent 的 pendingCommission;
    //    · 生成一条原版 OpportunitySite_ItemStash 任务 (藏宝处);
    //    · 藏宝处散布时 (Patch_ItemStash.ScatterAt) 见到 pendingCommission → 保底投放一本
    //      「优先卡节点」蓝图书 (限该档) 并清除标志 → 卡哪个节点就能定向拿到哪本。
    public class CommissionProperties : DefModExtension
    {
        public int noteCost = 2;            // 消耗残页情报张数
        public int questPoints = 600;       // 生成任务的威胁点数
        public int minDaysBetween = 2;      // 两次委托最小间隔 (天)
        public int forceTier = 0;           // >0 固定委托档位; 0=自动取玩家最卡档
    }

    public class CompProperties_BlueprintCommissionHSK : CompProperties
    {
        public CompProperties_BlueprintCommissionHSK()
        {
            compClass = typeof(CompBlueprintCommissionHSK);
        }
    }

    public class CompBlueprintCommissionHSK : ThingComp
    {
        private int lastCommissionAbsDay = -9999;

        private CommissionProperties Props
        {
            get
            {
                CommissionProperties p = parent.def.GetModExtension<CommissionProperties>();
                if (p == null)
                {
                    Log.Warning("[BlueprintUnlockHSK] " + parent.def.defName + " 缺少 CommissionProperties");
                }
                return p;
            }
        }

        private static ThingDef NoteDef
        {
            get { return DefDatabase<ThingDef>.GetNamedSilentFail("RK_TornNote"); }
        }

        private static int NotesOnMap(Map map)
        {
            ThingDef note = NoteDef;
            if (note == null || map == null)
            {
                return 0;
            }
            List<Thing> list = map.listerThings.ThingsOfDef(note);
            if (list == null)
            {
                return 0;
            }
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && !list[i].Destroyed)
                {
                    total += list[i].stackCount;
                }
            }
            return total;
        }

        private static void ConsumeNotes(Map map, int want)
        {
            ThingDef note = NoteDef;
            if (note == null || map == null)
            {
                return;
            }
            List<Thing> list = map.listerThings.ThingsOfDef(note);
            if (list == null)
            {
                return;
            }
            for (int i = list.Count - 1; i >= 0 && want > 0; i--)
            {
                Thing t = list[i];
                if (t == null || t.Destroyed || !t.Spawned)
                {
                    continue;
                }
                int take = want < t.stackCount ? want : t.stackCount;
                Thing cut = t.SplitOff(take);
                if (cut != null)
                {
                    cut.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    t.Destroy(DestroyMode.Vanish);
                }
                want -= take;
            }
        }

        private static float NowAbsDay
        {
            get { return GenTicks.TicksAbs / 60000f; }
        }

        // 0 = 不可委托; 否则可
        private int CanCommission(CommissionProperties p, out string whyNot)
        {
            whyNot = null;
            if (BlueprintGateDatabase.AllBlueprintDefs().Count == 0)
            {
                whyNot = "没有可投放的蓝图书";
                return 0;
            }
            float next = lastCommissionAbsDay + p.minDaysBetween;
            if (NowAbsDay < next)
            {
                whyNot = "线报已用尽, " + Mathf.CeilToInt(next - NowAbsDay) + " 天后可再次委托";
                return 0;
            }
            if (NotesOnMap(parent.Map) < p.noteCost)
            {
                whyNot = "残页情报不足: 需 " + p.noteCost + " 张, 当前 " + NotesOnMap(parent.Map) + " 张";
                return 0;
            }
            return 1;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (parent.Map == null || !parent.Map.IsPlayerHome)
            {
                yield break;
            }
            CommissionProperties p = Props;
            if (p == null)
            {
                yield break;
            }
            string whyNot;
            if (CanCommission(p, out whyNot) == 0)
            {
                yield break;
            }
            Command_Action act = new Command_Action();
            act.defaultLabel = "发布蓝图委托";
            act.defaultDesc = "把残页情报换成一条藏宝图委托: 一名线人会指认一处藏宝处, 里面必定藏着一份你正缺的蓝图。\n\n消耗残页情报 "
                              + p.noteCost + " 张。当你卡在某个科技节点时, 委托会优先给出该节点对应的蓝图书。";
            act.action = delegate { DoCommission(p); };
            yield return act;
        }

        private void DoCommission(CommissionProperties p)
        {
            string whyNot;
            if (CanCommission(p, out whyNot) == 0)
            {
                Messages.Message("无法发布委托: " + whyNot, parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            QuestScriptDef stash = DefDatabase<QuestScriptDef>.GetNamedSilentFail("OpportunitySite_ItemStash");
            if (stash == null)
            {
                Log.Warning("[BlueprintUnlockHSK] 找不到 OpportunitySite_ItemStash 任务脚本");
                Messages.Message("委托失败: 藏宝处任务不可用。", parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            ConsumeNotes(parent.Map, p.noteCost);
            lastCommissionAbsDay = (int)NowAbsDay;

            // 先立保底标志, 再生成任务 (散布可能在生成时或玩家抵达时发生, 标志会一直保留到被消费)。
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            int tier = p.forceTier > 0 ? p.forceTier : BlueprintStuckMonitor.MostStuckTier();
            if (tracker != null)
            {
                tracker.pendingCommission = true;
                tracker.pendingCommissionTier = tier;
            }
            QuestUtility.GenerateQuestAndMakeAvailable(stash, p.questPoints);
            Find.LetterStack.ReceiveLetter("蓝图委托已发布",
                "一名线人递来一张藏宝图: 某处藏宝点里, 有人替你把一份研究蓝图塞进了箱子。带人去把它挖出来吧。",
                LetterDefOf.PositiveEvent, parent, null, null);
        }

        public override string CompInspectStringExtra()
        {
            CommissionProperties p = Props;
            if (p == null)
            {
                return null;
            }
            string whyNot;
            int ok = CanCommission(p, out whyNot);
            string text = "残页情报: " + NotesOnMap(parent.Map) + " / 委托需 " + p.noteCost;
            if (ok > 0)
            {
                text += ";可发布委托";
            }
            else if (!whyNot.NullOrEmpty())
            {
                text += ";暂不可委托(" + whyNot + ")";
            }
            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref lastCommissionAbsDay, "lastCommissionAbsDay", -9999);
        }
    }
}


using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

// ============================================================================
//  P1 · 可搜刮战利品容器 (Medieval Overhaul 机制复刻, 零 MO DLL)
// ----------------------------------------------------------------------------
//  语义抄自 _tmp/mo_decomp/MedievalOverhaul/Building_Lootable.cs:
//    · 继承 RimWorld.Building_Casket → 天然带"打开/搜刮"作业(JobDriver_Open),无需自写 Job
//    · 生成时机 = SpawnSetup 且非读档 → 读 LootableExtensionHSK 掷物品与敌怪(不放 ThingSetMaker)
//    · 打开 = Open() 特效 + 脏网格 + 可选自毁; 倒出 = EjectContents() 并把敌怪设为永久猎杀/狂暴
//  与 MO 的差异(本环境定制):
//    1) 新增蓝图书投放字段 blueprintTier / blueprintSeriesTech / blueprintChance / blueprintCount
//       → 把"探索废墟"变成 112 本蓝图书的定向获取主渠道, 优先给**未读过**的卷册。
//    2) 敌怪池走 DefDatabase 安全解析 + 可空 faction, 不依赖任何 MO 派系 def。
//    3) 物品 stuff 只在 def.MadeFromStuff 时才随机材质, 避免给书/成品物品塞材质。
//    4) 品质掷档: 容器自身扩展可给 lootQualityRange, 物品侧仍支持 ItemLootExtensionHSK。
//    5) 性能(AGENTS §9): 全部逻辑只在 SpawnSetup/Open 触发, 不写 Tick, 零轮询。
//  命名与程序集: 并入 BlueprintUnlockHSK.dll (build.ps1 编译 Source\*.cs)。
// ============================================================================
namespace BlueprintUnlockHSK
{
    // 可选挂在"能被搜出来的物品"ThingDef 上, 控制品质掷档
    public class ItemLootExtensionHSK : DefModExtension
    {
        public IntRange qualityRange = new IntRange(2, 2);
    }

    public class LootableExtensionHSK : DefModExtension
    {
        // ---- 物品池 ----
        public bool isRandom = true;                 // true=从 randomItems 抽一件, false=固定 itemDefName
        public string itemDefName;
        public List<string> randomItems;
        public float lootChance = 1f;
        public IntRange lootCount = new IntRange(1, 2);
        public IntRange lootQualityRange = new IntRange(0, 4);   // 无物品级扩展时的默认品质档
        public int maxStacks = 1;                    // 一次搜刮最多几堆(容器格数由 ThingDef 决定)

        // ---- 蓝图书投放(核心融合点) ----
        public int blueprintTier = 0;                // 1=中世纪 2=工业 3=太空 4=极致 5=超凡; 0=不投
        public string blueprintSeriesTech;           // 指定目标科技系列(可空=按档位随机)
        public float blueprintChance = 0f;           // 每次搜刮投蓝图书的概率
        public int blueprintCount = 1;               // 投几本(去重后)

        // ---- 敌怪 ----
        public List<string> enemysToSpawn;
        public float enemySpawnChance = 0.01f;
        public int enemySpawnCount = 1;
        public bool hostileEnemy = true;             // 放出后置 ManhunterPermanent / Berserk
        public FactionDef faction;                   // 可空=不加阵营
        public bool spawnAsPlayerFaction;

        // ---- 表现 ----
        public bool isDestroyed;                     // 打开后自毁(如麻袋/纸堆)
        public GraphicData emptyGraphicData;         // 搜空后换贴图
        public FleckDef searchEffect;
        public float effectSize = 1f;
        public SoundDef searchSound;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string e in base.ConfigErrors())
            {
                yield return e;
            }
            if (isRandom && (randomItems == null || randomItems.Count == 0))
            {
                yield return "isRandom=true 但 randomItems 为空";
            }
            if (!isRandom && itemDefName.NullOrEmpty())
            {
                yield return "isRandom=false 但 itemDefName 未填";
            }
            if (emptyGraphicData != null && emptyGraphicData.texPath.NullOrEmpty())
            {
                yield return "emptyGraphicData 缺少 texPath";
            }
            if (blueprintTier < 0 || blueprintTier > 5)
            {
                yield return "blueprintTier 必须在 0~5, 当前 " + blueprintTier;
            }
        }
    }

    [StaticConstructorOnStartup]
    public class Building_LootableHSK : Building_Casket
    {
        private const int MaxItemsPerRoll = 6;       // 兜底: 防止配置写错一次刷爆容器
        private Graphic emptyColoredGraphicCached;
        private bool emptyGraphicResolved;

        // 搜空后的贴图(与 MO 一致: 只在正式游戏状态下解析, 避免编辑器/加载期抖动)
        private Graphic EmptyColoredGraphic
        {
            get
            {
                if (Scribe.mode == LoadSaveMode.Inactive)
                {
                    return null;
                }
                if (!emptyGraphicResolved)
                {
                    emptyGraphicResolved = true;
                    LootableExtensionHSK ext = Ext;
                    if (ext != null && ext.emptyGraphicData != null)
                    {
                        emptyColoredGraphicCached = ext.emptyGraphicData.GraphicColoredFor(this);
                    }
                }
                return emptyColoredGraphicCached;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                Graphic g = EmptyColoredGraphic;
                if (contentsKnown && g != null)
                {
                    return g;
                }
                return base.Graphic;
            }
        }

        public override bool CanOpen
        {
            get { return HasAnyContents; }
        }

        private static int ClampI(int v, int lo, int hi)
        {
            if (v < lo)
            {
                return lo;
            }
            return (v > hi) ? hi : v;
        }

        private LootableExtensionHSK Ext
        {
            get { return def.GetModExtension<LootableExtensionHSK>(); }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (contentsKnown || respawningAfterLoad)
            {
                return;
            }
            LootableExtensionHSK ext = Ext;
            if (ext == null)
            {
                return;
            }
            RollItems(ext);
            RollBlueprints(ext);
            RollEnemies(ext);
            contentsKnown = false;   // 未搜刮前不缓存内容显示(同 MO)
        }

        // ---------------------------------------------------------------- 物品
        private void RollItems(LootableExtensionHSK ext)
        {
            if (!Rand.Chance(ext.lootChance))
            {
                return;
            }
            int stacks = ClampI(ext.maxStacks, 1, MaxItemsPerRoll);
            for (int i = 0; i < stacks; i++)
            {
                ThingDef pick = null;
                if (ext.isRandom)
                {
                    if (ext.randomItems == null || ext.randomItems.Count == 0)
                    {
                        return;
                    }
                    pick = DefDatabase<ThingDef>.GetNamedSilentFail(ext.randomItems.RandomElement<string>());
                }
                else
                {
                    pick = DefDatabase<ThingDef>.GetNamedSilentFail(ext.itemDefName);
                }
                if (pick == null)
                {
                    continue;
                }
                Thing item = MakeLoot(pick, ext);
                if (item != null)
                {
                    innerContainer.TryAdd(item, item.stackCount, true);
                }
            }
        }

        private Thing MakeLoot(ThingDef pick, LootableExtensionHSK ext)
        {
            if (pick == null)
            {
                return null;
            }
            Thing thing;
            if (typeof(Book).IsAssignableFrom(pick.thingClass))
            {
                // 书类直接造(不走 BookUtility.MakeBook, 它会随机覆盖书名 → 蓝图书要求书名=def.label)
                thing = ThingMaker.MakeThing(pick, pick.MadeFromStuff ? GenStuff.RandomStuffFor(pick) : null);
            }
            else
            {
                thing = ThingMaker.MakeThing(pick, pick.MadeFromStuff ? GenStuff.RandomStuffFor(pick) : null);
            }
            if (thing == null)
            {
                return null;
            }
            CompQuality cq = thing.TryGetComp<CompQuality>();
            if (cq != null)
            {
                ItemLootExtensionHSK iext = pick.GetModExtension<ItemLootExtensionHSK>();
                IntRange range = (iext != null) ? iext.qualityRange : ext.lootQualityRange;
                int lo = ClampI(range.min, 0, 5);
                int hi = ClampI((range.max > lo + 1) ? range.max : (lo + 1), 1, 6);
                cq.SetQuality((QualityCategory)Rand.Range(lo, hi), ArtGenerationContext.Outsider);
            }
            if (thing.def.stackLimit > 1)
            {
                thing.stackCount = ext.lootCount.RandomInRange;
            }
            if (thing.stackCount <= 0)
            {
                return null;
            }
            return thing;
        }

        // -------------------------------------------------------------- 蓝图书
        private void RollBlueprints(LootableExtensionHSK ext)
        {
            if (ext.blueprintTier <= 0 || ext.blueprintChance <= 0f || !Rand.Chance(ext.blueprintChance))
            {
                return;
            }
            int want = ClampI(ext.blueprintCount, 1, 3);
            for (int i = 0; i < want; i++)
            {
                ThingDef book = PickBlueprint(ext);
                if (book == null)
                {
                    return;
                }
                Thing inst = ThingMaker.MakeThing(book);
                if (inst != null)
                {
                    innerContainer.TryAdd(inst, 1, true);
                }
            }
        }

        // 选一本蓝图书: 指定系列 > 该档未读 > 该档全部 > 全局随机
        private static ThingDef PickBlueprint(LootableExtensionHSK ext)
        {
            List<ThingDef> all = BlueprintGateDatabase.AllBlueprintDefs();
            if (all.Count == 0)
            {
                return null;
            }
            BlueprintUnlockTracker tracker = BlueprintUnlockTracker.Get();
            List<ThingDef> byTier = new List<ThingDef>();
            List<ThingDef> unread = new List<ThingDef>();
            for (int i = 0; i < all.Count; i++)
            {
                BlueprintTargetExtension be = BlueprintTargetExtension.Get(all[i]);
                if (be == null)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(ext.blueprintSeriesTech))
                {
                    if (!string.Equals(be.targetTech, ext.blueprintSeriesTech, StringComparison.Ordinal))
                    {
                        continue;
                    }
                }
                else if (BlueprintTargetExtension.GetTier(be) != ext.blueprintTier)
                {
                    continue;
                }
                byTier.Add(all[i]);
                // v6: 进度改为「按卷」记录, 无法再从 defName 判断某本书是否读过;
                // 改判「该系列科技是否还没解锁」—— 没解锁 = 这本书还有用。
                if (tracker == null || string.IsNullOrEmpty(be.targetTech)
                    || !tracker.IsTechUnlocked(be.targetTech))
                {
                    unread.Add(all[i]);
                }
            }
            if (unread.Count > 0)
            {
                return unread.RandomElement<ThingDef>();
            }
            if (byTier.Count > 0)
            {
                return byTier.RandomElement<ThingDef>();
            }
            return BlueprintGateDatabase.PickRandomBlueprint();
        }

        // ---------------------------------------------------------------- 敌怪
        private void RollEnemies(LootableExtensionHSK ext)
        {
            if (ext.enemysToSpawn == null || ext.enemysToSpawn.Count == 0 || !Rand.Chance(ext.enemySpawnChance))
            {
                return;
            }
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(ext.enemysToSpawn.RandomElement<string>());
            if (kind == null)
            {
                return;
            }
            Faction fac = null;
            if (ext.spawnAsPlayerFaction)
            {
                fac = Faction.OfPlayer;
            }
            else if (ext.faction != null)
            {
                fac = FactionUtility.DefaultFactionFrom(ext.faction);
            }
            Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, fac, PawnGenerationContext.NonPlayer, -1, true));
            if (p == null)
            {
                return;
            }
            innerContainer.TryAdd(p, ClampI(ext.enemySpawnCount, 1, 4), true);
        }

        // ---------------------------------------------------------------- 打开
        public override void Open()
        {
            LootableExtensionHSK ext = Ext;
            base.Open();
            contentsKnown = true;
            if (ext == null || Map == null)
            {
                return;
            }
            if (ext.searchEffect != null)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(DrawPos, Map, ext.searchEffect, ext.effectSize);
                data.rotationRate = Rand.RangeInclusive(-240, 240);
                data.velocitySpeed = Rand.Range(0.1f, 0.8f);
                Map.flecks.CreateFleck(data);
            }
            DirtyMapMesh(Map);
            if (ext.isDestroyed)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        public override void EjectContents()
        {
            LootableExtensionHSK ext = Ext;
            List<Pawn> pawns = new List<Pawn>();
            for (int i = 0; i < innerContainer.Count; i++)
            {
                Pawn p = innerContainer[i] as Pawn;
                if (p != null)
                {
                    pawns.Add(p);
                }
            }
            innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Direct);
            if (ext != null && ext.hostileEnemy)
            {
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn p = pawns[j];
                    if (p == null || p.Dead || p.MapHeld == null)
                    {
                        continue;
                    }
                    MentalStateDef msd = p.RaceProps.Animal ? MentalStateDefOf.ManhunterPermanent : MentalStateDefOf.Berserk;
                    if (msd != null && p.mindState != null && p.mindState.mentalStateHandler != null)
                    {
                        p.mindState.mentalStateHandler.TryStartMentalState(msd);
                    }
                }
            }
            contentsKnown = true;
        }
    }
    // ============================================================================
    //  M1 落地手段之一: "废墟遗栈"事件 —— 在地图边上刷一小堆可搜刮容器
    //  (M2 会用 KCSG 站点 + 任务脚本替代主要投放渠道, 本事件作为长期补充与调试入口)
    // ============================================================================
    public class RuinCacheProperties : DefModExtension
    {
        public List<string> buildings = new List<string>();      // 要刷的 Rustic_Loot* / 废墟 defName
        public IntRange count = new IntRange(3, 6);
        public int clusterRadius = 5;
        public string labelFallback = "废墟遗栈";
    }

    public class IncidentWorker_RuinCache : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }
            RuinCacheProperties props = def.GetModExtension<RuinCacheProperties>();
            if (props == null || props.buildings == null || props.buildings.Count == 0)
            {
                return false;
            }
            // 种子点: 地图边缘附近、无建筑、可站人
            IntVec3 seed;
            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 x) => x.GetEdifice(map) == null && x.Standable(map), map, 25, out seed))
            {
                return false;
            }
            int want = ClampStatic(props.count.RandomInRange, 1, 12);
            List<Thing> placed = new List<Thing>();
            for (int i = 0; i < want; i++)
            {
                ThingDef bdef = DefDatabase<ThingDef>.GetNamedSilentFail(props.buildings.RandomElement<string>());
                if (bdef == null)
                {
                    continue;
                }
                IntVec3 c;
                if (!CellFinder.TryFindRandomCellNear(seed, map, ClampStatic(props.clusterRadius, 1, 12),
                        (IntVec3 x) => x.GetEdifice(map) == null && x.Standable(map) && !x.Roofed(map), out c))
                {
                    continue;
                }
                Thing t = ThingMaker.MakeThing(bdef);
                if (t == null)
                {
                    continue;
                }
                GenPlace.TryPlaceThing(t, c, map, ThingPlaceMode.Direct);
                if (t.Spawned)
                {
                    placed.Add(t);
                }
            }
            if (placed.Count == 0)
            {
                return false;
            }
            string label = def.letterLabel.NullOrEmpty() ? props.labelFallback : def.letterLabel;
            string text = (def.letterText ?? "一支遗落的商队在这里留下了{0}件还能翻找的东西。")
                .Replace("{0}", placed.Count.ToString());
            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new TargetInfo(placed[0]), null, null);
            return true;
        }

        private static int ClampStatic(int v, int lo, int hi)
        {
            if (v < lo)
            {
                return lo;
            }
            return (v > hi) ? hi : v;
        }
    }
}

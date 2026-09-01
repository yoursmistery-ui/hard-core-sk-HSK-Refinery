using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// =====================================================================
//  通用石化气体网络 (Stage 2 · 泛化) — 并入 HSK工业科研大修
//  双介质 (天然气 / 氨气) 走同一套气管/气泵/气阀框架, 但每个 def 携带 gasType,
//  邻接并查集"仅在同介质节点间执行"→ 两种气体天然分线, 永不互串。
//  2026-08-28: 氢气并入天然气(等价通用)。PetroGas.Hydrogen 枚举值保留(老存档
//  Scribe 兼容), 但 DefOf/Kind 全部归一化为 NaturalGas —— 老存档的氢气瓶、
//  氢气罐读档后即按天然气工作, 任何残留 gasType=Hydrogen 的 def 也归天然气管网。
//
//  Sink 双模式: FuelPool (走原版 CompRefuelable, 锅炉/发电机烧天然气);
//                Deposit (把松散气物品按小额 reserve 丢到装置 footprint 扩 2 的地上,
//                        供原版 WorkGiver_DoBill 搬运消费 — 氨合成塔耗氢、中性胺塔耗氨/氢)。
//  差异危害: 氢节点 (罐/管/泵/阀) PostDestroy 按存量缩放 Flame 爆炸;
//             氨节点 PostDestroy 用 map.gasGrid.AddGas(GasType.ToxGas) 原生毒气云。
//
//  C#5 兼容 (无插值/无 ?./无表达式体成员/无内联 out var)。§9: 分网/通量均低频。
// =====================================================================

namespace BlueprintUnlockHSK
{
    public enum GasPortRole { Pipe, Pump, Valve, Source, Sink }
    public enum GasSinkMode { FuelPool, Deposit }
    // 石化介质 (Verse.GasType 是原版空气气体枚举, 名字不同, 避免混淆)
    // Hydrogen 已并入 NaturalGas: 枚举值仅为老存档序列化保留, 取值处一律经 Norm 归一。
    // Asphalt(沥青) 2026-08-31 加入: 与天然气/氨共用同一套管/阀/泵/出料口框架, 独立成线。
    public enum PetroGas { NaturalGas, Hydrogen, Ammonia, Asphalt }

    public static class PetroGasUtil
    {
        // 氢 → 天然气 归一化 (老存档/残留 def 兼容的唯一入口)
        public static PetroGas Norm(PetroGas k) { return k == PetroGas.Hydrogen ? PetroGas.NaturalGas : k; }

        public static ThingDef DefOf(PetroGas k)
        {
            k = Norm(k);
            switch (k)
            {
                case PetroGas.NaturalGas: return DefDatabase<ThingDef>.GetNamed("RK_Petro_NaturalGas", false);
                case PetroGas.Ammonia: return DefDatabase<ThingDef>.GetNamed("SyntheticAmmonia", false);
                case PetroGas.Asphalt: return DefDatabase<ThingDef>.GetNamed("Asphalt", false);
            }
            return null;
        }

        // v3(2026-08-29): 便携气瓶 RK_Petro_GasCylinder 已删除 —— 气体进出网改由
        // 立式储罐(Refuelable 进料) + 管网出料口(Deposit 落地)承担, 不再有"灌装气瓶"这条路径。

        // 建筑内 (Building_Storage) 指定气体数量
        public static int CountInStore(Building_Storage store, ThingDef gasDef)
        {
            if (store == null || gasDef == null) return 0;
            int n = 0;
            List<IntVec3> cells = store.AllSlotCellsList();
            if (cells == null) return 0;
            for (int i = 0; i < cells.Count; i++)
            {
                List<Thing> list = store.Map.thingGrid.ThingsListAt(cells[i]);
                for (int j = 0; j < list.Count; j++)
                {
                    Thing t = list[j];
                    if (t != null && t.def == gasDef) n += t.stackCount;
                }
            }
            return n;
        }

        public static int ConsumeFromStore(Building_Storage store, ThingDef gasDef, int want)
        {
            int taken = 0;
            if (store == null || gasDef == null || want <= 0) return 0;
            List<IntVec3> cells = store.AllSlotCellsList();
            if (cells == null) return 0;
            for (int i = 0; i < cells.Count && taken < want; i++)
            {
                List<Thing> list = store.Map.thingGrid.ThingsListAt(cells[i]);
                for (int j = list.Count - 1; j >= 0 && taken < want; j--)
                {
                    Thing t = list[j];
                    if (t == null || t.def != gasDef) continue;
                    int take = Mathf.Min(t.stackCount, want - taken);
                    if (t.stackCount > take) t.SplitOff(take).Destroy();
                    else t.Destroy();
                    taken += take;
                }
            }
            return taken;
        }

        // 兼容新版储罐:优先读 CompRefuelable.Fuel(fuelDef 匹配), 退路走 Building_Storage 槽位
        // 后续罐子改造完成,退路分支应彻底走 Refuelable 路径
        public static int CountInBuilding(Building b, ThingDef gasDef)
        {
            if (b == null || gasDef == null) return 0;
            // 优先 Refuelable(从 fuel 池中抽")
            ThingWithComps twc = b as ThingWithComps;
            if (twc != null)
            {
                CompRefuelable rf = twc.GetComp<CompRefuelable>();
                if (rf != null && rf.Props.fuelFilter != null && rf.Props.fuelFilter.AllowedThingDefs != null
                    && rf.Props.fuelFilter.AllowedThingDefs.Contains(gasDef))
                {
                    return Mathf.FloorToInt(rf.Fuel);
                }
            }
            // 退路: Building_Storage 槽位
            return CountInStore(b as Building_Storage, gasDef);
        }

        // 优先从 Refuelable.Fuel 抽, 退路走 Building_Storage 槽位
        public static int ConsumeFromBuilding(Building b, ThingDef gasDef, int want)
        {
            if (b == null || gasDef == null || want <= 0) return 0;
            ThingWithComps twc = b as ThingWithComps;
            if (twc != null)
            {
                CompRefuelable rf = twc.GetComp<CompRefuelable>();
                if (rf != null && rf.Props.fuelFilter != null && rf.Props.fuelFilter.AllowedThingDefs != null
                    && rf.Props.fuelFilter.AllowedThingDefs.Contains(gasDef))
                {
                    int take = Mathf.Min(want, Mathf.FloorToInt(rf.Fuel));
                    if (take > 0)
                    {
                        rf.ConsumeFuel(take);
                    }
                    return take;
                }
            }
            return ConsumeFromStore(b as Building_Storage, gasDef, want);
        }

        // 建筑周围 (footprint 扩 radius) 地上的松散气
        public static int CountLooseNear(Building b, ThingDef gasDef, int radius)
        {
            if (b == null || gasDef == null) return 0;
            int n = 0;
            CellRect zone = b.OccupiedRect().ExpandedBy(radius);
            for (int x = zone.minX; x <= zone.maxX; x++)
            {
                for (int z = zone.minZ; z <= zone.maxZ; z++)
                {
                    List<Thing> list = b.Map.thingGrid.ThingsListAt(new IntVec3(x, 0, z));
                    for (int k = 0; k < list.Count; k++)
                    {
                        Thing t = list[k];
                        if (t == null || t.def != gasDef) continue;
                        if (t.holdingOwner != null) continue; // 只算地上
                        n += t.stackCount;
                    }
                }
            }
            return n;
        }
    }

    // ---------- 端口 ----------
    public class CompProperties_GasPort : CompProperties
    {
        public GasPortRole role = GasPortRole.Pipe;
        public PetroGas gasType = PetroGas.NaturalGas;
        public GasSinkMode mode = GasSinkMode.FuelPool;
        public int depositReserve = 12;    // Deposit 模式: 装置周围最多囤这么多
        public CompProperties_GasPort() { compClass = typeof(CompGasPort); }
    }

    public class CompGasPort : ThingComp
    {
        public static readonly List<CompGasPort> AllPorts = new List<CompGasPort>();

        public GasPortRole Role
        {
            get { CompProperties_GasPort p = props as CompProperties_GasPort; return (p != null) ? p.role : GasPortRole.Pipe; }
        }
        public PetroGas Kind
        {
            get { CompProperties_GasPort p = props as CompProperties_GasPort; return PetroGasUtil.Norm((p != null) ? p.gasType : PetroGas.NaturalGas); }
        }
        public GasSinkMode Mode
        {
            get { CompProperties_GasPort p = props as CompProperties_GasPort; return (p != null) ? p.mode : GasSinkMode.FuelPool; }
        }
        public int DepositReserve
        {
            get { CompProperties_GasPort p = props as CompProperties_GasPort; return (p != null) ? p.depositReserve : 12; }
        }

        public bool Active
        {
            get
            {
                Building b = parent as Building;
                if (b == null || !b.Spawned) return false;
                if (b.Faction != Faction.OfPlayer) return false;
                if (Role == GasPortRole.Valve)
                {
                    CompFlickable fl = b.GetComp<CompFlickable>();
                    if (fl != null && !fl.SwitchIsOn) return false;
                }
                return true;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!AllPorts.Contains(this)) AllPorts.Add(this);
            MapComponent_GasNet.MarkDirty(parent.Map);
        }

        public override void PostExposeData() { base.PostExposeData(); }

        // 被 SectionLayer_GasPipeOverlay.TakePrintFrom 调用: 在该 layer 上按 parent 占据格打印连管轮廓。
        // 与 Rimefeller.CompPipe.PrintForGrid 同位。阀门关/非玩家时上层已过滤 (Active=false 不调到这里)。
        public void PrintForOverlay(SectionLayer layer)
        {
            if (parent == null || parent.Map == null) return;
            Graphic g = SectionLayer_GasPipeOverlay.GetOverlayGraphic(PetroGasUtil.Norm(Kind));
            g.Print(layer, parent, 0f);
        }


        public override string CompInspectStringExtra()
        {
            // 管网连接/泵送数据 (对齐 FuelStorage 选中显示): 所有端口都显示所接入网络的规模与通量预算
            string head = null;
            if (Role == GasPortRole.Sink) head = "管网供气: 已接入 (模式 " + Mode + ")";
            else if (Role == GasPortRole.Source)
            {
                int n = PetroGasUtil.CountInBuilding(parent as Building, PetroGasUtil.DefOf(Kind));
                head = "管网供气源: 现存 " + n;
            }
            MapComponent_GasNet mc = MapComponent_GasNet.Get(parent.Map);
            GasNetwork net = (mc != null) ? mc.NetworkOf(this) : null;
            if (net != null)
                head = (head == null ? "管网" : head) + " | 已接入 " + net.members.Count + " 节点, 通量 " + net.Budget().ToString("F0") + "/周期";
            else
                head = (head == null ? "管网" : head) + " | 未接入 (周围无同介质管段/端口)";
            return head;
        }

    }

    // ---------- 单个连通网络 (单一介质) ----------
    public class GasNetwork
    {
        public PetroGas kind;
        public ThingDef gasDef;
        public List<CompGasPort> members = new List<CompGasPort>();

        private const float FlowBase = 6f;
        private const float FlowPerPump = 24f;

        // 当前通量预算(与 Flow() 同口径): 基础涓流 + 每台通电气泵加成, 供选中显示用
        public float Budget()
        {
            int pumps = 0;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].Role != GasPortRole.Pump) continue;
                Building pb = members[i].parent as Building;
                if (pb == null) continue;
                CompPowerTrader pt = pb.GetComp<CompPowerTrader>();
                if (pt != null && pt.PowerOn) pumps++;
            }
            return FlowBase + pumps * FlowPerPump;
        }

        public void Flow(Map map)
        {
            if (gasDef == null) return;

            int pumps = 0;
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].Role != GasPortRole.Pump) continue;
                Building pb = members[i].parent as Building;
                if (pb == null) continue;
                CompPowerTrader pt = pb.GetComp<CompPowerTrader>();
                if (pt != null && pt.PowerOn) pumps++;
            }
            float budget = FlowBase + pumps * FlowPerPump;

            // 汇总缺口: FuelPool + Deposit
            List<SinkReq> reqs = new List<SinkReq>();
            float demand = 0f;
            for (int i = 0; i < members.Count; i++)
            {
                CompGasPort p = members[i];
                if (p.Role != GasPortRole.Sink) continue;
                if (p.Kind != kind) continue;
                Building mb = p.parent as Building;
                if (mb == null) continue;
                if (p.Mode == GasSinkMode.FuelPool)
                {
                    ThingWithComps twc = mb as ThingWithComps;
                    CompRefuelable refu = (twc != null) ? twc.GetComp<CompRefuelable>() : null;
                    if (refu == null) continue;
                    float cap = refu.Props.fuelCapacity;
                    float space = cap - refu.Fuel;
                    if (space <= 0.5f) continue;
                    reqs.Add(new SinkReq { pool = refu, building = null, need = space });
                    demand += space;
                }
                else
                {
                    int have = PetroGasUtil.CountLooseNear(mb, gasDef, 2);
                    int reserve = p.DepositReserve;
                    int space = reserve - have;
                    if (space <= 0) continue;
                    reqs.Add(new SinkReq { pool = null, building = mb, need = space });
                    demand += space;
                }
            }
            if (reqs.Count == 0) return;

            float want = Mathf.Min(budget, demand);
            if (want <= 0f) return;

            // 供: 同网络内所有 Source 储罐(Refuelable 优先, Building_Storage 退路)
            float moved = 0f;
            for (int i = 0; i < members.Count && moved < want; i++)
            {
                if (members[i].Role != GasPortRole.Source) continue;
                if (members[i].Kind != kind) continue;
                Building srcB = members[i].parent as Building;
                if (srcB == null) continue;
                int need = (int)(want - moved);
                moved += PetroGasUtil.ConsumeFromBuilding(srcB, gasDef, need);
            }

            // 分派: 优先 FuelPool (更贴近即时消耗), 再 Deposit
            float remaining = moved;
            for (int i = 0; i < reqs.Count && remaining > 0.5f; i++)
            {
                if (reqs[i].pool == null) continue;
                float space = reqs[i].pool.Props.fuelCapacity - reqs[i].pool.Fuel;
                if (space <= 0f) continue;
                float give = Mathf.Min(space, remaining);
                reqs[i].pool.Refuel(give);
                remaining -= give;
            }
            for (int i = 0; i < reqs.Count && remaining >= 1f; i++)
            {
                if (reqs[i].building == null) continue;
                int give = (int)Mathf.Min((float)reqs[i].need, remaining);
                if (give <= 0) continue;
                SpawnLooseAt(reqs[i].building, gasDef, give, map);
                remaining -= give;
            }
        }

        private void SpawnLooseAt(Building b, ThingDef def, int amount, Map map)
        {
            CellRect zone = b.OccupiedRect().ExpandedBy(2);
            int spawned = 0;
            for (int x = zone.minX; x <= zone.maxX && spawned < amount; x++)
            {
                for (int z = zone.minZ; z <= zone.maxZ && spawned < amount; z++)
                {
                    IntVec3 c = new IntVec3(x, 0, z);
                    if (!c.InBounds(map)) continue;
                    if (!c.Standable(map)) continue;
                    if (c.GetEdifice(map) != null) continue;
                    int take = Mathf.Min(amount - spawned, 75);
                    Thing t = ThingMaker.MakeThing(def);
                    t.stackCount = Mathf.Min(take, def.stackLimit);
                    if (GenPlace.TryPlaceThing(t, c, map, ThingPlaceMode.Direct))
                        spawned += t.stackCount;
                    else
                        break;
                    if (spawned >= amount) break;
                }
            }
        }


        private class SinkReq
        {
            public CompRefuelable pool;
            public Building building;
            public float need;
        }
    }

    // ---------- 每地图的气网管理器 ----------
    public class MapComponent_GasNet : MapComponent
    {
        private static readonly Dictionary<Map, MapComponent_GasNet> instances = new Dictionary<Map, MapComponent_GasNet>();

        private List<GasNetwork> networks = new List<GasNetwork>();
        private int tick;
        private bool dirty = true;

        public MapComponent_GasNet(Map map) : base(map) { instances[map] = this; }
        public MapComponent_GasNet(Map map, bool applyGameSpeed) : base(map) { instances[map] = this; }

        public static MapComponent_GasNet Get(Map map)
        {
            MapComponent_GasNet m;
            if (map != null && instances.TryGetValue(map, out m)) return m;
            return null;
        }

        public static void MarkDirty(Map map)
        {
            MapComponent_GasNet m = Get(map);
            if (m != null) m.dirty = true;
        }

        // 查某端口所属的连通网络(供选中显示; 低频, 仅 inspect 时调用)
        public GasNetwork NetworkOf(CompGasPort p)
        {
            if (p == null) return null;
            for (int i = 0; i < networks.Count; i++)
                if (networks[i].members.Contains(p)) return networks[i];
            return null;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            tick++;
            if (dirty || tick % 300 == 0) { Rebuild(); dirty = false; }
            if (networks.Count > 0 && tick % 60 == 0)
                for (int i = 0; i < networks.Count; i++) networks[i].Flow(map);
        }

        private void Rebuild()
        {
            networks.Clear();
            // 清理失效端口
            for (int d = CompGasPort.AllPorts.Count - 1; d >= 0; d--)
            {
                CompGasPort dp = CompGasPort.AllPorts[d];
                Building db = (dp != null) ? dp.parent as Building : null;
                if (dp == null || db == null || !db.Spawned) CompGasPort.AllPorts.RemoveAt(d);
            }
            List<CompGasPort> nodes = new List<CompGasPort>();
            for (int i = 0; i < CompGasPort.AllPorts.Count; i++)
            {
                CompGasPort p = CompGasPort.AllPorts[i];
                if (p == null) continue;
                Building b = p.parent as Building;
                if (b == null || b.Map != map) continue;
                if (!p.Active) continue;
                nodes.Add(p);
            }
            int n = nodes.Count;
            int[] parentArr = new int[n];
            for (int i = 0; i < n; i++) parentArr[i] = i;

            // **关键**: claim 按 gasType 分离 → 不同介质即便相邻也不 union, 天然分线
            Dictionary<IntVec3, int>[] claim = new Dictionary<IntVec3, int>[4];
            for (int t = 0; t < 4; t++) claim[t] = new Dictionary<IntVec3, int>();

            for (int i = 0; i < n; i++)
            {
                Building b = nodes[i].parent as Building;
                int ti = (int)nodes[i].Kind;
                if (ti < 0 || ti > 3) continue;
                CellRect zone = b.OccupiedRect().ExpandedBy(1);
                for (int x = zone.minX; x <= zone.maxX; x++)
                {
                    for (int z = zone.minZ; z <= zone.maxZ; z++)
                    {
                        IntVec3 c = new IntVec3(x, 0, z);
                        int owner;
                        if (claim[ti].TryGetValue(c, out owner)) Union(parentArr, i, owner);
                        else claim[ti][c] = i;
                    }
                }
            }

            Dictionary<int, GasNetwork> byRoot = new Dictionary<int, GasNetwork>();
            for (int i = 0; i < n; i++)
            {
                int root = Find(parentArr, i);
                GasNetwork net;
                if (!byRoot.TryGetValue(root, out net))
                {
                    net = new GasNetwork();
                    net.kind = nodes[i].Kind;
                    net.gasDef = PetroGasUtil.DefOf(nodes[i].Kind);
                    byRoot[root] = net;
                }
                net.members.Add(nodes[i]);
            }
            foreach (KeyValuePair<int, GasNetwork> kv in byRoot)
                networks.Add(kv.Value);
        }

        private static int Find(int[] p, int a)
        {
            while (p[a] != a) { p[a] = p[p[a]]; a = p[a]; }
            return a;
        }
        private static void Union(int[] p, int a, int b)
        {
            int ra = Find(p, a), rb = Find(p, b);
            if (ra != rb) p[rb] = ra;
        }
    }


    public static class GasHazards
    {
        public static bool FriendlyRemoval(DestroyMode m)
        {
            return m == DestroyMode.Deconstruct || m == DestroyMode.Vanish
                || m == DestroyMode.WillReplace || m == DestroyMode.Refund
                || m == DestroyMode.FailConstruction || m == DestroyMode.Cancel;
        }
    }

    // ---------- 气: 按存量缩放爆炸 (氢并入天然气后按罐内存气计) ----------
    public class CompProperties_GasExplosive : CompProperties
    {
        public float minRadius = 1.5f;
        public float perUnit = 0.02f;      // 每单位气体增加的半径
        public float maxRadius = 6f;
        public int nominalUnits = 6;       // 管段类节点固定视为持有这么多单位
        public CompProperties_GasExplosive() { compClass = typeof(CompGasExplosive); }
    }

    public class CompGasExplosive : ThingComp
    {
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (GasHazards.FriendlyRemoval(mode)) return;   // 玩家主动拆除/回收不炸
            Building b = parent as Building;
            if (b == null) return;
            CompProperties_GasExplosive p = props as CompProperties_GasExplosive;
            if (p == null) return;
            Map m = b.MapHeld != null ? b.MapHeld : previousMap;
            if (m == null) return;

            int units = p.nominalUnits;
            // 优先 CompRefuelable.Fuel (新 Refuelable 罐), 退路 Building_Storage 槽位
            ThingDef h2 = PetroGasUtil.DefOf(PetroGas.Hydrogen);
            int n = PetroGasUtil.CountInBuilding(b, h2);
            if (n > units) units = n;
            float radius = Mathf.Min(p.maxRadius, p.minRadius + p.perUnit * (float)units);
            if (radius <= 0f) return;
            GenExplosion.DoExplosion(b.PositionHeld, m, radius, DamageDefOf.Flame, null,
                explosionSound: null, weapon: null, projectile: null, intendedTarget: null,
                postExplosionSpawnThingDef: null, postExplosionSpawnChance: 0f, postExplosionSpawnThingCount: 1,
                preExplosionSpawnThingDef: null, preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0.25f, damageFalloff: true);
        }
    }

    // ---------- 氨: 泄漏原生 ToxGas 毒气云 ----------
    public class CompProperties_GasToxic : CompProperties
    {
        public int amountPerCell = 80;    // 单格初始浓度
        public int radius = 2;            // footprint 外扩几格布气
        public int nominalAmount = 120;   // 非罐体节点按此数量决定半径
        public CompProperties_GasToxic() { compClass = typeof(CompGasToxic); }
    }

    public class CompGasToxic : ThingComp
    {
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (GasHazards.FriendlyRemoval(mode)) return;   // 玩家主动拆除不泄漏
            Building b = parent as Building;
            if (b == null) return;
            CompProperties_GasToxic p = props as CompProperties_GasToxic;
            if (p == null) return;
            Map m = b.MapHeld != null ? b.MapHeld : previousMap;
            if (m == null) return;

            // 罐体按现存氨数量放大布气半径 (每 60 单位 +1 格, 上限 +6)
            // 优先 CompRefuelable.Fuel (新 Refuelable 罐), 退路 Building_Storage 槽位
            int extra = 0;
            ThingDef nh3 = PetroGasUtil.DefOf(PetroGas.Ammonia);
            int have = PetroGasUtil.CountInBuilding(b, nh3);
            extra = Mathf.Min(6, have / 60);
            int radius = p.radius + extra;
            CellRect zone = b.OccupiedRect().ExpandedBy(radius);
            for (int x = zone.minX; x <= zone.maxX; x++)
            {
                for (int z = zone.minZ; z <= zone.maxZ; z++)
                {
                    IntVec3 c = new IntVec3(x, 0, z);
                    if (!c.InBounds(m)) continue;
                    try { m.gasGrid.AddGas(c, GasType.ToxGas, p.amountPerCell, true); }
                    catch (Exception) { }
                }
            }
        }
    }
}

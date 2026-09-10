using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// =====================================================================
//  石化管网高亮 SectionLayer (2026-09-01) — 仿 Rimefeller 化合燃料管道 SectionLayer_PipeOverlay
//
//  效果(对齐 Rimefeller.CompoundFuel 管网覆盖):
//   1. 触发: 玩家选中 designator 准备放置某种气体的管道 (def 含 CompProperties_GasPort 且 gasType 匹配)
//      → 该气体的 SectionLayer_GasPipeOverlay 子类被绘制, 地图上整条同种管网亮起连管轮廓.
//   2. 范围: 同种气体的所有 CompGasPort 节点 (管道/阀门/气泵/出料口/储罐), Active=false (阀门关) 跳过.
//   3. 视觉: 复用 Rimefeller 的 PipeOverlay_Atlas (512x512 LA 灰度), 用 MetaOverlay shader 染色成对应
//      气体颜色; Graphic_GasPipeOverlay.Print 仿 Rimefeller.Graphic_LinkedPipeOverlay 按 OccupiedRect
//      逐格画 1x1 plane + LinkedDrawMatFrom 切 atlas, 保证 2x2 泵等大建筑每个被占格都盖到.
//
//  注册: 1.6 Verse.Section 构造里 typeof(SectionLayer).AllSubclassesNonAbstract() + Activator.CreateInstance
//  自动反射所有非 abstract 子类并 Add 到 layers, **不需要 Harmony patch 注入**. 我们的 3 个子类
//  (NG/Ammonia/Asphalt) 直接被 1.6 自动发现. §0.3: 三种介质各一个 layer, 互不干扰.
//
//  颜色: 改 ModeColors 数组即可。2026-09-01 用户要求三色必须与边缘化工(Rimefeller)、卫生(DBH)的
//  管道覆盖色**全部不重复**。已占用的色(避开):
//    Rimefeller 油/化合燃料 = 酒红 (145,15,76)
//    DBH 自来水 = 红 (208,64,62) / 排污 = 青绿 (47,225,169) / 供暖 = 绿 (29,182,134) / 通风 = 蓝紫 (83,62,208)
//  现取用空闲色系: NG=亮橙 / 氨气=品红 / 沥青=天蓝, 三者彼此及与上述五色的色相距离均 > 60°.
//
//  atlas 路径: 复用 Rimefeller 的 Things/Building/Linked/RK_Petro_PipeOverlay_Atlas.png (LA 灰度 + MetaOverlay).
// =====================================================================

namespace BlueprintUnlockHSK
{
    // 与 Rimefeller.Graphic_LinkedPipeOverlay 对位: Graphic_Linked 子类, 改写 Print 让大建筑按
    // OccupiedRect 逐格画 1x1 plane. 默认 Graphic_Linked.Print 只画 parent.Position 一格,
    // 对 2x2 气泵会漏掉 3 格.
    //
    // ⚠️ 必须覆写 ShouldLinkWith (2026-09-01 实测教训): 原版默认判定是
    //   linkGrid.LinkFlagsAt(c) & parent.def.graphicData.linkFlags
    // 只认 def 上声明的 linkFlags — 周围没有带 Custom 链位flag的邻接建筑时, 每格都取 atlas 里
    // 的"无连接"小胶囊块, 画出来就是一堆散点 (用户截图实测). Rimefeller 是按
    // Map.Rimefeller().ZoneAt(c, mode) (同介质管网 zone 网格) 判定的.
    // 我们这里等价实现: 本建筑内部格必链 + 邻格存在同介质 CompGasPort 建筑即链.
    public class Graphic_GasPipeOverlay : Graphic_Linked
    {
        // 本 graphic 服务哪种介质 (GetOverlayGraphic 里赋值, 已 Norm).
        public PetroGas mode;

        public Graphic_GasPipeOverlay() { }
        public Graphic_GasPipeOverlay(Graphic subGraphic) : base(subGraphic) { }

        // 仿 Rimefeller.ZoneAt(c, mode): 邻格属于同介质管网就链.
        // 含本建筑自身其它格 (3x2 储罐内部格), 否则孤立储罐画不出整块轮廓.
        public override bool ShouldLinkWith(IntVec3 c, Thing parent)
        {
            if (!parent.Spawned) return false;
            if (!c.InBounds(parent.Map)) return false;
            // 2026-09-01 性能(第二刀): 不再逐格翻 ThingsListAt — 查激活 layer 刚构建的
            // 同介质占用格集合(O(1) 查表). 语义与原实现一致: 本建筑内部格 + 邻格有同介质
            // CompGasPort 即链 (集合含全图所有同介质端口占用格, 含 parent 自己).
            return SectionLayer_GasPipeOverlay.IsModeCell(mode, c);
        }

        public override void Print(SectionLayer layer, Thing parent, float extraRotation)
        {
            CellRect rect = GenAdj.OccupiedRect(parent);
            for (int i = 0; i < rect.Width; i++)
            {
                for (int j = 0; j < rect.Height; j++)
                {
                    IntVec3 c = new IntVec3(rect.minX + i, 0, rect.minZ + j);
                    Vector3 pos = c.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                    Printer_Plane.PrintPlane(layer, pos, Vector2.one, LinkedDrawMatFrom(parent, c), extraRotation, false, null, null, 0.01f, 0f);
                }
            }
        }
    }

    // 抽象基类: 1.6 反射时会跳过 abstract, 只实例化 3 个具体子类.
    public abstract class SectionLayer_GasPipeOverlay : SectionLayer_Things
    {
        public PetroGas mode;

        // 沿用 Rimefeller 化合燃料的连管轮廓 atlas, 灰度+alpha, MetaOverlay 染色.
        private const string OverlayTexPath = "Things/Building/Linked/RK_Petro_PipeOverlay_Atlas";

        // 三种介质色. 顺序与 PetroGas 枚举一致 (NaturalGas=0, Hydrogen=1, Ammonia=2, Asphalt=3).
        // 全部避开 Rimefeller/DBH 已占用的 红/酒红/青绿/绿/蓝紫 五色系.
        private static readonly Color[] ModeColors = new Color[]
        {
            new Color(1.000f, 0.569f, 0.000f),  // NaturalGas 亮橙 (255,145,0)
            new Color(1.000f, 0.569f, 0.000f),  // Hydrogen   同 NG (Norm 归一, 实际用不到)
            new Color(0.882f, 0.235f, 0.784f),  // Ammonia    品红 (225,60,200)
            new Color(0.275f, 0.784f, 0.961f),  // Asphalt    天蓝 (70,200,245)
        };

        // 只读 graphic 缓存: 每介质一个, 全 mod 共享.
        private static readonly Dictionary<PetroGas, Graphic> GraphicCache = new Dictionary<PetroGas, Graphic>();

        public static Graphic GetOverlayGraphic(PetroGas k)
        {
            Graphic g;
            if (GraphicCache.TryGetValue(k, out g)) return g;
            int idx = (int)k;
            Color c = (idx >= 0 && idx < ModeColors.Length) ? ModeColors[idx] : Color.white;
            Graphic sub = GraphicDatabase.Get<Graphic_Single>(OverlayTexPath, ShaderDatabase.MetaOverlay, Vector2.one, c);
            Graphic_GasPipeOverlay linked = new Graphic_GasPipeOverlay(sub);
            linked.mode = k;  // ShouldLinkWith 按此介质判邻格; 调用方传的已是 Norm 值
            g = linked;
            GraphicCache[k] = g;
            return g;
        }

        public SectionLayer_GasPipeOverlay(Section section) : base(section)
        {
            // 仿 Rimefeller: 不进主地图 mesh 渲染流程, 只在 DrawLayer() 主动判定时绘制.
            requireAddToMapMesh = false;
            // 1.6: relevantChangeTypes 是 ulong, MapMeshFlagDef 有 implicit operator ulong, 直接隐式赋值.
            relevantChangeTypes = MapMeshFlagDefOf.Buildings;
        }

        // 仿 Rimefeller.SectionLayer_PipeOverlay.DrawLayer: 只在 designator 指向同种气体管网时,
        // 才调 base.DrawLayer() 把这一层画出来. 否则整层不显示 (省 GPU + 不会遮挡).
        // 触发来源二选一 (见 PetroPipeOverlayUtil.ActiveOverlayMode):
        //   ① Designator_Build: 手里正拿着该气体的管道/阀门/泵/出料口准备放置
        //   ② Designator_RemoveGasPipe: 正用"拆除XX管"框选 (仿 Rimefeller 拆油管也亮网的行为)
        //
        // ⚠️ 性能 (2026-09-01 用户反馈"选拆除工具时卡"): 1.6 Section.DrawSection 每帧把
        // Buildings flag 标脏分发给所有 layer, 3 个介质 layer 全都 Dirty → 全部全 section 逐格
        // 遍历 Regenerate (SectionLayer_Things.Regenerate = 遍历 section.CellRect 每格 × 每格
        // things + TakePrintFrom 过滤), 而同时最多只有 1 个介质被激活. 修复:
        //   Regenerate 短路 — 未激活时只 ClearSubMeshes 记 pending, 不遍历; 激活时若 pending
        //   先在 DrawLayer 里补一次 base.Regenerate() 再绘制. 这样未激活的 2 个层零成本,
        //   激活层开销与 Rimefeller 单层持平.
        public override void Regenerate()
        {
            if (!ActiveNow)
            {
                pendingRegen = true;
                ClearSubMeshes(MeshParts.All);
                return;
            }
            pendingRegen = false;
            RebuildNow();
        }

        public override void DrawLayer()
        {
            if (!ActiveNow) return;
            if (pendingRegen)
            {
                pendingRegen = false;
                RebuildNow();
            }
            base.DrawLayer();
        }

        // 2026-09-01 性能(第二刀): 激活时不再走 SectionLayer_Things.Regenerate 的
        // "整 section 逐格扫描"(250x250 地图 = 62500 格 x ThingsListAt + 过滤),
        // 改遍历 MapComponent 缓存的 CompGasPort.AllPorts 节点表, 只画同介质 + Active + 非雾节点.
        // 扫描量从 O(全格) 降到 O(该介质端口数) ≈ 99% 下降.
        // fog 过滤保留(对齐原版: 雾里建筑不打印); 雪深过滤不保留(管网埋雪下仍应可辨, 与 Rimefeller 一致).
        private void RebuildNow()
        {
            ClearSubMeshes(MeshParts.All);
            BuildModeCellSet();
            Map map = Map;
            List<CompGasPort> ports = CompGasPort.AllPorts;
            for (int i = 0; i < ports.Count; i++)
            {
                CompGasPort p = ports[i];
                if (p == null) continue;
                Building b = p.parent as Building;
                if (b == null || b.Map != map) continue;
                if (PetroGasUtil.Norm(p.Kind) != mode) continue;
                if (!p.Active) continue;
                if (map.fogGrid.IsFogged(b.Position)) continue;
                p.PrintForOverlay(this);
            }
            FinalizeMesh(MeshParts.All);
        }

        // 该介质全图端口占用格集合 (ShouldLinkWith 查表用). 只在激活 layer 重建时构建一次,
        // 生命周期 = 一次 RebuildNow, 与 ShouldLinkWith 的调用窗口完全重合, 无陈旧风险.
        private static readonly Dictionary<PetroGas, HashSet<IntVec3>> ModeCellSets = new Dictionary<PetroGas, HashSet<IntVec3>>();

        public static bool IsModeCell(PetroGas m, IntVec3 c)
        {
            HashSet<IntVec3> set;
            if (!ModeCellSets.TryGetValue(m, out set)) return false;
            return set.Contains(c);
        }

        private void BuildModeCellSet()
        {
            HashSet<IntVec3> set;
            if (!ModeCellSets.TryGetValue(mode, out set))
            {
                set = new HashSet<IntVec3>();
                ModeCellSets[mode] = set;
            }
            set.Clear();
            Map map = Map;
            List<CompGasPort> ports = CompGasPort.AllPorts;
            for (int i = 0; i < ports.Count; i++)
            {
                CompGasPort p = ports[i];
                if (p == null) continue;
                Building b = p.parent as Building;
                if (b == null || b.Map != map) continue;
                if (PetroGasUtil.Norm(p.Kind) != mode) continue;
                CellRect r = GenAdj.OccupiedRect(b);
                for (int x = r.minX; x <= r.maxX; x++)
                {
                    for (int z = r.minZ; z <= r.maxZ; z++)
                    {
                        set.Add(new IntVec3(x, 0, z));
                    }
                }
            }
        }

        // SectionLayer_Things 覆写了 GetBoundaryRect 返回私有 bounds — 我们不走它的逐格扫描
        // (不调 base.Regenerate, bounds 永不更新), 这里改回基类语义: 整 section 可见范围, 防 culling 误判.
        public override CellRect GetBoundaryRect() { return section.CellRect; }

        // 当前 designator 是否指向本 layer 的介质 (每帧调用, 保持轻量: 不做 LINQ 分配).
        private bool ActiveNow
        {
            get
            {
                Designator des = Find.DesignatorManager.SelectedDesignator;
                if (des == null) return false;
                Designator_RemoveGasPipe rem = des as Designator_RemoveGasPipe;
                if (rem != null) return PetroGasUtil.Norm(rem.mode) == mode;
                Designator_Build db = des as Designator_Build;
                if (db == null) return false;
                ThingDef td = db.PlacingDef as ThingDef;
                if (td == null || td.comps == null) return false;
                for (int i = 0; i < td.comps.Count; i++)
                {
                    CompProperties_GasPort props = td.comps[i] as CompProperties_GasPort;
                    if (props != null) return PetroGasUtil.Norm(props.gasType) == mode;
                }
                return false;
            }
        }

        private bool pendingRegen = true;

        // 仿 Rimefeller.SectionLayer_PipeOverlay.TakePrintFrom: 每格每个 thing 都会调到这里.
        // 过滤: 同介质 + Active (阀门关/非玩家跳过) → CompGasPort.PrintForOverlay.
        protected override void TakePrintFrom(Thing t)
        {
            Building b = t as Building;
            if (b == null) return;
            CompGasPort p = b.GetComp<CompGasPort>();
            if (p == null) return;
            if (PetroGasUtil.Norm(p.Kind) != mode) return;
            if (!p.Active) return;
            p.PrintForOverlay(this);
        }
    }

    // 1.6 Section 构造里 typeof(SectionLayer).AllSubclassesNonAbstract() 自动发现这三个, 各 Section 各加一份.
    // 顺序无要求 (DrawLayer 各自按 mode 判定, 互不干扰).
    public class SectionLayer_NGPipeOverlay : SectionLayer_GasPipeOverlay
    {
        public SectionLayer_NGPipeOverlay(Section section) : base(section) { mode = PetroGas.NaturalGas; }
    }
    public class SectionLayer_AmmoniaPipeOverlay : SectionLayer_GasPipeOverlay
    {
        public SectionLayer_AmmoniaPipeOverlay(Section section) : base(section) { mode = PetroGas.Ammonia; }
    }
    public class SectionLayer_AsphaltPipeOverlay : SectionLayer_GasPipeOverlay
    {
        public SectionLayer_AsphaltPipeOverlay(Section section) : base(section) { mode = PetroGas.Asphalt; }
    }
}

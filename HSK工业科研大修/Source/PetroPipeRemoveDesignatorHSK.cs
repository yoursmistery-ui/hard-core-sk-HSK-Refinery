using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

// =====================================================================
//  石化管网"拆除XX管"工具 (2026-09-01)
//
//  挂位: 建筑菜单 → 边缘石化(Rimefeller) 分类, 与 Rimefeller 自带的"拆除管道"(只拆油管)并列.
//  形态: 一个 Designator_Dropdown 下拉分组 (用户要求: 不要并排 3 个按钮), 点开后选
//        拆除天然气管 / 拆除氨气管 / 拆除沥青管, 选中后即可拖框批量下"拆除"标记.
//
//  实现要点:
//   1. 仿 Rimefeller.Designator_RemovePipeline (同 CanDesignateThing / TopDeconstructibleInCell 结构),
//      差别只在过滤条件: Rimefeller 比对 Building_Pipe.pipe.mode, 我们比对 CompGasPort.Kind
//      (已 Norm, 故 Hydrogen 残留档自动按天然气管处理).
//   2. 只给"玩家阵营、尚无拆除/卸载标记、带 CompGasPort 且介质匹配"的建筑下 Deconstruct 标记,
//      不直接 Destroy — 仍走小人施工, 与手工拆除完全一致.
//   3. 选中本工具时, 对应介质的 SectionLayer_GasPipeOverlay 会亮起整条管网
//      (见 PetroPipeOverlayUtil.ActiveOverlayMode 分支 ②), 便于准确框选.
//
//  注册: 靠 XML 补丁把 BlueprintUnlockHSK.Designator_RemoveGasPipeGroup 写进
//  DesignationCategoryDef(Rimefeller) 的 specialDesignatorClasses, 由游戏 Activator.CreateInstance 无参构造.
//  文案: defaultLabel/defaultDesc 走 Keyed 翻译 (Languages/*/Keyed/Keys_PetroGasNet.xml).
// =====================================================================

namespace BlueprintUnlockHSK
{
    // 单个介质的拆除工具. 抽象: 自身不进 specialDesignatorClasses, 只被下拉分组持有.
    public abstract class Designator_RemoveGasPipe : Designator
    {
        public PetroGas mode;

        // 管道类工具都归到"区域"绘制风格, 与 Rimefeller 的拆除管道保持一致 (拖框带测量).
        public override DrawStyleCategoryDef DrawStyleCategory
        {
            get { return DrawStyleCategoryDefOf.Areas; }
        }

        public override bool DragDrawMeasurements
        {
            get { return true; }
        }

        protected Designator_RemoveGasPipe(PetroGas m)
        {
            mode = m;
            defaultLabel = LabelKey.Translate();
            defaultDesc = DescKey.Translate();
            icon = ContentFinder<Texture2D>.Get(IconPath, true);
            useMouseIcon = true;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            soundSucceeded = SoundDefOf.Designate_SmoothSurface;
        }

        protected abstract string LabelKey { get; }
        protected abstract string DescKey { get; }
        protected abstract string IconPath { get; }

        public override void ProcessInput(Event ev)
        {
            if (CheckCanInteract()) base.ProcessInput(ev);
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map)) return false;
            if (!DebugSettings.godMode && c.Fogged(Map)) return false;
            if (TopDeconstructibleInCell(c) == null) return false;
            return true;
        }

        public override void DesignateSingleCell(IntVec3 loc)
        {
            Thing t = TopDeconstructibleInCell(loc);
            if (t != null) DesignateThing(t);
        }

        // 同格多个建筑时取"最上层"那个可被本工具拆除的 (按 altitudeLayer 降序), 与 Rimefeller 一致.
        // ⚠️ 性能 (2026-09-01): 拖框时引擎对鼠标划过的每一格调 CanDesignateCell → 这里. 原 LINQ
        // orderby 每格分配枚举器 + 排序, 大框几百格/帧累积成卡顿. 改手写循环取最高层 (零分配).
        private Thing TopDeconstructibleInCell(IntVec3 loc)
        {
            Thing best = null;
            int bestAlt = int.MinValue;
            List<Thing> things = Map.thingGrid.ThingsListAt(loc);
            for (int i = 0; i < things.Count; i++)
            {
                Thing t = things[i];
                if (!CanDesignateThing(t).Accepted) continue;
                int alt = (int)t.def.altitudeLayer;
                if (alt > bestAlt)
                {
                    bestAlt = alt;
                    best = t;
                }
            }
            return best;
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            Building b = t as Building;
            if (b == null) return false;
            if (b.def.category != ThingCategory.Building) return false;

            if (!DebugSettings.godMode && b.Faction != Faction.OfPlayer)
            {
                if (b.Faction != null) return false;
                if (!b.ClaimableBy(Faction.OfPlayer)) return false;
            }
            if (Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) return false;
            if (Map.designationManager.DesignationOn(t, DesignationDefOf.Uninstall) != null) return false;

            CompGasPort port = b.GetComp<CompGasPort>();
            if (port == null) return false;
            if (PetroGasUtil.Norm(port.Kind) != PetroGasUtil.Norm(mode)) return false;
            return true;
        }

        // 只下"拆除"标记, 不直接销毁 — 由小人照常施工拆除 (爆炸/毒气 comp 的玩家主动拆除不触发).
        public override void DesignateThing(Thing t)
        {
            Map.designationManager.AddDesignation(
                new Designation(new LocalTargetInfo(t), DesignationDefOf.Deconstruct, null));
        }

        public override void SelectedUpdate()
        {
            GenUI.RenderMouseoverBracket();
        }
    }

    public class Designator_RemoveGasPipe_NG : Designator_RemoveGasPipe
    {
        public Designator_RemoveGasPipe_NG() : base(PetroGas.NaturalGas) { }
        protected override string LabelKey { get { return "RK_Petro_RemoveGasPipe_NG"; } }
        protected override string DescKey { get { return "RK_Petro_RemoveGasPipe_NG_Desc"; } }
        protected override string IconPath { get { return "Things/Building/RK_Petro_GasPipe_NG_MenuIcon"; } }
    }

    public class Designator_RemoveGasPipe_Ammonia : Designator_RemoveGasPipe
    {
        public Designator_RemoveGasPipe_Ammonia() : base(PetroGas.Ammonia) { }
        protected override string LabelKey { get { return "RK_Petro_RemoveGasPipe_NH3"; } }
        protected override string DescKey { get { return "RK_Petro_RemoveGasPipe_NH3_Desc"; } }
        protected override string IconPath { get { return "Things/Building/RK_Petro_GasPipe_NH3_MenuIcon"; } }
    }

    public class Designator_RemoveGasPipe_Asphalt : Designator_RemoveGasPipe
    {
        public Designator_RemoveGasPipe_Asphalt() : base(PetroGas.Asphalt) { }
        protected override string LabelKey { get { return "RK_Petro_RemoveGasPipe_Asphalt"; } }
        protected override string DescKey { get { return "RK_Petro_RemoveGasPipe_Asphalt_Desc"; } }
        protected override string IconPath { get { return "Things/Building/RK_Petro_GasPipe_Asphalt_MenuIcon"; } }
    }

    // 建筑菜单里的那一个按钮: 显示当前选中项 + 右下角"+"角标, 点开列出三种介质.
    // Designator_Dropdown 自己会把 Label/Desc/icon/绘制全转发给 activeDesignator, 这里只管填元素.
    public class Designator_RemoveGasPipeGroup : Designator_Dropdown
    {
        public Designator_RemoveGasPipeGroup()
        {
            Add(new Designator_RemoveGasPipe_NG());
            Add(new Designator_RemoveGasPipe_Ammonia());
            Add(new Designator_RemoveGasPipe_Asphalt());
        }
    }
}

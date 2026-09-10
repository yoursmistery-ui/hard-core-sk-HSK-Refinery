using RimWorld;
using UnityEngine;
using Verse;

// =====================================================================
//  储罐液位显示 (2026-08-31) — 并入 HSK工业科研大修
//  仿 Rimefeller.CompProperties_StorageTank 的"选中悬浮液位条 + 储量文本"效果,
//  但 Rimefeller 原版 comp 的 Contents 只认 Fuel/Oil 且绑它自己的油/燃气管网,
//  无法承载本 mod 的沥青/天然气/氨。这里改为读同 def 上的 CompRefuelable 燃料量
//  (沥青罐=纯 Refuelable; 天然气/氨罐=Refuelable + GasPort 管网, 存量都落在 Fuel 上)。
//  选中罐子时在地面画一条 GenDraw.FillableBar, info 卡显示 "储量 X/CAP"。
//  空罐时画原版"缺料"闪烁图标 (OverlayTypes.OutOfFuel, 与化合燃料同款)。
//  C#5 兼容 (无插值/无 ?./无表达式体成员)。§9: 仅选中时绘制液位条, 无 tick 开销。
// =====================================================================

namespace BlueprintUnlockHSK
{
    public class CompProperties_TankGaugeHSK : CompProperties
    {
        public string label = "储量";
        public Color barColor = new Color(0.2f, 0.8f, 1f);

        public CompProperties_TankGaugeHSK()
        {
            compClass = typeof(CompTankGaugeHSK);
        }
    }

    public class CompTankGaugeHSK : ThingComp
    {
        private Material filledMat;
        private Material unfilledMat;

        private CompProperties_TankGaugeHSK Props
        {
            get { return (CompProperties_TankGaugeHSK)props; }
        }

        private CompRefuelable Refuel
        {
            get { return parent.GetComp<CompRefuelable>(); }
        }

        private float Cap
        {
            get
            {
                CompRefuelable r = Refuel;
                return (r != null) ? r.Props.fuelCapacity : 0f;
            }
        }

        private float Fill
        {
            get
            {
                CompRefuelable r = Refuel;
                return (r != null) ? r.Fuel : 0f;
            }
        }

        // 空罐 → 画原版"缺料"闪烁图标 (与化合燃料完全同款 OverlayTypes.OutOfFuel)。
        // 不受 CompRefuelable 的 allowAutoRefuel 门控影响, 只要罐子空了就一定出现。
        // DrawOverlay 内部按位 OR, 与原版 comp 自身那次调用幂等, 不会重复渲染。§9: 仅一次字典写入。
        public override void PostDraw()
        {
            base.PostDraw();
            CompRefuelable r = Refuel;
            if (r != null && Cap > 0f && !r.HasFuel && parent.Map != null)
            {
                parent.Map.overlayDrawer.DrawOverlay(parent, OverlayTypes.OutOfFuel);
            }
        }

        // 用 PostDrawExtraSelectionOverlays: 仅选中时调用, 且画在选择框之上(比 PostDraw 更稳, 不被罐体遮挡)
        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Cap <= 0f)
            {
                return;
            }
            if (!parent.Spawned)
            {
                return;
            }
            if (filledMat == null)
            {
                filledMat = SolidColorMaterials.SimpleSolidColorMaterial(Props.barColor, false);
                unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.28f, 0.28f, 0.30f, 0.85f), false);
            }
            GenDraw.FillableBarRequest req = default(GenDraw.FillableBarRequest);
            // 抬到罐顶上方, 避免被实心罐体挡住(chemTank 罐身低、条在底部能露出; 我们的横罐图罐体饱满)
            Vector2 barSize = new Vector2(1.2f, 0.22f);
            float elev = 0.1f;
            if (parent.def != null && parent.def.graphicData != null)
            {
                Vector2 ds = parent.def.graphicData.drawSize;
                elev = ds.y * 0.5f + 0.15f;
                // 液位条垂直于罐体长轴, 长度对齐罐体短边(drawSize.y)并限幅, 明显缩短避免竖条贯穿整罐
                barSize = new Vector2(Mathf.Clamp(ds.y * 0.78f, 1.0f, 1.6f), 0.22f);
            }
            req.center = parent.DrawPos + Vector3.up * elev;
            req.size = barSize;
            req.fillPercent = Mathf.Clamp01(Fill / Cap);
            req.filledMat = filledMat;
            req.unfilledMat = unfilledMat;
            req.margin = 0.15f;
            Rot4 rot = parent.Rotation;
            rot.Rotate(RotationDirection.Clockwise);
            req.rotation = rot;
            GenDraw.DrawFillableBar(req);
        }

        public override string CompInspectStringExtra()
        {
            if (Cap <= 0f)
            {
                return null;
            }
            return Props.label + ": " + Fill.ToString("F0") + " / " + Cap.ToString("F0");
        }
    }
}

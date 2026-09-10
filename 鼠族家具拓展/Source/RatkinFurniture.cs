// 鼠族家具拓展 - 动态件实现
//
// 整合自 Ratkin Lifestyle+(workshop 3263391066, Feng Xinzi)的动画逻辑,
// 全部重命名到本 mod 的 RatkinFurniture 命名空间, 只保留家具用到的 4 个类:
//   - Boat            木船漂浮动画(整体上下轻微浮动)
//   - Windmill        风车叶片旋转(叶片贴图 RL/RL_Wing)
//   - CompPowerWind   发电风车(风力发电, 原版 WindTurbine 简化版)
//   - CompFireOverlay 烛台火焰(有燃料时画火焰)
//
// 编译(系统 csc, C#5):
//   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:library
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\netstandard.dll"
//     /r:"<RimWorld>\Mods\Harmony\Current\Assemblies\0Harmony.dll"
//     /out:RatkinFurniture.dll RatkinFurniture.cs StyleIconFix.cs
using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RatkinFurniture
{
    public class Boat : Building
    {
        protected override void Tick()
        {
            base.Tick();
            angle += 0.5f;
            if (angle >= 360f)
            {
                angle = 0f;
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            drawLoc += PosOffset;
            base.DrawAt(drawLoc, flip);
        }

        private Vector3 PosOffset
        {
            get
            {
                return new Vector3(0f, 0f, 0.18f * Mathf.Sin(angle * Mathf.Deg2Rad));
            }
        }

        private float angle;
    }

    public class Windmill : Building
    {
        protected override void Tick()
        {
            base.Tick();
            float speed = Mathf.Min(this.Map.windManager.WindSpeed, 1.5f);
            speed = Mathf.Min(speed, 6f);
            ticks += 0.75f * speed;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            Vector3 drawPos = DrawPos;
            drawPos.y = AltitudeLayer.Blueprint.AltitudeFor();
            drawPos += this.def.graphicData.drawOffset + new Vector3(0f, 0f, 0.5f);
            Matrix4x4 matrix = default(Matrix4x4);
            Quaternion q = ticks.ToQuat();
            matrix.SetTRS(drawPos, q, new Vector3(this.def.graphicData.drawSize.x, 1f, this.def.graphicData.drawSize.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom("RL/RL_Wing", ShaderDatabase.Transparent), 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticks, "ticks");
        }

        public float ticks;
    }

    public class CompPowerWind : CompPowerPlant
    {
        public int updateWeatherEveryXTicks = 250;

        private int ticksSinceWeatherUpdate;

        private float cachedPowerOutput;

        private const float MaxUsableWindIntensity = 1.5f;

        protected override float DesiredPowerOutput
        {
            get
            {
                return cachedPowerOutput;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!base.PowerOn)
            {
                cachedPowerOutput = 0f;
                return;
            }
            ticksSinceWeatherUpdate++;
            if (ticksSinceWeatherUpdate >= updateWeatherEveryXTicks)
            {
                float num = Mathf.Min(parent.Map.windManager.WindSpeed, MaxUsableWindIntensity);
                ticksSinceWeatherUpdate = 0;
                cachedPowerOutput = 0f - base.Props.PowerConsumption * num;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksSinceWeatherUpdate, "updateCounter", 0);
            Scribe_Values.Look(ref cachedPowerOutput, "cachedPowerOutput", 0f);
        }
    }

    [StaticConstructorOnStartup]
    public class CompFireOverlay : CompFireOverlayBase
    {
        private CompRefuelable refuelableComp;

        public static readonly Graphic FireGraphic = GraphicDatabase.Get<Graphic_Flicker>("Things/Special/Fire", ShaderDatabase.TransparentPostLight, Vector2.one, Color.white);

        public new CompProperties_FireOverlay Props
        {
            get
            {
                return (CompProperties_FireOverlay)props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (refuelableComp == null || refuelableComp.HasFuel)
            {
                Vector3 drawPos = parent.DrawPos + Props.offset;
                drawPos.y += 1f / 26f;
                FireGraphic.Draw(drawPos, parent.Rotation, parent);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelableComp = parent.GetComp<CompRefuelable>();
        }

        public override void CompTick()
        {
            if ((refuelableComp == null || refuelableComp.HasFuel) && startedGrowingAtTick < 0)
            {
                startedGrowingAtTick = GenTicks.TicksAbs;
            }
        }
    }
}

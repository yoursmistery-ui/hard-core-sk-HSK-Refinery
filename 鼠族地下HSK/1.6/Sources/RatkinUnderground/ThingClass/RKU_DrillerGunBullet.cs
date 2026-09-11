using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RatkinUnderground
{
    public class RKU_DrillerGunBullet : Projectile_Explosive
    {
        int ticks = 0;
        int effectTick = 0;
        Effecter effecter;
        
        private float ArcHeightFactor
        {
            get
            {
                float num = def.projectile.arcHeightFactor;
                float num2 = (destination - origin).MagnitudeHorizontalSquared();
                if (num * num > num2 * 0.2f * 0.2f)
                {
                    num = Mathf.Sqrt(num2) * 0.2f;
                }
                return num;
            }
        }

        protected override void Tick()
        {
            base.Tick();
            ticks++;
            effectTick++;
            if (effectTick < 10) return;
            effectTick = 0;
            SpawnSustainedEffecter();

            if (ticks > 12000)
            {
                Destroy();
            }
        }

        void SpawnSustainedEffecter()
        {
            for (int i = 0; i < 3; i++)
            {
                EffecterDef sustainedDef = def.building.groundSpawnerSustainedEffecter;
                if (sustainedDef == null)
                {
                    return;
                }
                effecter = sustainedDef.SpawnMaintained(ThingMaker.MakeThing(this.def), Map, 0.5f);

                effecter.Trigger(
                    A: new TargetInfo(this),
                    B: new TargetInfo(this)
                );
                effecter.EffectTick(
                    A: new TargetInfo(this),
                    B: new TargetInfo(this)
                );
                effecter?.Cleanup();
            }
        }
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 position = Position;
            
            // 触发完成效果
            EffecterDef completeDef = def.building.groundSpawnerCompleteEffecter;
            if (completeDef != null)
            {
                effecter = completeDef.SpawnMaintained(ThingMaker.MakeThing(this.def), map);
                effecter.Trigger(
                    A: new TargetInfo(position, map),
                    B: new TargetInfo(position, map)
                );
                effecter.EffectTick(
                    A: new TargetInfo(position, map),
                    B: new TargetInfo(position, map)
                );
                effecter?.Cleanup();
                effecter = null;
            }
            
            ThingDef drillerGunDef = DefDatabase<ThingDef>.GetNamedSilentFail("RKU_DrillerGun");
            if (drillerGunDef != null && position.IsValid && map != null)
            {
                if (GenConstruct.CanPlaceBlueprintAt(drillerGunDef, position, Rot4.North, map, false, null, null).Accepted)
                {
                    Building drillerGun = (Building)ThingMaker.MakeThing(drillerGunDef,ThingDefOf.Steel);
                    if (drillerGun != null)
                    {
                        GenSpawn.Spawn(drillerGun, position, map, Rot4.North);
                        if (launcher != null && launcher.Faction != null)
                        {
                            drillerGun.SetFaction(launcher.Faction);
                        }
                    }
                }
            }

            Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float num = ArcHeightFactor * GenMath.InverseParabola(DistanceCoveredFractionArc);
            Vector3 vector = drawLoc + new Vector3(0f, 0f, 1f) * num;

            Graphic.Draw(vector, Rot4.North, this);
            Comps_PostDraw();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticks, "ticks", 0);
            Scribe_Values.Look(ref effectTick, "effectTick", 0);
        }
    }
}


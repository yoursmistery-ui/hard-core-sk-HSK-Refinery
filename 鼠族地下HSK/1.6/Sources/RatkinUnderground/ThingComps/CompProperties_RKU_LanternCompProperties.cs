using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Noise;

namespace RatkinUnderground
{
    public class RKU_LanternComponent : ThingComp,
        IThingGlower
    {
        bool isOpen = true;
        Thing light = null;

        string lightDef => Props?.lightDef;

        Map CurrentMap => pawn?.Map ?? parent?.Map;

        public CompProperties_RKU_LanternCompProperties Props => props as CompProperties_RKU_LanternCompProperties;

        public Pawn pawn
        {
            get
            {
                if (parent == null)
                    return null;
                if (parent is Apparel apparel)
                    return apparel.Wearer;
                if (parent is Pawn result)
                    return result;
                return null;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent == null)
                return;
            if (!parent.IsHashIntervalTick(45)) // 45帧判断一次
                return;
            if (pawn == null)
                return;
            if (CurrentMap == null || !pawn.Spawned)
            {
                DestroyLit();
                return;
            }
            if (!ShouldBeLitNow())
            {
                if (light != null && light.Spawned && !light.Destroyed)
                    light.Destroy();
                light = null;
                return;
            }
            UpdateLit();
        }
        
        /// <summary>
        /// 是否点亮
        /// </summary>
        /// <returns></returns>
        public bool ShouldBeLitNow()
        {
            if (!isOpen)
                return false;
            if (pawn == null || CurrentMap == null)
                return false;
            return true;
        }

        /// <summary>
        /// 刷新移动光源
        /// </summary>
        /// <param name="map">pawn所在地图</param>
        void UpdateLit()
        {
            if (pawn == null || pawn.Map == null)
                return;
            if (light == null || light.Destroyed || !light.Spawned)
            {
                SpawnLit();
            }
            if (light != null && !light.Destroyed && light.Spawned)
                MoveLit(pawn.Position);
        }

        /// <summary>
        /// 更新光源位置
        /// </summary>
        /// <param name="position">新位置</param>
        void MoveLit(IntVec3 position)
        {
            Map map = pawn?.Map;
            if (map == null || light == null || light.Destroyed || !light.Spawned)
                return;
            light.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(light, position, map);
        }

        /// <summary>
        /// 生成光源
        /// </summary>
        void SpawnLit()
        {
            if (string.IsNullOrEmpty(lightDef))
                return;
            ThingDef td = DefDatabase<ThingDef>.GetNamedSilentFail(lightDef);
            if (td == null)
                return;
            Map map = CurrentMap;
            if (map == null || parent == null)
                return;
            if (light != null)
            {
                if (light.Spawned && !light.Destroyed)
                    light.Destroy();
                light = null;
            }
            light = ThingMaker.MakeThing(td);
            IntVec3 cell = pawn != null ? pawn.Position : parent.Position;
            GenSpawn.Spawn(light, cell, map);
        }
        
        /// <summary>
        /// 销毁光源
        /// </summary>
        void DestroyLit()
        {
            if (light == null)
                return;
            if (light.Spawned && !light.Destroyed)
                light.Destroy();
            light = null;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action openStat = new Command_Action
            {
                defaultLabel = "RKU.LanternLightSwitch".Translate(),
                activateSound = SoundDefOf.Tick_Tiny,
                icon= Resources.lightSwitch,
                action = delegate
                {
                    isOpen = !isOpen;
                    if (isOpen == false)
                    {
                        DestroyLit();
                    }
                    else
                    {
                        SpawnLit();
                    }
                    //Log.Message($"当前开关状态:{isOpen}");
                }
            };
            yield return openStat;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref isOpen, "RKU_Lantern_isOpen", defaultValue: true);
            Scribe_References.Look(ref light, "RKU_Lantern_light");
        }
    }

    public class CompProperties_RKU_LanternCompProperties : CompProperties
    {
        public string lightDef;
        public CompProperties_RKU_LanternCompProperties()
        {
            this.compClass = typeof(RKU_LanternComponent);
        }

        public CompProperties_RKU_LanternCompProperties(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }
    }
}

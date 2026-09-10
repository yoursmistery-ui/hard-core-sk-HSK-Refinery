using RimWorld;
using Verse;
using UnityEngine;
using Defaults.Defs;

namespace Defaults.StockpileZones.Buildings
{
    public class ZoneType_Building : ZoneType
    {
        public ThingDef buildingDef;
        private ThingDef iconDef;

        public ZoneType_Building()
        {
        }

        public ZoneType_Building(ThingDef buildingDef)
        {
            this.buildingDef = buildingDef;
            SetToDefault();
            FindIconDef();
        }

        public ZoneType_Building(ThingDef buildingDef, ZoneType other)
        {
            this.buildingDef = buildingDef;
            priority = other.priority;
            filter.CopyAllowancesFrom(other.filter);
            locked = other.locked;
            FindIconDef();
        }

        private void SetToDefault()
        {
            priority = buildingDef.building.defaultStorageSettings.Priority;
            filter.CopyAllowancesFrom(buildingDef.building.defaultStorageSettings.filter);
        }

        private void FindIconDef()
        {
            iconDef = DefDatabase<ThingDef>.AllDefsListForReading.FirstOrDefault(d => d.building?.turretGunDef == buildingDef) ?? buildingDef;
        }

        public override string Name => "Defaults_StorageBuildingSettings".Translate(buildingDef.LabelCap);

        public override Texture2D Icon => iconDef.uiIcon;

        public override Color IconColor => GenStuff.DefaultStuffFor(iconDef)?.stuffProps.color ?? Color.white;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs_Silent.Look(ref buildingDef, "buildingDef");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                FindIconDef();
            }
        }
    }
}

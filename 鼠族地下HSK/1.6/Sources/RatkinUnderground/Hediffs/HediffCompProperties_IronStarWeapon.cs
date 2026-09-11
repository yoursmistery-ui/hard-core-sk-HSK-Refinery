using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RatkinUnderground
{
    public class HediffCompProperties_IronStarWeapon : HediffCompProperties
    {
        public HediffCompProperties_IronStarWeapon()
        {
            this.compClass = typeof(HediffComp_IronStarWeapon);
        }
    }

    public class HediffComp_IronStarWeapon : HediffComp
    {
        private int tickCounter = 0;
        private const int CHECK_INTERVAL = 3000;

        public HediffCompProperties_IronStarWeapon Props => (HediffCompProperties_IronStarWeapon)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            tickCounter++;
            if (tickCounter < CHECK_INTERVAL)
            {
                return;
            }
            tickCounter = 0;
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Destroyed || pawn.equipment == null)
            {
                return;
            }

            ThingWithComps primaryEquipment = pawn.equipment.Primary;
            ThingDef ironStarCannonDef = DefDatabase<ThingDef>.GetNamedSilentFail("RKU_IronStarCannon");
            
            if (ironStarCannonDef == null)
            {
                return;
            }

            // 如果当前没有武器或武器不是钢铁星机炮，则替换
            if (primaryEquipment == null || primaryEquipment.def.defName != "RKU_IronStarCannon")
            {
                if (primaryEquipment != null)
                {
                    pawn.equipment.Remove(primaryEquipment);
                }
                ThingWithComps newWeapon = (ThingWithComps)ThingMaker.MakeThing(ironStarCannonDef, null);
                if (newWeapon != null)
                {
                    CompQuality qualityComp = newWeapon.TryGetComp<CompQuality>();
                    if (qualityComp != null)
                    {
                        qualityComp.SetQuality(QualityCategory.Legendary, ArtGenerationContext.Outsider);
                    }
                    pawn.equipment.AddEquipment(newWeapon);
                }
            }
        }
    }
}


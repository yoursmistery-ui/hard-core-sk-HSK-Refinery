using RimWorld;
using Verse;

namespace Defaults.Misc.PlantType
{
    public static class PlantTypeUtility
    {
        public static float GetPlantListPriority(this ThingDef plantDef)
        {
            if (plantDef.plant.IsTree)
            {
                return 1f;
            }
            switch (plantDef.plant.purpose)
            {
                case PlantPurpose.Food:
                    return 4f;
                case PlantPurpose.Health:
                    return 3f;
                case PlantPurpose.Beauty:
                    return 2f;
                case PlantPurpose.Misc:
                    return 0f;
                default:
                    return 0f;
            }
        }
    }
}

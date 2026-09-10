using RimWorld;
using Verse;

namespace Defaults.Medicine
{
    public static class MedicineUtility
    {
        public static void SetMedicineToCarry(Pawn pawn, Pawn_InventoryStockTracker inventoryStock)
        {
            if (inventoryStock != null && pawn.IsColonist)
            {
                inventoryStock.SetThingForGroup(InventoryStockGroupDefOf.Medicine, Settings.Get<ThingDef>(Settings.MEDICINE_TO_CARRY));
                if (Settings.GetValue<bool>(Settings.GUESTS_CARRY_MEDICINE) || (!pawn.HasExtraMiniFaction() && !pawn.HasExtraHomeFaction()))
                {
                    inventoryStock.SetCountForGroup(InventoryStockGroupDefOf.Medicine, Settings.GetValue<int>(Settings.MEDICINE_AMOUNT_TO_CARRY));
                }
            }
        }
    }
}

using Verse;

namespace Defaults.StockpileZones.Shelves
{
    public static class ShelfUtility
    {
        public static bool IsShelf(this ThingDef def) => def == ThingDef.Named("Shelf") || def == ThingDef.Named("ShelfSmall");
    }
}

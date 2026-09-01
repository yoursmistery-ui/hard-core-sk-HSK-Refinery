using System.Reflection;
using HarmonyLib;
using Verse;

namespace TechGapTradePrice
{
    public class TechGapTradePriceMod : Mod
    {
        public const string PackageId = "local.techgaptradeprice";

        public TechGapTradePriceMod(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony(PackageId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}

using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.StockpileZones.Buildings
{
    public class DefaultSettingWorker_AllowRemovingFromOutfitStand : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_AllowRemovingFromOutfitStand(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.ALLOW_REMOVE_OUTFIT;

        protected override bool? Default => false;
    }
}

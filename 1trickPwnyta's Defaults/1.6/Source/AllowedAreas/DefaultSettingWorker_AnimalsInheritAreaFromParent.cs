using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.AllowedAreas
{
    public class DefaultSettingWorker_AnimalsInheritAreaFromParent : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_AnimalsInheritAreaFromParent(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.ANIMALS_INHERIT_AREA_FROM_PARENT;

        protected override bool? Default => true;
    }
}

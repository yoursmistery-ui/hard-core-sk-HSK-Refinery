using Defaults.Defs;
using Defaults.Workers;

namespace Defaults.AllowedAreas
{
    public class DefaultSettingWorker_ChildrenInheritAreaFromParent : DefaultSettingWorker_Checkbox
    {
        public DefaultSettingWorker_ChildrenInheritAreaFromParent(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.CHILDREN_INHERIT_AREA_FROM_PARENT;

        protected override bool? Default => true;
    }
}

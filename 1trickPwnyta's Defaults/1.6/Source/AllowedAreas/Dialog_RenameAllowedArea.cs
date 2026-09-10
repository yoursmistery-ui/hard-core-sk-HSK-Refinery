using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.AllowedAreas
{
    public class Dialog_RenameAllowedArea : Dialog_Rename<AllowedArea>
    {
        public Dialog_RenameAllowedArea(AllowedArea renaming) : base(renaming)
        {
        }

        protected override AcceptanceReport NameIsValid(string name)
        {
            return Settings.Get<List<AllowedArea>>(Settings.ALLOWED_AREAS).Concat(DefaultSettingsCategoryWorker_AllowedAreas.HomeArea).Any(a => a != renaming && a.name == name)
                ? (AcceptanceReport)"NameIsInUse".Translate()
                : base.NameIsValid(name);
        }
    }
}

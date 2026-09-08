using Defaults.Defs;
using Defaults.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.ResourceCategories
{
    public class Dialog_ResourceCategories : Dialog_SettingsCategory
    {
        private static readonly List<ThingCategoryDef> rootThingCategories = DefDatabase<ThingCategoryDef>.AllDefs.Where(c => c.resourceReadoutRoot && CountAsResource(c)).ToList();

        public Dialog_ResourceCategories(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        private float y;
        private Vector2 scrollPosition;

        private static bool CountAsResource(ThingCategoryDef def)
        {
            return def.childThingDefs.Any(d => d.CountAsResource) || def.childCategories.Any(c => CountAsResource(c));
        }

        public override Vector2 InitialSize => new Vector2(475f, 640f);

        public override void DoSettings(Rect rect)
        {
            Rect viewRect = new Rect(0f, 0f, rect.width - 20f, y);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            Listing_ResourceCategories listing_ResourceCategories = new Listing_ResourceCategories();
            listing_ResourceCategories.Begin(viewRect);
            listing_ResourceCategories.nestIndentWidth = 7f;
            listing_ResourceCategories.lineHeight = 24f;
            listing_ResourceCategories.verticalSpacing = 0f;
            foreach (ThingCategoryDef def in rootThingCategories)
            {
                listing_ResourceCategories.DoCategory(def.treeNode, 0);
            }
            y = listing_ResourceCategories.CurHeight;
            listing_ResourceCategories.End();
            Widgets.EndScrollView();
        }
    }
}

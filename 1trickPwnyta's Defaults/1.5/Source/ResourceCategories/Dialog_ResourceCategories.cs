using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.ResourceCategories
{
    public class Dialog_ResourceCategories : Window
    {
        private static List<ThingCategoryDef> rootThingCategories = DefDatabase<ThingCategoryDef>.AllDefs.Where(c => c.resourceReadoutRoot && CountAsResource(c)).ToList();

        private static bool CountAsResource(ThingCategoryDef def)
        {
            return def.childThingDefs.Any(d => d.CountAsResource) || def.childCategories.Any(c => CountAsResource(c));
        }

        public Dialog_ResourceCategories()
        {
            this.doCloseX = true;
            this.doCloseButton = true;
            this.optionalTitle = "Defaults_ResourceCategories".Translate();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(406f, 640f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_ResourceCategories listing_ResourceCategories = new Listing_ResourceCategories();
            listing_ResourceCategories.Begin(inRect);
            listing_ResourceCategories.nestIndentWidth = 7f;
            listing_ResourceCategories.lineHeight = 24f;
            listing_ResourceCategories.verticalSpacing = 0f;
            foreach (ThingCategoryDef def in rootThingCategories)
            {
                listing_ResourceCategories.DoCategory(def.treeNode, 0);
            }
            listing_ResourceCategories.End();
        }
    }
}

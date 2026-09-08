using Defaults.UI;
using Defaults.WorkbenchBills;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Compatibility
{
    public class Dialog_BetterWorkbenchAdditionalItems : Dialog_Common
    {
        private readonly ThingFilter baseFilter = AccessTools.TypeByName("ImprovedWorkbenches.Dialog_ThingFilter").Field("baseFilter").GetValue(null) as ThingFilter;
        private readonly List<SpecialThingFilterDef> specialThingDefs = AccessTools.TypeByName("ImprovedWorkbenches.Dialog_ThingFilter").Field("specialThingDefs").GetValue(null) as List<SpecialThingFilterDef>;
        private readonly ThingFilterUI.UIState state = new UIState_Ext();
        private readonly ThingFilter filter = new ThingFilter();
        private readonly BetterWorkbenchOptions options;

        public Dialog_BetterWorkbenchAdditionalItems(BetterWorkbenchOptions options)
        {
            doCloseX = false;
            this.options = options;
            foreach (ThingDef def in options.CountAdditionalItems)
            {
                filter.SetAllow(def, true);
            }
        }

        public override Vector2 InitialSize => new Vector2(300f, 500f);

        public override string CloseButtonText => "OK".Translate();

        public override void PostClose()
        {
            options.CountAdditionalItems = filter.AllowedThingDefs.ToHashSet();
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            ThingFilterUI.DoThingFilterConfigWindow(inRect.TopPartPixels(inRect.height - CloseButSize.y - Margin), state, filter,
                openMask: TreeOpenMasks.ThingFilter,
                forceHiddenFilters: specialThingDefs,
                parentFilter: baseFilter);
        }
    }
}

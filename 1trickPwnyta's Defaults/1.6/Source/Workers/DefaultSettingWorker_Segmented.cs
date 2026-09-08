using Defaults.Defs;
using Defaults.Medicine;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.Workers
{
    public abstract class DefaultSettingWorker_Segmented<T> : DefaultSettingWorker<T>
    {
        private static int? painting;

        protected DefaultSettingWorker_Segmented(DefaultSettingDef def) : base(def)
        {
        }

        protected abstract IEnumerable<T> Options { get; }

        protected abstract Texture2D GetIcon(T option);

        protected virtual TaggedString GetTip(T option) => null;

        protected override void DoWidget(Rect rect)
        {
            Rect iconRect = new Rect(rect.xMax - rect.height, rect.y, rect.height, rect.height);
            int hash = Options.Sum(x => x.GetHashCode());
            foreach (T option in Options.Reverse())
            {
                Widgets.DrawHighlightIfMouseover(iconRect);
                MouseoverSounds.DoRegion(iconRect);
                GUI.DrawTexture(iconRect.ContractedBy(1f), GetIcon(option));
                Widgets.DraggableResult draggableResult = Widgets.ButtonInvisibleDraggable(iconRect);
                if (draggableResult == Widgets.DraggableResult.Dragged)
                {
                    painting = hash;
                }
                if ((painting == hash && Mouse.IsOver(iconRect) && !setting.Equals(option)) || draggableResult.AnyPressed())
                {
                    setting = option;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                if (setting.Equals(option))
                {
                    Widgets.DrawBox(iconRect, 2);
                }
                TaggedString tip = GetTip(option);
                if (tip != null && Mouse.IsOver(iconRect))
                {
                    TooltipHandler.TipRegion(iconRect, tip);
                }
                iconRect.x -= iconRect.width;
            }
            if (!Input.GetMouseButton(0))
            {
                painting = null;
            }
        }
    }
}

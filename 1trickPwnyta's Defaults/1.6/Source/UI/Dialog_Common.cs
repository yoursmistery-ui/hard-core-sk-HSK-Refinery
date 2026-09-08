using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace Defaults.UI
{
    [StaticConstructorOnStartup]
    public abstract class Dialog_Common : Window
    {
        private static readonly Texture2D quickOptionsTex = ContentFinder<Texture2D>.Get("UI/Icons/Options/OptionsGeneral");

        protected Rect reorderableRect;
        private readonly QuickSearchWidget searchWidget = new QuickSearchWidget();
        private bool alreadyOpen;

        public Dialog_Common()
        {
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        protected int ReorderableGroup { get; private set; }

        protected virtual IList ReorderableItems => null;

        protected virtual bool DoSearchWidget => false;

        protected virtual IEnumerable<FloatMenuOption> QuickOptions
        {
            get
            {
                yield break;
            }
        }

        public override QuickSearchWidget CommonSearchWidget => DoSearchWidget ? searchWidget : null;

        protected override void LateWindowOnGUI(Rect inRect)
        {
            if (UIUtility.TopWindow != this)
            {
                Widgets.DrawRectFast(inRect.ExpandedBy(Margin), Color.black.WithAlpha(0.5f));
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            IList reorderableItems = ReorderableItems;
            if (reorderableItems != null && reorderableRect != null && Event.current.type == EventType.Repaint)
            {
                ReorderableGroup = ReorderableWidget.NewGroup((from, to) =>
                {
                    object o = reorderableItems[from];
                    reorderableItems.Insert(to, o);
                    reorderableItems.RemoveAt(from < to ? from : from + 1);
                }, ReorderableDirection.Vertical, reorderableRect);
            }

            if (DoSearchWidget && !alreadyOpen && !SteamDeck.IsSteamDeck)
            {
                searchWidget.Focus();
                alreadyOpen = true;
            }

            List<FloatMenuOption> quickOptions = QuickOptions.ToList();
            if (quickOptions.Any())
            {
                Rect quickOptionsRect = inRect.BottomPartPixels(CloseButSize.y - 3f).RightPartPixels(CloseButSize.y - 3f);
                Widgets.DrawButtonGraphic(quickOptionsRect);
                GUI.DrawTexture(quickOptionsRect.ContractedBy(6f), quickOptionsTex);
                if (Widgets.ButtonInvisible(quickOptionsRect))
                {
                    Find.WindowStack.Add(new FloatMenu(quickOptions));
                }
            }
        }
    }
}

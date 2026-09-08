using Defaults.Defs;
using Defaults.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Workers
{
    public abstract class DefaultSettingWorker_Dropdown<T> : DefaultSettingWorker<T>
    {
        protected DefaultSettingWorker_Dropdown(DefaultSettingDef def) : base(def)
        {
        }

        protected abstract IEnumerable<T> Options { get; }

        protected virtual bool ShowIconAndTextInWidget => false;

        protected virtual Texture2D GetIcon(T option) => null;

        protected virtual TaggedString GetText(T option) => string.Empty;

        protected virtual TaggedString GetTip(T option) => null;

        protected virtual TaggedString GetMenuTip(T option) => null;

        protected virtual float Width => Options.Max(o => Text.CalcSize(GetText(o)).x) + 20f;

        protected override void DoWidget(Rect rect)
        {
            Texture2D icon = GetIcon(setting);
            TaggedString text = GetText(setting);
            if (ShowIconAndTextInWidget && (icon == null || text.NullOrEmpty()))
            {
                throw new Exception("If ShowIconAndTextInWidget is true for option " + setting + ", both GetIcon and GetText must be defined.");
            }
            if (icon == null && text.NullOrEmpty())
            {
                throw new Exception("At least one of GetIcon or GetText must be defined for option " + setting + ".");
            }

            float width = 0f;
            if (icon != null)
            {
                width += rect.height;
            }
            if (icon == null || ShowIconAndTextInWidget)
            {
                width += Width;
            }
            rect.x += rect.width - width;
            rect.width = width;

            if (ShowIconAndTextInWidget)
            {
                if (UIUtility.DoImageTextButton(rect, icon, text))
                {
                    DoMenu();
                }
            }
            else if (icon != null)
            {
                if (Widgets.ButtonImage(rect.ContractedBy(3f), icon))
                {
                    DoMenu();
                }
            }
            else
            {
                if (Widgets.ButtonText(rect, text))
                {
                    DoMenu();
                }
            }

            TaggedString tooltip = GetTip(setting);
            if (tooltip != null)
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private void DoMenu()
        {
            Find.WindowStack.Add(new FloatMenu(Options.Select(o => new FloatMenuOption(GetText(o), () =>
            {
                setting = o;
            }, GetIcon(o), Color.white)
            {
                tooltip = GetMenuTip(o)
            }).ToList()));
        }
    }
}

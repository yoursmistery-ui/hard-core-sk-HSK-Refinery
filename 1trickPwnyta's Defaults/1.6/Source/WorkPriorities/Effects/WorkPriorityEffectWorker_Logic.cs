using Defaults.Defs;
using Defaults.Workers;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.WorkPriorities.Effects
{
    [StaticConstructorOnStartup]
    public class WorkPriorityEffectWorker_Logic : WorkPriorityEffectWorker<Effect_Logic>
    {
        private static readonly Texture2D editIcon = ContentFinder<Texture2D>.Get("UI/Defaults_Edit");

        public WorkPriorityEffectWorker_Logic(WorkPriorityEffectDef def) : base(def)
        {
        }

        protected override void DoUI(Rect rect, Effect_Logic effect)
        {
            Rect buttonRect = rect.LeftPartPixels(rect.height);
            Widgets.DrawButtonGraphic(buttonRect);
            using (new TextBlock(GameFont.Tiny, TextAnchor.UpperRight)) Widgets.Label(buttonRect.LeftPartPixels(buttonRect.width - 3f), effect.rules.Count.ToString());
            GUI.DrawTexture(buttonRect, editIcon);
            if (Widgets.ButtonInvisible(buttonRect))
            {
                Window window = new Dialog_Rules("Defaults_EditSubrules".Translate(), effect.rules);
                Find.WindowStack.Add(window);
                window.windowRect.position += Vector2.one * ((Find.WindowStack.Windows.OfType<Dialog_Rules>().Count() + 4) % 10 - 5) * 45f;
            }
        }
    }
}

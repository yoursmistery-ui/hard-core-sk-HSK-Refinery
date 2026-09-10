using RimWorld;
using UnityEngine;
using Verse;

namespace Defaults.UI
{
    public abstract class Dialog_Input : Dialog_Common
    {
        private readonly string title;
        private readonly TaggedString prompt;
        private readonly bool forceInput;
        private readonly bool destructive;
        private Vector2 scrollPosition;
        private float height;

        public Dialog_Input(string title = null, TaggedString prompt = default, bool forceInput = false, bool destructive = false)
        {
            this.title = title;
            this.prompt = prompt;
            this.forceInput = forceInput;
            this.destructive = destructive;
            closeOnAccept = false;
            closeOnClickedOutside = !forceInput;
            forcePause = forceInput;
            doCloseX = !forceInput;
            doCloseButton = false;
        }

        protected abstract float DoInput(Rect rect);

        protected abstract bool ProcessInput();

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            float y = 0f;

            if (!title.NullOrEmpty())
            {
                using (new TextBlock(GameFont.Medium))
                {
                    Rect titleRect = new Rect(inRect.x, inRect.y + y, inRect.width, Text.LineHeight + 10f);
                    Widgets.Label(titleRect, title);
                    y += titleRect.height;
                }
            }

            if (!prompt.NullOrEmpty())
            {
                Rect promptRect = new Rect(inRect.x, inRect.y + y, inRect.width, Text.CalcHeight(prompt, inRect.width) + 10f);
                Widgets.Label(promptRect, prompt);
                y += promptRect.height;
            }

            Rect outRect = new Rect(inRect.x, inRect.y + y, inRect.width, inRect.height - y - 10f - 35f - 10f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 20f, height);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            height = DoInput(viewRect);
            Widgets.EndScrollView();

            Rect acceptButtonRect = forceInput
                ? new Rect(inRect.x, inRect.yMax - 35f - 10f, inRect.width, 35f)
                : new Rect(inRect.x + inRect.width / 2 + 5f, inRect.yMax - 35f - 10f, inRect.width / 2 - 5f, 35f);
            if (destructive)
            {
                GUI.color = new Color(1f, 0.3f, 0.35f);
            }
            if (Widgets.ButtonText(acceptButtonRect, (destructive ? "Confirm" : "OK").Translate()))
            {
                if (ProcessInput())
                {
                    Close();
                }
            }
            GUI.color = Color.white;

            if (!forceInput)
            {
                Rect cancelButtonRect = new Rect(inRect.x, inRect.yMax - 35f - 10f, inRect.width / 2 - 5f, 35f);
                if (Widgets.ButtonText(cancelButtonRect, "Cancel".Translate()))
                {
                    Close();
                }
            }
        }
    }
}

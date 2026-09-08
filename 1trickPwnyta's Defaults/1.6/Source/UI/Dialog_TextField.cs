using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace Defaults.UI
{
    public class Dialog_TextField<T> : Dialog_Input
    {
        private string input;
        private readonly Action<T> confirmAction;
        private readonly Func<string, T> parser;
        private readonly Func<T, AcceptanceReport> validator;

        public Dialog_TextField(Action<T> confirmAction, Func<string, T> parser, string title = null, string prompt = null, T defaultValue = default, Func<T, AcceptanceReport> validator = null) : base(title, prompt)
        {
            this.confirmAction = confirmAction;
            this.parser = parser;
            this.validator = validator;
            input = defaultValue.ToString();
        }

        public override Vector2 InitialSize => new Vector2(480f, 225f);

        protected override bool ProcessInput()
        {
            T result = parser(input);
            AcceptanceReport acceptanceReport = validator != null ? validator(result) : AcceptanceReport.WasAccepted;
            if (!acceptanceReport.Accepted)
            {
                if (acceptanceReport.Reason.NullOrEmpty())
                {
                    Messages.Message("Defaults_InputIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            else
            {
                confirmAction(result);
                return true;
            }
        }

        protected override float DoInput(Rect rect)
        {
            GUI.SetNextControlName("InputField");
            Rect textRect = new Rect(rect.x, rect.y, rect.width, 35f);
            input = Widgets.TextField(textRect, input);
            Verse.UI.FocusControl("InputField", this);
            return textRect.height;
        }
    }

    public class Dialog_TextField_String : Dialog_TextField<string>
    {
        public Dialog_TextField_String(Action<string> confirmAction, string title = null, string prompt = null, string defaultValue = null, Func<string, AcceptanceReport> validator = null) : base(confirmAction, s => s, title, prompt, defaultValue, validator)
        {
        }
    }
}

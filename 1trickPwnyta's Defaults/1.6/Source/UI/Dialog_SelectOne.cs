using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.UI
{
    public class Dialog_SelectOne<T> : Dialog_Select
    {
        private readonly HashSet<T> staticChoices;
        private readonly Func<IEnumerable<T>> dynamicChoices;
        private readonly Func<T, T, bool> equals;
        private readonly Action<T> acceptAction;
        private readonly Func<T, TaggedString> toString;
        private readonly Func<Rect, T, float> doSideOptions;
        private T selectedChoice;

        private Dialog_SelectOne(string title, TaggedString text, Action<T> acceptAction, bool forceInput = false, bool destructive = false, Func<T, TaggedString> toString = null, Func<Rect, T, float> doSideOptions = null) : base(text, title, forceInput, destructive)
        {
            this.acceptAction = acceptAction;
            if (toString != null)
            {
                this.toString = toString;
            }
            else
            {
                this.toString = t => t.ToString();
            }
            this.doSideOptions = doSideOptions;
        }

        public Dialog_SelectOne(string title, TaggedString text, IEnumerable<T> choices, Action<T> acceptAction, bool forceInput = false, bool destructive = false, Func<T, TaggedString> toString = null, Func<Rect, T, float> doSideOptions = null) : this(title, text, acceptAction, forceInput, destructive, toString, doSideOptions)
        {
            staticChoices = choices.ToHashSet();
            selectedChoice = choices.First();
        }

        public Dialog_SelectOne(string title, TaggedString text, Func<IEnumerable<T>> dynamicChoices, Action<T> acceptAction, Func<T, T, bool> equals = null, bool forceInput = false, bool destructive = false, Func<T, TaggedString> toString = null, Func<Rect, T, float> doSideOptions = null) : this(title, text, acceptAction, forceInput, destructive, toString, doSideOptions)
        {
            this.dynamicChoices = dynamicChoices;
            this.equals = equals;
            selectedChoice = dynamicChoices().FirstOrDefault();
        }

        protected override float DoInput(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard() { maxOneColumn = true };
            listing.Begin(rect);
            IEnumerable<T> choices = dynamicChoices?.Invoke() ?? staticChoices;
            if (!choices.Any(c => equals?.Invoke(c, selectedChoice) ?? c.Equals(selectedChoice)))
            {
                selectedChoice = choices.FirstOrDefault();
            }
            foreach (T choice in choices)
            {
                Rect choiceRect = listing.GetRect(30f);
                if (doSideOptions != null)
                {
                    choiceRect.xMax -= doSideOptions(choiceRect, choice);
                }
                GUI.DrawTexture(choiceRect.LeftPartPixels(30f).ContractedBy(3f), (equals?.Invoke(selectedChoice, choice) ?? selectedChoice.Equals(choice)) ? Widgets.RadioButOnTex : typeof(Widgets).Field("RadioButOffTex").GetValue(null) as Texture2D);
                using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(choiceRect.RightPartPixels(choiceRect.width - 35f), toString(choice));
                if (Widgets.ButtonInvisible(choiceRect))
                {
                    selectedChoice = choice;
                    SoundDefOf.Click.PlayOneShot(null);
                }
            }
            listing.End();
            return listing.CurHeight;
        }

        protected override bool ProcessInput()
        {
            if (dynamicChoices?.Invoke().Any() ?? true)
            {
                acceptAction(selectedChoice);
                return true;
            }
            else
            {
                SoundDefOf.ClickReject.PlayOneShot(null);
                return false;
            }
        }
    }
}

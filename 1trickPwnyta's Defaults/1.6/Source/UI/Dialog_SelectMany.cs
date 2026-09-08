using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.UI
{
    public class Dialog_SelectMany : Dialog_Select
    {
        private readonly Dictionary<TaggedString, bool> choices;
        private readonly List<Tuple<string, Dictionary<TaggedString, bool>>> categorizedChoices;
        private readonly Action<IEnumerable<TaggedString>> acceptAction;
        private readonly Action<IEnumerable<Tuple<string, TaggedString>>> categorizedAcceptAction;

        public Dialog_SelectMany(string title, TaggedString text, IEnumerable<TaggedString> choices, bool defaultEnable, Action<IEnumerable<TaggedString>> acceptAction, bool forceInput = false, bool destructive = false) : base(text, title, forceInput, destructive)
        {
            this.choices = new Dictionary<TaggedString, bool>();
            foreach (TaggedString choice in choices)
            {
                this.choices[choice] = defaultEnable;
            }
            this.acceptAction = acceptAction;
        }

        public Dialog_SelectMany(string title, TaggedString text, IEnumerable<Tuple<string, IEnumerable<TaggedString>>> categorizedChoices, bool defaultEnable, Action<IEnumerable<Tuple<string, TaggedString>>> acceptAction, bool forceInput = false, bool destructive = false) : base(text, title, forceInput, destructive)
        {
            this.categorizedChoices = new List<Tuple<string, Dictionary<TaggedString, bool>>>();
            foreach (Tuple<string, IEnumerable<TaggedString>> choice in categorizedChoices)
            {
                Dictionary<TaggedString, bool> category = new Dictionary<TaggedString, bool>();
                foreach (TaggedString label in choice.Item2)
                {
                    category[label] = defaultEnable;
                }
                this.categorizedChoices.Add(new Tuple<string, Dictionary<TaggedString, bool>>(choice.Item1, category));
            }
            categorizedAcceptAction = acceptAction;
        }

        protected override float DoInput(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard() { maxOneColumn = true };
            listing.Begin(rect);
            if (choices != null)
            {
                foreach (TaggedString choice in choices.Keys.ToList())
                {
                    bool enabled = choices[choice];
                    listing.CheckboxLabeled(choice, ref enabled);
                    choices[choice] = enabled;
                }
            }
            if (categorizedChoices != null)
            {
                foreach (Tuple<string, Dictionary<TaggedString, bool>> category in categorizedChoices.Where(c => c.Item2.Any()))
                {
                    listing.Label(category.Item1.Translate().CapitalizeFirst());
                    listing.GapLine();
                    foreach (TaggedString choice in category.Item2.Keys.ToList())
                    {
                        bool enabled = category.Item2[choice];
                        listing.CheckboxLabeled(choice, ref enabled);
                        category.Item2[choice] = enabled;
                    }
                    listing.Gap();
                }
            }
            listing.End();
            return listing.CurHeight;
        }

        protected override bool ProcessInput()
        {
            acceptAction?.Invoke(choices.Keys.Where(c => choices[c]));
            categorizedAcceptAction?.Invoke(categorizedChoices.SelectMany(c => c.Item2.Keys.Where(k => c.Item2[k]).Select(k => new Tuple<string, TaggedString>(c.Item1, k))));
            return true;
        }
    }
}

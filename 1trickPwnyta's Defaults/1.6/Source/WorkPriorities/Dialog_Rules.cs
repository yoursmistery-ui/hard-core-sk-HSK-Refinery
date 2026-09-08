using Defaults.Defs;
using Defaults.UI;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.WorkPriorities
{
    public class Dialog_Rules : Dialog_Common
    {
        private static readonly float uiPadding = 5f;

        private readonly string title;
        private readonly List<Rule> rules;
        private Vector2 scrollPosition;
        private float height;
        private bool highlightInvalid = false;

        public Dialog_Rules(string title, List<Rule> rules)
        {
            this.title = title;
            this.rules = rules;
            onlyOneOfTypeAllowed = false;
        }

        public override Vector2 InitialSize => new Vector2(1050f, 500f);

        protected override IList ReorderableItems => rules;

        public override bool OnCloseRequest()
        {
            if (rules.Any(r => !r.IsValid))
            {
                Messages.Message("Defaults_WorkPriorityRulesInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                highlightInvalid = true;
                return false;
            }

            return true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            float y = 0f;

            using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect, title.CapitalizeFirst());
            y += 45f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), "Defaults_WorkPriorityRulesDesc".Translate());
            y += 30f;

            Rect headerRect = new Rect(inRect.x, y, inRect.width, 30f);
            Widgets.DrawRectFast(headerRect, Widgets.MenuSectionBGFillColor);
            if (Widgets.ButtonText(headerRect.RightPartPixels(100f).ContractedBy(3f), "Defaults_WorkPriorityAddRule".Translate()))
            {
                rules.Add(new Rule());
                SoundDefOf.Click.PlayOneShot(null);
            }
            if (rules.Any())
            {
                Rect copyRect = headerRect.RightPartPixels(headerRect.height);
                copyRect.x -= 100f + headerRect.height;
                copyRect = copyRect.ContractedBy(3f);
                if (Widgets.ButtonImage(copyRect, TexButton.Copy, tooltip: "CopyAll".Translate()))
                {
                    WorkPriorityUtility.ruleClipboard = rules.ListFullCopy();
                    SoundDefOf.Tick_High.PlayOneShot(null);
                }
            }
            if (!WorkPriorityUtility.ruleClipboard.NullOrEmpty())
            {
                Rect pasteRect = headerRect.RightPartPixels(headerRect.height);
                pasteRect.x -= 100f;
                pasteRect = pasteRect.ContractedBy(3f);
                if (Widgets.ButtonImage(pasteRect, TexButton.Paste, tooltip: "Paste".Translate()))
                {
                    rules.AddRange(WorkPriorityUtility.ruleClipboard.Select(r => r.MakeCopy()));
                    SoundDefOf.Tick_Low.PlayOneShot(null);
                }
            }
            headerRect.width -= 20f;
            headerRect.xMin += 45f;
            headerRect.xMax -= 90f;
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(headerRect.LeftHalf().ContractedBy(uiPadding), "Defaults_WorkPriorityCondition".Translate());
                Widgets.Label(headerRect.RightHalf().ContractedBy(uiPadding), "Defaults_WorkPriorityEffect".Translate());
            }
            y += 30f;

            Rect outRect = new Rect(inRect.x, y, inRect.width, inRect.height - y);
            reorderableRect = outRect;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, height);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            height = 0f;
            Rule toDelete = null;
            foreach (Rule rule in rules)
            {
                Rect controlRect = new Rect(viewRect.x, height, viewRect.width, 45f);

                Rect dragRect = controlRect.LeftPartPixels(controlRect.height).ContractedBy(uiPadding);

                Rect ruleRect = controlRect.MiddlePartPixels(controlRect.width - controlRect.height * 2, controlRect.height);
                ruleRect.width -= controlRect.height;

                Rect conditionRect = ruleRect.LeftHalf();
                if (UIUtility.DoImageTextButton(conditionRect.LeftHalf().ContractedBy(uiPadding), rule.condition?.def.Icon, rule.condition?.def.LabelCap ?? "None".Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(DefDatabase<WorkPriorityConditionDef>.AllDefsListForReading.Select(c => new FloatMenuOption(c.LabelCap, () =>
                    {
                        rule.condition = c.Worker.MakeCondition();
                    }, c.Icon, Color.white)).ToList()));
                }
                rule.condition?.def.Worker.DoUI(conditionRect.RightHalf().ContractedBy(uiPadding), rule.condition);

                Rect effectRect = ruleRect.RightHalf();
                if (UIUtility.DoImageTextButton(effectRect.LeftHalf().ContractedBy(uiPadding), rule.effect?.def.Icon, rule.effect?.def.LabelCap ?? "None".Translate()))
                {
                    Find.WindowStack.Add(new FloatMenu(DefDatabase<WorkPriorityEffectDef>.AllDefsListForReading.Select(e => new FloatMenuOption(e.LabelCap, () =>
                    {
                        rule.effect = e.Worker.MakeEffect();
                    }, e.Icon, Color.white)).ToList()));
                }
                rule.effect?.def.Worker.DoUI(effectRect.RightHalf().ContractedBy(uiPadding), rule.effect);

                Rect copyRect = controlRect.RightPartPixels(controlRect.height);
                copyRect.x -= controlRect.height;
                copyRect = copyRect.ContractedBy(uiPadding);
                if (Widgets.ButtonImage(copyRect, TexButton.Copy, tooltip: "Copy".Translate()))
                {
                    WorkPriorityUtility.ruleClipboard = new List<Rule>() { rule };
                    SoundDefOf.Tick_High.PlayOneShot(null);
                }

                Rect deleteRect = controlRect.RightPartPixels(controlRect.height).ContractedBy(uiPadding);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete, tooltip: "Delete".Translate()))
                {
                    toDelete = rule;
                    SoundDefOf.Click.PlayOneShot(null);
                }

                if (highlightInvalid && !rule.IsValid)
                {
                    using (new TextBlock(Color.yellow)) Widgets.DrawBox(controlRect.ContractedBy(2f), 2);
                }

                UIUtility.DoDraggable(ReorderableGroup, controlRect, dragRect, dragRect);

                height += 45f;
            }
            if (toDelete != null)
            {
                rules.Remove(toDelete);
            }
            Widgets.EndScrollView();
        }
    }
}

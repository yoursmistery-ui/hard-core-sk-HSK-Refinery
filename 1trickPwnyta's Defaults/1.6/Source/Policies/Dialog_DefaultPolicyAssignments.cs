using Defaults.UI;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.Policies
{
    public class Dialog_DefaultPolicyAssignments : Dialog_Common
    {
        private static readonly string defaultLabel = $"({"Default".Translate().ToLower()})";

        private bool dragDefault;
        private Policy dragValue;

        public override Vector2 InitialSize => new Vector2(1000f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            float y = inRect.y;

            using (new TextBlock(GameFont.Medium)) Widgets.Label(inRect, "Defaults_DefaultPolicyAssignments".Translate());
            y += 45f;

            string desc = "Defaults_DefaultPolicyAssignmentsDesc".Translate();
            Widgets.Label(new Rect(inRect.x, y, inRect.width, inRect.height), desc);
            y += Text.CalcHeight(desc, inRect.width) + 10f;

            Rect headerRect = new Rect(inRect.x, y, inRect.width / 5, 30f);
            using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(headerRect, "Defaults_PawnType".Translate());
            headerRect.x += headerRect.width;
            using (new TextBlock(TextAnchor.MiddleCenter))
            {
                Widgets.Label(headerRect, "Defaults_ApparelPolicies".Translate().CapitalizeFirst());
                headerRect.x += headerRect.width;
                Widgets.Label(headerRect, "Defaults_FoodPolicies".Translate().CapitalizeFirst());
                headerRect.x += headerRect.width;
                Widgets.Label(headerRect, "Defaults_DrugPolicies".Translate().CapitalizeFirst());
                headerRect.x += headerRect.width;
                Widgets.Label(headerRect, "Defaults_ReadingPolicies".Translate().CapitalizeFirst());
                y += headerRect.height;
            }

            DefaultPolicyAssignments assignments = Settings.Get<DefaultPolicyAssignments>(Settings.POLICY_ASSIGNMENTS);
            foreach (PawnType pawnType in assignments.PolicyAssignments.Keys.Where(p => p.IsActive()))
            {
                float x = inRect.x;
                Rect rect = new Rect(x, y, inRect.width, 30f);
                Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width, Widgets.InactiveColor);

                Rect pawnTypeRect = new Rect(x, rect.y, rect.width / 5, rect.height);
                using (new TextBlock(TextAnchor.MiddleLeft)) Widgets.Label(pawnTypeRect, pawnType.GetLabel().CapitalizeFirst());
                x += pawnTypeRect.width;

                x += DoPolicyButton(new Rect(x, rect.y, rect.width / 5, rect.height), ref assignments.PolicyAssignments[pawnType].apparelPolicy, Settings.Get<List<ApparelPolicy>>(Settings.POLICIES_APPAREL), p => assignments.PolicyAssignments[pawnType].apparelPolicy = p, pawnType);
                x += DoPolicyButton(new Rect(x, rect.y, rect.width / 5, rect.height), ref assignments.PolicyAssignments[pawnType].foodPolicy, Settings.Get<List<FoodPolicy>>(Settings.POLICIES_FOOD), p => assignments.PolicyAssignments[pawnType].foodPolicy = p, pawnType);
                x += DoPolicyButton(new Rect(x, rect.y, rect.width / 5, rect.height), ref assignments.PolicyAssignments[pawnType].drugPolicy, Settings.Get<List<DrugPolicy>>(Settings.POLICIES_DRUG), p => assignments.PolicyAssignments[pawnType].drugPolicy = p, pawnType);
                x += DoPolicyButton(new Rect(x, rect.y, rect.width / 5, rect.height), ref assignments.PolicyAssignments[pawnType].readingPolicy, Settings.Get<List<ReadingPolicy>>(Settings.POLICIES_READING), p => assignments.PolicyAssignments[pawnType].readingPolicy = p, pawnType);

                Widgets.DrawHighlightIfMouseover(rect);

                y += rect.height;
            }

            if (!Input.GetMouseButton(0))
            {
                dragValue = null;
                dragDefault = false;
            }
        }

        private float DoPolicyButton<T>(Rect rect, ref T policy, List<T> options, Action<T> setPolicy, PawnType pawnType) where T : Policy
        {
            if (pawnType == PawnType.Guest && typeof(T) == typeof(ApparelPolicy))
            {
                using (new TextBlock(TextAnchor.MiddleCenter)) Widgets.Label(rect, "Unchangeable".Translate());
                return rect.width;
            }
            Rect buttonRect = rect.ContractedBy(1f);
            Widgets.DraggableResult result = Widgets.ButtonTextDraggable(buttonRect, policy?.RenamableLabel ?? defaultLabel);
            if (result == Widgets.DraggableResult.Pressed)
            {
                Find.WindowStack.Add(new FloatMenu(options.Prepend(null).Select(p => new FloatMenuOption(p?.RenamableLabel ?? defaultLabel, () => setPolicy(p))).ToList()));
            }
            if (result == Widgets.DraggableResult.Dragged)
            {
                dragValue = policy;
                dragDefault = dragValue == null;
            }
            if (Mouse.IsOver(buttonRect) && (dragDefault || dragValue is T) && policy != dragValue)
            {
                policy = dragDefault ? null : dragValue as T;
                SoundDefOf.Click.PlayOneShot(null);
            }
            return rect.width;
        }
    }
}

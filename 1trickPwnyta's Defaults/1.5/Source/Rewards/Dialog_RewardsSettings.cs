using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Rewards
{
    public class Dialog_RewardsSettings : Window
    {
        private Vector2 scrollPosition;
        private float viewRectHeight;

        public Dialog_RewardsSettings()
        {
            this.doCloseX = true;
            this.doCloseButton = true;
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(700f, 440f);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, InitialSize.x / 2f, 40f), "Defaults_Rewards".Translate());
            Text.Font = GameFont.Small;
            string text = "ChooseRewardsDesc".Translate();
            float num = Text.CalcHeight(text, inRect.width);
            Rect rect = new Rect(0f, 40f, inRect.width, num);
            Widgets.Label(rect, text);
            IEnumerable<FactionDef> allFactionDefs = DefDatabase<FactionDef>.AllDefs.OrderBy(def => def.configurationListOrderPriority);
            Rect outRect = new Rect(inRect);
            outRect.yMax -= Window.CloseButSize.y;
            outRect.yMin += 44f + rect.height + 4f;
            float num2 = 0f;
            Rect rect2 = new Rect(0f, num2, outRect.width - 16f, viewRectHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, rect2, true);
            int num3 = 0;
            foreach (FactionDef def in allFactionDefs)
            {
                if (!DefaultsSettings.DefaultRewardPreferences.ContainsKey(def.defName) || DefaultsSettings.DefaultRewardPreferences[def.defName] == null)
                {
                    DefaultsSettings.DefaultRewardPreferences[def.defName] = new RewardPreference();
                }
                if (!def.isPlayer)
                {
                    float num4 = 0f;
                    if (def.HasRoyalTitles)
                    {
                        DoFactionInfo(rect2, def, ref num4, ref num2, ref num3);
                        TaggedString label = "Defaults_AcceptRoyalFavor".Translate(def.royalFavorLabel).CapitalizeFirst();
                        Rect rect3 = new Rect(num4, num2, label.GetWidthCached(), 45f);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        Widgets.Label(rect3, label);
                        Text.Anchor = TextAnchor.UpperLeft;
                        if (Mouse.IsOver(rect3))
                        {
                            TooltipHandler.TipRegion(rect3, "Defaults_AcceptRoyalFavorDesc".Translate(def.royalFavorLabel, def.LabelCap));
                            Widgets.DrawHighlight(rect3);
                        }
                        Widgets.Checkbox(rect2.width - 150f, num2 + 12f, ref DefaultsSettings.DefaultRewardPreferences[def.defName].allowRoyalFavorRewards, 24f, false, false, null, null);
                        num2 += 45f;
                    }
                    if (!def.permanentEnemy && !def.hidden)
                    {
                        num4 = 0f;
                        DoFactionInfo(rect2, def, ref num4, ref num2, ref num3);
                        TaggedString label2 = "AcceptGoodwill".Translate().CapitalizeFirst();
                        Rect rect4 = new Rect(num4, num2, label2.GetWidthCached(), 45f);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        Widgets.Label(rect4, label2);
                        Text.Anchor = TextAnchor.UpperLeft;
                        if (Mouse.IsOver(rect4))
                        {
                            TooltipHandler.TipRegion(rect4, "Defaults_AcceptGoodwillDesc".Translate(def.LabelCap));
                            Widgets.DrawHighlight(rect4);
                        }
                        Widgets.Checkbox(rect2.width - 150f, num2 + 12f, ref DefaultsSettings.DefaultRewardPreferences[def.defName].allowGoodwillRewards, 24f, false, false, null, null);
                        FactionRelationKind relationKind = def.naturalEnemy ? FactionRelationKind.Hostile : FactionRelationKind.Neutral;
                        Widgets.Label(new Rect(rect2.width - 100f, num2 + 12f, 100f, 35f), relationKind.GetLabelCap().Colorize(relationKind.GetColor()));
                        num2 += 45f;
                    }
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                viewRectHeight = num2;
            }
            Widgets.EndScrollView();
        }

        private void DoFactionInfo(Rect rect, FactionDef def, ref float curX, ref float curY, ref int index)
        {
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(new Rect(curX, curY, rect.width, 45f));
            }
            DrawFactionIconWithTooltip(new Rect(curX, curY + 5f, 35f, 35f), def);
            curX += 45f;
            Rect rect2 = new Rect(curX, curY, 250f, 45f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect2, def.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            curX += 250f;
            index++;
        }

        private void DrawFactionIconWithTooltip(Rect r, FactionDef def)
        {
            GUI.color = def.colorSpectrum.First();
            GUI.DrawTexture(r, def.FactionIcon);
            GUI.color = Color.white;
        }
    }
}

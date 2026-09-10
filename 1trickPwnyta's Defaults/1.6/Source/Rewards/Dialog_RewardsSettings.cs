using Defaults.Defs;
using Defaults.UI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Rewards
{
    public class Dialog_RewardsSettings : Dialog_SettingsCategory
    {
        private Vector2 scrollPosition;
        private float viewRectHeight;

        public Dialog_RewardsSettings(DefaultSettingsCategoryDef category) : base(category)
        {
        }

        public override Vector2 InitialSize => new Vector2(700f, 780f);

        public override void DoSettings(Rect rect)
        {
            string text = "Defaults_ChooseRewardsDesc".Translate();
            float num = Text.CalcHeight(text, rect.width);
            Rect descRect = new Rect(rect.x, rect.y, rect.width, num);
            Widgets.Label(descRect, text);
            IEnumerable<FactionDef> allFactionDefs = DefDatabase<FactionDef>.AllDefs.OrderBy(def => def.configurationListOrderPriority);
            Rect outRect = new Rect(rect);
            outRect.yMin += 10f + descRect.height + 4f;
            float num2 = 0f;
            Rect rect2 = new Rect(0f, num2, outRect.width - 16f, viewRectHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, rect2, true);
            int num3 = 0;
            Dictionary<FactionDef, RewardPreference> rewards = Settings.Get<Dictionary<FactionDef, RewardPreference>>(Settings.REWARDS);
            foreach (FactionDef def in allFactionDefs)
            {
                if (!rewards.ContainsKey(def) || rewards[def] == null)
                {
                    rewards[def] = new RewardPreference();
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
                        Widgets.Checkbox(rect2.width - 150f, num2 + 12f, ref rewards[def].allowRoyalFavorRewards, paintable: true);
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
                        Widgets.Checkbox(rect2.width - 150f, num2 + 12f, ref rewards[def].allowGoodwillRewards, paintable: true);
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
            GUI.color = def.colorSpectrum?.FirstOrDefault() ?? Color.white;
            GUI.DrawTexture(r, def.FactionIcon);
            GUI.color = Color.white;
        }
    }
}

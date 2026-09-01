using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 悬赏榜弹窗(v5 海报改版 2026-08-30):左侧悬赏列表(搜索/派系/人种筛选),
    /// 右侧自绘「通缉令海报」详情卡——不再复用 SW 的 Warrant.Draw(裸贴图+三行文字)。
    /// 海报含:类型徽章/旋转印章倒计时/照片框头像/生死双酬/实物加成清单/派系色边框。
    /// 仅在 OnGUI 时工作;列表有签名+30 tick 缓存,海报元素随选中项重建,无 tick 开销。
    /// </summary>
    public class Dialog_BountyBoard : Window
    {
        private enum ViewTab { Available, Related, Pending }

        private const float LeftWidth = 430f;
        private const float RowH = 46f;

        private ViewTab tab = ViewTab.Available;
        private Warrant selected;
        private Vector2 listScroll;
        private string search = "";
        private Faction facFilter;
        private string xenoFilter;

        // 列表缓存:OnGUI 每帧重算 LINQ 违反性能铁律,按签名+30 tick 节流缓存
        private string listCacheSig;
        private int listCacheTick = -1;
        private List<Warrant> listCache;

        // 海报实物图标缓存:随选中项重建,避免每帧 MakeThing
        private string posterCacheId;
        private List<BountyRewards.RewardEntry> posterEntries;
        private List<Thing> posterIconThings;

        private static Texture2D tameIcon;
        private static Texture2D questIcon;

        // 海报纸色/印章辅助色:为海报质感引入的少量自定义色
        // (RimThemesLite 只映射原版颜色常量,这些保持固定;语义色一律走 ColoredText/派系色)
        private static readonly Color PaperColor = new Color(0.235f, 0.220f, 0.172f);
        private static readonly Color PaperDarkColor = new Color(0.150f, 0.140f, 0.110f);
        private static readonly Color StampGreenColor = new Color(0.56f, 0.71f, 0.45f);
        private static readonly Color StampBlueColor = new Color(0.50f, 0.69f, 0.85f);
        private static readonly Color BadgeCaptureColor = new Color(0.29f, 0.42f, 0.54f);
        private static readonly Color BadgeHuntColor = new Color(0.54f, 0.29f, 0.26f);
        private static readonly Color BadgeTameColor = new Color(0.35f, 0.48f, 0.29f);
        private static readonly Color BadgeArtifactColor = new Color(0.42f, 0.35f, 0.54f);

        private const float PhotoW = 150f;
        private const float PhotoH = 172f;

        public override Vector2 InitialSize => new Vector2(1120f, 700f);

        public Dialog_BountyBoard()
        {
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            forcePause = false;
            BountyRewards.ClearCache();
            posterCacheId = null;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var mgr = WarrantsManager.Instance;
            if (mgr == null)
            {
                GUI.color = DimmedTextColor();
                Widgets.Label(inRect, "RK_Bounty.NoGame".Translate());
                GUI.color = Color.white;
                return;
            }

            // —— 顶栏:标题 + 打开任务页 ——
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, 320f, 34f), "RK_Bounty.Title".Translate());
            Text.Font = GameFont.Small;

            if (questIcon == null)
                questIcon = ContentFinder<Texture2D>.Get("UI/Commands/ViewQuest", false);
            // 发布通缉按钮(放在"打开任务页"左侧,2026-09-01 新增)
            Rect issueBtn = new Rect(inRect.xMax - 350f, inRect.y + 2f, 170f, 30f);
            if (Widgets.ButtonText(issueBtn, "RK_Bounty.IssueWarrantButton".Translate()))
            {
                Find.WindowStack.Add(new Dialog_IssueWarrantHSK());
            }
            Rect openQuests = new Rect(inRect.xMax - 170f, inRect.y + 2f, 170f, 30f);
            if (Widgets.ButtonText(openQuests, "RK_Bounty.OpenQuests".Translate()))
            {
                Close();
                Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests);
            }

            float contentTop = inRect.y + 40f;
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio != null && radio.pending.Count > 0)
            {
                GUI.color = ColoredText.ThreatColor;
                Widgets.Label(new Rect(inRect.x, inRect.y + 38f, inRect.width, 22f),
                    "RK_Bounty.PendingHint".Translate(radio.pending.Count));
                GUI.color = Color.white;
                contentTop += 24f;
            }
            Rect left = new Rect(inRect.x, contentTop, LeftWidth, inRect.yMax - contentTop);
            Rect right = new Rect(left.xMax + 14f, contentTop, inRect.xMax - (left.xMax + 14f), left.height);

            float y = left.y;

            // 过滤行:搜索 + 派系 + 人种
            Rect searchRect = new Rect(left.x, y, 124f, 26f);
            search = Widgets.TextField(searchRect, search);
            if (Widgets.ButtonText(new Rect(searchRect.xMax + 6f, y, 136f, 26f),
                "SW.FactionFilter".Translate(facFilter != null ? facFilter.Name : "RK_Bounty.All".Translate())))
            {
                Find.WindowStack.Add(new FloatMenu(FactionMenuOptions()));
            }
            if (Widgets.ButtonText(new Rect(searchRect.xMax + 148f, y, 138f, 26f),
                "SW.XenotypeFilter".Translate(xenoFilter ?? "RK_Bounty.All".Translate())))
            {
                Find.WindowStack.Add(new FloatMenu(XenotypeMenuOptions(mgr)));
            }
            y += 32f;

            // 页签:可接 / 已接 / 无线电
            float tabW = (LeftWidth - 24f) / 3f;
            Rect tabA = new Rect(left.x, y, tabW, 30f);
            Rect tabR = new Rect(tabA.xMax + 12f, y, tabW, 30f);
            Rect tabP = new Rect(tabR.xMax + 12f, y, tabW, 30f);
            DrawTabButton(tabA, "RK_Bounty.Available".Translate(mgr.availableWarrants.Count), tab == ViewTab.Available, ViewTab.Available);
            DrawTabButton(tabR, "RK_Bounty.Related".Translate(RelatedList(mgr).Count), tab == ViewTab.Related, ViewTab.Related);
            DrawTabButton(tabP, "RK_Bounty.RadioTab".Translate(radio != null ? radio.pending.Count : 0), tab == ViewTab.Pending, ViewTab.Pending);
            y += 38f;

            // 列表(带缓存)
            List<Warrant> list = CurrentList(mgr, radio);
            if (selected != null
                && !mgr.availableWarrants.Contains(selected)
                && !RelatedList(mgr).Contains(selected)
                && (radio == null || !radio.pending.Contains(selected)))
                selected = null;

            Rect scrollOut = new Rect(left.x, y, left.width, left.yMax - y);
            Rect scrollView = new Rect(0f, 0f, scrollOut.width - 18f, Mathf.Max(1, list.Count) * (RowH + 6f));
            Widgets.BeginScrollView(scrollOut, ref listScroll, scrollView);
            if (list.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = DimmedTextColor();
                Widgets.Label(new Rect(0f, 0f, scrollOut.width, 60f), tab == ViewTab.Available
                    ? "SW.NoPublicWarrantsAvailable".Translate()
                    : tab == ViewTab.Pending
                        ? "RK_Bounty.NoPending".Translate()
                        : "SW.NoRelatedWarrantsAvailable".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            float rowY = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                Rect row = new Rect(0f, rowY, scrollView.width, RowH);
                DrawListRow(row, list[i]);
                rowY += RowH + 6f;
            }
            Widgets.EndScrollView();

            // —— 右栏:通缉令海报 ——
            if (selected != null)
                DrawPoster(right, selected, mgr, radio);
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = DimmedTextColor();
                Widgets.Label(right, "RK_Bounty.SelectHint".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        // ================= 通缉令海报 =================

        private void DrawPoster(Rect r, Warrant w, WarrantsManager mgr, BountyRadioManager radio)
        {
            bool inPublic = mgr.availableWarrants.Contains(w);
            bool inPending = radio != null && radio.pending.Contains(w);
            bool related = !inPublic && !inPending;

            Faction fac = w.issuer ?? Faction.OfPlayer;
            Color facCol = fac.Color;
            int daysLeft = -1;
            if (inPending)
                daysLeft = BountyRadioManager.PendingDaysLeft(w);
            else if (inPublic)
                daysLeft = BountyRadioManager.AvailableDaysLeft(w);

            // 纸底 + 派系色边框与角饰
            GUI.color = PaperColor;
            GUI.DrawTexture(r, BaseContent.WhiteTex);
            DrawFrameLines(r, facCol, 2f);
            DrawCornerBracket(r, facCol, top: true);
            DrawCornerBracket(r, facCol, top: false);

            float pad = 14f;
            float x = r.x + pad;
            float width = r.width - pad * 2f;
            float y = r.y + pad;

            // —— 头部:类型图标 + 令名 + 类型徽章 ——
            string docName, typeName;
            Color badgeCol;
            PosterHeaderInfo(w, out docName, out typeName, out badgeCol);
            Texture2D icon = WarrantIcon(w);
            Rect iconRect = new Rect(x, y, 38f, 38f);
            GUI.color = PaperDarkColor;
            GUI.DrawTexture(iconRect, BaseContent.WhiteTex);
            if (icon != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect.ContractedBy(4f), icon, ScaleMode.ScaleToFit);
            }
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            Widgets.Label(new Rect(iconRect.xMax + 10f, y, width - 160f, 38f), docName);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            Rect badge = new Rect(x + width - 76f, y + 6f, 76f, 24f);
            GUI.color = badgeCol;
            GUI.DrawTexture(badge, BaseContent.WhiteTex);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(badge, typeName);
            Text.Anchor = TextAnchor.UpperLeft;

            y += 48f;
            GUI.color = facCol;
            DrawLine(x, y, x + width, y, 1.5f);
            y += 8f;

            // —— 旋转印章:剩余天数 / 待接收 / 已接受 ——
            string stampText;
            Color stampCol;
            if (related)
            {
                if (w.issuer == Faction.OfPlayer && w.accepteer == null)
                {
                    stampText = "RK_Bounty.StampMine".Translate();
                    stampCol = StampBlueColor;
                }
                else
                {
                    stampText = "RK_Bounty.StampAccepted".Translate();
                    stampCol = StampGreenColor;
                }
            }
            else if (inPending)
            {
                stampText = daysLeft <= 2 ? "RK_Bounty.DaysLeft".Translate(daysLeft) : "RK_Bounty.StampRadio".Translate();
                stampCol = daysLeft <= 2 ? ColoredText.ThreatColor : StampBlueColor;
            }
            else
            {
                stampText = "RK_Bounty.DaysLeft".Translate(daysLeft);
                stampCol = daysLeft <= 2 ? ColoredText.ThreatColor : DimmedTextColor();
            }
            DrawStamp(new Rect(x + width - 128f, y + 4f, 120f, 34f), stampText, stampCol);

            // —— 主体:照片框 + 信息栏 ——
            float bodyTop = y + 6f;
            Rect photoRect = new Rect(x, bodyTop, PhotoW, PhotoH);
            DrawPhotoFrame(photoRect, w);

            // 照片下:银赏金芯片
            float chipY = photoRect.yMax + 8f;
            float chipH = 26f;
            if (w is Warrant_Pawn wp)
            {
                if (wp.rewardForLiving > 0)
                {
                    DrawPayChip(new Rect(x, chipY, PhotoW, chipH),
                        "RK_Bounty.PosterPayLiving".Translate(), wp.rewardForLiving);
                    chipY += chipH + 6f;
                }
                if (wp.rewardForDead > 0)
                {
                    DrawPayChip(new Rect(x, chipY, PhotoW, chipH),
                        "RK_Bounty.PosterPayDead".Translate(), wp.rewardForDead);
                    chipY += chipH + 6f;
                }
            }
            else
            {
                int single = w is Warrant_TameAnimal wt ? wt.Reward
                           : w is Warrant_Artifact wa ? wa.reward : w.MaxRewardValue();
                DrawPayChip(new Rect(x, chipY, PhotoW, chipH),
                    "RK_Bounty.PosterPaySingle".Translate(), single);
                chipY += chipH + 6f;
            }

            // 信息栏(照片右侧)
            float infoX = photoRect.xMax + 14f;
            float infoW = x + width - infoX;
            float iy = bodyTop;
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(infoX, iy, infoW - 130f, 56f), WarrantLabel(w));
            Text.Font = GameFont.Small;
            iy += 58f;

            string xeno = WarrantXenotype(w);
            if (!xeno.NullOrEmpty())
            {
                iy = DrawInfoRow(infoX, infoW, iy, "RK_Bounty.PosterXeno".Translate(), xeno);
            }
            if (w is Warrant_Pawn wp2 && !wp2.reason.NullOrEmpty())
            {
                iy = DrawInfoRow(infoX, infoW, iy, "RK_Bounty.PosterReason".Translate(), wp2.reason);
            }
            string issuerLine = fac.Name;
            if (fac.def != null && fac.def.permanentEnemy)
                issuerLine += "  (" + "RK_Bounty.PosterHostile".Translate() + ")";
            GUI.color = facCol;
            iy = DrawInfoRowRaw(infoX, infoW, iy, "RK_Bounty.PosterIssuer".Translate(), issuerLine);
            GUI.color = Color.white;

            // 六期:被通缉方派系/科技档、我方关系倍率、科技惩罚
            if (w is Warrant_Pawn wpFac && wpFac.Pawn != null && !wpFac.Pawn.RaceProps.Animal)
            {
                Faction tFac = wpFac.Pawn.Faction;
                if (tFac != null && tFac != Faction.OfPlayer)
                {
                    TechLevel tt = tFac.def.techLevel;
                    iy = DrawInfoRow(infoX, infoW, iy, "RK_Bounty.PosterTarget".Translate(),
                        tFac.Name + " · " + BountyRules.TechLevelLabel(tt));
                    int gw = Faction.OfPlayer.GoodwillWith(tFac);
                    float relM = BountyRules.RelationMultiplier(gw);
                    iy = DrawInfoRow(infoX, infoW, iy, "RK_Bounty.PosterRelation".Translate(),
                        gw.ToStringWithSign() + "  ×" + relM.ToString("0.00"),
                        gw < 0 ? ColoredText.ThreatColor : Color.white);
                    if (tt < BountyRules.PlayerTechLevel())
                    {
                        iy = DrawInfoRow(infoX, infoW, iy, "RK_Bounty.PosterTechPenalty".Translate(),
                            "RK_Bounty.TechPenaltyApplied".Translate(), ColoredText.ThreatColor);
                    }
                    if (wpFac.issuer == Faction.OfPlayer)
                    {
                        iy = DrawInfoRow(infoX, infoW, iy, "RK_Bounty.PosterSuccessRate".Translate(),
                            BountyRules.WarrantSuccessChance(tt).ToString("P0"),
                            BountyRules.WarrantSuccessChance(tt) < 0.5f ? ColoredText.ThreatColor : Color.white);
                    }
                }
            }

            if (!related && daysLeft >= 0)
            {
                string dl = inPending
                    ? "RK_Bounty.PendingFor".Translate(daysLeft)
                    : "RK_Bounty.AvailableFor".Translate(daysLeft);
                iy = DrawInfoRow(infoX, infoW, iy, "RK_Bounty.PosterDeadline".Translate(), dl,
                    daysLeft <= 2 ? ColoredText.ThreatColor : Color.white);
            }

            // —— 实物加成清单 ——
            RefreshPosterCache(w);
            if (posterEntries != null && posterEntries.Count > 0)
            {
                float by = Mathf.Max(chipY, iy) + 6f;
                GUI.color = ColoredText.CurrencyColor;
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(new Rect(x, by, width, 22f), "RK_Bounty.PosterBonus".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                by += 24f;
                int shown = Mathf.Min(posterEntries.Count, 4);
                for (int i = 0; i < shown; i++)
                    DrawBonusRow(new Rect(x, by, width, 26f), posterEntries[i], posterIconThings != null ? posterIconThings[i] : null);
                if (posterEntries.Count > shown)
                {
                    GUI.color = DimmedTextColor();
                    Widgets.Label(new Rect(x + 32f, by + shown * 26f, width - 32f, 20f),
                        "RK_Bounty.BonusMore".Translate(posterEntries.Count - shown));
                }
                by += Mathf.Min(posterEntries.Count, 4) * 26f + (posterEntries.Count > shown ? 20f : 0f);
                y = by + 6f;
            }
            else
            {
                y = Mathf.Max(chipY, iy) + 6f;
            }

            // —— 口信 ——
            if (!w.message.NullOrEmpty())
            {
                float msgH = 58f;
                Rect msg = new Rect(x, y, width, msgH);
                GUI.color = PaperDarkColor;
                GUI.DrawTexture(msg, BaseContent.WhiteTex);
                GUI.color = facCol;
                GUI.DrawTexture(new Rect(msg.x, msg.y, 3f, msg.height), BaseContent.WhiteTex);
                GUI.color = new Color(0.82f, 0.79f, 0.70f);
                Text.Anchor = TextAnchor.UpperLeft;
                Widgets.Label(msg.ContractedBy(6f), w.message);
                GUI.color = Color.white;
                y += msgH + 8f;
            }

            // —— 待接收横幅 ——
            if (inPending)
            {
                Rect banner = new Rect(x, y, width, 36f);
                GUI.color = new Color(0.12f, 0.15f, 0.19f);
                GUI.DrawTexture(banner, BaseContent.WhiteTex);
                DrawFrameLines(banner, StampBlueColor, 1f);
                GUI.color = StampBlueColor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(banner.x + 6f, banner.y, banner.width - 12f, banner.height), "RK_Bounty.PosterRadioNote".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                y += 44f;
            }

            // —— 按钮区 ——
            float btnY = r.yMax - pad - 36f;
            if (inPublic)
            {
                Rect decline = new Rect(x + width - 246f, btnY, 110f, 36f);
                Rect accept = new Rect(decline.xMax + 10f, btnY, 136f, 36f);
                if (Widgets.ButtonText(decline, "SW.Decline".Translate()))
                    mgr.availableWarrants.Remove(w);
                if (Widgets.ButtonText(accept, "Accept".Translate()))
                    w.DoAcceptAction();
            }
            else if (related)
            {
                if (w.issuer == Faction.OfPlayer && w.accepteer == null)
                {
                    Rect remove = new Rect(x + width - 150f, btnY, 150f, 36f);
                    if (Widgets.ButtonText(remove, "SW.RemoveWarrant".Translate()))
                        RemoveEverywhere(mgr, w);
                }
                else if (w.ShouldShowCompensateButton())
                {
                    Rect comp = new Rect(x + width - 150f, btnY, 150f, 36f);
                    if (Widgets.ButtonText(comp, "SW.Compensate".Translate()))
                        w.DoCompensateAction();
                }
                else
                {
                    GUI.color = DimmedTextColor();
                    Text.Anchor = TextAnchor.MiddleRight;
                    Widgets.Label(new Rect(x, btnY + 4f, width, 28f), "RK_Bounty.PosterAccepted".Translate());
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                }
            }
            else
            {
                GUI.color = DimmedTextColor();
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(new Rect(x, btnY + 4f, width, 28f), "RK_Bounty.PosterRadioLocked".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
        }

        private void RefreshPosterCache(Warrant w)
        {
            string id = w.loadID;
            if (posterCacheId == id && posterEntries != null)
                return;
            posterCacheId = id;
            posterEntries = BountyRewards.GetPackage(w, useCache: true);
            posterIconThings = null;
            if (posterEntries != null && posterEntries.Count > 0)
            {
                posterIconThings = new List<Thing>();
                foreach (var e in posterEntries)
                {
                    Thing t = ThingMaker.MakeThing(e.def, e.stuff);
                    t.stackCount = Mathf.Min(e.count, Mathf.Max(1, e.def.stackLimit));
                    var q = t.TryGetComp<CompQuality>();
                    if (q != null)
                        q.SetQuality(e.quality, null);
                    posterIconThings.Add(t);
                }
            }
        }

        private void DrawBonusRow(Rect r, BountyRewards.RewardEntry e, Thing iconThing)
        {
            GUI.color = PaperDarkColor;
            GUI.DrawTexture(r, BaseContent.WhiteTex);
            GUI.color = Color.white;
            if (iconThing != null)
                Widgets.ThingIcon(new Rect(r.x + 3f, r.y + 2f, 22f, 22f), iconThing);
            Text.Anchor = TextAnchor.MiddleLeft;
            string label = e.def.label + " ×" + e.count;
            if (e.quality != QualityCategory.Normal && e.def.HasComp(typeof(CompQuality)))
                label += "  (" + QualityUtility.GetLabel(e.quality) + ")";
            GUI.color = Color.white;
            Widgets.Label(new Rect(r.x + 30f, r.y, r.width - 130f, r.height), label);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = ColoredText.CurrencyColor;
            Widgets.Label(new Rect(r.xMax - 96f, r.y, 92f, r.height), Mathf.RoundToInt(e.value).ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawPayChip(Rect r, string label, int amount)
        {
            GUI.color = PaperDarkColor;
            GUI.DrawTexture(r, BaseContent.WhiteTex);
            DrawFrameLines(r, new Color(0.35f, 0.32f, 0.25f), 1f);
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = DimmedTextColor();
            Widgets.Label(new Rect(r.x + 8f, r.y, r.width - 90f, r.height), label);
            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = ColoredText.CurrencyColor;
            Widgets.Label(new Rect(r.xMax - 86f, r.y, 80f, r.height), amount.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private float DrawInfoRow(float x, float w, float y, string key, string value, Color? valueColor = null)
        {
            GUI.color = DimmedTextColor();
            Widgets.Label(new Rect(x, y, 56f, 24f), key);
            GUI.color = valueColor ?? Color.white;
            Widgets.Label(new Rect(x + 62f, y, w - 62f, 24f), value);
            GUI.color = Color.white;
            return y + 25f;
        }

        private float DrawInfoRowRaw(float x, float w, float y, string key, string value)
        {
            GUI.color = DimmedTextColor();
            Widgets.Label(new Rect(x, y, 56f, 24f), key);
            GUI.color = GUI.color; // 值颜色由调用方设好
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(x + 62f, y, w - 62f, 24f), value);
            GUI.color = Color.white;
            return y + 25f;
        }

        private void DrawPhotoFrame(Rect r, Warrant w)
        {
            GUI.color = PaperDarkColor;
            GUI.DrawTexture(r, BaseContent.WhiteTex);
            DrawFrameLines(r, new Color(0.40f, 0.37f, 0.29f), 2f);

            Texture2D tex = null;
            RenderTexture rt = null;
            if (w is Warrant_Pawn wp && wp.Pawn != null && !wp.Pawn.Dead && !wp.Pawn.Destroyed)
            {
                rt = PortraitsCache.Get(wp.Pawn, new Vector2(PhotoW, PhotoH), Rot4.South);
                GUI.color = Color.white;
                GUI.DrawTexture(r.ContractedBy(3f), rt);
            }
            else
            {
                if (w is Warrant_TameAnimal wt)
                {
                    ThingDef race = wt.AnimalRace != null ? wt.AnimalRace.race : null;
                    if (race != null && race.graphic != null)
                        tex = (Texture2D)race.graphic.MatSouth.mainTexture;
                }
                else if (w.thing != null && w.thing.Graphic != null)
                    tex = (Texture2D)w.thing.Graphic.MatSouth.mainTexture;
                if (tex == null)
                    tex = Warrant_Pawn.IconCapture;
                GUI.color = Color.white;
                Rect inner = r.ContractedBy(14f);
                GUI.DrawTexture(inner, tex, ScaleMode.ScaleToFit);
            }
        }

        private void DrawStamp(Rect r, string text, Color col)
        {
            Matrix4x4 m = GUI.matrix;
            try
            {
                GUIUtility.RotateAroundPivot(-9f, r.center);
                GUI.color = new Color(col.r, col.g, col.b, 0.28f);
                GUI.DrawTexture(r, BaseContent.WhiteTex);
                DrawFrameLines(r, col, 2f);
                DrawFrameLines(r.ContractedBy(3f), col, 1f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = col;
                Widgets.Label(r, text);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            finally
            {
                GUI.matrix = m;
            }
            GUI.color = Color.white;
        }

        private static void PosterHeaderInfo(Warrant w, out string docName, out string typeName, out Color badgeCol)
        {
            if (w is Warrant_Pawn && w.thing != null && w.thing is Pawn p && p.RaceProps.Animal)
            {
                docName = "RK_Bounty.DocHunt".Translate();
                typeName = "RK_Bounty.TypeHunt".Translate();
                badgeCol = BadgeHuntColor;
                return;
            }
            if (w is Warrant_Pawn)
            {
                docName = "RK_Bounty.DocCapture".Translate();
                typeName = "RK_Bounty.TypeCapture".Translate();
                badgeCol = BadgeCaptureColor;
            }
            else if (w is Warrant_TameAnimal)
            {
                docName = "RK_Bounty.DocTame".Translate();
                typeName = "RK_Bounty.TypeTame".Translate();
                badgeCol = BadgeTameColor;
            }
            else if (w is Warrant_Artifact)
            {
                docName = "RK_Bounty.DocArtifact".Translate();
                typeName = "RK_Bounty.TypeArtifact".Translate();
                badgeCol = BadgeArtifactColor;
            }
            else
            {
                docName = "RK_Bounty.DocCapture".Translate();
                typeName = "RK_Bounty.TypeCapture".Translate();
                badgeCol = BadgeCaptureColor;
            }
        }

        // Warrant_Pawn 动物猎杀单在 SW 无独立类型,按 thing 是否动物归入猎杀/抓捕,
        // 但 DrawListRow 的图标逻辑(WarrantIcon)已区分,此处标签跟随即可。

        private static void RemoveEverywhere(WarrantsManager mgr, Warrant w)
        {
            mgr.createdWarrants.Remove(w);
            mgr.availableWarrants.Remove(w);
            mgr.acceptedWarrants.Remove(w);
            mgr.takenWarrants.Remove(w);
            mgr.postponedWarrants.Remove(w);
        }

        private static void DrawLine(float x1, float y1, float x2, float y2, float thickness)
        {
            Widgets.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), GUI.color, thickness);
        }

        private static void DrawFrameLines(Rect r, Color col, float t)
        {
            GUI.color = col;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), BaseContent.WhiteTex);
            GUI.color = Color.white;
        }

        private static void DrawCornerBracket(Rect r, Color col, bool top)
        {
            const float len = 16f;
            const float t = 2.5f;
            GUI.color = col;
            if (top)
            {
                GUI.DrawTexture(new Rect(r.x + 4f, r.y + 4f, len, t), BaseContent.WhiteTex);
                GUI.DrawTexture(new Rect(r.x + 4f, r.y + 4f, t, len), BaseContent.WhiteTex);
            }
            else
            {
                GUI.DrawTexture(new Rect(r.xMax - 4f - len, r.yMax - 4f - t, len, t), BaseContent.WhiteTex);
                GUI.DrawTexture(new Rect(r.xMax - 4f - t, r.yMax - 4f - len, t, len), BaseContent.WhiteTex);
            }
            GUI.color = Color.white;
        }

        private void DrawTabButton(Rect rect, string label, bool active, ViewTab target)
        {
            if (!active)
                GUI.color = DimmedTextColor();
            if (Widgets.ButtonText(rect, label) && !active)
            {
                tab = target;
                selected = null;
            }
            if (!active)
                GUI.color = Color.white;
        }

        // 次要文字用原版标准灰(与提示/货币色一致,主题 mod 对原版常量做整体映射)
        private static Color DimmedTextColor()
        {
            return ColoredText.SubtleGrayColor;
        }

        private void DrawListRow(Rect row, Warrant w)
        {
            // 行背景/选中态/悬停全部走原版颜色字段(RimThemesLite 按主题映射整体替换)
            if (Mouse.IsOver(row) && selected != w)
            {
                GUI.color = GenUI.MouseoverColor;
                GUI.DrawTexture(row, BaseContent.WhiteTex);
                GUI.color = Color.white;
            }
            Widgets.DrawOptionBackground(row, selected == w);

            Texture2D icon = WarrantIcon(w);
            if (icon != null)
                GUI.DrawTexture(new Rect(row.x + 6f, row.y + 7f, 32f, 32f), icon, ScaleMode.ScaleToFit);

            float textX = row.x + 46f;
            float textW = row.width - 46f - 86f;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(textX, row.y + 4f, textW, 22f), WarrantLabel(w));

            string sub = w.issuer != null ? w.issuer.Name : "—";
            string xeno = WarrantXenotype(w);
            if (!xeno.NullOrEmpty())
                sub += " · " + xeno;
            int bonus = tab == ViewTab.Available ? BountyRewards.BonusCount(w) : 0;
            if (bonus > 0)
                sub += " · " + "RK_Bounty.BonusPiecesTag".Translate(bonus);
            GUI.color = w.issuer != null ? w.issuer.Color : DimmedTextColor();
            Widgets.Label(new Rect(textX, row.y + 24f, textW, 20f), sub);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = ColoredText.CurrencyColor;
            Widgets.Label(new Rect(row.xMax - 82f, row.y + 6f, 78f, 26f), w.MaxRewardValue().ToString());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // 有效期倒计时:可接=榜上 15 天;无线电=播报 7 天
            if (tab == ViewTab.Available || tab == ViewTab.Pending)
            {
                int daysLeft = tab == ViewTab.Pending
                    ? BountyRadioManager.PendingDaysLeft(w)
                    : BountyRadioManager.AvailableDaysLeft(w);
                if (daysLeft >= 0)
                {
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.MiddleRight;
                    GUI.color = daysLeft <= 2 ? ColoredText.ThreatColor : DimmedTextColor();
                    Widgets.Label(new Rect(row.xMax - 82f, row.y + 28f, 78f, 16f),
                        "RK_Bounty.DaysLeft".Translate(daysLeft));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            }

            if (Widgets.ButtonInvisible(row))
                selected = w;
        }

        // ---------- 数据侧 ----------

        private List<Warrant> CurrentList(WarrantsManager mgr, BountyRadioManager radio)
        {
            string sig = $"{tab}|{search}|{facFilter?.loadID}|{xenoFilter}|{mgr.availableWarrants.Count}|{mgr.acceptedWarrants.Count}|{mgr.createdWarrants.Count}|{mgr.takenWarrants.Count}|{radio?.pending.Count ?? 0}";
            int now = Find.TickManager.TicksGame;
            if (listCache != null && sig == listCacheSig && now - listCacheTick < 30)
                return listCache;
            listCache = tab switch
            {
                ViewTab.Pending => radio != null ? radio.pending.ToList() : new List<Warrant>(),
                ViewTab.Related => RelatedList(mgr),
                _ => AvailableList(mgr),
            };
            listCacheSig = sig;
            listCacheTick = now;
            return listCache;
        }

        private List<Warrant> AvailableList(WarrantsManager mgr)
        {
            IEnumerable<Warrant> q = mgr.availableWarrants.Where(x => x.thing == null || x.thing.Faction != Faction.OfPlayer);
            if (facFilter != null)
                q = q.Where(x => x.issuer == facFilter);
            if (!xenoFilter.NullOrEmpty())
                q = q.Where(x => WarrantXenotype(x) == xenoFilter);
            if (!search.NullOrEmpty())
            {
                string s = search.ToLowerInvariant();
                q = q.Where(x => WarrantLabel(x).ToLowerInvariant().Contains(s));
            }
            return q.OrderByDescending(x => x.createdTick).ToList();
        }

        private List<Warrant> RelatedList(WarrantsManager mgr)
        {
            return mgr.acceptedWarrants
                .Concat(mgr.createdWarrants)
                .Concat(mgr.takenWarrants)
                .OrderByDescending(x => x.createdTick)
                .ToList();
        }

        private List<FloatMenuOption> FactionMenuOptions()
        {
            var opts = new List<FloatMenuOption>
            {
                new FloatMenuOption("RK_Bounty.All".Translate(), delegate { facFilter = null; })
            };
            var facs = WarrantsManager.Instance.availableWarrants
                .Where(x => x.issuer != null)
                .Select(x => x.issuer).Distinct().OrderBy(f => f.Name);
            foreach (Faction f in facs)
            {
                Faction ff = f;
                opts.Add(new FloatMenuOption(ff.Name, delegate
                {
                    facFilter = facFilter == ff ? null : ff;
                }));
            }
            return opts;
        }

        private List<FloatMenuOption> XenotypeMenuOptions(WarrantsManager mgr)
        {
            var opts = new List<FloatMenuOption>
            {
                new FloatMenuOption("RK_Bounty.All".Translate(), delegate { xenoFilter = null; })
            };
            var xnos = mgr.availableWarrants
                .Select(WarrantXenotype)
                .Where(x => !x.NullOrEmpty()).Distinct().OrderBy(x => x);
            foreach (string x in xnos)
            {
                string xx = x;
                opts.Add(new FloatMenuOption(xx, delegate
                {
                    xenoFilter = xenoFilter == xx ? null : xx;
                }));
            }
            return opts;
        }

        // ---------- Warrant 适配 ----------

        internal static string WarrantLabel(Warrant w)
        {
            switch (w)
            {
                case Warrant_Pawn wp:
                    Pawn p = wp.Pawn;
                    return p != null ? p.LabelShortCap : "…";
                case Warrant_TameAnimal wt:
                    return wt.AnimalRace != null ? wt.AnimalRace.label : "…";
                case Warrant_Artifact wa:
                    return wa.thing != null ? wa.thing.Label : "…";
                default:
                    return w.GetUniqueLoadID();
            }
        }

        internal static string WarrantXenotype(Warrant w)
        {
            if (!(w is Warrant_Pawn wp) || !ModsConfig.BiotechActive)
                return null;
            Pawn p = wp.Pawn;
            if (p == null || p.genes == null || p.genes.Xenotype == null)
                return null;
            return p.genes.Xenotype.label;
        }

        internal static Texture2D WarrantIcon(Warrant w)
        {
            if (w is Warrant_Artifact)
                return Warrant_Artifact.IconRetrieve;
            if (w is Warrant_TameAnimal)
            {
                if (tameIcon == null)
                    tameIcon = ContentFinder<Texture2D>.Get("UI/Designators/Tame", false);
                return tameIcon;
            }
            if (w is Warrant_Pawn wp && wp.Pawn != null && wp.Pawn.RaceProps.Animal)
                return Warrant_Pawn.IconDeath;
            return Warrant_Pawn.IconCapture;
        }
    }
}

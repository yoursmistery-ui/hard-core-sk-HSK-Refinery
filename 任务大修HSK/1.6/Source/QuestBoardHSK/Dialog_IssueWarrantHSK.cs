using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using SimpleWarrants;
using TargetType = SimpleWarrants.TargetType; // 与 Verse.TargetType 消歧

namespace QuestBoardHSK
{
    /// <summary>
    /// 玩家发布通缉对话框(2026-09-01 新版):取代 SW 原版 MainTabWindow_Warrants.CreateWarrant Tab 的发单表单。
    /// 字段完全仿 SW 原版(便于将来按需同步 SW 行为变化),但玩家手填金额为准、不走 AssignRewards 覆盖;
    /// 预付费拦截(科技门槛 / 银不足)复用 BountyRules.PrepayForTech 等纯函数。
    /// Dialog_SelectPawn/Animal/Artifact 必须传 MainTabWindow_Warrants parent 参数,这里传 stub 实例(不会调其 DoWindowContents)。
    /// </summary>
    public class Dialog_IssueWarrantHSK : Window
    {
        private static readonly Vector2 WinSize = new Vector2(560f, 640f);

        // SW 原版同名字段(参考 MainTabWindow_Warrants:997)
        private string buffCurCapturePayment;
        private string buffCurDeathPayment;
        private string buffCurReward;
        private bool capturePaymentEnabled;
        private Pawn curAnimal;
        private Thing curArtifact;
        private int curCapturePayment;
        private int curDeathPayment;
        private string curMessage;
        private Pawn curPawn;
        private string curReason;
        private int curReward;
        private TargetType curType;
        private bool deathPaymentEnabled;

        // stub,仅用于 Dialog_SelectPawn/Animal/Artifact 的构造参数(它们只读 parent.curPawn/curAnimal/curArtifact/curReward 这些字段)
        private MainTabWindow_Warrants stubParent;

        public override Vector2 InitialSize => WinSize;

        public Dialog_IssueWarrantHSK()
        {
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = false;
            closeOnAccept = false;
            closeOnClickedOutside = false;
            forcePause = false;
        }

        public void AssignPawn(Pawn pawn) => curPawn = pawn;
        public void AssignAnimal(Pawn animal) => curAnimal = animal;
        public void AssignArtifact(Thing artifact) => curArtifact = artifact;

        public override void DoWindowContents(Rect inRect)
        {
            // SW 未激活:整个发单表单依赖 Dialog_SelectPawn/Animal/Artifact(在 SimpleWarrants.dll),
            // 缺失时无法走"选择"流程,只显示提示。
            if (!SimpleWarrantsActive())
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = ColoredText.ThreatColor;
                Widgets.Label(inRect.ContractedBy(20f), "RK_Bounty.NeedsSimpleWarrants".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // 同步 stubParent 上被 Dialog_SelectPawn/Dialog_SelectAnimal/Dialog_SelectArtifact
            // 通过 AssignPawn/AssignAnimal/AssignArtifact 写入的字段(SW 的 MainTabWindow_Warrants 上是私有)
            SyncFromStub();

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // —— 顶部:标题 + 科技档提示 + 关闭 ——
            Rect titleRect = new Rect(inRect.x, inRect.y, 320f, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "RK_Bounty.IssueWarrantTitle".Translate());
            Text.Font = GameFont.Small;
            GUI.color = DimmedTextColor();
            Widgets.Label(new Rect(inRect.x, inRect.y + 28f, inRect.width - 60f, 22f),
                "RK_Bounty.IssuePlayerTech".Translate(BountyRules.PlayerTechLevel().ToString()));
            GUI.color = Color.white;
            if (Widgets.ButtonText(new Rect(inRect.xMax - 60f, inRect.y, 50f, 28f), "Close".Translate()))
            {
                Close();
                return;
            }

            float y = inRect.y + 60f;

            // —— 通缉对象下拉(Human / Animal / Artifact)——
            Rect typeLabel = new Rect(inRect.x, y, 80f, 24f);
            Widgets.Label(typeLabel, "SW.WarrantSubject".Translate());
            Rect typeBtn = new Rect(typeLabel.xMax, y, 180f, 24f);
            if (Widgets.ButtonText(typeBtn, GetLabel(curType)))
            {
                var opts = new List<FloatMenuOption>();
                foreach (TargetType t in System.Enum.GetValues(typeof(TargetType)))
                {
                    TargetType tt = t;
                    opts.Add(new FloatMenuOption(GetLabel(tt), delegate { SetType(tt); }));
                }
                Find.WindowStack.Add(new FloatMenu(opts));
            }
            y += 30f;

            // —— 当前目标显示(头像 + 选择按钮)——
            Rect createWarrant = new Rect(inRect.x, y, inRect.width - 16f, 200f);
            float drawY = createWarrant.y;
            if (curType == TargetType.Human || curType == TargetType.Animal)
            {
                Pawn pawn = (curType == TargetType.Human) ? curPawn : curAnimal;
                if (pawn == null)
                {
                    pawn = RandomPawnForType(curType);
                    if (curType == TargetType.Human) curPawn = pawn; else curAnimal = pawn;
                }
                drawY = DrawPawnSection(createWarrant, drawY, pawn);
            }
            else if (curType == TargetType.Artifact)
            {
                if (curArtifact == null)
                    curArtifact = ThingMaker.MakeThing(Utils.AllArtifactDefs.RandomElement(), null);
                drawY = DrawArtifactSection(createWarrant, drawY, curArtifact);
            }

            y = drawY + 6f;

            // —— 罪名(仅 Human)——
            if (curType == TargetType.Human)
            {
                if (curReason.NullOrEmpty() && curPawn != null)
                    curReason = Utils.GenerateTextFromRule(SW_DefOf.SW_WantedFor, curPawn.thingIDNumber);
                Rect reasonLabel = new Rect(inRect.x, y, 60f, 24f);
                Widgets.Label(reasonLabel, "SW.Reason".Translate());
                Rect reasonField = new Rect(reasonLabel.xMax, y, 220f, 24f);
                curReason = Widgets.TextArea(reasonField, curReason, false);
                y += 28f;

                Rect msgLabel = new Rect(inRect.x, y, 60f, 24f);
                Widgets.Label(msgLabel, "SW.Message".Translate());
                Rect msgField = new Rect(msgLabel.x, y + 22f, 260f, 60f);
                curMessage = Widgets.TextArea(msgField, curMessage ?? "", false);
                y += 86f;
            }

            // —— 活捉报酬 / 击杀报酬(pawn) 或 单一报酬(artifact)——
            if (curType == TargetType.Artifact)
            {
                Rect rewardLabel = new Rect(inRect.x, y, 100f, 24f);
                Widgets.Label(rewardLabel, "SW.CapturePayment".Translate());
                Rect rewardField = new Rect(rewardLabel.xMax, y, 80f, 24f);
                Widgets.TextFieldNumeric<int>(rewardField, ref curReward, ref buffCurReward, 0f, 1E+09f);
                y += 30f;
            }
            else
            {
                Rect capLabel = new Rect(inRect.x, y, 100f, 24f);
                Widgets.Label(capLabel, "SW.CapturePayment".Translate());
                Rect capField = new Rect(capLabel.xMax, y, 80f, 24f);
                if (capturePaymentEnabled)
                    Widgets.TextFieldNumeric<int>(capField, ref curCapturePayment, ref buffCurCapturePayment, 0f, 1E+09f);
                Rect capBox = new Rect(capField.xMax + 5f, y, 24f, 24f);
                Widgets.Checkbox(capBox.x, capBox.y, ref capturePaymentEnabled);
                y += 30f;

                Rect deadLabel = new Rect(inRect.x, y, 100f, 24f);
                Widgets.Label(deadLabel, "SW.DeathPayment".Translate());
                Rect deadField = new Rect(deadLabel.xMax, y, 80f, 24f);
                if (deathPaymentEnabled)
                    Widgets.TextFieldNumeric<int>(deadField, ref curDeathPayment, ref buffCurDeathPayment, 0f, 1E+09f);
                Rect deadBox = new Rect(deadField.xMax + 5f, y, 24f, 24f);
                Widgets.Checkbox(deadBox.x, deadBox.y, ref deathPaymentEnabled);
                y += 30f;
            }

            // —— 预付费提示(仅 NPC 派系目标,且为目标 > 我方 1档才显示)——
            if (TryGetTargetFaction(out Faction targetFac, out bool isNpcTarget))
            {
                if (isNpcTarget)
                {
                    int prepay = BountyRules.PrepayForTech(targetFac.def.techLevel);
                    bool overTech = targetFac.def.techLevel > BountyRules.PlayerTechLevel();
                    if (overTech)
                        prepay = Mathf.RoundToInt(prepay * BountyRules.AboveTechCostFactor);
                    GUI.color = overTech ? ColoredText.ThreatColor : DimmedTextColor();
                    Widgets.Label(new Rect(inRect.x, y, inRect.width - 16f, 22f),
                        overTech
                            ? "RK_Bounty.PrepayHintOver".Translate(prepay, targetFac.Name)
                            : "RK_Bounty.PrepayHint".Translate(prepay, targetFac.Name));
                    GUI.color = Color.white;
                    y += 24f;
                }
            }

            // —— 发布按钮 + 取消按钮 ——
            float btnY = inRect.yMax - 36f;
            if (Widgets.ButtonText(new Rect(inRect.x, btnY, 160f, 30f), "Cancel".Translate()))
            {
                Close();
                return;
            }
            Rect publish = new Rect(inRect.xMax - 170f, btnY, 160f, 30f);
            GUI.color = Color.green;
            if (Widgets.ButtonText(publish, "RK_Bounty.Publish".Translate()))
            {
                TryPublishFromUi();
            }
            GUI.color = Color.white;
        }

        // ============== 子段绘制 ==============

        private float DrawPawnSection(Rect createWarrant, float y, Pawn pawn)
        {
            if (pawn == null)
                return y;
            Rect photo = new Rect(createWarrant.x + 40f, y + 10f, 72f, 100f);
            RenderTexture rt = PortraitsCache.Get(pawn, new Vector2(photo.width, photo.height), Rot4.South, Vector3.zero, 1.2f, true, true, true, true, null, null, false, null);
            GUI.DrawTexture(photo, rt);
            Widgets.InfoCardButton(photo.xMax, photo.y, pawn);

            Rect nameRect = new Rect(createWarrant.x, photo.yMax + 4f, createWarrant.width, 22f);
            Widgets.Label(nameRect, curType == TargetType.Human ? (string)pawn.Name.ToString() : pawn.def.LabelCap.Resolve());

            Rect selectRect = new Rect(createWarrant.x, nameRect.yMax + 6f, createWarrant.width, 24f);
            if (Widgets.ButtonTextSubtle(selectRect, "SW.Select".Translate()))
            {
                EnsureStubParent();
                SyncToStub(); // 把当前字段同步到 stub,Dialog_SelectPawn 从 stub 读 allPawns 列表
                if (curType == TargetType.Human)
                    Find.WindowStack.Add(new Dialog_SelectPawn(stubParent));
                else
                    Find.WindowStack.Add(new Dialog_SelectAnimal(stubParent));
            }
            return selectRect.yMax + 8f;
        }

        private float DrawArtifactSection(Rect createWarrant, float y, Thing artifact)
        {
            Rect icon = new Rect(createWarrant.x + 40f, y + 10f, 72f, 72f);
            Widgets.ThingIcon(icon, artifact);
            Widgets.InfoCardButton(icon.xMax, icon.y, artifact);

            Rect nameRect = new Rect(createWarrant.x, icon.yMax + 4f, createWarrant.width, 22f);
            Widgets.Label(nameRect, artifact.LabelCap);

            Rect selectRect = new Rect(createWarrant.x, nameRect.yMax + 6f, createWarrant.width, 24f);
            if (Widgets.ButtonTextSubtle(selectRect, "SW.Select".Translate()))
            {
                EnsureStubParent();
                SyncToStub();
                Find.WindowStack.Add(new Dialog_SelectArtifact(stubParent));
            }
            return selectRect.yMax + 8f;
        }

        // ============== 类型切换/目标 ==============

        private void SetType(TargetType t)
        {
            if (curType == t)
                return;
            curType = t;
            // 切换类型后清掉旧的目标字段,避免跨界引用
            if (t == TargetType.Human) { curAnimal = null; curArtifact = null; }
            else if (t == TargetType.Animal) { curPawn = null; curArtifact = null; }
            else { curPawn = null; curAnimal = null; }
            // 重置 stub 让 Select 子窗刷新
            if (stubParent != null) { stubParent = null; }
        }

        private Pawn RandomPawnForType(TargetType t)
        {
            if (t == TargetType.Human)
            {
                PawnKindDef pk = DefDatabase<PawnKindDef>.AllDefs
                    .Where(x => x.RaceProps.Humanlike).RandomElement();
                Faction f = (pk.defaultFactionDef != null ? Find.FactionManager.FirstFactionOfDef(pk.defaultFactionDef) : null)
                    ?? Find.FactionManager.AllFactions.Where(x => x.def.humanlikeFaction && !x.defeated && !x.IsPlayer && !x.Hidden).RandomElement();
                return PawnGenerator.GeneratePawn(pk, f);
            }
            return PawnGenerator.GeneratePawn(Utils.AllWorthAnimalDefs.RandomElement(), null);
        }

        private bool TryGetTargetFaction(out Faction targetFac, out bool isNpcTarget)
        {
            targetFac = null;
            isNpcTarget = false;
            Pawn pawn = curType == TargetType.Human ? curPawn : (curType == TargetType.Animal ? curAnimal : null);
            if (pawn != null && pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
            {
                targetFac = pawn.Faction;
                isNpcTarget = true;
                return true;
            }
            return false;
        }

        // ============== stub parent ==============

        private void EnsureStubParent()
        {
            if (stubParent != null)
                return;
            stubParent = new MainTabWindow_Warrants();
            SyncToStub(); // 初次创建时把自己的字段同步过去
        }

        // Dialog_SelectPawn 等通过 parent.AssignPawn(pawn) 写入私有 curPawn 字段,
        // 这里每帧 OnGUI 开头反射读 stubParent 上对应字段,把它们带回自己实例。
        private static readonly System.Reflection.BindingFlags FInst =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        private static System.Reflection.FieldInfo fiCurPawn;
        private static System.Reflection.FieldInfo fiCurAnimal;
        private static System.Reflection.FieldInfo fiCurArtifact;
        private static System.Reflection.FieldInfo fiCurReward;

        private static System.Reflection.FieldInfo Fi(System.Reflection.FieldInfo cache, string name)
        {
            return cache ?? typeof(MainTabWindow_Warrants).GetField(name, FInst);
        }

        private void SyncFromStub()
        {
            if (stubParent == null) return;
            fiCurPawn = Fi(fiCurPawn, "curPawn");
            fiCurAnimal = Fi(fiCurAnimal, "curAnimal");
            fiCurArtifact = Fi(fiCurArtifact, "curArtifact");
            fiCurReward = Fi(fiCurReward, "curReward");
            if (fiCurPawn != null) curPawn = (Pawn)fiCurPawn.GetValue(stubParent);
            if (fiCurAnimal != null) curAnimal = (Pawn)fiCurAnimal.GetValue(stubParent);
            if (fiCurArtifact != null) curArtifact = (Thing)fiCurArtifact.GetValue(stubParent);
            if (fiCurReward != null) curReward = (int)fiCurReward.GetValue(stubParent);
        }

        private void SyncToStub()
        {
            if (stubParent == null) return;
            fiCurPawn = Fi(fiCurPawn, "curPawn");
            fiCurAnimal = Fi(fiCurAnimal, "curAnimal");
            fiCurArtifact = Fi(fiCurArtifact, "curArtifact");
            fiCurReward = Fi(fiCurReward, "curReward");
            fiCurPawn?.SetValue(stubParent, curPawn);
            fiCurAnimal?.SetValue(stubParent, curAnimal);
            fiCurArtifact?.SetValue(stubParent, curArtifact);
            fiCurReward?.SetValue(stubParent, curReward);
        }

        // ============== 创建 ==============

        private void TryPublishFromUi()
        {
            string failReason;
            Warrant w = TryCreateWarrant(out failReason);
            if (w == null)
            {
                Find.WindowStack.Add(new Dialog_MessageBox(failReason));
                return;
            }

            // 预付费拦截(科技门槛已在 SW.CreateWarrant 校验,这里二次保险)
            if (w is Warrant_Pawn wp && wp.Pawn != null)
            {
                Faction targetFac = wp.Pawn.Faction;
                if (targetFac != null && targetFac != Faction.OfPlayer)
                {
                    TechLevel targetTech = targetFac.def.techLevel;
                    TechLevel myTech = BountyRules.PlayerTechLevel();
                    if (targetTech > myTech + BountyRules.MaxTechGap)
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                            "RK_Bounty.IssueTooHighTech".Translate(targetFac.Name, BountyRules.TechLevelLabel(targetTech))));
                        return;
                    }
                    int prepay = BountyRules.PrepayForTech(targetTech);
                    bool overTech = targetTech > myTech;
                    if (overTech)
                        prepay = Mathf.RoundToInt(prepay * BountyRules.AboveTechCostFactor);
                    List<Thing> silvers = Utils.AllPlayerSilver();
                    int have = silvers.Sum(t => t.stackCount);
                    if (have < prepay)
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                            "RK_Bounty.NeedPrepay".Translate(prepay, have)));
                        return;
                    }
                    w.Pay(silvers, prepay);
                    Messages.Message(overTech
                        ? "RK_Bounty.PrepayPaidOver".Translate(prepay)
                        : "RK_Bounty.PrepayPaid".Translate(prepay),
                        MessageTypeDefOf.NeutralEvent, false);
                }
            }

            // 非敌对目标二次确认(SW 原版行为,OnCreate 钩子 AngerOnIssue 会代替固定 -80 激怒)
            if (w is Warrant_Pawn wp2 && wp2.Pawn != null && wp2.Pawn.Faction != null
                && wp2.Pawn.Faction != Faction.OfPlayer && !FactionUtility.HostileTo(wp2.Pawn.Faction, Faction.OfPlayer))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "SW.ConfirmationPrompt".Translate(wp2.Pawn.Named("PAWN"), wp2.Pawn.Faction.Name),
                    "Confirm".Translate(),
                    delegate
                    {
                        CommitWarrant(w);
                    }));
                return;
            }

            CommitWarrant(w);
        }

        private void CommitWarrant(Warrant w)
        {
            w.OnCreate();
            WarrantsManager.Instance.createdWarrants.Add(w);
            Close();
        }

        // 仿 SW MainTabWindow_Warrants.CreateWarrant + CreatePawnWarrant + CreateArtifactWarrant 流程
        private Warrant TryCreateWarrant(out string failReason)
        {
            failReason = "";
            if (WarrantsManager.Instance == null)
            {
                failReason = "RK_Bounty.NoGame".Translate();
                return null;
            }
            if (WarrantsManager.Instance.createdWarrants.Count >= 10)
            {
                failReason = "SW.TooManyPlayerWarrants".Translate(10);
                return null;
            }
            switch (curType)
            {
                case TargetType.Human:
                case TargetType.Animal:
                    return CreatePawnWarrant(ref failReason);
                case TargetType.Artifact:
                    return CreateArtifactWarrant(ref failReason);
                default:
                    failReason = "RK_Bounty.SelectTargetFirst".Translate();
                    return null;
            }
        }

        private Warrant CreatePawnWarrant(ref string failReason)
        {
            Pawn pawn = curType == TargetType.Human ? curPawn : curAnimal;
            if (pawn == null)
            {
                failReason = "RK_Bounty.SelectPawnFirst".Translate();
                return null;
            }
            var wp = new Warrant_Pawn
            {
                loadID = WarrantsManager.Instance.GetWarrantID(),
                issuer = Faction.OfPlayer,
                createdTick = Find.TickManager.TicksGame
            };
            wp.thing = pawn;
            wp.rewardForLiving = curCapturePayment;
            wp.rewardForDead = curDeathPayment;
            wp.reason = curReason ?? "";
            if (!curMessage.NullOrEmpty())
                wp.message = curMessage;
            else
                wp.message = Utils.GenerateTextFromRule(SW_DefOf.SW_Messages);

            if (deathPaymentEnabled && curDeathPayment <= 0)
                failReason = "SW.YouMustFillAmountForDeadReward".Translate();
            else if (capturePaymentEnabled && curCapturePayment <= 0)
                failReason = "SW.YouMustFillAmountForCaptureReward".Translate();

            return wp;
        }

        private Warrant CreateArtifactWarrant(ref string failReason)
        {
            if (curArtifact == null)
            {
                failReason = "RK_Bounty.SelectArtifactFirst".Translate();
                return null;
            }
            var wa = new Warrant_Artifact
            {
                loadID = WarrantsManager.Instance.GetWarrantID(),
                issuer = Faction.OfPlayer,
                createdTick = Find.TickManager.TicksGame,
                thing = curArtifact,
                reward = curReward,
                message = Utils.GenerateTextFromRule(SW_DefOf.SW_Messages)
            };
            if (curReward <= 0)
                failReason = "SW.YouMustFillAmountForReward".Translate();
            return wa;
        }

        private static string GetLabel(TargetType t)
        {
            return t switch
            {
                TargetType.Human => "SW.Pawn".Translate(),
                TargetType.Animal => "SW.Animal".Translate(),
                TargetType.Artifact => "SW.Artifact".Translate(),
                _ => "?"
            };
        }

        private static bool SimpleWarrantsActive()
        {
            return DefDatabase<MainButtonDef>.GetNamedSilentFail("SW_Warrants") != null;
        }

        private static Color DimmedTextColor() => ColoredText.SubtleGrayColor;
    }
}
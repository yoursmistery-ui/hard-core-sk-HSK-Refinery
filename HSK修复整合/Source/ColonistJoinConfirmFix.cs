using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ColonistJoinConfirmHSK
{
    // 殖民地"静默加人"确认守门。
    //
    // 根因(反编译确认): HSK 环境里多条路径把外来 pawn 直接改成玩家阵营,全程不留历史、不发信:
    //   - Core_SK.dll  SK.IncidentWorker_RefugeeChased.CreateAcceptOption -> refugee.SetFaction(Faction.OfPlayer)
    //   - NewRatkin.dll Dialog_CaravanSettlers.AcceptPawn/AcceptAllSettlers -> SetFaction(Faction.OfPlayer, null)
    //   - 美狐HSK拓展   IncidentWorker_SeasonalSantaDrop -> GeneratePawn(kind, Faction.OfPlayer)
    // 这些都不走原版入伙流程,所以殖民者名单里"一瞬间"多个人,右下角最多一条飘字。
    //
    // 本补丁: postfix 挂在 Verse.Pawn.SetFaction(Faction, Pawn) 上,凡是"外来阵营 -> 玩家阵营"
    //   且不是玩家主动操作(无 recruiter、非囚犯/奴隶转正、不在白名单)的转换,补发一封抉择信:
    //   [收留] 保持现状 / [遣返] 退回原阵营并送回大地图旅行队。
    //
    // 为什么用"事后可否决"而不是"加入前拦截": 待确认者若保持原阵营(如被追捕的难民其原阵营
    //   与玩家敌对),会当场以敌对身份站在殖民地里开火,同时各 mod 的事件流程假设 SetFaction
    //   已成功,跳过原方法会留下半截状态。故放行入伙、由玩家在信上否决。
    [StaticConstructorOnStartup]
    public static class ColonistJoinConfirmFix
    {
        // 玩家已在自己 UI 上确认过的渠道,不再二次弹信(按 kindDef.defName 前缀匹配)。
        private static readonly string[] WhitelistKindPrefixes = new string[]
        {
            "RK_PawnKind_",            // 鼠族流浪商队定居者:游商对话里点"提议/全部提议"招募
            "Miho_Seasonal_SantaGift"  // 天降美狐:圣诞空投
        };

        private const string JoinConfirmLetterDef = "HSK_Letter_JoinConfirm";

        // 遣返时自己调用 SetFaction 的放行闸门,避免递归发信。
        internal static bool bypass;

        // 已发过信待处理的 pawn,防同一人反复转正刷屏。
        private static readonly HashSet<int> pending = new HashSet<int>();

        static ColonistJoinConfirmFix()
        {
            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(Pawn), "SetFaction",
                    new Type[] { typeof(Faction), typeof(Pawn) });
                if (target == null)
                {
                    return;
                }
                Type t = typeof(ColonistJoinConfirmFix);
                Harmony harmony = new Harmony("local.hskfixpack.colonistjoinconfirm");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(t.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)),
                    postfix: new HarmonyMethod(t.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception)
            {
            }
        }

        // 转换前的状态只有这里能看到(原方法会清掉 guest 状态并改写阵营),
        // 所以判定全在 prefix 做,postfix 只负责发信。
        private static void Prefix(Pawn __instance, Faction newFaction, Pawn recruiter, out Faction __state)
        {
            __state = null;
            if (bypass)
            {
                return;
            }
            if (newFaction == null || !newFaction.IsPlayer)
            {
                return;
            }
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            Pawn p = __instance;
            if ((object)p == null || p.RaceProps == null || !p.RaceProps.Humanlike)
            {
                return;
            }
            if (recruiter != null)
            {
                return; // 有招募者 = 玩家在招募/谈判界面主动操作
            }
            Faction origin = p.Faction;
            if (origin == null || origin.IsPlayer)
            {
                return; // 无阵营野生 / 本来就是殖民者
            }
            if (p.IsPrisonerOfColony || p.IsSlaveOfColony)
            {
                return; // 囚犯招募、奴隶买断等既有流程
            }
            if (p.kindDef == null || p.kindDef.defName == null)
            {
                return;
            }
            for (int i = 0; i < WhitelistKindPrefixes.Length; i++)
            {
                if (p.kindDef.defName.StartsWith(WhitelistKindPrefixes[i]))
                {
                    return;
                }
            }
            lock (pending)
            {
                if (pending.Contains(p.thingIDNumber))
                {
                    return;
                }
                pending.Add(p.thingIDNumber);
            }
            __state = origin;
        }

        private static void Postfix(Pawn __instance, Faction __state)
        {
            if (__state == null)
            {
                return;
            }
            try
            {
                TrySendConfirmLetter(__instance, __state);
            }
            catch (Exception)
            {
            }
        }

        private static void TrySendConfirmLetter(Pawn pawn, Faction origin)
        {
            LetterDef def = DefDatabase<LetterDef>.GetNamedSilentFail(JoinConfirmLetterDef);
            if (def == null || def.letterClass != typeof(ChoiceLetter_ColonistJoinConfirm))
            {
                Release(pawn);
                return;
            }
            ChoiceLetter_ColonistJoinConfirm letter =
                LetterMaker.MakeLetter(
                    "HSK_JoinConfirm_Label".Translate(pawn.LabelShort, origin.Name),
                    "HSK_JoinConfirm_Text".Translate(
                        pawn.LabelShort, origin.Name, pawn.kindDef.label, pawn.def.LabelCap),
                    def,
                    new LookTargets(pawn),
                    origin) as ChoiceLetter_ColonistJoinConfirm;
            if (letter == null)
            {
                Release(pawn);
                return;
            }
            letter.joiner = pawn;
            letter.originFaction = origin;
            Find.LetterStack.ReceiveLetter(letter, null, 0, true);
        }

        internal static void Release(Pawn pawn)
        {
            if ((object)pawn == null)
            {
                return;
            }
            lock (pending)
            {
                pending.Remove(pawn.thingIDNumber);
            }
        }
    }

    public class ChoiceLetter_ColonistJoinConfirm : ChoiceLetter
    {
        public Pawn joiner;

        public Faction originFaction;

        public override bool CanDismissWithRightClick
        {
            get { return false; }
        }

        public override bool CanShowInLetterStack
        {
            get
            {
                return base.CanShowInLetterStack
                    && (object)joiner != null
                    && !joiner.Dead
                    && joiner.Faction == Faction.OfPlayer;
            }
        }

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                if (base.ArchivedOnly)
                {
                    yield return base.Option_Close;
                    yield break;
                }
                DiaOption keep = new DiaOption("HSK_JoinConfirm_Keep".Translate());
                keep.action = delegate { Accept(); };
                keep.resolveTree = true;
                DiaOption sendBack = new DiaOption("HSK_JoinConfirm_SendBack".Translate());
                sendBack.action = delegate { SendBack(); };
                sendBack.resolveTree = true;
                if ((object)joiner == null || joiner.Dead)
                {
                    keep.Disable("HSK_JoinConfirm_Gone".Translate());
                    sendBack.Disable("HSK_JoinConfirm_Gone".Translate());
                }
                yield return keep;
                yield return sendBack;
                if (lookTargets.IsValid())
                {
                    yield return base.Option_JumpToLocationAndPostpone;
                }
                yield return base.Option_Postpone;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref joiner, "joiner");
            Scribe_References.Look(ref originFaction, "originFaction");
        }

        private void Accept()
        {
            ColonistJoinConfirmFix.Release(joiner);
            Find.LetterStack.RemoveLetter(this);
        }

        private void SendBack()
        {
            Pawn pawn = joiner;
            Faction origin = originFaction;
            ColonistJoinConfirmFix.Release(pawn);
            if ((object)pawn != null && !pawn.Dead && !pawn.Destroyed && origin != null)
            {
                ColonistJoinConfirmFix.bypass = true;
                try
                {
                    if (pawn.Spawned)
                    {
                        pawn.DeSpawn();
                    }
                    pawn.SetFaction(origin, null);
                    Find.WorldPawns.PassToWorld(pawn);
                }
                finally
                {
                    ColonistJoinConfirmFix.bypass = false;
                }
            }
            Find.LetterStack.RemoveLetter(this);
        }
    }
}

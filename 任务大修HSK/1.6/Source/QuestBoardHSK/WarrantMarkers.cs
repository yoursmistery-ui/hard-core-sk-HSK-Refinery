using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 被通缉 pawn 表现增强(2026-09-02 通缉扩展):
    /// ①名字后缀「〔通缉中 赏金N〕」——Harmony postfix Pawn.LabelShort/LabelShortCap,
    ///   HashSet&lt;int&gt; 门控+每 pawn 缓存后缀串;集合按 600 tick 低频重建(通缉增删最迟 10 秒生效),
    ///   未挂赏 pawn 仅花一次 Contains(int),不做每帧翻译/拼接(性能铁律 §9);
    /// ②选中信息卡追加一行通缉信息(仅选中时调用,天然低频)。
    /// </summary>
    [HarmonyPatch]
    internal static class WarrantMarkers
    {
        private static readonly HashSet<int> wantedIds = new HashSet<int>();
        private static readonly Dictionary<int, string> suffixCache = new Dictionary<int, string>();
        private static int lastRebuildTick = -1;

        private static void EnsureFresh()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (lastRebuildTick >= 0 && now - lastRebuildTick < 600)
                return;
            lastRebuildTick = now;
            wantedIds.Clear();
            suffixCache.Clear();
            WarrantsManager mgr = WarrantsManager.Instance;
            if (mgr == null)
                return;
            Collect(mgr.createdWarrants);
            Collect(mgr.availableWarrants);
            Collect(mgr.takenWarrants);
            BountyRadioManager radio = BountyRadioManager.Get();
            if (radio != null)
                Collect(radio.pending);
        }

        private static void Collect(List<Warrant> warrants)
        {
            if (warrants == null)
                return;
            foreach (Warrant w in warrants)
            {
                if (!(w is Warrant_Pawn wp) || wp.Pawn == null || wp.Pawn.Dead)
                    continue;
                if (wantedIds.Add(wp.Pawn.thingIDNumber))
                    suffixCache[wp.Pawn.thingIDNumber] = BuildSuffix(wp);
            }
        }

        private static string BuildSuffix(Warrant_Pawn wp)
        {
            int reward = Mathf.Max(wp.rewardForLiving, wp.rewardForDead);
            return reward > 0
                ? "RK_Bounty.WantedSuffix".Translate(reward).ToString()
                : "RK_Bounty.WantedSuffixPlain".Translate().ToString();
        }

        private static bool TrySuffix(Pawn pawn, ref string result)
        {
            EnsureFresh();
            if (!wantedIds.Contains(pawn.thingIDNumber))
                return false;
            if (!suffixCache.TryGetValue(pawn.thingIDNumber, out string sfx) || sfx.NullOrEmpty())
                return false;
            if (result.Contains(sfx))
                return false;   // LabelShortCap 内部再调 LabelShort 时防双重后缀
            result = result + sfx;
            return true;
        }

        [HarmonyPatch(typeof(Pawn), "get_LabelShort")]
        [HarmonyPostfix]
        private static void LabelShort_Postfix(Pawn __instance, ref string __result)
        {
            TrySuffix(__instance, ref __result);
        }

        // 1.6 删除了 LabelShortCap,只挂 get_LabelShort 即可覆盖两类显示

        // 选中信息卡:仅在选中时由 InspectGizmoGrid 每帧调用,门控同上
        [HarmonyPatch(typeof(Pawn), "GetInspectString")]
        [HarmonyPostfix]
        private static void GetInspectString_Postfix(Pawn __instance, ref string __result)
        {
            EnsureFresh();
            if (!wantedIds.Contains(__instance.thingIDNumber))
                return;
            Warrant_Pawn wp = FindWarrant(__instance);
            if (wp == null)
                return;
            int days = BountyRadioManager.AvailableDaysLeft(wp);
            string line = days >= 0
                ? "RK_Bounty.InspectWanted".Translate(Mathf.Max(wp.rewardForLiving, wp.rewardForDead), days)
                : "RK_Bounty.InspectWantedNoExpire".Translate(Mathf.Max(wp.rewardForLiving, wp.rewardForDead));
            __result = __result.NullOrEmpty() ? line : __result + "\n" + line;
        }

        // 玩家发布的单优先(受雇进度/赏金对玩家最相关),无则取任意一条在案通缉
        private static Warrant_Pawn FindWarrant(Pawn pawn)
        {
            WarrantsManager mgr = WarrantsManager.Instance;
            if (mgr == null)
                return null;
            Warrant_Pawn any = null;
            foreach (List<Warrant> pool in new[] { mgr.createdWarrants, mgr.takenWarrants, mgr.availableWarrants })
            {
                if (pool == null)
                    continue;
                foreach (Warrant w in pool)
                {
                    if (!(w is Warrant_Pawn wp) || wp.Pawn != pawn)
                        continue;
                    if (wp.issuer == Faction.OfPlayer)
                        return wp;
                    any = any ?? wp;
                }
            }
            return any;
        }
    }
}

using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using SimpleWarrants;

namespace QuestBoardHSK
{
    /// <summary>
    /// 悬赏实物奖励(2026-08-30 v5):银赏金保留(SW 原逻辑),额外发放实物包。
    /// 以 warrant.loadID 做种子确定性生成——海报展示与商队/运输舱实发完全一致,
    /// 存档重开/换机结果相同,无需任何持久化。对应 Defs/ThingSetMakerDefs/RK_悬赏实物奖励池.xml。
    /// </summary>
    internal static class BountyRewards
    {
        internal struct RewardEntry
        {
            public ThingDef def;
            public ThingDef stuff;
            public int count;
            public QualityCategory quality;
            public float value;
        }

        // 赏金低于此值不加成(避免低价值悬赏发一堆破烂)
        private const float MinRewardValue = 300f;

        private const string PoolIngots = "RK_BountyReward_Ingots";
        private const string PoolArms = "RK_BountyReward_Arms";
        private const string PoolGear = "RK_BountyReward_Gear";
        private const string PoolSundries = "RK_BountyReward_Sundries";

        // 仅 UI 缓存:弹窗打开时清空(loadID 在不同存档会重号,不能跨局复用);
        // 发放路径不读缓存,靠种子重算保证与展示一致。
        private static readonly Dictionary<string, List<RewardEntry>> uiCache = new Dictionary<string, List<RewardEntry>>();

        internal static void ClearCache()
        {
            uiCache.Clear();
        }

        // 分档:<300 不加成;300~800 → 35%;800~1500 → 40%;≥1500 → 50%
        internal static float BonusFraction(float reward)
        {
            if (reward < MinRewardValue)
                return 0f;
            if (reward < 800f)
                return 0.35f;
            if (reward < 1500f)
                return 0.40f;
            return 0.50f;
        }

        // FNV-1a:跨进程/跨机稳定(string.GetHashCode 在部分运行时会随机化,不可用作存档相关种子)
        private static int SeedFor(Warrant w)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in w.loadID ?? "0")
                {
                    h ^= c;
                    h *= 16777619u;
                }
                return (int)h;
            }
        }

        private static (string pool, float share)[] MixFor(Warrant w)
        {
            if (w is Warrant_TameAnimal || w is Warrant_Artifact)
                return new[] { (PoolIngots, 0.40f), (PoolGear, 0.30f), (PoolSundries, 0.30f) };
            return new[] { (PoolIngots, 0.30f), (PoolArms, 0.35f), (PoolGear, 0.20f), (PoolSundries, 0.15f) };
        }

        /// <summary>实物包(UI 展示与发放共用);无加成返回 null。</summary>
        internal static List<RewardEntry> GetPackage(Warrant w, bool useCache)
        {
            float frac = BonusFraction(w.MaxRewardValue());
            if (frac <= 0f)
                return null;
            if (useCache && uiCache.TryGetValue(w.loadID, out var hit))
                return hit;

            float budget = w.MaxRewardValue() * frac;
            int seed = SeedFor(w);
            var entries = new List<RewardEntry>();
            (string, float)[] mix = MixFor(w);
            for (int i = 0; i < mix.Length; i++)
            {
                (string poolName, float share) = mix[i];
                float part = budget * share;
                if (part < 60f)
                    continue;
                ThingSetMakerDef tsm = DefDatabase<ThingSetMakerDef>.GetNamedSilentFail(poolName);
                if (tsm == null)
                    continue;
                var parms = new ThingSetMakerParams
                {
                    totalMarketValueRange = new FloatRange(part * 0.8f, part * 1.2f)
                };
                List<Thing> things;
                Rand.PushState(seed + i * 397);
                try
                {
                    things = tsm.root.Generate(parms);
                }
                finally
                {
                    Rand.PopState();
                }
                if (things == null)
                    continue;
                foreach (Thing t in things)
                {
                    if (t == null || t.def == null || t.def == ThingDefOf.Silver)
                        continue;
                    var q = t.TryGetComp<CompQuality>();
                    entries.Add(new RewardEntry
                    {
                        def = t.def,
                        stuff = t.Stuff,
                        count = t.stackCount,
                        quality = q?.Quality ?? QualityCategory.Normal,
                        value = t.MarketValue * t.stackCount
                    });
                }
            }
            if (useCache)
                uiCache[w.loadID] = entries;
            return entries;
        }

        /// <summary>实物包总件数(列表行角标用),无加成为 0。</summary>
        internal static int BonusCount(Warrant w)
        {
            List<RewardEntry> pkg = GetPackage(w, useCache: true);
            return pkg?.Count ?? 0;
        }

        internal static List<Thing> MakeThings(List<RewardEntry> entries)
        {
            var result = new List<Thing>();
            foreach (var e in entries)
            {
                int remaining = e.count;
                while (remaining > 0 && e.def.stackLimit > 0)
                {
                    Thing t = ThingMaker.MakeThing(e.def, e.stuff);
                    if (t == null)
                        break;
                    int take = Math.Min(remaining, e.def.stackLimit);
                    t.stackCount = take;
                    var q = t.TryGetComp<CompQuality>();
                    if (q != null)
                        q.SetQuality(e.quality, null);
                    result.Add(t);
                    remaining -= take;
                }
            }
            return result;
        }

        /// <summary>商队交付:实物逐件入队(SW 原生 GiveThing,自动进殖民者背包)。</summary>
        internal static void DeliverBonus(Warrant w, Caravan caravan)
        {
            if (caravan == null)
                return;
            List<RewardEntry> pkg = GetPackage(w, useCache: false);
            if (pkg == null || pkg.Count == 0)
                return;
            foreach (Thing t in MakeThings(pkg))
                Warrant.GiveThing(caravan, t);
        }

        /// <summary>运输舱交付:与 SW 掉银同款落点,实物掉在玩家主图交易点附近。</summary>
        internal static void DropBonusNearPlayer(Warrant w)
        {
            List<RewardEntry> pkg = GetPackage(w, useCache: false);
            if (pkg == null || pkg.Count == 0)
                return;
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
                return;
            DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(map), map,
                MakeThings(pkg), 110, false, false, true, true, true, null);
        }

        /// <summary>SW 上游 bug 修复:驯服委托商队交付不发银(基类无发放逻辑),此处按 Reward 补发。</summary>
        internal static void FixTameSilver(Warrant w, Caravan caravan)
        {
            if (caravan == null || !(w is Warrant_TameAnimal tame) || tame.Reward <= 0)
                return;
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = tame.Reward;
            Warrant.GiveThing(caravan, silver);
        }
    }
}

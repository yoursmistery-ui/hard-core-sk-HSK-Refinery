using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestBoardHSK
{
    /// <summary>
    /// 任务分层(一等 Def)。rank 1–4 = 通缉/委托/战争委任的档(按目标科技档落位)，rank 5 = 天网终章 boss(非通缉，走专章战令)。
    /// 奖励/合法性/UI 徽章读此表；缺表时 BountyRules 回退内置常量。
    /// </summary>
    public class TaskTierDef : Def
    {
        public int rank = 1;                       // 1..5
        public TechLevel minTech = TechLevel.Neolithic;
        public TechLevel maxTech = TechLevel.Archotech;
        public int minPoints = 0;
        public int maxPoints = 999999;
        public float rewardMult = 1f;
        public int failGoodwill = 0;               // 失败好感变化(暗杀类=0：不扣)
        public string badgeColor = "#c8a24a";      // UI 徽章色 hex
        public string summary;                     // 展示一句话(可 DefInjected)

        public bool Covers(TechLevel t) { return t >= minTech && t <= maxTech; }
    }

    /// <summary>任务分层表访问器（纯查表，无副作用；缺表返回哨兵由调用方回退）。</summary>
    [StaticConstructorOnStartup]
    internal static class TaskTiers
    {
        private static List<TaskTierDef> all;
        private static bool inited;

        private static void Ensure()
        {
            if (inited) return;
            inited = true;
            all = DefDatabase<TaskTierDef>.AllDefsListForReading;
        }

        /// <summary>按目标科技档找 rank(仅 1–4，5=boss 不接普通通缉)；无匹配/无表 → 0 哨兵。</summary>
        public static int RankForTech(TechLevel t)
        {
            Ensure();
            if (all == null) return 0;
            for (int i = 0; i < all.Count; i++)
            {
                TaskTierDef d = all[i];
                if (d.rank >= 5) continue;
                if (d.Covers(t)) return d.rank;
            }
            return 0;
        }

        public static TaskTierDef ByRank(int rank)
        {
            Ensure();
            if (all == null) return null;
            for (int i = 0; i < all.Count; i++)
                if (all[i].rank == rank) return all[i];
            return null;
        }

        /// <summary>该 rank 的奖励倍率；无表/无此 rank → 返回哨兵 -1（调用方回退）。</summary>
        public static float RewardMultOrSentinel(int rank)
        {
            TaskTierDef d = ByRank(rank);
            return d != null ? d.rewardMult : -1f;
        }

        /// <summary>#rrggbb hex（供 RichText `<color=...>` 用）；缺表 → 金 #c8a24a。</summary>
        public static string BadgeHex(int rank)
        {
            TaskTierDef d = ByRank(rank);
            if (d == null || d.badgeColor.NullOrEmpty()) return "#c8a24a";
            string s = d.badgeColor.TrimStart('#');
            return "#" + (s.Length >= 6 ? s.Substring(0, 6) : "c8a24a");
        }

        /// <summary>徽章色（表内 hex；缺表/解析失败 → 金 #c8a24a）。</summary>
        public static Color BadgeColor(int rank)
        {
            TaskTierDef d = ByRank(rank);
            return d != null ? HexToColor(d.badgeColor, new Color(0.78f, 0.64f, 0.29f)) : new Color(0.78f, 0.64f, 0.29f);
        }

        /// <summary>#rrggbb / #rrggbbaa 解析，失败回退 def。</summary>
        public static Color HexToColor(string hex, Color fallback)
        {
            try
            {
                if (hex.NullOrEmpty()) return fallback;
                string s = hex.TrimStart('#');
                if (s.Length != 6 && s.Length != 8) return fallback;
                int r = System.Convert.ToInt32(s.Substring(0, 2), 16);
                int g = System.Convert.ToInt32(s.Substring(2, 2), 16);
                int b = System.Convert.ToInt32(s.Substring(4, 2), 16);
                int a = s.Length == 8 ? System.Convert.ToInt32(s.Substring(6, 2), 16) : 255;
                return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            }
            catch { return fallback; }
        }
    }
}

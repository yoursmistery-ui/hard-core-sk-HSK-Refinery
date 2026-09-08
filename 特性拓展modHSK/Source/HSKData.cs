// 特性拓展modHSK —— 数据表(v5): 技能域映射 + 行为规则表(手写) + 跨 mod 语义冲突对
// v5 (2026-09-05) 用户定案: "条件和文案要自己写, 还要多加一点特性生成的逻辑"。
//   · 行为规则表 19 条全部手写(一条计数器只对应一条因果, 沿用 v2 修正原则):
//     达标后消费本次资格掷一次骰, 通过才授予; 未开窗(门控未过)不消费计数。
//   · 特性生成双路径:
//     A 行为沉淀(本表, Harmony 挂 EndCurrentJob/Kill/TryStartMentalState, 低频判定);
//     B 技能提炼(每 checkIntervalDays 掷骰, 特性池=环境内全量 TraitDef, commonality 加权+技能耦合)。
//   · 负向维度只有两个真正度量负面行为的计数器: 压力崩溃→厌世, 屠戮伴侣动物→仇兽者。
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HSKTraitExt
{
    public static class HSKData
    {
        // 技能 → 人格域
        public static string DomainOf(string skillDefName)
        {
            if (skillDefName == null) return "Life";
            switch (skillDefName)
            {
                case "Shooting": case "Melee": return "Combat";
                case "Social": case "Animals": return "Social";
                case "Medical": case "Cooking": case "Intellectual": case "Crafting": return "Work";
                case "Construction": case "Growing": case "Artistic": return "Work";
                case "Mining": return "Work";
                default: return "Life";
            }
        }

        // HSK_* 引擎专属特性(commonality=0, 无 skillGains)的域提示
        public static string DomainHint(string traitDefName)
        {
            if (traitDefName == null) return null;
            switch (traitDefName)
            {
                case "HSK_GymRat": return "Combat";
                case "HSK_Musician": return "Social";
                case "HSK_Tactician": return "Combat";
                case "HSK_Bookworm": return "Work";
                case "HSK_HealthNut": return "Work";
                case "HSK_Glutton": return "Work";
                case "HSK_Meditator": return "Life";
                case "HSK_NightOwl": return "Life";
                case "HSK_EarlyRiser": return "Life";
                default: return null;
            }
        }

        // ---- 行为沉淀规则(手写) ----
        // PosTrait/NegTrait 缺失时该维度静默跳过(GetNamedSilentFail 找不到也跳)。
        public struct LifestyleRule
        {
            public string Key;           // 维度名(同时是文案 slug 键)
            public string TriggerType;   // Job/WorkType/JoyKind/Plant/Drug/Night/Dawn/TempHot/TempCold/AbuseAnimal/MoodHigh/Kill/MentalBreak
            public string TriggerTarget; // jobDef/joyKind/毒品种类
            public int PosThreshold;
            public string PosTrait;
            public int PosDegree;
            public int NegThreshold;
            public string NegTrait;
            public int NegDegree;
            public float Chance;
        }

        public static readonly LifestyleRule[] LifestyleRules = new LifestyleRule[19]
        {
            // —— 作息 ——
            new LifestyleRule{ Key="作息-夜行", TriggerType="Night", TriggerTarget="", PosThreshold=240, PosTrait="HSK_NightOwl", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            new LifestyleRule{ Key="作息-晨行", TriggerType="Dawn", TriggerTarget="", PosThreshold=240, PosTrait="HSK_EarlyRiser", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            // —— 温度 ——
            new LifestyleRule{ Key="温度-耐热", TriggerType="TempHot", TriggerTarget="", PosThreshold=160, PosTrait="VTE_HeatInclined", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            new LifestyleRule{ Key="温度-耐寒", TriggerType="TempCold", TriggerTarget="", PosThreshold=160, PosTrait="VTE_ColdInclined", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            // —— 生产行为 ——
            new LifestyleRule{ Key="田间劳作", TriggerType="Plant", TriggerTarget="", PosThreshold=160, PosTrait="VTE_Groundbreaker", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            new LifestyleRule{ Key="修造不休", TriggerType="WorkType", TriggerTarget="Construction", PosThreshold=240, PosTrait="VTE_Workaholic", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            new LifestyleRule{ Key="医者仁心", TriggerType="Job", TriggerTarget="Doctor", PosThreshold=120, PosTrait="Medic", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            new LifestyleRule{ Key="烹调讲究", TriggerType="Job", TriggerTarget="CookMeal", PosThreshold=240, PosTrait="VTE_RefinedPalate", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.04f },
            // —— 社交/动物/战斗 ——
            new LifestyleRule{ Key="买卖周旋", TriggerType="Job", TriggerTarget="TradeWithPawn", PosThreshold=60, PosTrait="Trader", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            new LifestyleRule{ Key="驯养默契", TriggerType="Job", TriggerTarget="Tame", PosThreshold=60, PosTrait="VTE_AnimalLover", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            new LifestyleRule{ Key="嗜血", TriggerType="Kill", TriggerTarget="", PosThreshold=40, PosTrait="Bloodlust", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.03f },
            // —— 娱乐 ——
            new LifestyleRule{ Key="赢出来的算路", TriggerType="JoyKind", TriggerTarget="Gaming", PosThreshold=200, PosTrait="HSK_Tactician", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            new LifestyleRule{ Key="弦不离手", TriggerType="JoyKind", TriggerTarget="Music", PosThreshold=200, PosTrait="HSK_Musician", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.05f },
            // —— 心境 ——
            new LifestyleRule{ Key="心情高涨", TriggerType="MoodHigh", TriggerTarget="", PosThreshold=300, PosTrait="NaturalMood", PosDegree=2, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.03f },
            // —— 负向(真正的负向行为计数器) ——
            new LifestyleRule{ Key="压力崩溃", TriggerType="MentalBreak", TriggerTarget="", PosThreshold=0, PosTrait="", PosDegree=0, NegThreshold=48, NegTrait="VTE_WorldWeary", NegDegree=0, Chance=0.02f },
            new LifestyleRule{ Key="屠戮伴侣", TriggerType="AbuseAnimal", TriggerTarget="", PosThreshold=0, PosTrait="", PosDegree=0, NegThreshold=8, NegTrait="VTE_AnimalHater", NegDegree=0, Chance=0.05f },
            // —— 成瘾(消遣类摄入, 医用不算) ——
            new LifestyleRule{ Key="饮酒", TriggerType="Drug", TriggerTarget="Alcohol", PosThreshold=200, PosTrait="VTE_Lush", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.06f },
            new LifestyleRule{ Key="吸烟", TriggerType="Drug", TriggerTarget="Smokeleaf", PosThreshold=200, PosTrait="VTE_Stoner", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.06f },
            new LifestyleRule{ Key="药物滥用", TriggerType="Drug", TriggerTarget="Other", PosThreshold=200, PosTrait="DrugDesire", PosDegree=0, NegThreshold=0, NegTrait="", NegDegree=0, Chance=0.06f }
        };

        // 跨 mod 语义冲突对: 双向注入 conflictingTraits
        public static readonly string[][] ConflictPairs = new string[14][]
        {
            new string[2] { "Ascetic", "VTE_FunLoving" },
            new string[2] { "Bloodlust", "Kind" },
            new string[2] { "Butcher", "VTE_AnimalLover" },
            new string[2] { "Cannibal", "Kind" },
            new string[2] { "ColdLover", "VTE_HeatInclined" },
            new string[2] { "DeepSleeper", "QuickSleeper" },
            new string[2] { "DeepSleeper", "VTE_Insomniac" },
            new string[2] { "Extrovert", "Recluse" },
            new string[2] { "HeatLover", "VTE_ColdInclined" },
            new string[2] { "Kind", "VTE_MadSurgeon" },
            new string[2] { "Medic", "VTE_MadSurgeon" },
            new string[2] { "NaturalMood", "Neurotic" },
            new string[2] { "Perfectionist", "VTE_Slob" },
            new string[2] { "VTE_ColdInclined", "VTE_HeatInclined" }
        };
    }

    // 启动时把冲突对双向写进双方 TraitDef.conflictingTraits:
    // (1) 本引擎授予走 ConflictsWithAny 拦截; (2) 生成期 PawnGenerator 原生互斥检查也能拦到。
    [StaticConstructorOnStartup]
    public static class HSKConflictPump
    {
        static HSKConflictPump()
        {
            try
            {
                if (HSKData.ConflictPairs == null) return;
                int injected = 0;
                for (int i = 0; i < HSKData.ConflictPairs.Length; i++)
                {
                    string[] pair = HSKData.ConflictPairs[i];
                    if (pair == null || pair.Length < 2) continue;
                    if (Bind(pair[0], pair[1])) injected++;
                }
                Log.Message("[HSKTraitExt] 已注入语义冲突对 " + injected + " / " + HSKData.ConflictPairs.Length);
            }
            catch (Exception e)
            {
                Log.Error("[HSKTraitExt] 冲突注入失败: " + e);
            }
        }

        private static bool Bind(string a, string b)
        {
            TraitDef da = def(a); TraitDef db = def(b);
            if (da == null || db == null) return false;
            AddConflict(da, b);
            AddConflict(db, a);
            return true;
        }

        private static TraitDef def(string name)
        {
            try { return DefDatabase<TraitDef>.GetNamedSilentFail(name); }
            catch { return null; }
        }

        private static void AddConflict(TraitDef dst, string other)
        {
            if (dst.conflictingTraits == null) dst.conflictingTraits = new List<TraitDef>();
            for (int i = 0; i < dst.conflictingTraits.Count; i++)
                if (dst.conflictingTraits[i] != null && dst.conflictingTraits[i].defName == other) return;
            TraitDef od = def(other);
            if (od != null) dst.conflictingTraits.Add(od);
        }
    }
}

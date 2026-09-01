// 特性拓展modHSK —— mod 入口 + 设置 (v2)
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace HSKTraitExt
{
    public class HSKTraitMod : Mod
    {
        public static HSKTraitSetting settings;
        public HSKTraitMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<HSKTraitSetting>();
            HSKInit.ApplyHarmony();
        }
        public override string SettingsCategory() { return "特性拓展modHSK"; }
        public override void DoSettingsWindowContents(UnityEngine.Rect inRect) { settings.DoWindow(inRect); }
    }

    public class HSKTraitSetting : ModSettings
    {
        // v2 (2026-08-27) 节奏重做 —— 用户要求"这是附属玩法, 门槛要高、别一次性拿一堆":
        //   · 开局门  : 本局进行满 colonyStartGateDays(默认 730 天 = 2 年)后, 才做任何动态判定
        //   · 入队门  : 单个小人入队满 pawnJoinGateDays(默认 365 天)才有资格
        //   · 个人冷却: 获得一条后, 下次判定在 grantCooldownDays + rand(0~jitter) 之后
        //               (默认 365 + 0~183 = 1~1.5 年, 随机), 负面特性再乘 negCooldownScale
        //   · 年度配额: 全殖民地每年获得特性的人数由账本按人口算(<15人=1, 每多10人+1, 上限6)
        //   · 容量    : 单人动态特性总数 ≤ maxDynamicTraits(默认 2)
        // 阈值与概率的"抬高"落在 表/规则-生活习惯特性.csv(正向阈值约 ×2、负面特性概率 ×0.4);
        // 下面两个 scale 只是运行时的全局微调旋钮, 默认 1 表示按表生效。
        public int colonyStartGateDays = 730;      // 开局满多少天才开始判定(2 年)
        public int pawnJoinGateDays    = 365;      // 入队满多少天才算数
        public int grantCooldownDays   = 365;      // 个人获得间隔基准(1 年)
        public int grantCooldownJitterDays = 183;  // 个人获得间隔随机上沿(0.5 年) → 实际 1~1.5 年
        public float negCooldownScale  = 1.5f;     // 负面特性冷却倍率
        public int revokeIntervalDays  = 1095;     // 性格漂移最小间隔(3 年)
        public int maxDynamicTraits    = 2;        // 单人动态特性容量
        public float thresholdScale    = 1.0f;     // 全局阈值倍率(>1 更难)
        public float grantChanceScale  = 1.0f;     // 全局授予概率倍率(<1 更难)
        public float negChanceScale    = 0.4f;     // 负面特性概率再乘系数
        public float backgroundGrantChance = 0.25f;// 机制一: 事件窗口内授予成功率
        public int drugAbstinenceDays  = 730;      // 禁毒X天: 满 X 天无摄入则移除成瘾类动态特性
        public bool enableMechanismOne = true;     // 机制一(背景强关联事件)
        public bool enableMechanismTwo = true;     // 机制二(生活习惯计数)
        public bool enableTraitDrift   = true;     // 性格漂移(动态特性会退场)
        public bool enableBackstoryPairGuard = true; // 背景配对技能一致性(生成时)
        // 三种毒品分别定制(设置优先, 运行时覆盖规则表CSV)。v2 默认与表一致: 200 次 / 6%
        public int drugAlcoholThreshold = 200;
        public float drugAlcoholChance = 0.06f;
        public int drugSmokeleafThreshold = 200;
        public float drugSmokeleafChance = 0.06f;
        public int drugHardThreshold = 200;
        public float drugHardChance = 0.06f;

        // 键名统一带 V3 后缀: 顶掉旧存档里"机制已关闭"的历史值, 让 v2 新默认值生效。
        public override void ExposeData()
        {
            Scribe_Values.Look(ref colonyStartGateDays, "colonyStartGateDaysV3", 730);
            Scribe_Values.Look(ref pawnJoinGateDays, "pawnJoinGateDaysV3", 365);
            Scribe_Values.Look(ref grantCooldownDays, "grantCooldownDaysV3", 365);
            Scribe_Values.Look(ref grantCooldownJitterDays, "grantCooldownJitterDaysV3", 183);
            Scribe_Values.Look(ref negCooldownScale, "negCooldownScaleV3", 1.5f);
            Scribe_Values.Look(ref revokeIntervalDays, "revokeIntervalDaysV3", 1095);
            Scribe_Values.Look(ref maxDynamicTraits, "maxDynamicTraitsV3", 2);
            Scribe_Values.Look(ref thresholdScale, "thresholdScaleV3", 1.0f);
            Scribe_Values.Look(ref grantChanceScale, "grantChanceScaleV3", 1.0f);
            Scribe_Values.Look(ref negChanceScale, "negChanceScaleV3", 0.4f);
            Scribe_Values.Look(ref backgroundGrantChance, "backgroundGrantChanceV3", 0.25f);
            Scribe_Values.Look(ref drugAbstinenceDays, "drugAbstinenceDaysV3", 730);
            Scribe_Values.Look(ref enableMechanismOne, "enableMechanismOneV3", true);
            Scribe_Values.Look(ref enableMechanismTwo, "enableMechanismTwoV3", true);
            Scribe_Values.Look(ref enableTraitDrift, "enableTraitDriftV3", true);
            Scribe_Values.Look(ref enableBackstoryPairGuard, "enableBackstoryPairGuard", true);
            Scribe_Values.Look(ref drugAlcoholThreshold, "drugAlcoholThresholdV3", 200);
            Scribe_Values.Look(ref drugAlcoholChance, "drugAlcoholChanceV3", 0.06f);
            Scribe_Values.Look(ref drugSmokeleafThreshold, "drugSmokeleafThresholdV3", 200);
            Scribe_Values.Look(ref drugSmokeleafChance, "drugSmokeleafChanceV3", 0.06f);
            Scribe_Values.Look(ref drugHardThreshold, "drugHardThresholdV3", 200);
            Scribe_Values.Look(ref drugHardChance, "drugHardChanceV3", 0.06f);
        }

        private UnityEngine.Vector2 scrollPos = UnityEngine.Vector2.zero;

        public void DoWindow(UnityEngine.Rect inRect)
        {
            UnityEngine.Rect contentRect = new UnityEngine.Rect(0f, 0f, inRect.width - 24f, 1100f);
            Widgets.BeginScrollView(inRect, ref scrollPos, contentRect);
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(contentRect);
            ls.Label("—— 节奏门(决定“什么时候允许沉淀特性”) ——");
            ls.Label("开局满多少天才开始判定(天): " + colonyStartGateDays);
            colonyStartGateDays = (int)ls.Slider(colonyStartGateDays, 0f, 3650f);
            ls.Label("小人入队满多少天才算数(天): " + pawnJoinGateDays);
            pawnJoinGateDays = (int)ls.Slider(pawnJoinGateDays, 0f, 1825f);
            ls.Label("个人获得间隔基准(天): " + grantCooldownDays);
            grantCooldownDays = (int)ls.Slider(grantCooldownDays, 60f, 1825f);
            ls.Label("个人获得间隔随机上沿(天, 0=固定): " + grantCooldownJitterDays);
            grantCooldownJitterDays = (int)ls.Slider(grantCooldownJitterDays, 0f, 730f);
            ls.Label("单人动态特性容量: " + maxDynamicTraits);
            maxDynamicTraits = (int)ls.Slider(maxDynamicTraits, 1f, 6f);
            ls.Gap(6f);
            ls.Label("—— 难度旋钮(在规则表之上再乘) ——");
            ls.Label("全局阈值倍率(>1 更难攒够次数): " + thresholdScale.ToString("0.00"));
            thresholdScale = ls.Slider(thresholdScale, 0.5f, 5f);
            ls.Label("全局授予概率倍率(<1 更难命中): " + grantChanceScale.ToString("0.00"));
            grantChanceScale = ls.Slider(grantChanceScale, 0.1f, 3f);
            ls.Label("负面特性概率再乘系数: " + negChanceScale.ToString("0.00"));
            negChanceScale = ls.Slider(negChanceScale, 0.05f, 1f);
            ls.Label("负面特性冷却再乘系数: " + negCooldownScale.ToString("0.00"));
            negCooldownScale = ls.Slider(negCooldownScale, 1f, 4f);
            ls.Gap(6f);
            ls.Label("—— 退场机制 ——");
            ls.Label("性格漂移最小间隔(天): " + revokeIntervalDays);
            revokeIntervalDays = (int)ls.Slider(revokeIntervalDays, 365f, 4000f);
            ls.Label("禁毒多少天移除成瘾特性(天): " + drugAbstinenceDays);
            drugAbstinenceDays = (int)ls.Slider(drugAbstinenceDays, 365f, 3650f);
            ls.Gap(6f);
            ls.Label("—— 机制一(背景强关联·叙事者事件) ——");
            ls.Label("事件窗口内授予成功率: " + backgroundGrantChance.ToString("0.00"));
            backgroundGrantChance = ls.Slider(backgroundGrantChance, 0.05f, 1f);
            ls.Gap(6f);
            ls.CheckboxLabeled("启用 机制一(背景强关联事件)", ref enableMechanismOne);
            ls.CheckboxLabeled("启用 机制二(生活习惯计数)", ref enableMechanismTwo);
            ls.CheckboxLabeled("启用 性格漂移(动态特性会随生活改变而退场)", ref enableTraitDrift);
            ls.CheckboxLabeled("启用 背景配对技能一致性(生成时检查,幼年+→成年禁-2/-3等)", ref enableBackstoryPairGuard);
            ls.Gap(6f);
            ls.Label("—— 三种毒品分别定制(达标次数 / 授予概率) ——");
            ls.Label("饮酒(VTE_Lush) 达标次数: " + drugAlcoholThreshold);
            drugAlcoholThreshold = (int)ls.Slider(drugAlcoholThreshold, 20f, 1000f);
            ls.Label("饮酒 授予概率: " + drugAlcoholChance.ToString("0.000"));
            drugAlcoholChance = ls.Slider(drugAlcoholChance, 0.005f, 0.5f);
            ls.Label("吸烟(VTE_Stoner) 达标次数: " + drugSmokeleafThreshold);
            drugSmokeleafThreshold = (int)ls.Slider(drugSmokeleafThreshold, 20f, 1000f);
            ls.Label("吸烟 授予概率: " + drugSmokeleafChance.ToString("0.000"));
            drugSmokeleafChance = ls.Slider(drugSmokeleafChance, 0.005f, 0.5f);
            ls.Label("药物滥用(DrugDesire) 达标次数: " + drugHardThreshold);
            drugHardThreshold = (int)ls.Slider(drugHardThreshold, 20f, 1000f);
            ls.Label("药物滥用 授予概率: " + drugHardChance.ToString("0.000"));
            drugHardChance = ls.Slider(drugHardChance, 0.005f, 0.5f);
            ls.End();
            Widgets.EndScrollView();
        }
    }

    // Harmony 一次性注入
    [StaticConstructorOnStartup]
    public static class HSKInit
    {
        private static bool applied = false;
        public static void ApplyHarmony()
        {
            if (applied) return;
            applied = true;
            try
            {
                Harmony harmony = new Harmony("local.traitsextension.hsk");
                HSKBehaviorHook.Apply(harmony);
                HSKTraitConflictGuard.Apply(harmony);
                HSKBackstoryPairGuard.Apply(harmony);
                Log.Message("[HSKTraitExt] Harmony applied (v2 节奏门: 开局2年/年度配额/个人1~1.5年冷却)。");
            }
            catch (Exception e)
            {
                Log.Error("[HSKTraitExt] Harmony apply failed: " + e);
            }
        }
    }
}

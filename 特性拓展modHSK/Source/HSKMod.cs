// 特性拓展modHSK —— mod 入口 + 设置 (v5)
// v5 (2026-09-05): 双路径生成(行为沉淀+技能提炼) + 手写信件; 节奏旋钮 V5 键。
using System;
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
        // v5 节奏:
        //   · 开局门  : 满 colonyStartGateDays(730 天)后才开始判定
        //   · 行为沉淀: 达标(阈值×thresholdScale)后掷骰(Chance×grantChanceScale), 门控未开窗不消费计数
        //   · 技能提炼: 每 checkIntervalDays(45 天)掷一次 grantChancePerCheck(8%)
        //   · 年度上限: 全殖民地每年最多 colonyYearCap(2)人次
        //   · 个人冷却: 授予后 pawnCooldownDays + rand(0~jitter) 天(默认 365+0~183 = 1~1.5 年)
        //   · 容量    : 单人被授予特性 ≤ maxDynamicTraits(2)
        public bool enableGrowth = true;
        public bool enableBehavior = true;          // 路径A 行为沉淀
        public bool enableSkillPath = true;         // 路径B 技能提炼
        public int colonyStartGateDays = 730;
        public int pawnJoinGateDays = 365;
        public int checkIntervalDays = 45;
        public float skillYearlyChance = 0.05f;     // 路径B 每年总概率(按检查间隔折算成单次), 满级技能门槛
        public int colonyYearCap = 2;
        public int pawnCooldownDays = 365;
        public int pawnCooldownJitterDays = 183;
        public int maxDynamicTraits = 2;
        public float thresholdScale = 1.0f;         // 行为阈值倍率(>1 更难攒)
        public float grantChanceScale = 1.0f;       // 授予概率倍率(<1 更难命中)
        public bool enableBackstoryPairGuard = true;

        // 键名带 V5 后缀: 顶掉 v2/v3/v4 存档里的旧旋钮值。
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableGrowth, "enableGrowthV5", true);
            Scribe_Values.Look(ref enableBehavior, "enableBehaviorV5", true);
            Scribe_Values.Look(ref enableSkillPath, "enableSkillPathV5", true);
            Scribe_Values.Look(ref colonyStartGateDays, "colonyStartGateDaysV5", 730);
            Scribe_Values.Look(ref pawnJoinGateDays, "pawnJoinGateDaysV5", 365);
            Scribe_Values.Look(ref checkIntervalDays, "checkIntervalDaysV5", 45);
            Scribe_Values.Look(ref skillYearlyChance, "skillYearlyChanceV5", 0.05f);
            Scribe_Values.Look(ref colonyYearCap, "colonyYearCapV5", 2);
            Scribe_Values.Look(ref pawnCooldownDays, "pawnCooldownDaysV5", 365);
            Scribe_Values.Look(ref pawnCooldownJitterDays, "pawnCooldownJitterDaysV5", 183);
            Scribe_Values.Look(ref maxDynamicTraits, "maxDynamicTraitsV5", 2);
            Scribe_Values.Look(ref thresholdScale, "thresholdScaleV5", 1.0f);
            Scribe_Values.Look(ref grantChanceScale, "grantChanceScaleV5", 1.0f);
            Scribe_Values.Look(ref enableBackstoryPairGuard, "enableBackstoryPairGuard", true);
        }

        private UnityEngine.Vector2 scrollPos = UnityEngine.Vector2.zero;

        public void DoWindow(UnityEngine.Rect inRect)
        {
            UnityEngine.Rect contentRect = new UnityEngine.Rect(0f, 0f, inRect.width - 24f, 560f);
            Widgets.BeginScrollView(inRect, ref scrollPos, contentRect);
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(contentRect);
            ls.Label("—— 特性生成(双路径) ——");
            ls.CheckboxLabeled("启用 性格沉淀(总开关)", ref enableGrowth);
            ls.CheckboxLabeled("路径A 行为沉淀(收工/击杀/崩溃等行为计数→达标掷骰)", ref enableBehavior);
            ls.CheckboxLabeled("路径B 技能提炼(定期掷骰, 从全特性库按最强技能抽取)", ref enableSkillPath);
            ls.Label("开局满多少天才开始判定(天): " + colonyStartGateDays);
            colonyStartGateDays = (int)ls.Slider(colonyStartGateDays, 0f, 3650f);
            ls.Label("全殖民地每年最多几人获得特性: " + colonyYearCap);
            colonyYearCap = (int)ls.Slider(colonyYearCap, 0f, 6f);
            ls.Gap(6f);
            ls.Label("—— 路径B 技能提炼 ——");
            ls.Label("检查间隔(天): " + checkIntervalDays);
            checkIntervalDays = (int)ls.Slider(checkIntervalDays, 7f, 365f);
            ls.Label("每年授予概率(最强技能满级20才有资格): " + skillYearlyChance.ToString("0.00"));
            skillYearlyChance = ls.Slider(skillYearlyChance, 0.01f, 1f);
            ls.Gap(6f);
            ls.Label("—— 难度旋钮 ——");
            ls.Label("行为阈值倍率(>1 更难攒够次数): " + thresholdScale.ToString("0.00"));
            thresholdScale = ls.Slider(thresholdScale, 0.5f, 5f);
            ls.Label("授予概率倍率(<1 更难命中): " + grantChanceScale.ToString("0.00"));
            grantChanceScale = ls.Slider(grantChanceScale, 0.1f, 3f);
            ls.Gap(6f);
            ls.Label("—— 个人限制 ——");
            ls.Label("授予后个人冷却基准(天): " + pawnCooldownDays);
            pawnCooldownDays = (int)ls.Slider(pawnCooldownDays, 60f, 3650f);
            ls.Label("个人冷却随机上沿(天, 0=固定): " + pawnCooldownJitterDays);
            pawnCooldownJitterDays = (int)ls.Slider(pawnCooldownJitterDays, 0f, 730f);
            ls.Label("单人最多沉淀几条特性: " + maxDynamicTraits);
            maxDynamicTraits = (int)ls.Slider(maxDynamicTraits, 1f, 6f);
            ls.Gap(6f);
            ls.Label("—— 生成期守卫 ——");
            ls.CheckboxLabeled("启用 背景配对技能一致性(生成时检查,幼年+→成年禁-2/-3等)", ref enableBackstoryPairGuard);
            ls.End();
            Widgets.EndScrollView();
        }
    }

    // Harmony 一次性注入(行为沉淀挂钩 + 两个生成期守卫)
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
                Log.Message("[HSKTraitExt] Harmony applied (v5: 行为沉淀挂钩 + 生成期守卫)。");
            }
            catch (Exception e)
            {
                Log.Error("[HSKTraitExt] Harmony apply failed: " + e);
            }
        }
    }
}

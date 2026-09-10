// BotrDecisionTickThrottle.cs — 边境拓展(BOTR) 决策节拍降频
//
// 依据(2026-09-05 反编译 BordersOfTheRim.dll 行 21320): WorldComponent_Territories.WorldComponentTick 内部是
// `if (!postLoadInitializationPending && TicksGame % 60 == 17) { 13 个子系统 }`,摊到每 tick 0.056ms
// 但全挤在命中点上 ≈ 3.4ms 单帧尖峰。本前缀只放行 1/N 次命中,不改任何计算内容,不存在"算错"。
// 实测(2026-09-05 [WCP]): 周期 240 后组件循环 0.056 → 0.022~0.034 ms/tick。
//
// 配置: 选项 → Mod 设置 → HSK修复整合 - BOTR决策降频(默认 240;60 = 原版)
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。第三方类型全程反射,mod 不在场就静默不挂。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace BotrDecisionTickThrottle
{
    public class BdtSettings : ModSettings
    {
        public bool enabled = true;
        public int periodTicks = 240;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "bdtEnabled", true);
            Scribe_Values.Look(ref periodTicks, "bdtPeriodTicks", 240);
            base.ExposeData();
        }
    }

    public class BdtMod : Mod
    {
        public static BdtSettings settings;

        public BdtMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<BdtSettings>();
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect.ContractedBy(12f));
            list.CheckboxLabeled("BOTR 决策节拍降频(默认开,240 tick)", ref settings.enabled,
                "只放行 1/N 次原版 60-tick 命中点,不改任何计算内容。代价: 边界刷新/通牒/谈判/自治领/NPC 商队派生的整体响应变慢。");
            float f = settings.periodTicks;
            settings.periodTicks = (int)list.SliderLabeled("决策周期: " + settings.periodTicks + " tick (" +
                (settings.periodTicks / 60) + " 倍原版)", f, 60f, 1200f, 60f,
                tooltip: "60 = 原版。必须是 60 的整倍数。240 ≈ 4 倍,尖峰频次降到 1/4。");
            if (settings.periodTicks < 60) settings.periodTicks = 60;
            settings.periodTicks = settings.periodTicks / 60 * 60;
            list.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "HSK修复整合 - BOTR决策降频";
        }
    }

    [StaticConstructorOnStartup]
    public static class BdtInit
    {
        const int VanillaSlot = 60;        // 原版 TicksGame % 60 == 17
        const int VanillaOffset = 17;

        static BdtInit()
        {
            try
            {
                Type t = AccessTools.TypeByName("BordersOfTheRim.WorldComponent_Territories");
                if (t == null) return;
                MethodBase m = AccessTools.Method(t, "WorldComponentTick");
                if (m == null)
                {
                    Log.Error("[BDT] 找不到 WorldComponent_Territories.WorldComponentTick,BOTR 降频未挂载");
                    return;
                }
                new Harmony("local.ratkin.hskfix.bdt").Patch(m,
                    new HarmonyMethod(typeof(BdtInit), "Pre"), null, null, null);
            }
            catch (Exception e)
            {
                Log.Error("[BDT] 挂载失败: " + e);
            }
        }

        static bool Pre()
        {
            BdtSettings s = BdtMod.settings;
            if (s == null || !s.enabled || s.periodTicks <= VanillaSlot) return true;
            int now = Find.TickManager.TicksGame;
            if (now % VanillaSlot != VanillaOffset) return true;          // 原版本来就不跑
            int slot = s.periodTicks / VanillaSlot;
            return ((now - VanillaOffset) / VanillaSlot) % slot == 0;     // 只放行每 slot 次命中的第一次
        }
    }
}

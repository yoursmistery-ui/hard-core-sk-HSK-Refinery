// IncidentThrottle.cs — 事件节流(游玩时事件密集触发导致卡顿/崩溃的缓解)
//
// 背景: 事件多时 IncidentQueue 会瞬间堆满,一帧内连续 TryFire 多个事件
// (每个事件都要生成 pawns/物品/世界变化),计算风暴是卡退的触发点。
// 本补丁在不改任何游戏数值的前提下,把"同时执行"改成"错峰执行":
//   1. TryFire 层节流: 两次真实事件触发之间强制最小间隔(默认 1.5 秒游戏时间,
//      60 tick/秒 → 90 tick)。间隔内的后续事件直接跳过(下次间隔继续判定),
//      不排队堆积 —— 同一时刻至多一个事件在跑。
//   2. MakeIncidentsForInterval 层限流: 单次间隔生成的事件数封顶(默认 4),
//      防止一次生成太多排队。
// 两者都只"延后/丢弃"非关键事件,不改事件内容,不触碰原生逻辑数值。
//
// 配置: 游戏内 选项 → Mod 设置 → HSK修复整合 → 事件节流
//   enabled           总开关(默认开)
//   minIntervalTicks  最小事件间隔(默认 90 tick = 1.5 秒)
//   maxPerBatch       单次间隔生成上限(默认 4, 0 = 不限)
//
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IncidentThrottle
{
    // ===== 运行时节流状态(静态,全存档共享) =====
    public static class ThrottleState
    {
        // 上一次真实触发事件时的游戏 tick
        public static int lastFiredTick = int.MinValue;
        // 本次间隔已排队的事件数(用于 MakeIncidentsForInterval 限流)
        public static int generatedThisInterval;
    }

    // ===== 游戏内 Mod 设置 =====
    public class IncidentThrottleMod : Mod
    {
        public static IncidentThrottleSettings settings;

        public IncidentThrottleMod(ModContentPack content)
            : base(content)
        {
            settings = GetSettings<IncidentThrottleSettings>();
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.CheckboxLabeled("事件节流: 同一时刻只触发一个事件(错峰)", ref settings.enabled,
                tooltip: "事件密集时把同时触发改为按最小间隔逐个触发,降低瞬间计算峰值。只延后/丢弃非关键事件,不改事件内容。");
            settings.minIntervalTicks = (int)list.SliderLabeled(
                "最小事件间隔( tick, 60 tick = 1 秒): " + settings.minIntervalTicks,
                settings.minIntervalTicks, 0f, 600f,
                tooltip: "两次真实事件触发之间的最小间隔。0 = 关闭间隔限制。默认 90 = 1.5 秒。");
            settings.maxPerBatch = (int)list.SliderLabeled(
                "单次间隔生成上限: " + (settings.maxPerBatch == 0 ? "不限" : settings.maxPerBatch.ToString()),
                settings.maxPerBatch, 0f, 20f,
                tooltip: "一次间隔最多生成的事件数,防止一次堆太多。0 = 不限。默认 4。");
            list.End();
            if (settings.minIntervalTicks < 0) settings.minIntervalTicks = 0;
            if (settings.maxPerBatch < 0) settings.maxPerBatch = 0;
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "HSK修复整合 - 事件节流";
        }
    }

    public class IncidentThrottleSettings : ModSettings
    {
        public bool enabled = true;
        public int minIntervalTicks = 90;
        public int maxPerBatch = 4;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "incidentThrottleEnabled", true);
            Scribe_Values.Look(ref minIntervalTicks, "incidentThrottleMinIntervalTicks", 90);
            Scribe_Values.Look(ref maxPerBatch, "incidentThrottleMaxPerBatch", 4);
            base.ExposeData();
        }
    }

    // ===== Harmony 补丁入口 =====
    [StaticConstructorOnStartup]
    public static class IncidentThrottleInit
    {
        static IncidentThrottleInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.ratkin.hskfix.incidentthrottle");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[IncidentThrottle] 事件节流已加载(默认 间隔 " +
                    (IncidentThrottleMod.settings != null ? IncidentThrottleMod.settings.minIntervalTicks : 90) +
                    " tick / 上限 " +
                    (IncidentThrottleMod.settings != null ? IncidentThrottleMod.settings.maxPerBatch : 4) + ")");
            }
            catch (Exception e)
            {
                Log.Error("[IncidentThrottle] 初始化失败: " + e);
            }
        }

        // ===== Patch A: Storyteller.TryFire —— 同一时刻只触发一个事件 =====
        [HarmonyPatch(typeof(Storyteller), "TryFire")]
        public static class Patch_TryFire
        {
            // 需要 __instance(def) 拿 difficulty 无关;只需当前 tick 与配置。
            // FiringIncident 是参数(incident),不需要改它的内容。
            public static bool Prefix(Storyteller __instance, ref bool __result)
            {
                if (IncidentThrottleMod.settings == null || !IncidentThrottleMod.settings.enabled)
                {
                    return true;
                }
                int interval = IncidentThrottleMod.settings.minIntervalTicks;
                if (interval <= 0)
                {
                    return true; // 未启用间隔限制
                }
                int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                if (ThrottleState.lastFiredTick != int.MinValue &&
                    now - ThrottleState.lastFiredTick < interval)
                {
                    // 距上次触发太近 → 本次事件延后(跳过),保持"同一时刻至多一个事件"
                    __result = false;
                    return false;
                }
                return true;
            }

            // 真实触发成功后记录时间点
            public static void Postfix(Storyteller __instance, ref bool __result)
            {
                if (__result && IncidentThrottleMod.settings != null &&
                    IncidentThrottleMod.settings.enabled &&
                    IncidentThrottleMod.settings.minIntervalTicks > 0)
                {
                    ThrottleState.lastFiredTick =
                        Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                }
            }
        }
    }
}

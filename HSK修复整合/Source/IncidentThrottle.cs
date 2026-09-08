// IncidentThrottle.cs — 事件错峰 v2(2026-09-06 根治版: 只延后、不丢弃)
//
// 背景: 事件密集时一帧内连续 TryFire 多个事件(每个都要生成 pawns/物品/世界变化),
// 计算风暴是卡退触发点。v1 直接把间隔内的事件丢弃(__result=false), 撞上原版两个
// 机制后造成"事件大量消失/只剩袭击"(2026-09-06 老存档事件饥饿 bug 的根因):
//   (a) 原版 StorytellerTick 只在 TicksGame % 1000 == 0 的整点批量结算全部组件候选,
//       同一 tick 内按 def 组件顺序依次 TryFire —— RandomMain(袭击)排第一, 只要该
//       整点抽中袭击, 排在后面的商队/访客/任务/穿梭机候选全部被 v1 吞掉;
//   (b) 原版 IncidentQueue 中 retryDurationTicks==0 的条目一次 TryFire 失败即被移除
//       (永久丢失), 任务脚本延迟触发的事件(穿梭机/增援/奖励舱等)多属此类。
// v2 改为"错峰入队": 被闸门挡住的事件重新入队(IncidentQueue.Add, 带 1 游戏天重试
// 窗口)排到下一个空位执行 —— 事件只会晚到, 永不消失; 闸门只负责拉开执行间距。
//
// 机制(v2):
//   1. 错峰(原机制1, 丢弃→延后): 非队列来源的事件落在最小间隔内 → 延后入队到
//      下一个 interval 空位。队列来源(queued==true, 含我们自己延迟入队的事件)直接
//      放行, 绝不二次节流 —— 既杜绝重复入队, 又原样保留队列条目的原版重试语义。
//   2. 重型事件分池冷却(原机制3, 丢弃→延后, 共享池→两池): 任务(GiveQuest_* 等
//      defName 含 quest)与派系战争(factionwar)单次生成要 0.7~1.8 秒冻结。v1 两族
//      共享一池, 出一个任务就把后续任务/穿梭机事件全丢一天。v2 拆成两池, 池内事件
//      至少相隔指定天数, 冷却期命中 → 延后入队到池开放时刻(入队即预留下一格),
//      两个重型事件永不背靠背, 且全部会执行。只约束叙事者组件排出的候选
//      (fi.source != null), mod 强制事件不受冷却。
//   (2026-09-08 二次修改: 原"机制3 总体降频(每 N 个丢 1 个)"整体删除 —— 用户要求
//    提高事件频率, 该丢弃逻辑与节流器"只延后、不丢弃"的设计初衷相悖; 本节流器现在
//    只做错峰与延后, 永不丢弃任何事件。)
//   (v1 的"机制2 单次间隔生成上限 maxPerBatch"从未被实现, v2 移除该死设置;
//    settings 键 incidentThrottleMaxPerBatch 不再读取, 旧设置文件里的残留键无害。)
//
// 配置: 游戏内 选项 → Mod 设置 → HSK修复整合 → 事件节流
//   enabled            总开关(默认开)
//   minIntervalTicks   最小事件间隔(默认 90 tick = 1.5 秒, 0 = 关闭错峰)
//   heavyEnabled       重型事件分池冷却开关(默认开)
//   heavyIntervalDays  任务池 / 派系战争池各自的最小间隔天数(默认 1 天, 0 = 关闭)
//
// 原版事实备忘(反编译 1.6 定论, v2 设计依据):
//   - TryFire(FiringIncident fi, bool queued = false) 只有一个重载; 队列触发固定传
//     queued=true, 叙事者整点批量与 mod 直调默认 false。
//   - IncidentQueue.Add(def, fireTick, parms, retryDurationTicks) 公开可复用;
//     条目到点触发失败且 retry>0 时每 ~833 tick 重试, 直到成功或超出窗口。
//
// 编译: 并入 HSKFixPack.dll(系统 csc, C#5 —— 禁字符串插值/null 条件/表达式体成员)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace IncidentThrottle
{
    // ===== 运行时节流状态(静态, 全存档共享; 读档/域重载后归零 = 首个整点闸门全开) =====
    public static class ThrottleState
    {
        // 非队列事件最早可直接触发的 tick(真实触发成功后推进 interval)
        public static int gateTick;
        // 错峰延迟入队的下一个空位 fireTick(保证同批被挡的事件彼此也错开)
        public static int slotTick;
        // 重型事件冷却池开放时刻: questPoolTick=任务池, warPoolTick=派系战争池
        public static int questPoolTick;
        public static int warPoolTick;
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
            list.CheckboxLabeled("事件错峰: 同一时刻只执行一个事件(其余延后, 不丢弃)", ref settings.enabled,
                tooltip: "事件密集时把同时触发改为按最小间隔逐个触发, 降低瞬间计算峰值。间隔内的事件自动排队延后到下一个空位, 只会晚到、永不消失。不改事件内容与数值。");
            settings.minIntervalTicks = (int)list.SliderLabeled(
                "最小事件间隔( tick, 60 tick = 1 秒): " + settings.minIntervalTicks,
                settings.minIntervalTicks, 0f, 600f,
                tooltip: "两次真实事件触发之间的最小间隔。间隔内的事件会延后入队, 按间隔逐个执行。0 = 关闭错峰。默认 90 = 1.5 秒。");
            list.CheckboxLabeled("重型事件冷却: 任务 / 派系战争分池错峰(默认1天)", ref settings.heavyEnabled,
                tooltip: "任务(GiveQuest_* 等)与派系战争(SrFactionWar)单次生成需要 0.7~1.8 秒冻结。两族各自独立冷却池, 池内事件至少相隔指定天数; 冷却期内的事件自动延后到池开放时刻, 全部会执行、只是错开, 不再丢失。只约束叙事者排出的候选, mod 强制事件不受影响。");
            settings.heavyIntervalDays = (int)list.SliderLabeled(
                "重型事件最小间隔( 游戏天 ): " + (settings.heavyIntervalDays == 0 ? "关闭" : settings.heavyIntervalDays.ToString() + " 天"),
                settings.heavyIntervalDays, 0f, 15f,
                tooltip: "任务池 / 派系战争池各自的最小间隔, 按游戏内天计。0 = 关闭冷却。默认 1 天。");
            list.End();
            if (settings.minIntervalTicks < 0) settings.minIntervalTicks = 0;
            if (settings.heavyIntervalDays < 0) settings.heavyIntervalDays = 0;
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
        public bool heavyEnabled = true;
        public int heavyIntervalDays = 1;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "incidentThrottleEnabled", true);
            Scribe_Values.Look(ref minIntervalTicks, "incidentThrottleMinIntervalTicks", 90);
            Scribe_Values.Look(ref heavyEnabled, "incidentThrottleHeavyEnabled", true);
            Scribe_Values.Look(ref heavyIntervalDays, "incidentThrottleHeavyIntervalDays", 1);
            base.ExposeData();
        }
    }

    // ===== Harmony 补丁入口 =====
    [StaticConstructorOnStartup]
    public static class IncidentThrottleInit
    {
        // 延迟入队的重试窗口: 1 游戏天(目标暂时失效时按原版节奏 ~833 tick 重试, 过期才放弃)
        const int RetryWindowTicks = 60000;

        static IncidentThrottleInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.ratkin.hskfix.incidentthrottle");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[IncidentThrottle] 事件错峰 v2 已加载(间隔 " +
                    (IncidentThrottleMod.settings != null ? IncidentThrottleMod.settings.minIntervalTicks : 90) +
                    " tick / 只延后不丢弃 / 任务·派系战争分池冷却)");
            }
            catch (Exception e)
            {
                Log.Error("[IncidentThrottle] 初始化失败: " + e);
            }
        }

        // ===== 重型事件识别(单次生成极重, 需分池错峰) =====
        // defName 子串匹配(忽略大小写), 与 v1 一致; v2 起误匹配只会造成额外错峰, 无害。
        static readonly string[] QuestKeywords = { "givequest", "quest" };
        const string WarKeyword = "factionwar";

        // 返回 0=非重型, 1=任务池, 2=派系战争池
        static int HeavyPoolOf(FiringIncident fi)
        {
            if (fi == null || fi.def == null || fi.def.defName == null) return 0;
            string dn = fi.def.defName.ToLowerInvariant();
            if (dn.Contains(WarKeyword)) return 2;
            for (int i = 0; i < QuestKeywords.Length; i++)
            {
                if (dn.Contains(QuestKeywords[i])) return 1;
            }
            return 0;
        }

        static int PoolGateTick(int pool)
        {
            return pool == 2 ? ThrottleState.warPoolTick : ThrottleState.questPoolTick;
        }

        // 延后入队占用池: 下一格排到 fireTick+gap 之后(多个重型候选同批到达也彼此错开)
        static void ReservePool(int pool, int fireTick, int gapTicks)
        {
            int next = fireTick + gapTicks;
            if (pool == 2) ThrottleState.warPoolTick = next;
            else ThrottleState.questPoolTick = next;
        }

        // 真实触发成功后推进池时钟
        static void StampPool(int pool, int now, int gapTicks)
        {
            int next = now + gapTicks;
            if (pool == 2) ThrottleState.warPoolTick = next;
            else ThrottleState.questPoolTick = next;
        }

        // ===== Patch A: Storyteller.TryFire —— 错峰 + 重型分池冷却 + 总体降频 =====
        [HarmonyPatch(typeof(Storyteller), "TryFire")]
        public static class Patch_TryFire
        {
            public static bool Prefix(Storyteller __instance, FiringIncident fi, bool queued, ref bool __result)
            {
                if (queued)
                {
                    // 队列来源(含我们延迟入队的事件)直接放行:
                    // 不二次节流 → 不会重复入队; 原版队列条目重试语义原样保留。
                    return true;
                }
                if (IncidentThrottleMod.settings == null || !IncidentThrottleMod.settings.enabled)
                {
                    return true;
                }
                if (__instance == null || __instance.incidentQueue == null || Find.TickManager == null)
                {
                    return true;
                }
                int now = Find.TickManager.TicksGame;

                int interval = IncidentThrottleMod.settings.minIntervalTicks;
                bool heavyOn = IncidentThrottleMod.settings.heavyEnabled;
                int pool = heavyOn ? HeavyPoolOf(fi) : 0;
                int gapTicks = heavyOn ? IncidentThrottleMod.settings.heavyIntervalDays * GenDate.TicksPerDay : 0;
                // 叙事者组件候选 source != null; mod/调试强制事件 source == null, 不受重型冷却
                bool modForced = fi.source == null;

                // 机制2: 重型分池冷却 —— 池未开放 → 延后入队到池开放时刻, 并预留下一格
                if (pool != 0 && gapTicks > 0 && !modForced)
                {
                    int poolGate = PoolGateTick(pool);
                    if (now < poolGate)
                    {
                        __instance.incidentQueue.Add(fi.def, poolGate, fi.parms, RetryWindowTicks);
                        ReservePool(pool, poolGate, gapTicks);
                        return Denied(out __result);
                    }
                }

                // 机制1: 错峰 —— 最小间隔内的候选延后入队到下一个空位(不丢弃)
                if (interval > 0 && now < ThrottleState.gateTick)
                {
                    int ft = ThrottleState.slotTick > ThrottleState.gateTick ? ThrottleState.slotTick : ThrottleState.gateTick;
                    ThrottleState.slotTick = ft + interval;
                    __instance.incidentQueue.Add(fi.def, ft, fi.parms, RetryWindowTicks);
                    return Denied(out __result);
                }
                return true;
            }

            static bool Denied(out bool __result)
            {
                __result = false;
                return false;
            }

            // 只在"放行且真实触发成功"时推进时钟: prefix 返回 false 时 Harmony 跳过 postfix;
            // 队列来源(queued=true)的触发不推进任何时钟, 防止延迟入队的事件重复 stamp。
            public static void Postfix(FiringIncident fi, bool queued, ref bool __result)
            {
                if (queued || !__result) return;
                if (IncidentThrottleMod.settings == null || !IncidentThrottleMod.settings.enabled) return;
                if (Find.TickManager == null) return;
                int now = Find.TickManager.TicksGame;
                int interval = IncidentThrottleMod.settings.minIntervalTicks;
                if (interval > 0)
                {
                    ThrottleState.gateTick = now + interval;
                }
                if (IncidentThrottleMod.settings.heavyEnabled)
                {
                    int pool = HeavyPoolOf(fi);
                    if (pool != 0)
                    {
                        int gapTicks = IncidentThrottleMod.settings.heavyIntervalDays * GenDate.TicksPerDay;
                        if (gapTicks > 0)
                        {
                            StampPool(pool, now, gapTicks);
                        }
                    }
                }
            }
        }
    }
}

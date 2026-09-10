// EmptyNpcCaravanCleanup.cs — 空壳商队清理(非玩家派系、0 成员的商队收尾)
//
// 现象(2026-09-05 存档核对): 边境拓展(BOTR) 的 Faction_188 贸易商队仍挂在 tile 276836 上、
// 带着整包商品(存档块 ~110KB),但 `pawns` 列表是空的 —— 0 成员的商队既不会到达也不会成交,
// 而 BOTR 自己的 ValidateFactionTradeCaravanRoute 只检查"目的地是否有效",不看"还有没有人",
// 所以这个壳子会永久留在存档与世界地图上。别的 mod(流浪商队/任务队伍)也可能留下同类残留。
// 原版 WorldPawnGC.GetCriticalPawnReason 明确保护 CaravanMember,所以壳子不是 GC 吃的,是收尾路径被打断留下的。
//
// 做法: 挂在 RimWorld.Planet.Caravan.TickInterval 的尾缀上,按原版/BOTR 同款的
// `TicksGame % 2500 == ID % 2500` 节拍轮询(不额外加每 tick 逻辑)。
// 需要**连续两次**(相隔 2500 tick)都观察到 0 成员才动手,避开"正在把成员交还给定居点"
// 这类合法瞬时空仓(例如 DiscardAtDestination 会先 RemoveAllPawns 再 Destroy)。
// 动手时: BOTR 的商队优先反射调它自己的 DiscardAtDestination()(完整收尾),其余一律 WorldObject.Destroy()。
// 玩家商队永不碰。
//
// 配置: 选项 → Mod 设置 → HSK修复整合 - 空壳商队清理
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5),手动 harmony.Patch,第三方类型全走反射。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace EmptyNpcCaravanCleanup
{
    public class EcsSettings : ModSettings
    {
        public bool enabled = true;
        public bool logActions = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "ecsEnabled", true);
            Scribe_Values.Look(ref logActions, "ecsLogActions", false);
            base.ExposeData();
        }
    }

    public class EcsMod : Mod
    {
        public static EcsSettings settings;

        public EcsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<EcsSettings>();
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect.ContractedBy(12f));
            list.CheckboxLabeled("清理 0 成员的非玩家商队(默认开)", ref settings.enabled,
                "每 2500 tick 轮询一次,连续两次发现某只非玩家商队没有任何成员才销毁,避免误伤正在交接成员的商队。玩家商队永不处理。");
            list.CheckboxLabeled("  销毁时打一条日志(默认关)", ref settings.logActions, "便于确认是否真有残留被清掉。");
            list.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "HSK修复整合 - 空壳商队清理";
        }
    }

    [StaticConstructorOnStartup]
    public static class EcsInit
    {
        static readonly HashSet<int> seenEmpty = new HashSet<int>();
        static MethodInfo miDiscardAtDestination;
        static bool resolved;

        static EcsInit()
        {
            try
            {
                MethodBase target = AccessTools.Method(typeof(Caravan), "TickInterval", new Type[] { typeof(int) });
                if (target == null)
                {
                    Log.Error("[ECS] 找不到 Caravan.TickInterval,空壳商队清理未挂载");
                    return;
                }
                new Harmony("local.ratkin.hskfix.emptynpcaravan").Patch(target, null,
                    new HarmonyMethod(typeof(EcsInit), "Post"), null, null);
            }
            catch (Exception e)
            {
                Log.Error("[ECS] 挂载失败: " + e);
            }
        }

        static void ResolveBotrTeardown(Caravan c)
        {
            if (resolved) return;
            resolved = true;
            try
            {
                Type t = c.GetType();
                if (t.FullName != null && t.FullName.StartsWith("BordersOfTheRim."))
                {
                    miDiscardAtDestination = AccessTools.Method(t, "DiscardAtDestination");
                }
            }
            catch (Exception)
            {
                miDiscardAtDestination = null;
            }
        }

        static void Post(Caravan __instance)
        {
            EcsSettings s = EcsMod.settings;
            if (s == null || !s.enabled) return;
            WorldObject wo = (WorldObject)__instance;
            if (wo.Destroyed)
            {
                seenEmpty.Remove(wo.ID);
                return;
            }
            if (__instance.IsPlayerControlled) return;
            int now = Find.TickManager.TicksGame;
            if (now % 2500 != wo.ID % 2500) return;                 // 与 BOTR 同款节拍,不额外加热点
            if (__instance.PawnsListForReading.Count > 0)
            {
                seenEmpty.Remove(wo.ID);
                return;
            }
            if (!seenEmpty.Contains(wo.ID))
            {
                seenEmpty.Add(wo.ID);                               // 第一次观察到,下一轮再确认
                return;
            }
            seenEmpty.Remove(wo.ID);
            Discard(__instance, s);
        }

        static void Discard(Caravan c, EcsSettings s)
        {
            try
            {
                ResolveBotrTeardown(c);
                if (miDiscardAtDestination != null && miDiscardAtDestination.DeclaringType.IsInstanceOfType(c))
                {
                    miDiscardAtDestination.Invoke(c, null);
                }
                else
                {
                    ((WorldObject)c).Destroy();
                }
                if (s.logActions)
                {
                }
            }
            catch (Exception e)
            {
                Log.Error("[ECS] 清理商队失败: " + e);
            }
        }
    }
}

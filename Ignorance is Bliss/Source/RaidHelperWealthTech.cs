// RaidHelperWealthTech.cs — 袭击派系科技档上限 + 增援延期 + 财富挂钩(2026-08-31 用户需求)
//
// 【2026-09-09 迁移】本文件已从 HSK修复整合 整体搬到 Ignorance Is Bliss,HSK 侧不再保留。
//   职责划分:
//     · 派系科技档准入(袭击 / 商队 / 来访 统一) -> 由本 mod 的 IgnoranceIsBliss.dll 托管
//       (设置: 科技算法 = Actual colonist tech level;NumTechsAhead = 1;NumTechsBehind = -1,
//        即 [无限制, 玩家档+1])。玩家档取 Faction.OfPlayer.def.techLevel,由 Tech Advancing 实时改写。
//     · 本文件继续负责: 固定门槛大敌(机械/虫族/天网/美狐)的科技档下限 + 财富门槛、
//       增援延期、中心空投/双倍袭击/袭击间隔/派系档加权等财富挂钩、Core_SK 设置页禁用、
//       事件型大敌门槛。
//   两套逻辑叠加关系是取交集,不会互相放大;科技档上限两边一致(+1),故行为不变。
//
// 需求:
//   1. 普通袭击派系科技等级最多高玩家一个档(玩家档由 Tech Advancing 模组维护,
//      它直接改写 Faction.OfPlayer.def.techLevel,故直接读该值)。
//      机械体/虫族/天网是"固定门槛大敌"(2026-09-04 用户调整): 不再豁免科技档,
//      改吃"玩家科技档下限 + 财富门槛"双条件(与年限叠加,三条件并存才触发):
//        Mechanoid             -> Industrial + 100 万财富
//        Insect / Insectoid    -> Industrial + 50 万财富(09-04 二次下调: 100→50)
//        SkynetHumanlike(天网) -> Spacer     + 200 万财富
//      年限(最早袭击日)由 Patches/13_袭击首日延期.xml + 18_威胁事件年限提前.xml 控制:
//      机械 180 / 虫族 240 / 天网 120 天。
//   2. 后期增援(Raid Assistant)EnableAfterDaysPassed 120 → 400(RimWorld 1年=60天,400天≈6.7年)。
//   3. 袭击与财富挂钩:
//      a) 中心空投概率: 财富除数 1,000,000 → 2,000,000,上限压到 0.12(原版 0.15,第一次改 0.30 后用户要求再降);
//      b) 双倍袭击概率: 基础 RaidDoubleChance + 财富/60,000,000(最多+0.20),封顶 0.35;
//      c) 袭击间隔: 财富 25,000,000 时最长缩短 25%;
//      d) 派系档加权: 每 2,000,000 财富,比玩家高 1 档的派系权重 +100%(最多 +200%)。
//   4. Core_SK 的 Mod 设置页(ProjectSettings.DoWindowContents,含袭击全部滑条)用补丁禁用,
//      防止上述锁定的数值被游戏内改回。
//
// 覆盖面: SK.RaidHelperComponent.ResolveFaction(HSK 自研袭击调度)+ 原版
// IncidentWorker_RaidEnemy.TryResolveRaidFaction(讲故事人自发袭击)双路都设上限。
//
// 编译: 并入 HskRaidTech.dll(系统 csc,C#5),由同目录 build.ps1 构建。
//       需要引用 Core_SK.dll(SK.RaidHelperComponent / ProjectSettings / FactionDefOfLocal 等)。
//       补丁生效入口见 RaidTechBootstrap.cs(自带 [StaticConstructorOnStartup] + PatchAll)。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using SK;

namespace RaidHelperWealthTech
{
    public static class WealthRaidHooks
    {
        private static int cachedTick = -1;
        private static float cachedWealth;

        // 玩家主图财富总量(每 tick 缓存一次,调用点均为低频路径)
        public static float ColonyWealth()
        {
            if (Find.TickManager == null || Find.Maps == null)
            {
                return 0f;
            }
            int t = Find.TickManager.TicksGame;
            if (t == cachedTick)
            {
                return cachedWealth;
            }
            float w = 0f;
            foreach (Map m in Find.Maps)
            {
                if (m.IsPlayerHome)
                {
                    w = m.wealthWatcher.WealthTotal;
                    break;
                }
            }
            cachedTick = t;
            cachedWealth = w;
            return w;
        }

        // 袭击派系科技档上限 = 玩家档 + 1(封顶 Archotech)
        public static int MaxRaidTech()
        {
            if (Faction.OfPlayer == null || Faction.OfPlayer.def == null)
            {
                return int.MaxValue;
            }
            int t = (int)Faction.OfPlayer.def.techLevel + 1;
            int mx = (int)TechLevel.Archotech;
            return (t > mx) ? mx : t;
        }

        // 玩家当前科技档(Tech Advancing 直接改写 Faction.OfPlayer.def.techLevel)
        public static int PlayerTechLevel()
        {
            if (Faction.OfPlayer == null || Faction.OfPlayer.def == null)
            {
                return 0;
            }
            return (int)Faction.OfPlayer.def.techLevel;
        }

        // "固定门槛大敌"派系判定(2026-09-04): 机械体/虫族不再豁免科技档,
        // 与天网一样吃"玩家科技档下限 + 财富"双门槛。
        // (天网 Skynet_SK 未纳入编译引用,按其 defName 判,未装该 mod 时恒 false 安全)
        public static bool IsFixedGateFaction(Faction f)
        {
            if (f == null || f.def == null)
            {
                return false;
            }
            if (f.def == FactionDefOf.Mechanoid)
            {
                return true;
            }
            if (f.def == FactionDefOfLocal.Insectoid || f.def == FactionDefOfLocal.Insect)
            {
                return true;
            }
            if (f.def.defName == "SkynetHumanlike")
            {
                return true;
            }
            // 美狐海盗(2026-09-07): 只挂科技门槛(Industrial),不设财富门槛(返回 0 恒过)
            if (f.def.defName == "Miho_Faction_Supremacist")
            {
                return true;
            }
            return false;
        }

        // 固定门槛派系要求的最低玩家科技档
        public static int FixedGateMinTech(Faction f)
        {
            if (f == null || f.def == null)
            {
                return 0;
            }
            if (f.def == FactionDefOf.Mechanoid)
            {
                return (int)TechLevel.Industrial;
            }
            if (f.def == FactionDefOfLocal.Insectoid || f.def == FactionDefOfLocal.Insect)
            {
                return (int)TechLevel.Industrial;
            }
            if (f.def.defName == "SkynetHumanlike")
            {
                return (int)TechLevel.Spacer;
            }
            if (f.def.defName == "Miho_Faction_Supremacist")
            {
                return (int)TechLevel.Industrial;
            }
            return 0;
        }

        // 固定门槛派系的财富门槛(2026-09-04 定值: 机械族 100 万, 虫族 50 万(二次下调), 天网 200 万;
        // 2026-09-07 用户要求降 40%: 机械 60 万, 虫族 30 万, 天网 120 万)
        public static float FixedGateWealth(Faction f)
        {
            if (f == null || f.def == null)
            {
                return 0f;
            }
            if (f.def == FactionDefOf.Mechanoid)
            {
                return 600000f;
            }
            if (f.def == FactionDefOfLocal.Insectoid || f.def == FactionDefOfLocal.Insect)
            {
                return 300000f;
            }
            if (f.def.defName == "SkynetHumanlike")
            {
                return 1200000f;
            }
            return 0f;
        }

        // 派系整体准入判定(ResolveFaction / TryResolveRaidFaction 共用):
        //   普通派系: techLevel ≤ 玩家档+1
        //   固定门槛大敌(机械体/虫族/天网): 玩家档≥下限 且 财富≥门槛
        public static bool FactionAllowed(Faction f)
        {
            if (IsFixedGateFaction(f))
            {
                if (PlayerTechLevel() < FixedGateMinTech(f))
                {
                    return false;
                }
                return ColonyWealth() >= FixedGateWealth(f);
            }
            return (int)f.def.techLevel <= MaxRaidTech();
        }

        // 双倍袭击概率: 基础 + 财富加成,封顶 0.35(经 transpile 替换 ldfld RaidDoubleChance)
        public static float ScaledDoubleChance(ProjectSettings c)
        {
            float baseChance = (c != null) ? c.RaidDoubleChance : 0.15f;
            float bonus = ColonyWealth() / 60000000f;
            if (bonus > 0.20f)
            {
                bonus = 0.20f;
            }
            float v = baseChance + bonus;
            return (v > 0.35f) ? 0.35f : v;
        }

        // 高财富时高 tech 档派系权重放大(每 200 万财富,高 1 档 +100%,最多 +200%)
        // 固定门槛大敌(机械/虫族/天网)不加成,避免后期被财富放大刷屏
        public static float FactionWeight(Faction f, float commonality)
        {
            if (commonality <= 0f || IsFixedGateFaction(f))
            {
                return commonality;
            }
            float wealth = ColonyWealth();
            float bias = wealth / 2000000f;
            if (bias > 2f)
            {
                bias = 2f;
            }
            if (Faction.OfPlayer == null || Faction.OfPlayer.def == null)
            {
                return commonality;
            }
            int dt = (int)f.def.techLevel - (int)Faction.OfPlayer.def.techLevel;
            if (dt <= 0)
            {
                return commonality;
            }
            return commonality * (1f + bias * dt);
        }
    }

    // 需求1: SK.RaidHelperComponent.ResolveFaction 整体替换(原方法无科技档过滤)
    [HarmonyPatch(typeof(RaidHelperComponent), "ResolveFaction")]
    public static class RaidHelper_ResolveFaction_TechCap
    {
        public static bool Prefix(RaidHelperComponent __instance, IncidentParms parms, ref Faction __result)
        {
            if (__instance.onlyInsectoidFaction)
            {
                __result = FactionUtility.DefaultFactionFrom(FactionDefOfLocal.Insectoid);
                return false;
            }
            float raidpoints = parms.points;
            IncidentWorker_RaidEnemy worker = (IncidentWorker_RaidEnemy)IncidentDefOf.RaidEnemy.Worker;
            IEnumerable<Faction> cands = Find.FactionManager.AllFactions.Where(delegate (Faction f)
            {
                if (f.IsPlayer || f.defeated)
                {
                    return false;
                }
                if (!worker.FactionCanBeGroupSource(f, parms))
                {
                    return false;
                }
                if (!__instance.HasPawnGroupMakers(f, raidpoints))
                {
                    return false;
                }
                if (!__instance.FreeToRaid(f))
                {
                    return false;
                }
                return WealthRaidHooks.FactionAllowed(f);
            });
            Faction chosen = null;
            cands.TryRandomElementByWeight(delegate (Faction f)
            {
                return WealthRaidHooks.FactionWeight(f, f.def.RaidCommonalityFromPoints(raidpoints));
            }, out chosen);
            __result = chosen;
            return false;
        }
    }

    // 需求1: 原版讲故事人袭击 TryResolveRaidFaction 事后校验(超档派系改为在合法池重选,选不出则取消)
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryResolveRaidFaction")]
    public static class Vanilla_RaidFaction_TechCap
    {
        public static void Postfix(IncidentWorker_RaidEnemy __instance, IncidentParms parms, ref bool __result)
        {
            if (!__result || parms.faction == null)
            {
                return;
            }
            if (WealthRaidHooks.FactionAllowed(parms.faction))
            {
                return;
            }
            Faction f;
            bool ok = PawnGroupMakerUtility.TryGetRandomFactionForCombatPawnGroupWeighted(parms, out f,
                delegate (Faction x)
                {
                    return __instance.FactionCanBeGroupSource(x, parms) && WealthRaidHooks.FactionAllowed(x);
                }, true, true, true);
            if (ok && f != null)
            {
                parms.faction = f;
            }
            else
            {
                __result = false;
            }
        }
    }

    // 需求2: 后期增援 120 → 400 天(每张地图组建 RaidHelperComponent 时强制回写一次)
    // 注意: RaidHelperComponent 是 MapComponent,唯一构造为 (Map map) 无参构造不存在;
    //       MethodType.Constructor 不写参数类型时 Harmony 找无参构造 → Undefined target method
    //       → 整个 HSKFixPack.dll 的 PatchAll 崩(连带 FacilityCrashFix/IncidentThrottle)。须显式给签名。
    [HarmonyPatch(typeof(RaidHelperComponent), MethodType.Constructor, new Type[] { typeof(Map) })]
    public static class RaidHelper_AssistDelay_400d
    {
        public static void Postfix(RaidHelperComponent __instance, ProjectSettings ___c)
        {
            if (___c != null)
            {
                ___c.EnableAfterDaysPassed = 400;
            }
        }
    }

    // 需求3a: 中心空投概率财富缩放 1,000,000 → 2,000,000,上限压到 0.12
    [HarmonyPatch(typeof(RaidHelperComponent), "ResolveRaidArrivalMode")]
    public static class RaidHelper_LandingWealth
    {
        public static bool Prefix(RaidHelperComponent __instance, IncidentParms parms, float colonywealth, ProjectSettings ___c, ref PawnsArrivalModeDef __result)
        {
            float num = ___c.DefaultRaidLandingChance + colonywealth / 2000000f;
            float cap = 0.12f;
            PawnsArrivalModeDef def = ((((int)parms.faction.def.techLevel < 5) || !(Rand.Value < ((num > cap) ? cap : num))) ? PawnsArrivalModeDefOf.EdgeWalkIn : PawnsArrivalModeDefOf.CenterDrop);
            if (!def.Worker.CanUseWith(parms))
            {
                __result = null;
                return false;
            }
            __result = def;
            return false;
        }
    }

    // 需求3c: 袭击间隔随财富缩短(财富 2500 万时最多 -25%)
    [HarmonyPatch(typeof(RaidHelperComponent), "get_daysToAttackRndDelay")]
    public static class RaidHelper_IntervalWealth
    {
        public static void Postfix(ref float __result)
        {
            float factor = 1f - WealthRaidHooks.ColonyWealth() / 25000000f;
            if (factor < 0.75f)
            {
                factor = 0.75f;
            }
            __result = Mathf.Max(1f, __result * factor);
        }
    }

    // 需求3b: ResolveRaid 内 ldfld RaidDoubleChance → 财富缩放版
    [HarmonyPatch(typeof(RaidHelperComponent), "ResolveRaid")]
    public static class RaidHelper_DoubleChanceWealth
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction ci in instructions)
            {
                FieldInfo fi = ci.operand as FieldInfo;
                if (ci.opcode == OpCodes.Ldfld && fi != null && fi.Name == "RaidDoubleChance" && fi.DeclaringType == typeof(ProjectSettings))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WealthRaidHooks), "ScaledDoubleChance"));
                }
                else
                {
                    yield return ci;
                }
            }
        }
    }

    // 需求4: 禁用 Core_SK 的 Mod 设置页(ProjectSettings.DoWindowContents,
    // Main/RaidHelper 两个标签页含全部袭击滑条),防止锁定的数值被游戏内改回。
    [HarmonyPatch(typeof(ProjectSettings), "DoWindowContents")]
    public static class CoreSK_Settings_Disabled
    {
        public static bool Prefix(Rect canvas)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(canvas, "HSK修复整合: Core_SK 袭击参数已锁定,此设置页已禁用。");
            Text.Anchor = TextAnchor.UpperLeft;
            return false;
        }
    }

    // 需求5(2026-09-04 用户调整): 事件型大敌的科技档/财富门槛。
    // IncidentDef 无原生"科技"字段;1.6 事件 worker 的附加检查走 CanFireNowSub,
    // 但随机调度必经基类 IncidentWorker.CanFireNow -> 在此打 Prefix 按 defName 拦表:
    //   PsychicEmanatorShipPartCrash / DefoliatorShipPartCrash(心灵/枯萎者飞船)
    //       -> 玩家需 Industrial+(财富沿用 IncidentDef.minThreatPoints 8000,不另设)
    //   Salvation / AgentPodCrash / AgentTravelerGroup(天网 3 事件)
    //       -> 玩家需 Spacer+ 且财富 ≥ 2,000,000
    // 命中门槛即 __result=false(本次不触发),其余事件不受影响。
    [HarmonyPatch(typeof(IncidentWorker), "CanFireNow")]
    public static class ThreatEvent_CanFireNow_TechGate
    {
        private static bool TryEventGate(string defName, out int minTech, out float wealth)
        {
            minTech = 0;
            wealth = 0f;
            if (defName == "PsychicEmanatorShipPartCrash" || defName == "DefoliatorShipPartCrash")
            {
                minTech = (int)TechLevel.Industrial;
                return true;
            }
            if (defName == "Salvation" || defName == "AgentPodCrash" || defName == "AgentTravelerGroup")
            {
                minTech = (int)TechLevel.Spacer;
                wealth = 2000000f;
                return true;
            }
            return false;
        }

        public static bool Prefix(IncidentWorker __instance, ref bool __result)
        {
            if (__instance == null || __instance.def == null)
            {
                return true;
            }
            int minTech;
            float wealth;
            if (!TryEventGate(__instance.def.defName, out minTech, out wealth))
            {
                return true;
            }
            if (WealthRaidHooks.PlayerTechLevel() < minTech)
            {
                __result = false;
                return false;
            }
            if (wealth > 0f && WealthRaidHooks.ColonyWealth() < wealth)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}

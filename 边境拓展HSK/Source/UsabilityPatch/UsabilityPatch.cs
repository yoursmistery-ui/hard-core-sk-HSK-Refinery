using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BordersOfTheRim;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace BordersOfTheRimHskPatch
{
    // 边境拓展HSK 本地适配补丁(2026-08-31):
    // 1) 宜居放宽——用户规则"周边没有山就是宜居": 有效格 = 非水域 且 非不可通行(hilliness==Impassable)。
    //    BOTR 原判定额外要求 群系 canBuildBase/implemented 且 群系权重×派系温度曲线 > 0,
    //    在群系破碎/温度极端的星球上会让首都安置探测永远达不到目标, 整块 BFS 反复扫描(实测 capitals 一步 65.2s)。
    // 2) 家园散布半径钉值——用户不想手动调滑条: 把 homelandRadius 钉死为 48, 并从设置界面删除该滑条。
    //    该值是各派系定居点场半径的下限(实际半径 = Max(钉值, ceil(√(定居点数-1)×5.5)), 钳制 12–240),
    //    48 在 77 派系/35 首都的密集星球上保持家园紧凑、留出空隙, 大派系仍可按密度自然扩张。
    // 3) 事件降频(2026-09-06)——用户要求"索要城市/边境战争这些事件间隔拉大3倍": 转译器把
    //    WorldComponent_Territories 六个调度器的基础间隔常量 ×3(战争检查 5→15 天、索要定居点/飞地交换
    //    4→12 天、战争任务检查 2→6 天、每战任务冷却 5→15 天、会盟检查 3→9 天、附庸/保护检查 4→12 天)。
    //    只改基础常量, 设置里的频率滑条语义不变(仍可在新基准上再调); 稳定度巡检(内战由状态触发, 非定时)、
    //    战斗结算节奏、贸易商队节奏不动。每处常量要求唯一命中, 否则放弃替换(宁可不改也不产出坏 IL)。
    // 4) 事件距离门控(2026-09-06)——用户要求"按科技等级限制最大事件发生距离, 太远的不触发, 降低性能损耗":
    //    以玩家定居点为圆心, 最大事件半径 = 星球半径 × 科技档系数(无研究/原始 0.30 → 中世纪 0.45 → 工业
    //    0.65 → 太空 0.85 → 极致 1.10 → 更高不限), 玩家科技档 = 已完成研究的最高 techLevel。
    //    拦截点: CanStartTerritorialWarAtBorder(宣战配对 + 被拒索要升级为战争的共同门槛)、ChooseDemandTarget
    //    (索要选址, 返回 null 即该配对不出候选)、TryLaunchVisibleWarBattle(远处活跃战争不发可见战团, 回退
    //    幕后结算, 战争照常推进不冻死)、6 个 CreateXxxMission 任务工厂(远处战争不生成玩家任务)。
    //    会盟/附庸/内战不拦(纯新闻或内嵌在派系分裂流程里, 硬拦会留悬空派系; 远处战争被拦后它们自然变少)。
    //    半径/科技/玩家定居点缓存 2000 ticks, 拦截只发生在日级事件检查里, 符合性能铁律 §9。
    [StaticConstructorOnStartup]
    public static class UsabilityPatch
    {
        private const int PinnedHomelandRadius = 48;
        private const string ModTypeName = "BordersOfTheRim.BordersOfTheRimMod";
        private const string SettingsTypeName = "BordersOfTheRim.BordersOfTheRimSettings";
        private const int SwitchSentinel = -9999;

        private static readonly Dictionary<int, int> OperandSizes = BuildOperandSizes();

        static UsabilityPatch()
        {
            try
            {
                PatchUsability();
            }
            catch (Exception e)
            {
                Log.Error("[BOTR宜居放宽] 宜居补丁应用失败: " + e);
            }
            try
            {
                PatchHomelandRadius();
            }
            catch (Exception e)
            {
                Log.Error("[BOTR宜居放宽] 家园散布钉值补丁应用失败: " + e);
            }
            try
            {
                PatchEventFrequency();
            }
            catch (Exception e)
            {
                Log.Error("[BOTR事件降频] 事件降频补丁应用失败: " + e);
            }
            try
            {
                PatchEventDistance();
            }
            catch (Exception e)
            {
                Log.Error("[BOTR事件距离] 事件距离门控补丁应用失败: " + e);
            }
        }

        private static void PatchUsability()
        {
            var harmony = new Harmony("local.ratkin.botr.usability");
            var type = AccessTools.TypeByName("BordersOfTheRim.WorldGenStep_ClusterFactionSettlements");
            if (type == null)
            {
                Log.Warning("[BOTR宜居放宽] 未找到 BordersOfTheRim.WorldGenStep_ClusterFactionSettlements, 补丁未应用。");
                return;
            }
            // 三个重载里只挂 6 参实现, 4/5 参重载内部都会调用它(重载歧义铁律: 显式给参数类型)
            var canPlace = AccessTools.Method(type, "CanPlaceSettlement", new[]
            {
                typeof(PlanetLayer), typeof(PlanetTile), typeof(HashSet<int>),
                typeof(HashSet<int>), typeof(List<PlanetTile>), typeof(bool)
            });
            var suitability = AccessTools.Method(type, "SettlementSuitability", new[]
            {
                typeof(PlanetTile), typeof(Faction)
            });
            if (canPlace == null || suitability == null)
            {
                Log.Warning("[BOTR宜居放宽] 未找到 CanPlaceSettlement/SettlementSuitability 目标方法, 补丁未应用。");
                return;
            }
            harmony.Patch(canPlace, prefix: new HarmonyMethod(AccessTools.Method(typeof(UsabilityPatch), nameof(CanPlaceSettlement_Prefix))));
            harmony.Patch(suitability, prefix: new HarmonyMethod(AccessTools.Method(typeof(UsabilityPatch), nameof(SettlementSuitability_Prefix))));
            Log.Message("[BOTR宜居放宽] 已应用: 宜居 = 非水域 且 非不可通行, 不再检查群系 canBuildBase/implemented 与温度曲线。");
        }

        private static void PatchHomelandRadius()
        {
            var harmony = new Harmony("local.ratkin.botr.homelandradius");
            var settingsType = AccessTools.TypeByName(SettingsTypeName);
            var modType = AccessTools.TypeByName(ModTypeName);
            if (settingsType == null || modType == null)
            {
                Log.Warning("[BOTR宜居放宽] 未找到边境拓展设置类型, 钉值补丁未应用。");
                return;
            }

            PinHomelandRadius();

            // 设置文件每次载入都会走 ExposeData, 之后再钉一次, 保证任何读取路径都是钉值
            var expose = AccessTools.Method(settingsType, "ExposeData");
            if (expose != null)
            {
                harmony.Patch(expose, postfix: new HarmonyMethod(AccessTools.Method(typeof(UsabilityPatch), nameof(ExposeData_Postfix))));
            }

            // 滑条行在世界标签页的编译器生成委托里, 按"方法体含 ClusterRadiusTip 字符串"定位, 不依赖易变的生成名
            var sliderMethod = FindMethodContainingLdstr(settingsType.Assembly, "BOTR_Settings_ClusterRadiusTip");
            bool paramOk = sliderMethod != null && Array.Exists(sliderMethod.GetParameters(), p => p.ParameterType == typeof(Listing_Standard));
            if (paramOk)
            {
                harmony.Patch(sliderMethod, transpiler: new HarmonyMethod(AccessTools.Method(typeof(UsabilityPatch), nameof(RemoveClusterRadiusSlider))));
            }
            else
            {
                Log.Warning("[BOTR宜居放宽] 未找到含家园散布滑条的设置绘制委托, 滑条未移除(钉值仍生效)。");
            }
        }

        private static void PinHomelandRadius()
        {
            try
            {
                var modType = AccessTools.TypeByName(ModTypeName);
                var prop = modType == null ? null : AccessTools.Property(modType, "Settings");
                var settings = prop == null ? null : prop.GetValue(null);
                var field = AccessTools.Field(AccessTools.TypeByName(SettingsTypeName), "homelandRadius");
                if (settings != null && field != null && field.FieldType == typeof(int) && (int)field.GetValue(settings) != PinnedHomelandRadius)
                {
                    field.SetValue(settings, PinnedHomelandRadius);
                    Log.Message("[BOTR宜居放宽] 家园散布半径已钉为 " + PinnedHomelandRadius + "。");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[BOTR宜居放宽] 家园散布钉值失败: " + e);
            }
        }

        private static void ExposeData_Postfix()
        {
            PinHomelandRadius();
        }

        private static void PatchEventFrequency()
        {
            var harmony = new Harmony("local.ratkin.botr.eventfreq");
            var type = AccessTools.TypeByName("BordersOfTheRim.WorldComponent_Territories");
            if (type == null)
            {
                Log.Warning("[BOTR事件降频] 未找到 BordersOfTheRim.WorldComponent_Territories, 事件降频补丁未应用。");
                return;
            }
            PatchInterval(harmony, type, "TickWarSystem", nameof(WarIntervalTranspiler), "边境战争检查(5天→15天)");
            PatchInterval(harmony, type, "TickNegotiationSystem", nameof(NegotiationIntervalTranspiler), "索要定居点/飞地交换(4天→12天)");
            PatchInterval(harmony, type, "TickMissionSystem", nameof(MissionCheckIntervalTranspiler), "战争任务检查(2天→6天)");
            PatchInterval(harmony, type, "TryOfferTerritorialMission", nameof(MissionCooldownIntervalTranspiler), "每战任务冷却(5天→15天)");
            PatchInterval(harmony, type, "TickPoliticsSystem", nameof(PoliticsIntervalTranspiler), "会盟检查(3天→9天)");
            PatchInterval(harmony, type, "TickDependencySystem", nameof(DependencyIntervalTranspiler), "附庸/保护检查(4天→12天)");
        }

        private static void PatchInterval(Harmony harmony, Type type, string targetName, string transpilerName, string label)
        {
            var target = AccessTools.Method(type, targetName);
            var transpiler = AccessTools.Method(typeof(UsabilityPatch), transpilerName);
            if (target == null || transpiler == null)
            {
                Log.Warning("[BOTR事件降频] 未找到 " + targetName + " 或转译器, " + label + " 未应用。");
                return;
            }
            harmony.Patch(target, transpiler: new HarmonyMethod(transpiler));
        }

        // 把方法体里唯一一处 ldc.r4 <from> 常量改为 <to>; 命中数≠1 视为目标漂移, 原样返回
        private static IEnumerable<CodeInstruction> ScaleIntervalFloat(IEnumerable<CodeInstruction> instructions, float from, float to, string label)
        {
            var codes = new List<CodeInstruction>(instructions);
            int hit = -1;
            int count = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float f && Math.Abs(f - from) < 0.5f)
                {
                    count++;
                    hit = i;
                }
            }
            if (count != 1)
            {
                Log.Warning("[BOTR事件降频] " + label + ": 目标常量 " + from + " 命中 " + count + " 处(应为 1), 放弃替换。");
                return codes;
            }
            codes[hit].operand = to;
            Log.Message("[BOTR事件降频] " + label + " 已应用。");
            return codes;
        }

        private static IEnumerable<CodeInstruction> WarIntervalTranspiler(IEnumerable<CodeInstruction> instructions)
            => ScaleIntervalFloat(instructions, 300000f, 900000f, "边境战争检查(5天→15天)");

        private static IEnumerable<CodeInstruction> NegotiationIntervalTranspiler(IEnumerable<CodeInstruction> instructions)
            => ScaleIntervalFloat(instructions, 240000f, 720000f, "索要定居点/飞地交换(4天→12天)");

        private static IEnumerable<CodeInstruction> MissionCheckIntervalTranspiler(IEnumerable<CodeInstruction> instructions)
            => ScaleIntervalFloat(instructions, 120000f, 360000f, "战争任务检查(2天→6天)");

        private static IEnumerable<CodeInstruction> MissionCooldownIntervalTranspiler(IEnumerable<CodeInstruction> instructions)
            => ScaleIntervalFloat(instructions, 300000f, 900000f, "每战任务冷却(5天→15天)");

        private static IEnumerable<CodeInstruction> PoliticsIntervalTranspiler(IEnumerable<CodeInstruction> instructions)
            => ScaleIntervalFloat(instructions, 180000f, 540000f, "会盟检查(3天→9天)");

        private static IEnumerable<CodeInstruction> DependencyIntervalTranspiler(IEnumerable<CodeInstruction> instructions)
            => ScaleIntervalFloat(instructions, 240000f, 720000f, "附庸/保护检查(4天→12天)");

        // ===== 4) 事件距离门控 =====
        private const int DistanceCacheWindowTicks = 2000;
        private static int distanceCacheTick = int.MinValue;
        private static float cachedMaxEventDistance;
        private static TechLevel lastLoggedTech = TechLevel.Undefined;
        private static readonly Dictionary<int, float> SettlementPlayerDistanceCache = new Dictionary<int, float>();
        private static readonly List<PlanetTile> PlayerSettlementTiles = new List<PlanetTile>();

        private static void PatchEventDistance()
        {
            var harmony = new Harmony("local.ratkin.botr.eventdistance");
            var territories = AccessTools.TypeByName("BordersOfTheRim.WorldComponent_Territories");
            if (territories == null)
            {
                Log.Warning("[BOTR事件距离] 未找到 BordersOfTheRim.WorldComponent_Territories, 距离门控未应用。");
                return;
            }
            var gateWarPair = AccessTools.Method(typeof(UsabilityPatch), nameof(WarPairDistanceGate));
            var gateDemandTarget = AccessTools.Method(typeof(UsabilityPatch), nameof(DemandTargetDistanceGate));
            var gateVisibleRaid = AccessTools.Method(typeof(UsabilityPatch), nameof(VisibleRaidDistanceGate));
            var gateMissionFactory = AccessTools.Method(typeof(UsabilityPatch), nameof(MissionFactoryDistanceGate));
            var warBorder = AccessTools.Method(territories, "CanStartTerritorialWarAtBorder");
            var demandTarget = AccessTools.Method(territories, "ChooseDemandTarget");
            var visibleRaid = AccessTools.Method(territories, "TryLaunchVisibleWarBattle");
            if (warBorder == null || demandTarget == null || visibleRaid == null || gateWarPair == null
                || gateDemandTarget == null || gateVisibleRaid == null || gateMissionFactory == null)
            {
                Log.Warning("[BOTR事件距离] 未找到距离门控目标方法, 补丁未应用。");
                return;
            }
            harmony.Patch(warBorder, prefix: new HarmonyMethod(gateWarPair));
            harmony.Patch(demandTarget, prefix: new HarmonyMethod(gateDemandTarget));
            harmony.Patch(visibleRaid, prefix: new HarmonyMethod(gateVisibleRaid));
            string[] factories =
            {
                "CreateCaptureMission", "CreateSabotageMission", "CreateHoldFrontierMission",
                "CreateBreakSiegeMission", "CreateEscortMission", "CreateDefendOccupiedMission"
            };
            foreach (string name in factories)
            {
                var factory = AccessTools.Method(territories, name);
                if (factory == null)
                {
                    Log.Warning("[BOTR事件距离] 未找到任务工厂 " + name + ", 该工厂未门控。");
                    continue;
                }
                harmony.Patch(factory, prefix: new HarmonyMethod(gateMissionFactory));
            }
            Log.Message("[BOTR事件距离] 已应用: 宣战配对/索要选址/可见战团/任务工厂 按 玩家科技等级→事件半径 门控。");
        }

        // 宣战配对门槛: 两个派系任一定居点离玩家超出事件半径 → 不许在它们边境开战(含被拒索要升级为战争)
        private static bool WarPairDistanceGate(Faction first, Faction second, ref bool __result)
        {
            if (first == null || second == null || PairWithinEventRange(first, second))
            {
                return true;
            }
            __result = false;
            return false;
        }

        // 索要选址: 配对过远 → 返回 null, TryIssueTerritorialDemand 对该配对不出候选
        private static bool DemandTargetDistanceGate(Faction claimant, Faction respondent, ref Settlement __result)
        {
            if (claimant == null || respondent == null || PairWithinEventRange(claimant, respondent))
            {
                return true;
            }
            __result = null;
            return false;
        }

        // 可见战团: 战略目标过远 → 不发世界地图行进部队, 调用方原逻辑回退幕后结算, 战争照常推进不冻死
        private static bool VisibleRaidDistanceGate(TerritorialWar war, out string detail, ref bool __result)
        {
            detail = null;
            if (war == null || !WarObjectiveBeyondEventRange(war))
            {
                return true;
            }
            detail = "[BOTR事件距离] 战略目标距玩家过远, 不发可见战团, 改为幕后结算。";
            __result = false;
            return false;
        }

        // 任务工厂: 战争目标过远 → 返回 null, TryOfferTerritorialMission 逐工厂换下一个, 全 null 则本轮不出任务
        private static bool MissionFactoryDistanceGate(TerritorialWar war, ref TerritorialMission __result)
        {
            if (war == null || !WarObjectiveBeyondEventRange(war))
            {
                return true;
            }
            __result = null;
            return false;
        }

        private static bool PairWithinEventRange(Faction first, Faction second)
        {
            RefreshDistanceCache();
            if (cachedMaxEventDistance <= 0f)
            {
                return true;
            }
            var surface = SurfaceLayer();
            if (surface == null)
            {
                return true;
            }
            float firstDist = FactionMinDistanceToPlayer(surface, first);
            if (firstDist <= cachedMaxEventDistance)
            {
                return true;
            }
            float secondDist = FactionMinDistanceToPlayer(surface, second);
            return secondDist <= cachedMaxEventDistance;
        }

        private static bool WarObjectiveBeyondEventRange(TerritorialWar war)
        {
            RefreshDistanceCache();
            if (cachedMaxEventDistance <= 0f)
            {
                return false;
            }
            var objective = SettlementById(war.objectiveSettlementId);
            if (objective == null)
            {
                return false;
            }
            var surface = SurfaceLayer();
            if (surface == null)
            {
                return false;
            }
            return SettlementMinDistanceToPlayer(surface, objective) > cachedMaxEventDistance;
        }

        private static PlanetLayer SurfaceLayer()
        {
            var grid = Find.WorldGrid;
            return grid == null ? null : (PlanetLayer)grid.Surface;
        }

        private static void RefreshDistanceCache()
        {
            int now = (Find.TickManager != null) ? Find.TickManager.TicksGame : 0;
            if (distanceCacheTick != int.MinValue && now >= distanceCacheTick && now - distanceCacheTick < DistanceCacheWindowTicks)
            {
                return;
            }
            distanceCacheTick = now;
            SettlementPlayerDistanceCache.Clear();
            PlayerSettlementTiles.Clear();
            var settlements = Find.WorldObjects?.Settlements;
            var surface = SurfaceLayer();
            if (settlements != null && surface != null)
            {
                for (int i = 0; i < settlements.Count; i++)
                {
                    var s = settlements[i];
                    if (s == null || s.Destroyed || s.Faction != Faction.OfPlayer || ((WorldObject)s).Tile.Layer != surface)
                    {
                        continue;
                    }
                    PlayerSettlementTiles.Add(((WorldObject)s).Tile);
                }
            }
            cachedMaxEventDistance = ComputeMaxEventDistance(surface);
        }

        private static float ComputeMaxEventDistance(PlanetLayer surface)
        {
            if (surface == null || PlayerSettlementTiles.Count == 0)
            {
                return 0f;
            }
            float radius = (float)Math.Sqrt((double)surface.TilesCount / (4.0 * Math.PI));
            TechLevel tech = PlayerMaxFinishedTechLevel();
            float fraction = RadiusFractionFor(tech);
            if (tech != lastLoggedTech)
            {
                lastLoggedTech = tech;
                Log.Message("[BOTR事件距离] 星球半径≈" + radius.ToString("0") + " 格, 玩家科技档=" + tech
                    + " → 最大事件半径≈" + (radius * fraction).ToString("0") + " 格");
            }
            return radius * fraction;
        }

        private static TechLevel PlayerMaxFinishedTechLevel()
        {
            TechLevel best = TechLevel.Undefined;
            foreach (var def in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                if (def != null && def.IsFinished && def.techLevel > best)
                {
                    best = def.techLevel;
                }
            }
            return (best == TechLevel.Undefined) ? TechLevel.Neolithic : best;
        }

        // 星球半径系数表: 调"事件能有多远"只改这里
        private static float RadiusFractionFor(TechLevel tech)
        {
            switch (tech)
            {
                case TechLevel.Archotech:
                    return float.PositiveInfinity;
                case TechLevel.Ultra:
                    return 1.1f;
                case TechLevel.Spacer:
                    return 0.85f;
                case TechLevel.Industrial:
                    return 0.65f;
                case TechLevel.Medieval:
                    return 0.45f;
                default:
                    return 0.3f;
            }
        }

        private static float FactionMinDistanceToPlayer(PlanetLayer surface, Faction faction)
        {
            if (faction == null)
            {
                return float.MaxValue;
            }
            var settlements = Find.WorldObjects.Settlements;
            float best = float.MaxValue;
            for (int i = 0; i < settlements.Count; i++)
            {
                var s = settlements[i];
                if (s == null || s.Destroyed || s.Faction != faction)
                {
                    continue;
                }
                float d = SettlementMinDistanceToPlayer(surface, s);
                if (d < best)
                {
                    best = d;
                }
            }
            return best;
        }

        private static float SettlementMinDistanceToPlayer(PlanetLayer surface, Settlement settlement)
        {
            int id = ((WorldObject)settlement).ID;
            if (SettlementPlayerDistanceCache.TryGetValue(id, out float cached))
            {
                return cached;
            }
            float best = float.MaxValue;
            var tile = ((WorldObject)settlement).Tile;
            for (int i = 0; i < PlayerSettlementTiles.Count; i++)
            {
                float d = surface.ApproxDistanceInTiles(tile, PlayerSettlementTiles[i]);
                if (d < best)
                {
                    best = d;
                }
            }
            SettlementPlayerDistanceCache[id] = best;
            return best;
        }

        private static Settlement SettlementById(int id)
        {
            if (id < 0 || Find.WorldObjects == null)
            {
                return null;
            }
            var settlements = Find.WorldObjects.Settlements;
            for (int i = 0; i < settlements.Count; i++)
            {
                var s = settlements[i];
                if (s != null && ((WorldObject)s).ID == id)
                {
                    return s;
                }
            }
            return null;
        }

        // 从委托方法里删掉"家园散布半径"滑条整条语句。
        // 语句形如: Settings.homelandRadius = Mathf.RoundToInt(listing.SliderLabeled(Translate("BOTR_Settings_ClusterRadius", ...), ...));
        // IL 顺序: call get_Settings(存值目标) → ldloc listing(接收者) → ldstr ... → ... → stfld homelandRadius。
        // 任一结构校验不过就原样返回, 宁可不删也不产出坏 IL。
        private static IEnumerable<CodeInstruction> RemoveClusterRadiusSlider(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int ldstrIdx = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string s && s == "BOTR_Settings_ClusterRadius")
                {
                    ldstrIdx = i;
                    break;
                }
            }
            if (ldstrIdx < 2)
            {
                return codes;
            }
            var callIns = codes[ldstrIdx - 2];
            bool isSettingsGetter = (callIns.opcode == OpCodes.Call || callIns.opcode == OpCodes.Callvirt)
                && callIns.operand is MethodInfo getter && getter.Name == "get_Settings"
                && getter.DeclaringType != null && getter.DeclaringType.Name == "BordersOfTheRimMod";
            if (!isSettingsGetter || !IsLocalOrArgLoad(codes[ldstrIdx - 1].opcode))
            {
                return codes;
            }
            int end = -1;
            for (int i = ldstrIdx; i < codes.Count && i < ldstrIdx + 80; i++)
            {
                if (codes[i].opcode != OpCodes.Stfld && codes[i].opcode != OpCodes.Stsfld)
                {
                    continue;
                }
                var f = codes[i].operand as FieldInfo;
                if (f != null && f.Name == "homelandRadius" && f.DeclaringType != null && f.DeclaringType.Name == "BordersOfTheRimSettings")
                {
                    end = i;
                    break;
                }
            }
            if (end < 0)
            {
                return codes;
            }
            codes.RemoveRange(ldstrIdx - 2, end - (ldstrIdx - 2) + 1);
            Log.Message("[BOTR宜居放宽] 已从边境拓展设置界面移除\"家园散布\"滑条。");
            return codes;
        }

        private static bool IsLocalOrArgLoad(OpCode op)
        {
            return op == OpCodes.Ldloc || op == OpCodes.Ldloc_S
                || op == OpCodes.Ldloc_0 || op == OpCodes.Ldloc_1 || op == OpCodes.Ldloc_2 || op == OpCodes.Ldloc_3
                || op == OpCodes.Ldarg || op == OpCodes.Ldarg_S
                || op == OpCodes.Ldarg_0 || op == OpCodes.Ldarg_1 || op == OpCodes.Ldarg_2 || op == OpCodes.Ldarg_3;
        }

        private static MethodInfo FindMethodContainingLdstr(Assembly asm, string target)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }
            foreach (var t in types)
            {
                if (t == null)
                {
                    continue;
                }
                MethodInfo[] methods;
                try
                {
                    methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                }
                catch
                {
                    continue;
                }
                foreach (var m in methods)
                {
                    if (m.IsAbstract || m.ContainsGenericParameters)
                    {
                        continue;
                    }
                    MethodBody body;
                    try
                    {
                        body = m.GetMethodBody();
                    }
                    catch
                    {
                        continue;
                    }
                    if (body == null)
                    {
                        continue;
                    }
                    byte[] il = body.GetILAsByteArray();
                    if (il == null || il.Length == 0)
                    {
                        continue;
                    }
                    if (BodyContainsLdstr(il, m.Module, target))
                    {
                        return m;
                    }
                }
            }
            return null;
        }

        // 按指令边界走方法体, 只在真正的 ldstr 指令上解析字符串令牌(避免在操作数字节里误判)
        private static bool BodyContainsLdstr(byte[] il, Module module, string target)
        {
            int pos = 0;
            while (pos < il.Length)
            {
                int opValue;
                int opLen;
                if (il[pos] == 0xFE)
                {
                    if (pos + 1 >= il.Length)
                    {
                        return false;
                    }
                    opValue = 0xFE00 | il[pos + 1];
                    opLen = 2;
                }
                else
                {
                    opValue = il[pos];
                    opLen = 1;
                }
                int size;
                if (!OperandSizes.TryGetValue(opValue, out size))
                {
                    return false;
                }
                int operandStart = pos + opLen;
                if (size == SwitchSentinel)
                {
                    if (operandStart + 4 > il.Length)
                    {
                        return false;
                    }
                    int n = BitConverter.ToInt32(il, operandStart);
                    pos = operandStart + 4 + n * 4;
                    continue;
                }
                if (opValue == 0x72 && operandStart + 4 <= il.Length)
                {
                    int token = BitConverter.ToInt32(il, operandStart);
                    try
                    {
                        if (module.ResolveString(token) == target)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // 非法令牌, 忽略
                    }
                }
                pos = operandStart + size;
            }
            return false;
        }

        // ECMA-335 硬编码操作数尺寸表(实测 .NET 反射建表对双字节操作码不可靠, 一律硬编码; 键为完整操作码值)
        private static Dictionary<int, int> BuildOperandSizes()
        {
            return new Dictionary<int, int>
            {
                // ===== 单字节: 0 操作数 =====
                [0x00] = 0, [0x01] = 0, [0x02] = 0, [0x03] = 0, [0x04] = 0, [0x05] = 0, [0x06] = 0, [0x07] = 0,
                [0x08] = 0, [0x09] = 0, [0x0A] = 0, [0x0B] = 0, [0x0C] = 0, [0x0D] = 0, [0x14] = 0, [0x15] = 0,
                [0x16] = 0, [0x17] = 0, [0x18] = 0, [0x19] = 0, [0x1A] = 0, [0x1B] = 0, [0x1C] = 0, [0x1D] = 0,
                [0x1E] = 0, [0x25] = 0, [0x26] = 0, [0x2A] = 0,
                [0x46] = 0, [0x47] = 0, [0x48] = 0, [0x49] = 0, [0x4A] = 0, [0x4B] = 0, [0x4C] = 0, [0x4D] = 0,
                [0x4E] = 0, [0x4F] = 0, [0x50] = 0, [0x51] = 0, [0x52] = 0, [0x53] = 0, [0x54] = 0, [0x55] = 0,
                [0x56] = 0, [0x57] = 0, [0x58] = 0, [0x59] = 0, [0x5A] = 0, [0x5B] = 0, [0x5C] = 0, [0x5D] = 0,
                [0x5E] = 0, [0x5F] = 0, [0x60] = 0, [0x61] = 0, [0x62] = 0, [0x63] = 0, [0x64] = 0, [0x65] = 0,
                [0x66] = 0, [0x67] = 0, [0x68] = 0, [0x69] = 0, [0x6A] = 0, [0x6B] = 0, [0x6C] = 0, [0x6D] = 0,
                [0x6E] = 0, [0x76] = 0, [0x7A] = 0,
                [0x82] = 0, [0x83] = 0, [0x84] = 0, [0x85] = 0, [0x86] = 0, [0x87] = 0, [0x88] = 0, [0x89] = 0,
                [0x8A] = 0, [0x8B] = 0, [0x8E] = 0,
                [0x90] = 0, [0x91] = 0, [0x92] = 0, [0x93] = 0, [0x94] = 0, [0x95] = 0, [0x96] = 0, [0x97] = 0,
                [0x98] = 0, [0x99] = 0, [0x9A] = 0, [0x9B] = 0, [0x9C] = 0, [0x9D] = 0, [0x9E] = 0, [0x9F] = 0,
                [0xA0] = 0, [0xA1] = 0, [0xA2] = 0,
                [0xB3] = 0, [0xB4] = 0, [0xB5] = 0, [0xB6] = 0, [0xB7] = 0, [0xB8] = 0, [0xB9] = 0, [0xBA] = 0,
                [0xC3] = 0, [0xD1] = 0, [0xD2] = 0, [0xD3] = 0, [0xD4] = 0, [0xD5] = 0, [0xD6] = 0, [0xD7] = 0,
                [0xD8] = 0, [0xD9] = 0, [0xDA] = 0, [0xDB] = 0, [0xDC] = 0, [0xDF] = 0, [0xE0] = 0,
                // ===== 单字节: 1 操作数 =====
                [0x0E] = 1, [0x0F] = 1, [0x10] = 1, [0x11] = 1, [0x12] = 1, [0x13] = 1, [0x1F] = 1,
                [0x2B] = 1, [0x2C] = 1, [0x2D] = 1, [0x2E] = 1, [0x2F] = 1, [0x30] = 1, [0x31] = 1, [0x32] = 1,
                [0x33] = 1, [0x34] = 1, [0x35] = 1, [0x36] = 1, [0x37] = 1, [0xDE] = 1,
                // ===== 单字节: 4 操作数 =====
                [0x20] = 4, [0x22] = 4, [0x27] = 4, [0x28] = 4, [0x29] = 4,
                [0x38] = 4, [0x39] = 4, [0x3A] = 4, [0x3B] = 4, [0x3C] = 4, [0x3D] = 4, [0x3E] = 4, [0x3F] = 4,
                [0x40] = 4, [0x41] = 4, [0x42] = 4, [0x43] = 4, [0x44] = 4,
                [0x6F] = 4, [0x70] = 4, [0x71] = 4, [0x72] = 4, [0x73] = 4, [0x74] = 4, [0x75] = 4, [0x79] = 4,
                [0x7B] = 4, [0x7C] = 4, [0x7D] = 4, [0x7E] = 4, [0x7F] = 4, [0x80] = 4, [0x81] = 4,
                [0x8C] = 4, [0x8D] = 4, [0x8F] = 4, [0xA3] = 4, [0xA4] = 4, [0xA5] = 4,
                [0xC2] = 4, [0xC6] = 4, [0xD0] = 4, [0xDD] = 4,
                // ===== 单字节: 8 操作数 =====
                [0x21] = 8, [0x23] = 8,
                // ===== 单字节: switch =====
                [0x45] = SwitchSentinel,
                // ===== 双字节(0xFE 前缀) =====
                [0xFE00] = 0, [0xFE01] = 0, [0xFE02] = 0, [0xFE03] = 0, [0xFE04] = 0, [0xFE05] = 0,
                [0xFE06] = 4, [0xFE07] = 4, [0xFE09] = 2, [0xFE0A] = 2, [0xFE0B] = 2, [0xFE0C] = 2, [0xFE0D] = 2,
                [0xFE0E] = 2, [0xFE0F] = 0, [0xFE11] = 0, [0xFE12] = 1, [0xFE13] = 0, [0xFE14] = 0,
                [0xFE15] = 4, [0xFE16] = 4, [0xFE17] = 0, [0xFE18] = 0, [0xFE19] = 1, [0xFE1A] = 0,
                [0xFE1C] = 4, [0xFE1D] = 0, [0xFE1E] = 0,
            };
        }

        private static bool CanPlaceSettlement_Prefix(PlanetLayer layer, PlanetTile tile, HashSet<int> blockedTiles,
            HashSet<int> reservedTiles, List<PlanetTile> neighbors, bool requireSiblingBuffer, ref bool __result)
        {
            if (!tile.Valid || blockedTiles.Contains(tile.tileId) || reservedTiles.Contains(tile.tileId))
            {
                __result = false;
                return false;
            }
            var t = layer[tile];
            if (t == null || t.WaterCovered || (int)t.hilliness == (int)Hilliness.Impassable)
            {
                __result = false;
                return false;
            }
            neighbors.Clear();
            layer.GetTileNeighbors(tile, neighbors);
            if (requireSiblingBuffer)
            {
                for (int i = 0; i < neighbors.Count; i++)
                {
                    if (reservedTiles.Contains(neighbors[i].tileId))
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            __result = true;
            return false;
        }

        private static bool SettlementSuitability_Prefix(PlanetTile tile, Faction faction, ref float __result)
        {
            var t = tile.Tile;
            bool usable = t != null && !t.WaterCovered && (int)t.hilliness != (int)Hilliness.Impassable;
            __result = usable ? 1f : 0f;
            return false;
        }
    }
}

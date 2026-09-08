// [守卫] GameRules.DesignatorAllowed NRE 防护(2026-09-03 真凶已定位, 守卫转为静默兜底保留)
// 真凶: 1trickPwnyta's Defaults(Defaults.dll) 用 Postfix 整体替换 Zone 分类的
//       AllResolvedAndIdeoDesignators, 其 AddDesignators 状态机在
//       "内置物资仓库设计器 FirstOrDefault==null(被 ArchitectSense HideDesignator 移出
//       Zone 列表 或 SK 环境改造) + 用户保存的默认区(DefaultStockpileZones)含
//       Designator_ZoneAddStockpile_Resources 型" 时直接 yield null
//       (Defaults.decompiled.cs 16389 / 16428-16432, 无 null 保护)。
// 触发链: Building.GetGizmos→BuildCopyCommand→FindAllowedDesignator(顶层循环遍历各分类
//       AllResolvedAndIdeoDesignators)→FindAllowedDesignatorRecursive(对元素不判空)
//       →GameRules.DesignatorAllowed(null)→原版 d.GetType() NRE(反编译 47/52 行)。
// 处置(本文件两件事):
//   ① FindAllowedDesignatorRecursive 加 null 前缀守卫: designator==null 时直接返回
//      null 跳过原版(根治复制扫描, 不依赖补丁顺序, 对未来同类 null 源通用);
//   ② DesignatorAllowed 侧保留兜底: prefix 首次调用重建 null 集合(旧存档缺节点的 Scribe 坑)
//      + null 设计器静默放行; finalizer 吞掉首个异常按"允许"返回, 不阻断游戏。
// 并入 HSKFixPack.dll。手动 Patch(与 BillGiverDiag 同理, 避开 FacilityCrashFix 的 PatchAll)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GameRulesDesignatorGuard
{
    [StaticConstructorOnStartup]
    public static class GameRulesDesignatorGuardInit
    {
        internal static FieldInfo FBuildings;
        internal static FieldInfo FTypes;
        private static bool _repaired;
        private static bool _loggedException;

        static GameRulesDesignatorGuardInit()
        {
            try
            {
                var target = AccessTools.Method(typeof(GameRules), "DesignatorAllowed");
                if (target == null)
                {
                    Log.Error("[GameRulesDesignatorGuard] 未找到 GameRules.DesignatorAllowed");
                    return;
                }
                FBuildings = AccessTools.Field(typeof(GameRules), "disallowedBuildings");
                FTypes = AccessTools.Field(typeof(GameRules), "disallowedDesignatorTypes");
                var harmony = new Harmony("local.hskfixpack.gamerulesdesignatorguard");
                // 根治: 复制扫描顶层循环若遇 null 设计器(真凶=Defaults.dll Zone 包装器 yield null),
                // 在进入 DesignatorAllowed 之前就按"无匹配"短路, 不打扰规则判定。
                var recTarget = AccessTools.Method(
                    typeof(BuildCopyCommandUtility), "FindAllowedDesignatorRecursive",
                    new[] { typeof(Designator), typeof(BuildableDef), typeof(bool) });
                if (recTarget != null)
                {
                    harmony.Patch(recTarget,
                        prefix: new HarmonyMethod(typeof(GameRulesDesignatorGuardInit), "RecursionNullPrefix"));
                }
                else
                {
                    Log.Warning("[GameRulesDesignatorGuard] 未找到 FindAllowedDesignatorRecursive, 递归 null 守卫未挂载");
                }
                harmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(GameRulesDesignatorGuardInit), "Prefix"),
                    finalizer: new HarmonyMethod(typeof(GameRulesDesignatorGuardInit), "Finalizer"));
                Log.Message("[GameRulesDesignatorGuard] 已加载");
            }
            catch (Exception e)
            {
                Log.Warning("[GameRulesDesignatorGuard] 加载失败: " + e);
            }
        }

        // 热路径(每次 gizmo 刷新每个设计器一次): 常态只做一次 bool 判断
        public static bool Prefix(Designator d, ref bool __result, GameRules __instance)
        {
            if (!_repaired)
            {
                _repaired = true;
                if (FBuildings != null && FBuildings.GetValue(__instance) == null)
                {
                    FBuildings.SetValue(__instance, new HashSet<ThingDef>());
                    Log.Warning("[GameRulesDesignatorGuard] disallowedBuildings 为 null, 已重建(旧存档缺节点)");
                }
                if (FTypes != null && FTypes.GetValue(__instance) == null)
                {
                    FTypes.SetValue(__instance, new HashSet<Type>());
                    Log.Warning("[GameRulesDesignatorGuard] disallowedDesignatorTypes 为 null, 已重建(旧存档缺节点)");
                }
            }
            if (d == null)
            {
                // 静默兜底(真凶已被 FindAllowedDesignatorRecursive 前缀守卫拦下; 此分支防其余调用方)
                __result = true;
                return false;
            }
            return true;
        }

        // 根治守卫: 复制扫描的递归入口收到 null 设计器时, 按"无匹配"短路跳过原版,
        // null 不再进入 DesignatorAllowed(原版 47/52 行 d.GetType() 对 null 必 NRE)。
        public static bool RecursionNullPrefix(Designator designator, ref Designator_Build __result)
        {
            if (designator == null)
            {
                __result = null;
                return false;
            }
            return true;
        }

        public static Exception Finalizer(Exception __exception, Designator d, ref bool __result, GameRules __instance)
        {
            if (__exception == null) return null;
            if (!_loggedException)
            {
                _loggedException = true;
                string dDesc = d == null ? "null" : d.GetType().FullName;
                string bDesc = (FBuildings == null || FBuildings.GetValue(__instance) == null) ? "null" : "ok";
                string tDesc = (FTypes == null || FTypes.GetValue(__instance) == null) ? "null" : "ok";
                Log.Error("[GameRulesDesignatorGuard] DesignatorAllowed 异常被吞(按允许返回): d=" + dDesc
                    + " | disallowedBuildings=" + bDesc
                    + " | disallowedDesignatorTypes=" + tDesc
                    + "\n" + __exception
                    + "\n调用点:\n" + TrimStack());
            }
            __result = true;
            return null;
        }

        private static string TrimStack()
        {
            try
            {
                var frames = new System.Diagnostics.StackTrace(2, false).GetFrames();
                if (frames == null) return "(无栈)";
                var sb = new System.Text.StringBuilder();
                int n = 0;
                foreach (var f in frames)
                {
                    var m = f.GetMethod();
                    sb.Append("  at ").Append(m.DeclaringType != null ? m.DeclaringType.FullName + "." : "")
                      .Append(m.Name).Append('\n');
                    if (++n >= 8) break;
                }
                return sb.ToString();
            }
            catch
            {
                return "(栈不可用)";
            }
        }
    }
}

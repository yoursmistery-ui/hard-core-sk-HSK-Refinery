// [功能] Tech Advancing 科研工作量调整(2026-09-03)
// 两件事, 都叠在 TechAdvancing 自身的 CostFactor 计算之上:
//   (A) 全局科研工作量倍率: 强制 = ×1.5(150%), 覆盖 TechAdvancing 的 configChangeResearchCostFac 滑条
//       (存档里那份缓存值也一并顶掉 —— 滑条对全局倍率失效, 由本补丁统一决定)。
//   (B) 开局科技额外惩罚: 按【初始科技水平】加惩罚。太空(Spacer)开局=无惩罚; 每往前(更低)一档 +20% 工作量。
//       Industrial→×1.2 / Medieval→×1.4 / Neolithic→×1.6 ...  ≥Spacer 开局→无惩罚(下限锁 0)。
//
// 原理(反编译 TechAdvancing.dll + Assembly-CSharp 确认):
//   Verse.ResearchProjectDef.CostFactor(TechLevel) 是"科研工作量"唯一入口 ——
//     · 显示花费 CostApparent = Cost * CostFactor(...)
//     · 实际进度 ResearchManager.ResearchPerformed 里 amount /= CostFactor(...)
//   TechAdvancing 用【Harmony postfix】按"当前科技等级"改写返回值, 并在最末尾无条件 `__result *= ConfigChangeResearchCostFacAsFloat()`(全局倍率)。
//   本补丁挂一个【finalizer】: 严格晚于所有 postfix(含 TechAdvancing 那份), __result 已含其全局倍率。
//   于是先 `__result /= 其全局倍率`(把滑条的影响除干净), 再 `× 我们的全局倍率 × 开局惩罚`。
//
// "初始科技水平"取 TechAdvancing 的 TA_ResearchManager.factionDefault(开局捕获一次、后续不随升级变), 反射读取。
// 未安装 TechAdvancing → 类型/成员取不到 → 不打补丁, 原版花费完全不变(no-op)。
//
// 并入 HSKFixPack.dll。手动 Patch(与 BeggarRequestFallbackFix/GameRulesDesignatorGuard 同理, 避开 FacilityCrashFix 的 assembly-wide PatchAll)。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;   // TechLevel
using UnityEngine; // Time(热路径节流)
using Verse;      // ResearchProjectDef / Log / AccessTools

namespace TechAdvancingStartPenaltyFix
{
    [StaticConstructorOnStartup]
    public static class TechAdvancingStartPenaltyInit
    {
        // ===== 可调参数 =====
        private const float GlobalResearchFactor = 1.5f;         // (A) 全局科研工作量倍率: ×1.5 = 150%。改这里调幅度
        private const int SpacerAnchor = (int)TechLevel.Spacer;  // (B) 无惩罚基准档 = Spacer
        private const float PenaltyPerLevel = 0.20f;             // (B) 每低一档 +20% 工作量

        private static FieldInfo _factionDefault;                // TechAdvancing.TA_ResearchManager.factionDefault
        private static MethodInfo _costFacMethod;                // TechAdvancing.TechAdvancing_Config_Tab.ConfigChangeResearchCostFacAsFloat()

        // 热路径节流: CostFactor 每帧可能被调多次(研究进度/界面显示), 反射读+装箱不该每调都做。
        // 组合系数(= 全局 × 开局惩罚 ÷ TechAdvancing自带全局)至多每 0.25s 重算一次, 其余帧直接用缓存。
        private const float RefreshInterval = 0.25f;
        private static float _cachedMult = GlobalResearchFactor;
        private static float _lastCompute = -999f;

        static TechAdvancingStartPenaltyInit()
        {
            try
            {
                Type taMgr = AccessTools.TypeByName("TechAdvancing.TA_ResearchManager");
                if (taMgr == null)
                {
                    Log.Message("[TechAdvancingStartPenalty] 未检测到 Tech Advancing, 科研工作量调整不启用。");
                    return;
                }
                _factionDefault = taMgr.GetField("factionDefault", BindingFlags.Public | BindingFlags.Static);

                Type taCfg = AccessTools.TypeByName("TechAdvancing.TechAdvancing_Config_Tab");
                if (taCfg != null)
                    _costFacMethod = AccessTools.Method(taCfg, "ConfigChangeResearchCostFacAsFloat");

                var target = AccessTools.Method(
                    typeof(ResearchProjectDef), "CostFactor", new[] { typeof(TechLevel) });
                if (target == null)
                {
                    Log.Error("[TechAdvancingStartPenalty] 未找到 ResearchProjectDef.CostFactor, 调整未启用。");
                    return;
                }

                var harmony = new Harmony("local.hskfixpack.techadvancingstartpenalty");
                harmony.Patch(target,
                    finalizer: new HarmonyMethod(typeof(TechAdvancingStartPenaltyInit), "Finalizer"));
                Log.Message("[TechAdvancingStartPenalty] 已挂载: 全局科研工作量 ×" + GlobalResearchFactor
                    + "; 开局科技惩罚(基准 Spacer, 每低一档 +" + (int)(PenaltyPerLevel * 100) + "%)。");
            }
            catch (Exception e)
            {
                Log.Warning("[TechAdvancingStartPenalty] 加载失败: " + e);
            }
        }

        // finalizer 始终在所有 postfix(含 TechAdvancing)之后执行, 故此处 __result 已含其全局倍率。
        public static Exception Finalizer(Exception __exception, ref float __result)
        {
            if (__exception != null)
                return __exception; // 出异常时原样抛出, 不去改动一个可能未初始化的 __result

            // 低频重算组合系数(反射读只在重算时发生), 每帧热路径只做一次乘法。
            float now = Time.realtimeSinceStartup;
            if (now - _lastCompute >= RefreshInterval)
            {
                _lastCompute = now;
                float theirFac = TheirGlobalFac();                 // TechAdvancing 末尾乘的全局倍率
                float inv = theirFac > 0.0001f ? GlobalResearchFactor / theirFac : GlobalResearchFactor;
                _cachedMult = inv * CalcStartPenalty();
            }

            __result = (float)Math.Round(__result * _cachedMult, 2);
            return null;
        }

        // TechAdvancing 末尾乘的那个全局倍率(读不到 → 返回 1, 即不除)。
        private static float TheirGlobalFac()
        {
            if (_costFacMethod == null)
                return 1f;
            try
            {
                object v = _costFacMethod.Invoke(null, null);   // boxed float
                return v == null ? 1f : (float)v;
            }
            catch
            {
                return 1f;
            }
        }

        // 返回 >=1 的开局惩罚系数; 未初始化 / 基准及以上 → 1(无惩罚)。
        private static float CalcStartPenalty()
        {
            if (_factionDefault == null)
                return 1f;
            int start;
            try
            {
                object v = _factionDefault.GetValue(null);       // TechLevel 装箱
                start = v == null ? 0 : (int)v;
            }
            catch
            {
                return 1f;
            }
            if (start <= (int)TechLevel.Undefined)               // 0 = 尚未捕获初始等级, 先不惩罚
                return 1f;

            int below = SpacerAnchor - start;                    // 比 Spacer 低几档
            if (below <= 0)
                return 1f;

            return 1f + PenaltyPerLevel * below;
        }
    }
}

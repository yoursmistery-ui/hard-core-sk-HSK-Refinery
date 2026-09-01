// SRAI(Smarter Raid AI, pogo.ai)机械体袭击 AISapper 异常摘除(2026-08-15 用户授权)
//
// 背景: SRAI 的 JobGiver_AISapper_TryGiveJob_Patch.Prefix 每 5 秒全图扫描一次
// (尸体/倒地者/殖民者装备/殖民者炮塔),对每个 ai_combatDangerous 建筑用反射
// 读取 CE 字段(GunCompEq/Active/PowerComp/CurrentTarget/EmptyMagazine/IsMannable/
// MannableComp/powerComp 等属性名硬编码),异常偏移 0x443 落在
// "call Create + ldstr 'powerComp' + callvirt Field" 反射链上 —— 炮塔某字段
// 反射结果 null 后继续调用 → NullReferenceException;整个方法 try-catch 包着,
// catch 里打印 "SRAI Exception: " + Log.Error → 每 5 秒刷一条;
// 同时 Prefix 返回 false 跳过原方法(JobGiver_AISapper.TryGiveJob),
// 机械体袭击者拿不到有效任务 → vanilla AI 反复重试 → "started 10 jobs in one tick"
// 任务风暴(2026-08-15 日志: 80+ SRAI Exception + 5 条 10-jobs)。
//
// 方案: 摘掉 SRAI 挂在 RimWorld.JobGiver_AISapper.TryGiveJob 上的 prefix
// (Harmony 按 owner 定位,owner = "pogo.ai"),机械体/袭击者恢复原版 AISapper。
// 只动这一个方法,保留 SRAI 其他功能(攻城/破墙/战斗 AI 等)。不动 PogoAI.dll 本体
// (workshop mod,更新会被覆盖;放 HSK修复整合 里永续生效)。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SRaidAIFix
{
    [StaticConstructorOnStartup]
    public static class SRaidAIFixInit
    {
        static SRaidAIFixInit()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(JobGiver_AISapper), "TryGiveJob");
                if (target == null)
                {
                    Log.Warning("[SRaidAIFix] JobGiver_AISapper.TryGiveJob not found");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.sraifix");
                Patches patchInfo = Harmony.GetPatchInfo(target);
                bool removed = false;
                if (patchInfo != null && patchInfo.Prefixes != null)
                {
                    foreach (Patch p in patchInfo.Prefixes)
                    {
                        if (p.owner != null && p.owner.IndexOf("pogo", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            harmony.Unpatch(target, HarmonyPatchType.Prefix, p.owner);
                            Log.Message("[SRaidAIFix] unpatched SRAI prefix on JobGiver_AISapper.TryGiveJob (owner=" + p.owner + ")");
                            removed = true;
                        }
                    }
                }
                if (!removed)
                {
                    Log.Message("[SRaidAIFix] no SRAI prefix found on JobGiver_AISapper.TryGiveJob (SRAI not loaded or already clean)");
                }
            }
            catch (Exception e)
            {
                Log.Error("[SRaidAIFix] patch failed: " + e);
            }
        }
    }
}

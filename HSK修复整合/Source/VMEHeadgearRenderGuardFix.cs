// VMEHeadgearRenderGuardFix.cs — VME HeadgearVisible 渲染 NRE 防御(Animation Mod 合成渲染通道)
//
// 问题: Animation Mod(co.uk.epicguru.meleeanimation)用自己的合成渲染通道画处决/处刑动画
// (AM.AnimRenderer.DrawPawns → PawnRenderer.RenderPawnAt → PawnRenderTree.ParallelPreDraw),
// VanillaMemesExpanded 挂在 PawnRenderNodeWorker_Apparel_Head.HeadgearVisible 上的 postfix 在该
// 上下文缺它假设的对象 → 每帧抛 NullReferenceException。AM 把异常吞掉并打
// "[MeleeAnim] Rendering exception when doing animation Execution: Shank",但该动画渲染残废 +
// 每帧一次异常开销 + 日志噪音(09-06 实测 4 条 + Duplicate stacktrace 刷屏)。
//
// 方案: 只给 VME 这个 postfix 方法挂 Finalizer——NRE 吞掉并保留原方法(vanilla HeadgearVisible)
// 的返回值;其余异常原样上抛不受影响。VME 未安装时 TypeByName 落空,直接 no-op。
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace VMEHeadgearRenderGuardFix
{
    [StaticConstructorOnStartup]
    public static class VMEHeadgearRenderGuardFixInit
    {
        static VMEHeadgearRenderGuardFixInit()
        {
            try
            {
                Type t = AccessTools.TypeByName(
                    "VanillaMemesExpanded.VanillaMemesExpanded_PawnRenderNodeWorker_Apparel_Head_HeadgearVisible_Patch");
                if (t == null)
                {
                    return;                                   // 未装 VanillaMemesExpanded
                }
                MethodInfo post = AccessTools.Method(t, "Postfix");
                if (post == null)
                {
                    return;
                }
                Harmony h = new Harmony("local.hskfixpack.vmeheadgearrenderguard");
                h.Patch(post, null, null, null,
                    new HarmonyMethod(typeof(VMEHeadgearRenderGuardFixInit)
                        .GetMethod("Finalizer", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[VMEHeadgearFix] 已挂 VME HeadgearVisible postfix NRE 防御(保处决动画渲染通道)");
            }
            catch (Exception e)
            {
                Log.Error("[VMEHeadgearFix] 挂载失败: " + e);
            }
        }

        private static Exception Finalizer(Exception __exception)
        {
            if (__exception is NullReferenceException)
            {
                return null;
            }
            return __exception;
        }
    }
}

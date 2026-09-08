// TickRatePacer.cs — 1.6 相机动态更新率 超速档适配(常驻,无配置页)
//
// 问题(2026-09-06 [TP2] v10/v11 实测定案): 900TPS 超速档拉近镜头锁 30fps。
//   RimWorld 1.6 给普通档 thing 加了"相机动态更新率": Thing.UpdateRateTicks =
//   GenTicks.GetCameraUpdateRate —— 视野内按相机档取 zoom+1(拉到最近=1,即每 tick 跑
//   TickInterval 主逻辑),视野外一律 15。这是为原版 60TPS 平衡的设计;在 900TPS(15 倍速)下
//   "拉到最近=每秒 900 次主逻辑/物",纯浪费,却正好撞上超速档"帧率越低→每帧塞更多 tick
//   →单价越高"的正反馈,收敛在 30fps。实测: 拉近段 tickListN 0.4~0.6ms vs 拉远段 0.16~0.35,
//   拉近镜头与 30fps 段完全对应。
//
// 原版逻辑(反编译 Assembly-CSharp):
//   GetCameraUpdateRate(thing):
//     非当前图/正在渲染世界 → 15;
//     !cameraDriver.InViewOf(thing) → 15;
//     否则 → (int)cameraDriver.CurrentZoom + 1   (CameraZoomRange: 最近0→1,最远3→4)
//   Thing.DoTick: rate = Min(Max(UpdateRateTicks, MinTickIntervalRate=1), Max=15);
//     tickDelta>=rate || IsTickInterval(hashOffset, rate) 时才跑 TickInterval()。
//   调用方全引擎仅 Thing.UpdateRateTicks 一处(asc_full.cs grep 确认),动物 Pawn 已
//   override 固定 15,Min/Max 钳制属性无子类 override。
//
// 接管方式: 前缀吞掉 GetCameraUpdateRate,视野内给 max(原值, ViewRate=8),视野外保持 15。
//   ViewRate=8 时主逻辑频率=每秒 112 次/物 @900TPS,仍高于原版 60TPS 特写(rate1=60 次)近 2 倍,
//   游戏进程无感;预期拉近段 tick 单价回落到拉远水平(~0.65ms),拉近镜头有望 80~100fps。
//   v1 曾用 4(用户 09-06 晚要求先抬到 8 观察);想微调改本常量(15≈关闭视野内加速)。
//   动物不受本补丁影响(引擎已固定 15)。
//
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TickRatePacer
{
    [StaticConstructorOnStartup]
    public static class TrpInit
    {
        const int ViewRate = 8;     // 视野内更新率下限(原版=相机档 zoom+1,最近=1;15≈关闭视野内加速)

        static TrpInit()
        {
            try
            {
                MethodBase m = AccessTools.Method(typeof(GenTicks), "GetCameraUpdateRate");
                if (m == null)
                {
                    Log.Error("[TRP] 找不到 GenTicks.GetCameraUpdateRate,相机更新率适配未启用");
                    return;
                }
                new Harmony("local.ratkin.hskfix.trp").Patch(m,
                    new HarmonyMethod(typeof(TrpInit), "Pre"), null, null, null);
                Log.Message("[TRP] 1.6 相机更新率适配已挂载: 视野内下限=" + ViewRate + " (原版=zoom+1 最低1),视野外=15 不变");            }
            catch (Exception e)
            {
                Log.Error("[TRP] 挂载失败: " + e);
            }
        }

        static bool Pre(Thing thing, ref int __result)
        {
            if (!WorldRendererUtility.DrawingMap && Find.CurrentMap != null && thing.MapHeld == Find.CurrentMap)
            {
                CameraDriver cd = Find.CameraDriver;
                if (cd != null && cd.InViewOf(thing))
                {
                    int rate = (int)cd.CurrentZoom + 1;
                    __result = rate < ViewRate ? ViewRate : rate;
                    return false;
                }
            }
            __result = 15;
            return false;
        }
    }
}

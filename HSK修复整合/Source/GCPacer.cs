// GCPacer.cs — Unity Boehm 停世界 GC → Manual + 增量分帧回收(带自动回退守卫)
//
// 问题(2026-09-06 三会话 [TP2] 实测): 每 600-tick 窗口稳定出现 1 次 25~31ms 的
// tickListN 弥散尖峰(无任何慢 thing/组件点名,跨会话复现,另有 446/493ms 同签名大尖峰)。
// RimWorld 从不启用 Unity 增量 GC(原版零引用 GarbageCollector),Boehm 停世界整代回收
// 在 5.7 万件东西/多 GB 堆上单次 25~30ms,恰为该节奏性停顿的形态;进程内存峰值 7.6GB。
//
// 修法(运行时切增量,本 Unity 版本 isIncremental 只读、无 Incremental 枚举值):
//   GCMode = Manual(停用自动停世界回收) + 每帧 GarbageCollector.CollectIncremental(2ms 预算),
//   把整代回收摊进每帧 ≤2ms。挂 Verse.Root.Update 后缀(基类,Root_Play/Root_Entry 均经
//   base.Update 走此路径 → 菜单/游戏/读档全程覆盖)。
//
// 守卫(增量若不可用必须自动回退,防堆失控):
//   ①每 600 帧查 gen0 计数与堆量: 30 秒零回收进展且堆超低水位+32MB → 判"增量没在干活"回退;
//   ②堆超历史低水位+768MB → 判"增量跟不上分配"回退;
//   回退 = GCMode=Enabled(恢复原版自动 GC)+ 一次 GC.Collect(),损失最多回到现状。
//
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using HarmonyLib;
using UnityEngine.Scripting;
using Verse;

namespace GCPacer
{
    [StaticConstructorOnStartup]
    public static class GcInit
    {
        private static bool pacerActive;
        private static bool broken;
        private static long heapLow = -1;
        private static int frame;
        private static int lastCollCount;
        private static int lastCollFrame;

        static GcInit()
        {
            try
            {
                if (GarbageCollector.isIncremental)
                {
                    Log.Message("[HSKFix] Unity 已是增量 GC,GCPacer 不接管");
                    return;
                }
                Harmony h = new Harmony("local.ratkin.hskfix.gcpacer");
                h.Patch(AccessTools.Method(AccessTools.TypeByName("Verse.Root"), "Update"),
                    null, new HarmonyMethod(typeof(GcInit), "FramePost"));
                GarbageCollector.GCMode = GarbageCollector.Mode.Manual;
                pacerActive = true;
                lastCollCount = GC.CollectionCount(0);
                lastCollFrame = 0;
                Log.Message("[HSKFix] Unity GC 已切 Manual+每帧增量回收(≤2ms/帧,目标消灭 25~30ms 停世界节奏性停顿;带自动回退守卫)");
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] GCPacer 挂载失败,保持默认 GC: " + e.Message);
            }
        }

        private static void Revert(string reason)
        {
            broken = true;
            try
            {
                GarbageCollector.GCMode = GarbageCollector.Mode.Enabled;
                GC.Collect();
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] GCPacer 回退异常: " + e.Message);
            }
            Log.Warning("[HSKFix] GCPacer 回退自动 GC: " + reason);
        }

        private static void FramePost()
        {
            if (!pacerActive || broken)
            {
                return;
            }
            frame++;
            try
            {
                GarbageCollector.CollectIncremental(2000000uL);    // ≤2ms/帧
            }
            catch (Exception e)
            {
                Revert("CollectIncremental 抛异常: " + e.Message);
                return;
            }
            if (frame % 600 != 0)
            {
                return;                                            // ~10s 一检(60fps 口径)
            }
            long heap = GC.GetTotalMemory(false);
            int cc = GC.CollectionCount(0);
            if (heapLow < 0)
            {
                heapLow = heap;
                lastCollCount = cc;
                lastCollFrame = frame;
                return;
            }
            if (heap < heapLow)
            {
                heapLow = heap;
            }
            // 守卫①: 30 秒无任何回收进展且堆超低水位+32MB → 增量没在干活
            if (cc == lastCollCount && frame - lastCollFrame >= 1800 && heap > heapLow + 33554432L)
            {
                Revert("30 秒零回收进展(堆 " + (heap >> 20) + "MB)");
                return;
            }
            if (cc != lastCollCount)
            {
                lastCollCount = cc;
                lastCollFrame = frame;
            }
            // 守卫②: 增量跟不上分配
            if (heap > heapLow + 805306368L)
            {
                Revert("堆超低水位+768MB(当前 " + (heap >> 20) + "MB)");
            }
        }
    }
}

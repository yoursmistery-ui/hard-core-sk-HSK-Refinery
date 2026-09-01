// 修复开发者日志窗口(EditWindow_Log)每帧抛 "Collection was modified" 异常。
//
// 根因(1.6.4871 反编译确认):EditWindow_Log.DoMessagesListing 用
// foreach (LogMessage message in Log.Messages) 遍历 LogMessageQueue 的活列表;
// 写入端(Log.Message/Warning/Error)虽然都 lock(logLock) + messageQueue.Enqueue,
// 但读取端不加锁 —— 当后台线程(DPA 统计线程 / Harmony 日志线程等)在 GUI 遍历
// 期间写入消息,枚举器立刻抛 InvalidOperationException。
// 每次异常:①Exception.ToString 格式化堆栈(profile 实测单次 8.7ms,最大 76.7ms,
// 每帧平均 2.12ms);②GUI 抛错后 GUIClip 不平衡,连带再刷一条 "pushing more
// GUIClips" GUI Error —— 滚雪球,开着日志窗口时帧率大幅受损。
//
// 修复:原版其实提供了 Log.LockMessages()(LogLock 递增 logDisablers,使
// PreventLogging=true,Enqueue 全部被跳过)—— 这正是官方给"遍历消息"准备的
// 机制,但 EditWindow_Log 忘了用。本补丁给 DoMessagesListing 加
// prefix(拿锁)+finalizer(释放,异常路径也保证释放),遍历期间禁止新消息
// 入队,枚举天然安全;日志窗口关闭时零开销(方法根本不会被调用)。
// 被暂停的消息只在窗口打开的同一帧内被丢弃(与原版超限丢弃行为一致,可接受)。
//
// 编译:并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using HarmonyLib;
using LudeonTK;
using Verse;

namespace LogWindowEnumerateFix
{
    [StaticConstructorOnStartup]
    public static class LogWindowEnumerateFixInit
    {
        static LogWindowEnumerateFixInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.logwindowenumeratefix");
                harmony.Patch(AccessTools.Method(typeof(EditWindow_Log), "DoMessagesListing"),
                    prefix: new HarmonyMethod(typeof(LogWindowEnumerateFixInit), "Prefix"),
                    finalizer: new HarmonyMethod(typeof(LogWindowEnumerateFixInit), "Finalizer"));
                Log.Message("[HSKFix] patched EditWindow_Log.DoMessagesListing: hold LogLock during enumeration (Collection-was-modified fixed)");
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] LogWindowEnumerateFix patch failed: " + e);
            }
        }

        private static void Prefix(ref IDisposable __state)
        {
            try
            {
                __state = Log.LockMessages();
            }
            catch
            {
                __state = null;
            }
        }

        private static void Finalizer(IDisposable __state)
        {
            try
            {
                if (__state != null)
                {
                    __state.Dispose();
                }
            }
            catch
            {
            }
        }
    }
}

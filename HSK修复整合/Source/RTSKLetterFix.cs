using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RTSKLetterFix
{
    /// <summary>
    /// 修复 ResearchTreeSK 研究完成信件按钮 KeyNotFoundException。
    ///
    /// 根因(2026-08-20 排查):
    /// ResearchTreeSK 的 ResearchLetter("研究完成"信件)按钮回调调用
    /// researchDef.Node() -&gt; Tree.ResearchsToNodesCache[research]。
    /// 但 ResearchsToNodesCache 只在 Tree.Initialize() 时填充,
    /// 而 Tree.Initialize() 仅在研究树窗口 MainTabWindow_ResearchTree.PreOpen() 里被调用
    /// (即玩家打开过研究页)。若研究在玩家从未打开研究页时完成(开局/读档后常见),
    /// 点信件「研究界面」按钮时字典为空 -&gt; KeyNotFoundException("The given key 'X' was not present")。
    ///
    /// 修复: Harmony 前缀拦截 ResearchLetter.get_Choices(按钮回调所在枚举的 getter),
    /// 若 Tree.Initialized == false,先调用 Tree.Initialize() 同步建树,再放行原 getter。
    /// Tree.Initialize() 内部用 LongEventHandler.QueueLongEvent 排队,
    /// 本前缀在 GUI 主线程(非加载事件)中调用,队列会被同步执行,之后字典即填充完成。
    /// 与官方 MainTabWindow_ResearchTree.PreOpen 触发建树的路径完全一致。
    ///
    /// 全部通过反射定位,不依赖 ResearchTreeSK 的具体程序集名/方法签名,
    /// 未装 ResearchTreeSK 时自动跳过。
    /// </summary>
    public static class RTSKLetterFixInit
    {
        public static void Init()
        {
            try
            {
                // 定位 ResearchTreeSK.Tree(静态类,含 Initialize() 与 Initialized 字段)
                Type treeType = AccessTools.TypeByName("ResearchTreeSK.Tree");
                if (treeType == null)
                {
                    Log.Message("[HSKFixPack]RTSKLetterFix ResearchTreeSK.Tree not found, skip.");
                    return;
                }
                MethodInfo initializeMethod = AccessTools.Method(treeType, "Initialize", Type.EmptyTypes);
                FieldInfo initializedField = AccessTools.Field(treeType, "Initialized");
                if (initializeMethod == null || initializedField == null)
                {
                    Log.Message("[HSKFixPack]RTSKLetterFix ResearchTreeSK.Tree.Initialize/Initialized not found, skip.");
                    return;
                }

                // 定位 ResearchLetter(研究完成信件)。其 Choices getter 构造按钮,
                // 按钮回调调用 researchDef.Node() -> ResearchsToNodesCache[research]。
                // 拦截 get_Choices 比拦截 Dialog_ResearchInfo.PreOpen 更可靠:
                // 无论窗口以何种方式打开,只要读取 Choices(按钮回调所在枚举)必然先经此前缀。
                Type letterType = AccessTools.TypeByName("ResearchTreeSK.ResearchLetter");
                if (letterType == null)
                {
                    Log.Message("[HSKFixPack]RTSKLetterFix ResearchTreeSK.ResearchLetter not found, skip.");
                    return;
                }
                PropertyInfo choicesProperty = AccessTools.Property(letterType, "Choices");
                if (choicesProperty == null || choicesProperty.GetGetMethod() == null)
                {
                    Log.Message("[HSKFixPack]RTSKLetterFix ResearchTreeSK.ResearchLetter.Choices not found, skip.");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.rtskletterfix");
                harmony.Patch(choicesProperty.GetGetMethod(),
                    prefix: new HarmonyMethod(typeof(RTSKLetterFixInit), "PreOpenPrefix"),
                    postfix: null);

                // 记录反射到的句柄,供前缀使用
                InitializeMethod = initializeMethod;
                InitializedField = initializedField;

                Log.Message("[HSKFixPack]RTSKLetterFix patched ResearchTreeSK.ResearchLetter.get_Choices ok.");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFixPack]RTSKLetterFix failed to patch: " + e);
            }
        }

        private static MethodInfo InitializeMethod;
        private static FieldInfo InitializedField;

        private static bool PreOpenPrefix()
        {
            try
            {
                if (InitializeMethod == null || InitializedField == null)
                {
                    return true;
                }
                bool isInitialized = (bool)InitializedField.GetValue(null);
                if (!isInitialized)
                {
                    // 建树(内部 LongEventHandler 排队;非加载事件中调用会同步执行)
                    InitializeMethod.Invoke(null, null);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFixPack]RTSKLetterFix prefix error: " + e);
            }
            return true; // 放行原方法
        }
    }

    /// <summary>Harmony 在程序集加载后自动调用的入口(StaticConstructorOnStartup 也可)。</summary>
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            RTSKLetterFixInit.Init();
        }
    }
}

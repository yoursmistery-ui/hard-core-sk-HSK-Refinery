using HarmonyLib;
using Verse;

namespace QuestBoardHSK
{
    /// <summary>
    /// 程序集入口(七期补,2026-08-31):此前整个程序集没有 PatchAll 引导,
    /// 全部 HarmonyPatch 特性从未被应用、BountyRadioManager 从未注册(潜伏 bug)。
    /// Defs 加载完统一打补丁;GameComponent 在 FinalizeInit(新开局/读档都会走到)后补注册。
    /// 八期教训:PatchAll 是整体式的,一枚补丁目标失配即中断,排在后面的全部落空——
    /// 改为逐类打补丁并隔离异常,失败者 Log.Error 后继续其余。
    /// </summary>
    [StaticConstructorOnStartup]
    internal static class QuestBoardBootstrap
    {
        static QuestBoardBootstrap()
        {
            Harmony harmony = new Harmony("QuestBoardHSK.ratkin");
            foreach (System.Type type in typeof(QuestBoardBootstrap).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                    continue;
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
                catch (System.Exception ex)
                {
                    Log.Error("QuestBoardHSK 补丁失败 " + type.Name + ": " + ex.Message);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Game), "FinalizeInit")]
    internal static class Game_FinalizeInit_RegisterRadioManager_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(Game __instance)
        {
            if (__instance.GetComponent<BountyRadioManager>() == null)
                __instance.components.Add(new BountyRadioManager(__instance));
        }
    }
}

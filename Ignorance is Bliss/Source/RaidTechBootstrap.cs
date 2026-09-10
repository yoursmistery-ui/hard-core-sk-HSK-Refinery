// RaidTechBootstrap.cs — 本程序集(HskRaidTech.dll)的 Harmony 引导入口。
//
// 背景: RaidHelperWealthTech.cs 里的补丁全是声明式 [HarmonyPatch] 属性,
//   原先靠 HSK修复整合 的 FacilityCrashFix 里那句 assembly-wide `harmony.PatchAll()` 生效。
//   搬到本 mod 后没有那个宿主程序集,必须自带引导,否则所有袭击/财富相关补丁都不会应用。
//
// 时机: [StaticConstructorOnStartup] 在 def 全部解析后、游戏主循环前执行,
//   此时 Core_SK 的 RaidHelperComponent / ProjectSettings 类型已可解析。
// 安全: 整个引导包 try/catch,单个补丁失败不会炸掉启动流程(同 HSK 既有范式)。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace RaidHelperWealthTech
{
    [StaticConstructorOnStartup]
    public static class RaidTechBootstrap
    {
        public const string HarmonyId = "local.ignoranceisbliss.raidtech";

        static RaidTechBootstrap()
        {
            try
            {
                Harmony harmony = new Harmony(HarmonyId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[Ignorance Is Bliss / HSK RaidTech] 袭击·贸易科技档与财富挂钩补丁已应用。");
            }
            catch (Exception ex)
            {
                Log.Error("[Ignorance Is Bliss / HSK RaidTech] 补丁应用失败: " + ex);
            }
        }
    }
}

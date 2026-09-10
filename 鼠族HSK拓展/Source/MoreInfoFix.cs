// 鼠族HSK拓展 - HSK More Info(GCC_HSK_More_Info)空引用修复
//
// 背景: 第三方 mod「HSK More Info」的 MoreInfo_Utils.GetMoveSpeed 在选中某些小人
// (寻路器 pather 为空/未生成/异常状态,如部分读档后的小人)时会抛
// NullReferenceException,导致 GetInspectString 报错刷日志。
// 该 mod 官方(含最新 master 构建)均未做空保护,更新 DLL 无法解决。
//
// 修法: Harmony 前缀补丁直接替换 GetMoveSpeed 为空安全版本:
//   - pawn/pather 为空或不在移动 -> 返回 0;
//   - 其余情况按原逻辑计算(60 / 进入下一格的路径成本);
//   - 1.6 中 CostToMoveIntoCell 为私有重载,用反射调用,任何异常一律回退 0。
// 仅在本 mod 环境下生效,卸载本 mod 后 More Info 恢复原样。
//
// 编译(系统 csc, C#5):
//   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:library
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
//     /r:"<RimWorld>\Mods\Harmony\Current\Assemblies\0Harmony.dll"
//     /out:MoreInfoFix.dll MoreInfoFix.cs
using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace RKMoreInfoFix
{
    [StaticConstructorOnStartup]
    public static class MoreInfoFixInit
    {
        static MoreInfoFixInit()
        {
            try
            {
                Type util = AccessTools.TypeByName("MoreInfo.MoreInfo_Utils");
                if (util == null)
                {
                    Log.Message("[RKMoreInfoFix] MoreInfo not found, skip");
                    return;
                }
                MethodInfo target = AccessTools.Method(util, "GetMoveSpeed");
                if (target == null)
                {
                    Log.Message("[RKMoreInfoFix] MoreInfo_Utils.GetMoveSpeed not found, skip");
                    return;
                }
                Harmony harmony = new Harmony("local.ratkin.clothesweapons.moreinfofix");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(MoreInfoFixInit).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[RKMoreInfoFix] patched MoreInfo_Utils.GetMoveSpeed");
            }
            catch (Exception e)
            {
                Log.Error("[RKMoreInfoFix] patch failed: " + e);
            }
        }

        // 完全替换原方法: 空安全,任何异常回退 0。
        private static bool Prefix(Pawn pawn, ref float __result)
        {
            try
            {
                if (pawn == null || pawn.pather == null || !pawn.pather.Moving)
                {
                    __result = 0f;
                    return false;
                }
                MethodInfo costMethod = AccessTools.Method(typeof(Pawn_PathFollower), "CostToMoveIntoCell");
                if (costMethod == null)
                {
                    __result = 0f;
                    return false;
                }
                int cost = Convert.ToInt32(costMethod.Invoke(pawn.pather, new object[] { pawn.pather.nextCell }));
                __result = cost > 0 ? 60f / cost : 0f;
                return false;
            }
            catch (Exception)
            {
                __result = 0f;
                return false;
            }
        }
    }
}

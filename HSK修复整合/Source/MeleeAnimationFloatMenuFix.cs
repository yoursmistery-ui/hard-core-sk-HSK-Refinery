// Melee Animation 浮空菜单 NRE 防护(MeleeAnimationFloatMenuFix, 并入 HSK 修复整合, 2026-09-04)
//
// 现象: 右键(部分物件,如别人占用的床)时日志刷
//   "Error trying to make float menu: System.NullReferenceException",
//   栈顶 AM.Patches.Patch_FloatMenuMakerMap_GetOptions.Postfix [0x00021]
//   → RimWorld.FloatMenuMakerMap.GetOptions。
//
// 根因(反编译 zAnimationMod.dll + Assembly-CSharp.dll 佐证):
//   AM 的 postfix 逻辑极简:
//     if (selectedPawns.Count == 1)
//         __result.AddRange(DraftedFloatMenuOptionsUI.GenerateMenuOptions(clickPos, selectedPawns[0]));
//   它对 __result 不判空。而 FloatMenuMakerMap.GetOptions 被多个 mod patch:
//     PREFIX sensiblebedownership(返回 bool)
//     POSTFIX SimpleSidearms / meleeanimation / sensiblebedownership
//   原版 GetOptions 各分支都 return 非 null 的 list;但 SensibleBedOwnership 的
//   Prefix 在它自己接管床菜单的场景返回 false → Harmony 跳过原版方法 → 返回值
//   __result 停在引用类型默认的 null → 后续 postfix 拿到 null。SimpleSidearms 的
//   postfix 有 `if (__result==null) return;` 防护,唯独 AM 没有 → __result.AddRange(null
//   的 receiver)→ callvirt 在 null 上 → NRE(0x21 正落在该 callvirt)。
//
// 方案: 直接给 AM 的 Postfix 方法本身打一道 Harmony Prefix(它是普通静态方法,可被 patch):
//   当注入的 __result(第 3 个形参,index 2)为 null 时返回 false 跳过原方法。
//   此时本就没有可追加选项的菜单列表,跳过不损失任何功能;__result 非 null 时原样放行,
//   AM 的征召小人处决/决斗/套索等浮空选项照常生成。不改 AM 的 DLL,workshop 更新不覆盖。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
//   用 AccessTools 运行时定位 AM 类型,不新增对 zAnimationMod.dll 的编译引用。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MeleeAnimationFloatMenuFix
{
    [StaticConstructorOnStartup]
    public static class MeleeAnimationFloatMenuFixInit
    {
        static MeleeAnimationFloatMenuFixInit()
        {
            try
            {
                Type target = AccessTools.TypeByName("AM.Patches.Patch_FloatMenuMakerMap_GetOptions");
                if (target == null)
                {
                    return; // 近战动画HSK(Melee Animation)未装,跳过
                }
                MethodInfo postfix = AccessTools.Method(target, "Postfix");
                if (postfix == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.meleeanimfloatmenufix");
                harmony.Patch(postfix, prefix: new HarmonyMethod(
                    typeof(MeleeAnimationFloatMenuFixInit).GetMethod(
                        "Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[MeleeAnimFloatMenuFix] guarded AM Patch_FloatMenuMakerMap_GetOptions.Postfix against null __result");
            }
            catch (Exception e)
            {
                Log.Error("[MeleeAnimFloatMenuFix] patch failed: " + e);
            }
        }

        // AM Postfix 形参: (Vector3 clickPos /*0*/, List<Pawn> selectedPawns /*1*/, List<FloatMenuOption> __result /*2*/)
        // __result 为 null 时跳过原方法(否则原方法 __result.AddRange 必 NRE)。
        private static bool Prefix([HarmonyArgument(2)] List<FloatMenuOption> menuOptions)
        {
            return menuOptions != null;
        }
    }
}

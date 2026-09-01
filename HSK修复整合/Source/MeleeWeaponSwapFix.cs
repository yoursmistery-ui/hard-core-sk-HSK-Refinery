// 近战小人自动换武器修复(2026-08-16 用户反馈「点击攻击不会自动从工具换武器」)。
//
// 根因(反编译 SurvivalToolsLite.dll 确认): STL 的 RightToolForJob.EquipRightWeapon(Pawn)
//   只处理远程武器 ——
//     if (primary != null && primary.def.IsRangedWeapon) return;      // 已是枪 → 不动
//     CompInventory val = ...; if (val == null) return;
//     rangedWeaponList = val.rangedWeaponList;
//     if (rangedWeaponList 空) return;                                // ← 近战小人走到这里,工具原样保留
//     ... 把主手(工具)转进背包,再从 rangedWeaponList 找有弹药/弹匣的枪换上
//   因此:
//     · 近战小人(背包只有刀剑/钉头锤,没有枪)征召时,工具永远换不成近战武器;
//     · 远程小人若枪没装弹匣,STL 会把工具收走但找不到可用枪 → 空手。
//   触发时机: 征召(Drafted setter 的 Postfix_WeaponSwitcher)与 Hunt 任务,CE 环境才走。
//
// 修复: Harmony 后置补丁挂 RightToolForJob.EquipRightWeapon(Pawn) —— STL 跑完后:
//   · 主手是工具(thingClass=SurvivalTool + SurvivalToolProperties) 或 主手已空(被 STL 收走):
//     从 CE CompInventory.meleeWeaponList 选「最好的」近战武器(排除工具本身,评分
//     MeleeWeapon_AverageDPS,异常回退市场价值),TrySwitchToWeapon 换上。
//   · 主手已是武器(枪/刀) → 不干预。
//   TrySwitchToWeapon 内部自带主手腾空(转背包/MakeRoomFor),无需手动处理。
//   CE 未装 / STL 未装 / CompInventory 未找到时静默跳过,零副作用。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc 旧编译器,C#5,禁用 ?. 语法)。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MeleeWeaponSwapFix
{
    [StaticConstructorOnStartup]
    public static class MeleeWeaponSwapFixInit
    {
        private static Type survivalToolPropsType;
        private static Type compInventoryType;

        static MeleeWeaponSwapFixInit()
        {
            try
            {
                Type stl = AccessTools.TypeByName("SurvivalToolsLite.RightToolForJob");
                if (stl == null)
                {
                    return; // STL 未安装。
                }
                MethodInfo eq = AccessTools.Method(stl, "EquipRightWeapon");
                if (eq == null)
                {
                    Log.Warning("[HSKFix] SurvivalToolsLite.RightToolForJob.EquipRightWeapon not found");
                    return;
                }

                survivalToolPropsType = AccessTools.TypeByName("SurvivalToolsLite.SurvivalToolProperties");
                compInventoryType = AccessTools.TypeByName("CombatExtended.CompInventory");

                Harmony harmony = new Harmony("local.hskfixpack.meleeswap");
                harmony.Patch(
                    eq,
                    postfix: new HarmonyMethod(
                        typeof(MeleeWeaponSwapFixInit).GetMethod(
                            "Postfix",
                            BindingFlags.Static | BindingFlags.NonPublic)));

                Log.Message("[HSKFix] patched SurvivalToolsLite.RightToolForJob.EquipRightWeapon: melee pawns now switch tool -> best melee weapon");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] Melee weapon swap fix failed: " + e);
            }
        }

        private static bool IsSurvivalTool(ThingDef def)
        {
            if (def == null || survivalToolPropsType == null || def.modExtensions == null)
            {
                return false;
            }
            for (int i = 0; i < def.modExtensions.Count; i++)
            {
                if (def.modExtensions[i].GetType() == survivalToolPropsType)
                {
                    return true;
                }
            }
            return false;
        }

        private static ThingComp GetCompInventory(Pawn pawn)
        {
            if (compInventoryType == null || pawn == null || pawn.AllComps == null)
            {
                return null;
            }
            for (int i = 0; i < pawn.AllComps.Count; i++)
            {
                if (compInventoryType.IsAssignableFrom(pawn.AllComps[i].GetType()))
                {
                    return pawn.AllComps[i];
                }
            }
            return null;
        }

        private static List<ThingWithComps> GetMeleeWeaponList(ThingComp comp)
        {
            PropertyInfo prop = AccessTools.Property(comp.GetType(), "meleeWeaponList");
            if (prop == null)
            {
                return null;
            }
            return prop.GetValue(comp) as List<ThingWithComps>;
        }

        private static void TrySwitchToWeapon(ThingComp comp, ThingWithComps weapon)
        {
            MethodInfo m = AccessTools.Method(
                comp.GetType(),
                "TrySwitchToWeapon",
                new Type[] { typeof(ThingWithComps), typeof(bool) });
            if (m != null)
            {
                m.Invoke(comp, new object[] { weapon, false });
            }
        }

        private static float MeleeScore(ThingWithComps w)
        {
            try
            {
                float v = StatExtension.GetStatValue(w, StatDefOf.MeleeWeapon_AverageDPS, true, -1);
                if (v > 0f)
                {
                    return v;
                }
            }
            catch
            {
                // CE 环境下个别武器 stat worker 可能抛异常,回退市场价值。
            }
            return w.MarketValue;
        }

        private static void Postfix(Pawn pawn)
        {
            if (pawn == null || pawn.equipment == null)
            {
                return;
            }

            ThingWithComps primary = pawn.equipment.Primary;
            bool hasTool = primary != null && IsSurvivalTool(primary.def);
            bool emptyHands = primary == null;
            if (!hasTool && !emptyHands)
            {
                return; // 已持有武器(枪/近战) → STL 已处理,不干预。
            }

            ThingComp comp = GetCompInventory(pawn);
            if (comp == null)
            {
                return;
            }
            List<ThingWithComps> meleeList = GetMeleeWeaponList(comp);
            if (meleeList == null || meleeList.Count == 0)
            {
                return;
            }

            ThingWithComps best = null;
            float bestScore = -1f;
            for (int i = 0; i < meleeList.Count; i++)
            {
                ThingWithComps w = meleeList[i];
                if (w == primary)
                {
                    continue;
                }
                if (IsSurvivalTool(w.def))
                {
                    continue; // 背包里的其它工具不算近战武器。
                }
                float score = MeleeScore(w);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = w;
                }
            }

            if (best != null)
            {
                TrySwitchToWeapon(comp, best);
            }
        }
    }
}

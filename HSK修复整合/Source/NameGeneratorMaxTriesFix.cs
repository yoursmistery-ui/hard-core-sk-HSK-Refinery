using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FacilityCrashFix
{
    // 修复: "Could not get new name (first rule pack: NamerFactionOutlander)"
    //
    // 1.6.4871 的 NameGenerator.GenerateName(非 debug 路径)唯一性重试上限为 150 次
    // (IL: ldc.i4 0x96 + blt.s,1.5 及之前为 1000)。大型 mod 包(HSK + VEF 新派系生成 +
    // 多派系共用 NamerFactionOutlander 作 pawnNameMaker)下,个别 pawn 生成名字时可能
    // 连续 150 次命中校验器("名字已使用/易混淆"),触发该报错。
    //
    // 本补丁: 把重试上限 150 -> 1000(恢复到 1.5 行为),大幅降低报错概率。失败本身无害
    // (GenerateName 会把最后一次尝试的名字返回给 pawn),此补丁只为消除日志报错。
    //
    // 2026-08-31 补充: 已定位真正根因并根治 —— AsariRace 的 Baseline_Asari 异种人 nameMaker
    // 误指派系名包 NamerFactionOutlander(chanceToUseNameMaker=1),92号补丁给人类异种池留
    // Asari 0.002 点缀后偶发暴露。见 HSK修复整合 Patches/12_阿莎丽异种人命名修复.xml。
    // 本 transpiler 保留作通用缓解(其他命名字空间连续拒绝时降低报错概率)。
    [StaticConstructorOnStartup]
    public static class NameGeneratorMaxTriesFix
    {
        static NameGeneratorMaxTriesFix()
        {
            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(NameGenerator),
                    "GenerateName",
                    new Type[]
                    {
                        typeof(Verse.Grammar.GrammarRequest),
                        typeof(Predicate<string>),
                        typeof(bool),
                        typeof(string),
                        typeof(string)
                    });
                if (target == null)
                {
                    Log.Error("[HSKFixPack] 未找到 NameGenerator.GenerateName(5 参重载),跳过名字重试上限补丁");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.namegeneratortries");
                harmony.Patch(
                    target,
                    transpiler: new HarmonyMethod(
                        typeof(NameGeneratorMaxTriesFix).GetMethod(
                            "Transpiler",
                            BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception e)
            {
                Log.Error("[HSKFixPack] 名字重试上限补丁应用失败: " + e);
            }
        }

        // 非 debug 路径: ... ldloc.s attempt / ldc.i4 150 / blt.s loop ...
        // 把 150 替换为 1000。debug 路径(ldc.i4.s 100)不动。
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list = instructions.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Ldc_I4
                    && list[i].operand is int
                    && (int)list[i].operand == 150
                    && i + 1 < list.Count
                    && (list[i + 1].opcode == OpCodes.Blt || list[i + 1].opcode == OpCodes.Blt_S))
                {
                    list[i].operand = 1000;
                    Log.Message("[HSKFixPack] NameGenerator.GenerateName 重试上限 150 -> 1000");
                }
            }
            return list;
        }
    }
}

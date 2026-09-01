// NewRatkin.PriestPatch.GeneratePawn_Postfix 空值防护(并入 HSK 修复整合)
//
// 问题: 新开世界生成派系领袖(如 Core_SK 的 BrotherhoodSentinel)时,PawnGenerator.GeneratePawn
//  可能因校验失败返回 null(vanilla 行为,原版随后静默跳过该次生成)。而 NewRatkin(鼠族)
//  的 NewRatkin.PriestPatch:GeneratePawn_Postfix(Pawn __result) 没有判空,直接解引用
//  __result → System.NullReferenceException,异常沿 PawnGenerator.GeneratePawnRelations
//  一路上抛,最终 "Error in WorldGenStep" 把世界生成步骤打断并刷屏。
//
// 方案(Transpiler): 在 GeneratePawn_Postfix 的方法体开头插入
//    if (__result == null) return;
//  使它在生成失败(pawn 为 null)时直接早退,跳过其后续解引用逻辑。
//  __result 是原方法的普通形参(Harmony 生成的 postfix,签名 static void (Pawn __result)),
//  这里通过 original.GetParameters() 精确定位其在参数列表中的索引,不依赖 Harmony 的
//  __result 特殊注入 —— 因此不会触发 "Cannot get result from void method" 编译错误。
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace NewRatkinPriestNullFix
{
    [StaticConstructorOnStartup]
    public static class PriestNullFixInit
    {
        static PriestNullFixInit()
        {
            try
            {
                Type type = AccessTools.TypeByName("NewRatkin.PriestPatch");
                if (type == null)
                {
                    Log.Message("[HSKFix] NewRatkin.PriestPatch not found, skip null-guard");
                    return;
                }
                MethodInfo target = AccessTools.Method(type, "GeneratePawn_Postfix");
                if (target == null)
                {
                    Log.Message("[HSKFix] NewRatkin.PriestPatch.GeneratePawn_Postfix not found, skip null-guard");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.newratkin.priestnull");
                harmony.Patch(target,
                    transpiler: new HarmonyMethod(
                        typeof(PriestNullFixInit).GetMethod("Transpiler",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched NewRatkin.PriestPatch.GeneratePawn_Postfix (transpiler null guard)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] NewRatkin.PriestPatch null-guard failed: " + e);
            }
        }

        private static bool PawnIsNull(Pawn p)
        {
            return p == null;
        }

        // 在方法开头插入: if (__result == null) return;
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase original)
        {
            List<CodeInstruction> codes = instructions.ToList();

            // 定位形参(名为 __result 的 Pawn 参数)在参数列表中的真实索引。
            int argIdx = -1;
            ParameterInfo[] ps = original.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].Name == "__result")
                {
                    argIdx = original.IsStatic ? i : i + 1; // 实例方法 arg0 为 this
                    break;
                }
            }
            if (argIdx < 0)
            {
                Log.Warning("[HSKFix] NewRatkin priest null-guard: __result param not found, transpiler skipped");
                return codes;
            }

            Label earlyReturn = il.DefineLabel();

            var guard = new List<CodeInstruction>();
            switch (argIdx)
            {
                case 0: guard.Add(new CodeInstruction(OpCodes.Ldarg_0)); break;
                case 1: guard.Add(new CodeInstruction(OpCodes.Ldarg_1)); break;
                case 2: guard.Add(new CodeInstruction(OpCodes.Ldarg_2)); break;
                case 3: guard.Add(new CodeInstruction(OpCodes.Ldarg_3)); break;
                default: guard.Add(new CodeInstruction(OpCodes.Ldarg, argIdx)); break;
            }
            guard.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PriestNullFixInit), "PawnIsNull")));
            guard.Add(new CodeInstruction(OpCodes.Brtrue, earlyReturn));

            // 在方法最末追加 [earlyReturn:] return;
            var tail = new CodeInstruction(OpCodes.Ret);
            tail.labels.Add(earlyReturn);

            guard.AddRange(codes);
            guard.Add(tail);
            return guard;
        }
    }
}
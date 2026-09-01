// Kingfisher 死亡思想枚举修复(KingfisherThoughtsFix, 并入 HSK 修复整合)
//
// 问题: Kingfisher(Vortex.Kingfisher)的 PawnDiedOrDownedThoughtsRewrite.
// AddWitnessedDeathThoughtsForMap 用 foreach 直接枚举
// map.thingGrid.ThingsListAtFast(pos) 返回的活 List<Thing>(死亡者周围 12 格内
// 每格的全部 Thing)。该方法在 Pawn.DropBeforeDying 中被调用——此时同一格正在
// 发生死亡掉落(装备/尸体/物品 Spawn/DeSpawn 改 thingGrid 列表),遍历期间列表被
// 修改 → "Collection was modified; enumeration operation may not execute",
// Kingfisher 在 TryGiveDiedThoughts 里 try/catch 捕获后仅刷 Warning
// "Could not give thoughts: ...",导致附近小人的「目睹死亡」思想给不上。
//
// 方案: transpiler 在 callvirt ThingGrid.ThingsListAtFast 之后插入
// new List<Thing>(result) 拷贝构造,foreach 改为枚举快照副本,与原方法逻辑一致;
// 未装 Kingfisher 时 AccessTools.TypeByName 返回 null,自动跳过。
//
// ⚠️ 2026-08-14 坑: ThingGrid.ThingsListAtFast 有 IntVec3 / int 两个重载,
// 用 AccessTools.Method(typeof(ThingGrid), "ThingsListAtFast") 解析抛
// AmbiguousMatchException(首次部署实测日志),导致补丁未挂载。
// 改为按 callvirt + operand(Name/DeclaringType) 匹配,不再解析重载。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace KingfisherThoughtsFix
{
    [StaticConstructorOnStartup]
    public static class KingfisherThoughtsFixInit
    {
        static KingfisherThoughtsFixInit()
        {
            try
            {
                Type rewrite = AccessTools.TypeByName(
                    "Kingfisher.Features.PawnDiedOrDownedThoughtsRewrite");
                if (rewrite == null)
                {
                    return;
                }
                MethodInfo target = AccessTools.Method(
                    rewrite, "AddWitnessedDeathThoughtsForMap");
                if (target == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.kingfisher");
                harmony.Patch(
                    target,
                    transpiler: new HarmonyMethod(typeof(KingfisherThoughtsFixInit).GetMethod(
                        "Transpiler",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched Kingfisher.PawnDiedOrDownedThoughtsRewrite.AddWitnessedDeathThoughtsForMap (snapshot enumerate)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] Kingfisher thoughts fix failed: " + e);
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions)
        {
            ConstructorInfo listCtor = typeof(List<Thing>)
                .GetConstructor(new[] { typeof(IEnumerable<Thing>) });
            List<CodeInstruction> result = new List<CodeInstruction>();
            foreach (CodeInstruction instr in instructions)
            {
                result.Add(instr);
                MethodInfo operand = instr.operand as MethodInfo;
                if (instr.opcode == OpCodes.Callvirt
                    && operand != null
                    && operand.DeclaringType == typeof(ThingGrid)
                    && operand.Name == "ThingsListAtFast"
                    && listCtor != null)
                {
                    result.Add(new CodeInstruction(OpCodes.Newobj, listCtor));
                }
            }
            return result;
        }
    }
}

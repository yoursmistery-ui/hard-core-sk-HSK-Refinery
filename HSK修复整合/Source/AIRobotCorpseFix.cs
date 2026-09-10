// AIRobot 尸体销毁修复(2026-08-16;Misc Robots 自身 bug)
//
// 症状: 机器人被击杀时日志刷 "Couldn't destroy corpses."(Log.Warning,含
// [Ref xxx] 内层异常 "Collection was modified; enumeration operation may not execute")。
// 每次有尸体要清理的机器人死亡都会刷一条,纯日志噪音,且尸体会残留。
//
// 根因(反编译 AIRobot.dll 确认,`AIRobot.X2_AIRobot.Destroy(DestroyMode)`):
//   foreach (Thing item in val.listerThings.AllThings.Where(t => t.Spawned && t == this.Corpse))
//       item.Destroy(Vanish);
// 即「边遍历 AllThings 边 Destroy 匹配项」。item.Destroy() → Thing.DeSpawn →
// map.listerThings.Remove → 修改正在枚举的 AllThings 列表 → 下一次 MoveNext
// (IL_00e7) 抛 InvalidOperationException。外层 try/catch 接住后记
// "Couldn't destroy corpses.\n" + ex2.StackTrace。
//
// 修复: Harmony transpiler 在 Enumerable.Where<Thing> 调用(IL_00c2)之后插入
// Enumerable.ToList<Thing> 快照 —— 遍历的是物化 List,item.Destroy 不再影响枚举,
// 既消除异常/日志噪音,也保证尸体被真正销毁。stloc 局部变量声明为
// IEnumerable<Thing>,存入 List<Thing>(实现该接口)是合法引用赋值,验证器通过;
// GetEnumerator 走接口虚派发到 List 实现。仅此一次插入,方法内唯一 Where。
//
// 门控: 用 AccessTools.TypeByName("AIRobot.X2_AIRobot") 反射定位,不编译期引用
// AIRobot.dll;未装 Misc Robots 时 TypeByName 返回 null,整体跳过零副作用。
// 若 AIRobot 更新改了方法(不再有 Where),transpiler 不匹配即无操作,安全。
//
// 日志前缀 [AIRobotCorpseFix]。编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace AIRobotCorpseFix
{
    [StaticConstructorOnStartup]
    public static class AIRobotCorpseFixInit
    {
        static AIRobotCorpseFixInit()
        {
            try
            {
                Type robotType = AccessTools.TypeByName("AIRobot.X2_AIRobot");
                if (robotType == null)
                {
                    Log.Message("[AIRobotCorpseFix] AIRobot.X2_AIRobot not found (Misc Robots absent) — skip");
                    return;
                }
                MethodInfo destroy = AccessTools.Method(robotType, "Destroy", new Type[] { typeof(DestroyMode) });
                if (destroy == null)
                {
                    Log.Warning("[AIRobotCorpseFix] X2_AIRobot.Destroy not found — skip");
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.airobotcorpse");
                harmony.Patch(destroy, transpiler: new HarmonyMethod(
                    AccessTools.Method(typeof(AIRobotCorpseFixInit), "Transpiler")));
                Log.Message("[AIRobotCorpseFix] patched AIRobot.X2_AIRobot.Destroy (corpse-list snapshot, fixes 'Couldn't destroy corpses.')");
            }
            catch (Exception e)
            {
                Log.Error("[AIRobotCorpseFix] patch failed: " + e);
            }
        }

        // 在 Enumerable.Where<Thing>(...) 后插入 ToList<Thing>() 快照
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool inserted = false;
            foreach (CodeInstruction instr in instructions)
            {
                yield return instr;
                if (inserted)
                {
                    continue;
                }
                MethodInfo op = instr.operand as MethodInfo;
                if (instr.opcode == OpCodes.Call && op != null &&
                    op.DeclaringType == typeof(Enumerable) && op.Name == "Where")
                {
                    MethodInfo toList = AccessTools.Method(typeof(Enumerable), "ToList",
                        new Type[] { typeof(IEnumerable<Thing>) });
                    if (toList != null)
                    {
                        yield return new CodeInstruction(OpCodes.Call, toList);
                        inserted = true;
                    }
                }
            }
        }
    }
}

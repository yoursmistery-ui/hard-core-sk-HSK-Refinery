// RuntimeGC 清除污物浮菜单 NRE 修复(RuntimeGCFilthFix, 并入 HSK 修复整合, 2026-09-04)
//
// 现象: 点 RuntimeGC 右键菜单里的"清除污物 / 清理地面(家区域内)"后日志刷
//   "Exception filling window for Verse.FloatMenu: System.NullReferenceException"
//     at RimWorld.Filth.DeSpawn(DestroyMode) [0x00016]
//     at RuntimeGC.CleanserUtil.RemoveFilth(Map, bool) [0x00077]
//     at RuntimeGC.FloatMenuUtil.<.cctor>b__5()
//   紧接一行 "Tried to despawn Filth_RubbleRock#### which is already destroyed."
//
// 根因(反编译 RuntimeGC.dll + Assembly-CSharp.dll 佐证):
//   RuntimeGC.CleanserUtil.RemoveFilth 拿到的是 lister 的**活内部表**(非副本),
//   倒序原地 DeSpawn:
//     List<Thing> list = homearea ? map.listerFilthInHomeArea.FilthInHomeArea
//                                 : map.listerThings.ThingsInGroup(ThingRequestGroup.Filth);
//     for (int num2 = list.Count - 1; num2 > -1; num2--) ((Filth)list[num2]).DeSpawn(); ...
//   RimWorld 的 ThingLister 用 swap-remove(把末元素填进被删槽位再弹尾)维护该表,
//   遍历中表在原地重排+缩短, 于是低位的 list[num2] 会读到一个上一轮已销毁的 Filth,
//   对其再次 DeSpawn →
//     Thing.DeSpawn 打 "already destroyed" 并提前 return(不恢复 map);
//     Filth.DeSpawn 随后执行 map.listerFilthInHomeArea.Notify_FilthDespawned(this),
//     而此时 base.Map 已为 null → NullReferenceException, 冒泡到 FloatMenu 界面。
//
// 方案: 用 Harmony Prefix 完整接管 RemoveFilth(它是 public static,可被 patch),返回 false 跳过原实现。
//   新逻辑与原版语义等价, 仅两点安全化:
//     ①先把目标 Filth 集合快照进一份独立 List, 遍历副本, swap-remove 再也改不到我们迭代的表;
//     ②每个元素处理前判 Spawned && !Destroyed, 已经没了的直接跳过, 双保险杜绝二次 DeSpawn。
//   只碰 RuntimeGC 这一个错误调用方, 不改 vanilla Filth.DeSpawn, 也不改 RuntimeGC 的 DLL(workshop 更新不覆盖)。
//   全部成员(Map.listerFilthInHomeArea/listerThings, Filth, Thing.DeSpawn/Destroy/Discard/Destroyed/Discarded/Spawned,
//   ThingRequestGroup.Filth)均在 Assembly-CSharp, 用 AccessTools 运行时定位 RuntimeGC 类型, 不新增对其 DLL 的编译引用。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起, 系统 csc, C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RuntimeGCFilthFix
{
    [StaticConstructorOnStartup]
    public static class RuntimeGCFilthFixInit
    {
        static RuntimeGCFilthFixInit()
        {
            try
            {
                Type target = AccessTools.TypeByName("RuntimeGC.CleanserUtil");
                if (target == null)
                {
                    return; // RuntimeGC 未装, 跳过
                }
                MethodInfo method = AccessTools.Method(target, "RemoveFilth",
                    new Type[] { typeof(Map), typeof(bool) });
                if (method == null)
                {
                    return; // 版本签名变化, 保守放弃, 不误伤
                }
                Harmony harmony = new Harmony("local.hskfixpack.runtimegcfilthfix");
                harmony.Patch(method, prefix: new HarmonyMethod(
                    typeof(RuntimeGCFilthFixInit).GetMethod(
                        "Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[RuntimeGCFilthFix] replaced RuntimeGC.CleanserUtil.RemoveFilth with a safe snapshot loop");
            }
            catch (Exception e)
            {
                Log.Error("[RuntimeGCFilthFix] patch failed: " + e);
            }
        }

        // 接管 RemoveFilth: 返回 false 阻止原实现, 通过 ref __result 回填清除数量。
        private static bool Prefix(Map map, bool homearea, ref int __result)
        {
            if (map == null || map.listerFilthInHomeArea == null || map.listerThings == null)
            {
                __result = 0;
                return false;
            }

            // 快照: 原表是 lister 内部活引用, 遍历时 swap-remove 会重排它, 必须拷一份独立数组来迭代。
            List<Thing> source = homearea
                ? map.listerFilthInHomeArea.FilthInHomeArea
                : map.listerThings.ThingsInGroup(ThingRequestGroup.Filth);
            List<Thing> snapshot = new List<Thing>(source);

            int cleared = snapshot.Count;
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing t = snapshot[i];
                if (t == null)
                {
                    continue;
                }
                // 双保险: 上一轮/其它路径已清掉的对象, 这里绝不能再 DeSpawn(否则触发 vanilla Filth.DeSpawn 空 map NRE)。
                if (t.Destroyed || !t.Spawned)
                {
                    continue;
                }
                t.DeSpawn();
                if (!t.Destroyed)
                {
                    t.Destroy();
                }
                if (!t.Destroyed && !t.Discarded)
                {
                    t.Discard();
                }
            }

            __result = cleared;
            return false;
        }
    }
}

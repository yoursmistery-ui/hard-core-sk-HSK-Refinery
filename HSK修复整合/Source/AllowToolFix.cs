// AllowTool 急运缓存枚举修复(AllowToolFix, 并入 HSK 修复整合)
//
// 问题: RimWorld 1.6 的 AllowTool.HaulUrgentlyCacheHandler.GetMapHaulables
// 直接 foreach map.listerHaulables.ThingsPotentiallyNeedingHauling() 返回的
// 活 HashSet<Thing>;遍历期间 haulables 被 Notify_Spawned/DeSpawned/Forbidden/
// Unforbidden/AddedThing/SlotGroupChanged 等修改(物品生成/消失/禁用/储物组变化)
// 时抛 "Collection was modified; enumeration operation may not execute",
// HugsLib 每帧刷 AllowTool OnFixedUpdate 错误。
//
// 方案: 给 GetMapHaulables 加前缀,整体替换为"先 ToList() 快照再遍历"的等价值实现,
// 跳过原方法;未装 AllowTool 时 AccessTools.TypeByName 返回 null,自动跳过。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AllowToolFix
{
    [StaticConstructorOnStartup]
    public static class AllowToolFixInit
    {
        static AllowToolFixInit()
        {
            try
            {
                Type cacheHandler = AccessTools.TypeByName("AllowTool.HaulUrgentlyCacheHandler");
                if (cacheHandler == null)
                {
                    return;
                }
                MethodInfo target = AccessTools.Method(cacheHandler, "GetMapHaulables");
                if (target == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.allowtool");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(typeof(AllowToolFixInit).GetMethod(
                        "Prefix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched AllowTool.HaulUrgentlyCacheHandler.GetMapHaulables (snapshot enumerate)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] AllowTool fix failed: " + e);
            }
        }

        private static bool Prefix(
            Map map,
            IReadOnlyList<Thing> intersectWith,
            ICollection<Thing> targetList)
        {
            targetList.Clear();
            HashSet<Thing> intersect = new HashSet<Thing>(intersectWith);
            foreach (Thing item in map.listerHaulables.ThingsPotentiallyNeedingHauling().ToList())
            {
                if (intersect.Contains(item))
                {
                    targetList.Add(item);
                }
            }
            return false;
        }
    }
}

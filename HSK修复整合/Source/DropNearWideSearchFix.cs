// 掉落点"就近 3×3"失败导致物品凭空消失的兜底(2026-09-10)
//
// 现象:
//   Failed to place MealSurvivalPackXXXX at (161, 0, 143) in mode Near.
//     Verse.GenPlace:TryPlaceThing → Verse.GenDrop.TryDropSpawn → Verse.ThingOwner:TryDrop
//     → Pawn_InventoryTracker:DropAllNearPawn → Verse.Pawn:Strip ← Verse.Corpse:Strip
//     ← Toils_Recipe.CalculateIngredients(autoStripCorpses 类配方:屠宰/解剖/摘取器官消费尸体时先抖落其携带物)
//   本档里 (161,0,143) 是一台 TableKitchen(HSK 2×2 工作台),周围又被工作台/货架挤满。
//
// 根因(反编译确认): ThingPlaceMode.Near 的 TryFindPlaceSpotNear 只遍历 center 的 9 格;
//   PlaceSpotQualityAt 判 Unusable 的条件里,HSK 密集车间极易全部命中:
//     - 格子被 impassable 建筑占据(工作台/货架/机器)
//     - 货架 IHaulDestination.Accepts(thing) 为假
//     - c.GetItemCount >= c.GetMaxItemsAllowedInCell(地板格物品上限)
//   9 格全灭 → TryPlaceThing 直接 Log.Error 并返 false,GenDrop.TryDropSpawn 没有任何重试,
//   DropAllNearPawnHelper 也不会保留东西(尸体随配方被消耗)→ 物品静默消失。
//   原版自己在 WipeAndRefundExistingThings/CheckMoveItemsAside 里也是"Near 失败就 Destroy()",同一类丢件。
//
// 方案: TryDropSpawn 前缀,只在 mode == Near 且"原版会看的 9 格确实一格都用不上"时接管:
//   用 GenRadial 由近及远在同房间扩搜到半径 9,找到格后走 ThingPlaceMode.Direct(Direct 失败不报错)。
//   9 格里只要有 1 格可用就完全交回原版,选点/堆叠/音效逻辑与原版一致,不改变正常掉落行为。
//   扩搜仍找不到 → 交回原版,保持原来的报错与语义。
// 热路径约束: 只用格级/字段级读取(Walkable / GetRoom / thingGrid / def 判定),不拼字符串、不用 LINQ、提前短路。
using System;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.Sound;

namespace HSKFixDropNearFallback
{
    [StaticConstructorOnStartup]
    public static class DropNearWideSearchFix
    {
        private const int WideRadius = 9;

        private static readonly List<IntVec3> WideCells = new List<IntVec3>(512);

        private static int wideCellsBuilt;

        static DropNearWideSearchFix()
        {
            try
            {
                MethodInfo target = AccessTools.Method(typeof(GenDrop), "TryDropSpawn");
                if (target == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.dropnearwide");
                harmony.Patch(target, prefix: new HarmonyMethod(
                    typeof(DropNearWideSearchFix).GetMethod("Prefix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                // 挂载确认不打日志(用户口径:正式 DLL 零启动噪声),失败仍报 Error
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] DropNearWideSearchFix 挂载失败: " + e);
            }
        }

        private static void BuildWideCells()
        {
            if (wideCellsBuilt > 0)
            {
                return;
            }
            WideCells.Clear();
            IntVec3 zero = IntVec3.Zero;
            for (int i = 0; i < GenRadial.NumCellsInRadius(WideRadius); i++)
            {
                WideCells.Add(zero + GenRadial.RadialPattern[i]);
            }
            wideCellsBuilt = WideCells.Count;
        }

        // 原版 Near 会看的 9 格里有没有可用格(等价判定,够用即可;真正的选点仍交给原版)
        private static bool AnyUsableIn3x3(IntVec3 center, Map map, Thing thing, Predicate<IntVec3> validator)
        {
            if (Usable(center, center, map, thing, validator))
            {
                return true; // 最常见:本格就放得下
            }
            for (int dz = center.z - 1; dz <= center.z + 1; dz++)
            {
                for (int dx = center.x - 1; dx <= center.x + 1; dx++)
                {
                    if (dx == center.x && dz == center.z)
                    {
                        continue;
                    }
                    if (Usable(new IntVec3(dx, center.y, dz), center, map, thing, validator))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool Usable(IntVec3 c, IntVec3 center, Map map, Thing thing, Predicate<IntVec3> validator)
        {
            if (!c.InBounds(map))
            {
                return false;
            }
            if (!c.Walkable(map))
            {
                return false;
            }
            if (c.GetRoom(map) != center.GetRoom(map))
            {
                return false; // 不隔墙丢东西
            }
            if (validator != null && !validator(c))
            {
                return false;
            }
            return GenPlace.HaulPlaceBlockerIn(thing, c, map, true) == null;
        }

        private static bool Prefix(Thing thing, IntVec3 dropCell, Map map, ThingPlaceMode mode,
            ref Thing resultingThing, Action<Thing, int> placedAction, Predicate<IntVec3> nearPlaceValidator,
            bool playDropSound, ref bool __result)
        {
            if (mode != ThingPlaceMode.Near || thing == null || map == null)
            {
                return true;
            }
            if (!dropCell.InBounds(map) || thing.def.destroyOnDrop)
            {
                return true; // 越界/落地即毁仍由原版处理(含它自己的报错)
            }
            if (AnyUsableIn3x3(dropCell, map, thing, nearPlaceValidator))
            {
                return true; // 原版能找到地方,不插手
            }
            BuildWideCells();
            for (int i = 0; i < WideCells.Count; i++)
            {
                IntVec3 c = dropCell + WideCells[i];
                if (!Usable(c, dropCell, map, thing, nearPlaceValidator))
                {
                    continue;
                }
                Thing result;
                if (!GenPlace.TryPlaceThing(thing, c, map, ThingPlaceMode.Direct, out result, placedAction))
                {
                    continue;
                }
                if (playDropSound && thing.def.soundDrop != null)
                {
                    thing.def.soundDrop.PlayOneShot(SoundInfo.InMap(new TargetInfo(c, map)));
                }
                resultingThing = result;
                __result = true;
                return false;
            }
            return true; // 实在找不到,交回原版报错
        }
    }
}

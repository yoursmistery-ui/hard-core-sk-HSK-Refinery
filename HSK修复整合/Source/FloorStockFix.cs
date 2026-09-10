// 地板/建造下拉菜单库存显示修复(FloorStockFix,并入 HSK 修复整合)
//
// 问题: HSK 地板材质分组用 designatorDropdown 把同类型地板折叠成一个建造按钮后,
// 点击按钮弹出的材质子菜单(FloatMenu / FloatMenuGrid)只有名称和图标,
// 不再像原版平铺按钮那样能看出仓库里对应材料有多少库存。
//
// 方案: 给 Designator_Dropdown.SetupFloatMenu / SetupGridMenu 加后缀补丁,
// 对每个选项按 LabelCap 找到对应的 Designator_Place,取其 PlacingDef 的
// costList 主材料(与 GetDesignatorCost 同规则: 市场价值×数量最大者),
// 查询 Map.resourceCounter 得到仓库可用数量,追加到选项标签/悬浮提示:
//   灰 #999999 = 0;黄 #EAFF00 = 有但不足一次;绿 #BCF994 = 够一次;蓝 #97B7EF = 够两次以上。
// 无 costList(材质类建筑)或非放置类选项保持原样,不影响其他 mod 的下拉菜单。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HSKFloorStockFix
{
    [StaticConstructorOnStartup]
    public static class FloorStockFixInit
    {
        private const string Gray = "#999999";
        private const string Yellow = "#EAFF00";
        private const string Green = "#BCF994";
        private const string Blue = "#97B7EF";

        private struct StockInfo
        {
            public int required;
            public int available;
        }

        static FloorStockFixInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.floorstock");

                MethodInfo floatMenu = AccessTools.Method(typeof(Designator_Dropdown), "SetupFloatMenu");
                if (floatMenu != null)
                {
                    harmony.Patch(
                        floatMenu,
                        postfix: new HarmonyMethod(typeof(FloorStockFixInit).GetMethod(
                            "SetupFloatMenuPostfix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                }

                MethodInfo gridMenu = AccessTools.Method(typeof(Designator_Dropdown), "SetupGridMenu");
                if (gridMenu != null)
                {
                    harmony.Patch(
                        gridMenu,
                        postfix: new HarmonyMethod(typeof(FloorStockFixInit).GetMethod(
                            "SetupGridMenuPostfix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                }

                Log.Message("[HSKFix] patched Designator_Dropdown stock display");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] floor stock display patch failed: " + e);
            }
        }

        private static void SetupFloatMenuPostfix(Window __result, List<Designator> ___elements)
        {
            try
            {
                FloatMenu menu = __result as FloatMenu;
                if (menu == null || ___elements == null || ___elements.Count == 0)
                {
                    return;
                }
                FieldInfo optionsField = AccessTools.Field(typeof(FloatMenu), "options");
                if (optionsField == null)
                {
                    return;
                }
                List<FloatMenuOption> options = optionsField.GetValue(menu) as List<FloatMenuOption>;
                if (options == null || options.Count == 0)
                {
                    return;
                }
                Map map = Find.CurrentMap;
                if (map == null)
                {
                    return;
                }
                for (int i = 0; i < options.Count; i++)
                {
                    FloatMenuOption option = options[i];
                    if (option == null || option.Disabled)
                    {
                        continue;
                    }
                    StockInfo? info = GetStockInfo(___elements, option.Label, map);
                    if (info.HasValue)
                    {
                        option.Label = option.Label + " " + MakeColored(info.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] floor stock float menu failed: " + e);
            }
        }

        private static void SetupGridMenuPostfix(Window __result, List<Designator> ___elements)
        {
            try
            {
                FloatMenuGrid grid = __result as FloatMenuGrid;
                if (grid == null || ___elements == null || ___elements.Count == 0)
                {
                    return;
                }
                FieldInfo optionsField = AccessTools.Field(typeof(FloatMenuGrid), "options");
                if (optionsField == null)
                {
                    return;
                }
                List<FloatMenuGridOption> options = optionsField.GetValue(grid) as List<FloatMenuGridOption>;
                if (options == null || options.Count == 0)
                {
                    return;
                }
                Map map = Find.CurrentMap;
                if (map == null)
                {
                    return;
                }
                for (int i = 0; i < options.Count; i++)
                {
                    FloatMenuGridOption option = options[i];
                    if (option == null || option.Disabled || !option.tooltip.HasValue)
                    {
                        continue;
                    }
                    string label = option.tooltip.Value.text;
                    StockInfo? info = GetStockInfo(___elements, label, map);
                    if (info.HasValue)
                    {
                        option.tooltip = new TipSignal(label + " (" + MakeColored(info.Value) + ")");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] floor stock grid menu failed: " + e);
            }
        }

        private static StockInfo? GetStockInfo(List<Designator> elements, string label, Map map)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                Designator des = elements[i];
                if (des == null || !des.Visible || des.LabelCap != label)
                {
                    continue;
                }
                Designator_Place place = des as Designator_Place;
                if (place == null || place.PlacingDef == null)
                {
                    return null;
                }
                BuildableDef def = place.PlacingDef;
                if (def.CostList == null || def.CostList.Count == 0)
                {
                    return null;
                }
                ThingDefCountClass main = null;
                float best = float.MinValue;
                for (int j = 0; j < def.CostList.Count; j++)
                {
                    ThingDefCountClass c = def.CostList[j];
                    if (c == null || c.thingDef == null)
                    {
                        continue;
                    }
                    float value = c.thingDef.BaseMarketValue * c.count;
                    if (value > best)
                    {
                        best = value;
                        main = c;
                    }
                }
                if (main == null || main.thingDef == null)
                {
                    return null;
                }
                int available = map.resourceCounter.GetCount(main.thingDef);
                return new StockInfo { required = main.count, available = available };
            }
            return null;
        }

        private static string MakeColored(StockInfo info)
        {
            string color;
            if (info.available <= 0)
            {
                color = Gray;
            }
            else if (info.available < info.required)
            {
                color = Yellow;
            }
            else if (info.available < info.required * 2)
            {
                color = Green;
            }
            else
            {
                color = Blue;
            }
            return "<color=" + color + ">库存 " + info.available + "</color>";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SKCompFueledFilterGuard
{
    // SK.CompFueled.filterFuelCurrent 经 Scribe 反序列化可能被还原为 null
    // (存档里 <filterFuelCurrent xsi:nil="true"/> 或空节点), 而 SK.ITab_Fuel.FillTab
    // 与 SK.Window_ThingFilter.DoWindowContents 都不设防 → 打开燃料过滤窗口时
    // DrawHitPointsFilterConfig 对 null filter 解引用, 窗口每帧刷
    // "Exception filling window for SK.Window_ThingFilter" NRE。
    // 本守卫在加载完成后重建空 filter, 并给 Tab/窗口双重兜底。
    // SK 自家 CompRefuelableChangable 对同字段就有 null 防御, 说明这是已知状态。
    [StaticConstructorOnStartup]
    public static class SKCompFueledFilterGuardInit
    {
        private static bool _warnedRebuild;
        private static bool _warnedBurnerNull;
        private static Type _compFueledType;

        static SKCompFueledFilterGuardInit()
        {
            try
            {
                var harmony = new Harmony("local.skcompfueledfilterguard");

                Type compFueled = AccessTools.TypeByName("SK.CompFueled");
                Type tabFuel = AccessTools.TypeByName("SK.ITab_Fuel");
                Type windowFilter = AccessTools.TypeByName("SK.Window_ThingFilter");
                if (compFueled == null)
                {
                    Log.Warning("[SKFuelFilterGuard] SK.CompFueled not found, guard not applied");
                    return;
                }

                _compFueledType = compFueled;

                MethodInfo postExposeData = AccessTools.Method(compFueled, "PostExposeData");
                if (postExposeData != null)
                {
                    harmony.Patch(postExposeData,
                        postfix: new HarmonyMethod(typeof(SKCompFueledFilterGuardInit),
                            "PostExposeDataPostfix"));
                }

                MethodInfo fillTab = tabFuel != null ? AccessTools.Method(tabFuel, "FillTab") : null;
                if (fillTab != null)
                {
                    harmony.Patch(fillTab,
                        prefix: new HarmonyMethod(typeof(SKCompFueledFilterGuardInit),
                            "FillTabPrefix"));
                }

                MethodInfo doWindowContents = windowFilter != null
                    ? AccessTools.Method(windowFilter, "DoWindowContents")
                    : null;
                if (doWindowContents != null)
                {
                    harmony.Patch(doWindowContents,
                        prefix: new HarmonyMethod(typeof(SKCompFueledFilterGuardInit),
                            "WindowContentsPrefix"));
                }
            }
            catch (Exception e)
            {
                Log.Warning("[SKFuelFilterGuard] not applied: " + e);
            }
        }

        // 根修: 存档加载后 filterFuelCurrent 为 null 时按 def 燃料单重建。
        // PostExposeData 每个加载 pass 都会跑, 只在 PostLoadInit 处理一次。
        private static void PostExposeDataPostfix(object __instance)
        {
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                return;
            }

            EnsureFilter(__instance);
        }

        // 兜底 1: 燃料 Tab 绘制前修复。
        // ⚠ SK.ITab_Fuel.burner 只在原 FillTab 首行(burner = SelThing.TryGetComp<CompFueled>())赋值,
        // 而 Harmony prefix 先于被跳过的原方法体运行, 此刻字段恒为 null。旧写法直接读该字段 →
        // 每次真实燃料 Tab 都被误判为"无 burner"而跳过绘制(标签空白)并刷一次告警。
        // 正确做法: 从当前选中物直接解析 CompFueled(与原方法体绑定的是同一实例), 修好 filter 后放行;
        // 仅当选中物确实没有 CompFueled 时才跳过(那种情况下原方法体 burner.filterFuelCurrent 会 NRE)。
        private static bool FillTabPrefix(object __instance)
        {
            object burner = FindFueledComp(Find.Selector.SingleSelectedThing);
            if (burner == null)
            {
                return false;
            }

            EnsureFilter(burner);
            return true;
        }

        // 在 thing 的 comps 中查找 SK.CompFueled 实例(用缓存类型判断, 避开泛型反射)。
        // ⚠ 1.6: comps 字段在派生类 Verse.ThingWithComps 上, 不在 Verse.Thing。
        // 旧写法 AccessTools.Field(typeof(Thing), "comps") 只向上查基类, 查不到派生类字段 →
        // 恒返回 null → 所有建筑燃料 Tab 被误判"无 burner"而跳过绘制(标签全空白)。
        // 改用公有的 ThingWithComps.AllComps 遍历(与原版 SelThing.TryGetComp<CompFueled>() 同源)。
        private static object FindFueledComp(Thing thing)
        {
            if (thing == null || _compFueledType == null)
            {
                return null;
            }

            ThingWithComps twc = thing as ThingWithComps;
            if (twc == null)
            {
                return null;
            }

            List<ThingComp> comps = twc.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (_compFueledType.IsInstanceOfType(comps[i]))
                {
                    return comps[i];
                }
            }

            return null;
        }

        // 兜底 2: 燃料过滤窗口; filter 修不好就关窗, 不再每帧刷异常。
        private static bool WindowContentsPrefix(object __instance)
        {
            object burner = GetBurner(__instance, "SK.Window_ThingFilter");
            if (burner == null)
            {
                Find.WindowStack.TryRemove((Verse.Window)__instance, true);
                return false;
            }

            if (!EnsureFilter(burner))
            {
                Find.WindowStack.TryRemove((Verse.Window)__instance, true);
                return false;
            }

            return true;
        }

        private static object GetBurner(object instance, string who)
        {
            FieldInfo f = AccessTools.Field(instance.GetType(), "burner");
            object burner = f != null ? f.GetValue(instance) : null;
            if (burner == null && !_warnedBurnerNull)
            {
                _warnedBurnerNull = true;
                Log.Warning("[SKFuelFilterGuard] " + who + ".burner is null, UI drawing skipped");
            }

            return burner;
        }

        private static bool EnsureFilter(object burner)
        {
            FieldInfo f = AccessTools.Field(burner.GetType(), "filterFuelCurrent");
            if (f == null || f.GetValue(burner) != null)
            {
                return true;
            }

            ThingFilter filter = new ThingFilter();
            // 默认放行 def 燃料单(= CompFueled.Initialize 的重建逻辑); 燃料单缺失时允许全部, 不再炸 UI。
            PropertyInfo fuelProp = AccessTools.Property(burner.GetType(), "FuelFilter");
            object fuelFilter = fuelProp != null ? fuelProp.GetValue(burner, null) : null;
            filter.SetAllowAll(fuelFilter as ThingFilter, false);
            f.SetValue(burner, filter);
            if (!_warnedRebuild)
            {
                _warnedRebuild = true;
                Log.Warning("[SKFuelFilterGuard] rebuilt null filterFuelCurrent on " + burner.GetType().Name);
            }

            return true;
        }
    }
}

// PawnBadge HSK 修复版 - 注入器
// 复刻 1.5 版 Controller.EditDefs() 的官方接线方式(原作者为修复
// "其它 mod 对种族 ThingDef 调用 ResolveReferences 会移除徽章 Tab" 而写):
// 在 [StaticConstructorOnStartup] 时机(全部 Def 加载/继承/HAR 解析完成后),
// 运行时给所有 Humanlike 种族 def 补 CompProperties_Badge + ITab_Pawn_Badge,
// 再调 def.ResolveReferences() 重建 inspectorTabsResolved。
// DefDatabase.AllDefs 不含抽象 def → 每个种族恰好一条,天然无重复按钮,
// 因此彻底取代旧 XML 接线/去重补丁(已删除)。
// 已存在小人: 读档时 ThingWithComps.ExposeData → InitializeComps() 按 def.comps
// 重建实例 comps,注入对旧存档直接生效。

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnBadgeHSKFix
{
    [StaticConstructorOnStartup]
    public static class PawnBadgeInjector
    {
        static PawnBadgeInjector()
        {
            try
            {
                Type tabType = typeof(RR_PawnBadge.ITab_Pawn_Badge);
                Type compPropsType = typeof(RR_PawnBadge.CompProperties_Badge);
                bool whatTheHack = ModLister.HasActiveModWithName("What the hack") ||
                                   ModLister.GetActiveModWithIdentifier("roolo.whatthehack", false) != null;
                int injected = 0;
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs
                    .Where(d => d.race != null &&
                                (d.race.Humanlike || (whatTheHack && d.race.IsMechanoid))))
                {
                    try
                    {
                        if (def.comps == null)
                        {
                            def.comps = new List<CompProperties>();
                        }
                        if (!def.comps.Any(c => c != null && c.GetType() == compPropsType))
                        {
                            def.comps.Add((CompProperties)Activator.CreateInstance(compPropsType));
                            injected++;
                        }
                        if (def.inspectorTabs == null)
                        {
                            def.inspectorTabs = new List<Type>();
                        }
                        if (!def.inspectorTabs.Contains(tabType))
                        {
                            def.inspectorTabs.Add(tabType);
                        }
                        def.ResolveReferences();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[PawnBadgeHSKFix] failed on def " + def.defName + ": " + ex);
                    }
                }
                Log.Message("[PawnBadgeHSKFix] runtime badge wiring applied to " + injected + " race defs (1.5-style injector)");
            }
            catch (Exception e)
            {
                Log.Error("[PawnBadgeHSKFix] injector failed: " + e);
            }
        }
    }
}

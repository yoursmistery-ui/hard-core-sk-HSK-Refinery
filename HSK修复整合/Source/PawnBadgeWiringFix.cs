// PawnBadge 接线修复(PawnBadgeWiringFix,并入 HSK 修复整合,2026-09-08)
//
// 背景(反编译 + Unified.xml 终态实证):
// ①去重: 2026-09-06 的 91 号 XML 去重补丁是无效解——重复发生在 XML 继承合并阶段
//   (Pawn_Badge Base 补丁给父类 Humanlike 抽象基类加一份 comp/页签,HAR 补丁又给
//   有自带 race 节点的子类 def 加一份),而补丁执行期同一 def 的原始节点最多只有一份,
//   count>1 的 Remove 永远不命中。实测终态 美狐/鼠族/Alien_Nova/Alien_Dova 各 2 份,
//   表现为信息页出现两个"徽章"页签。
// ②补缺: Pawn_Badge 1.5 版官方 Controller.EditDefs() 注释自证: 其它 mod 对种族
//   ThingDef 调 ResolveReferences 会弄丢徽章页签,官方为此才写运行期接线。实测
//   魅狐 Kurin_Race 的 comp/页签全部来自父类 Human 继承,没有任何直接挂在自身节点
//   的锚,运行期被打断后信息页就没有"徽章"页签(2026-09-08 用户报告)。
//
// 方案: [StaticConstructorOnStartup] 时 def 已全部解析,反射取 RR_PawnBadge 类型
// (缺 mod 静默跳过,不做编译期引用),对全部 Humanlike 种族 def:
//   comps 去重补缺 + inspectorTabs 去重补缺,changed 或 inspectorTabsResolved 缺
//   徽章页签时调 def.ResolveReferences() 重建解析页签(复刻 1.5 官方接线方式)。
// DefDatabase.AllDefs 不含抽象 def → 每个种族恰好一条,不会引入新重复。
//
// 启动期一次性执行,无 Tick 开销。

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnBadgeWiringFix
{
    [StaticConstructorOnStartup]
    public static class PawnBadgeWiringFix
    {
        static PawnBadgeWiringFix()
        {
            try
            {
                Type tabType = GenTypes.GetTypeInAnyAssembly("RR_PawnBadge.ITab_Pawn_Badge");
                Type compPropsType = GenTypes.GetTypeInAnyAssembly("RR_PawnBadge.CompProperties_Badge");
                if (tabType == null || compPropsType == null)
                {
                    return; // 未装 Pawn_Badge,静默跳过
                }
                bool whatTheHack = ModLister.GetActiveModWithIdentifier("roolo.whatthehack", false) != null;
                List<ThingDef> races = DefDatabase<ThingDef>.AllDefs
                    .Where(d => d.race != null && (d.race.Humanlike || (whatTheHack && d.race.IsMechanoid)))
                    .ToList();
                int addedComps = 0;
                int deduped = 0;
                int rebuilt = 0;
                foreach (ThingDef def in races)
                {
                    try
                    {
                        bool changed = false;
                        // comps: 去重(保留首个)+ 补缺
                        if (def.comps == null)
                        {
                            def.comps = new List<CompProperties>();
                        }
                        int kept = 0;
                        for (int i = def.comps.Count - 1; i >= 0; i--)
                        {
                            CompProperties c = def.comps[i];
                            if (c != null && c.GetType() == compPropsType)
                            {
                                kept++;
                                if (kept > 1)
                                {
                                    def.comps.RemoveAt(i);
                                    changed = true;
                                    deduped++;
                                }
                            }
                        }
                        if (kept == 0)
                        {
                            def.comps.Add((CompProperties)Activator.CreateInstance(compPropsType));
                            changed = true;
                            addedComps++;
                        }
                        // inspectorTabs: 去重(保留首个)+ 补缺
                        if (def.inspectorTabs == null)
                        {
                            def.inspectorTabs = new List<Type>();
                        }
                        int first = def.inspectorTabs.IndexOf(tabType);
                        if (first < 0)
                        {
                            def.inspectorTabs.Add(tabType);
                            changed = true;
                        }
                        else
                        {
                            for (int i = def.inspectorTabs.Count - 1; i >= 0; i--)
                            {
                                if (i != first && def.inspectorTabs[i] == tabType)
                                {
                                    def.inspectorTabs.RemoveAt(i);
                                    changed = true;
                                    deduped++;
                                }
                            }
                        }
                        // 终态校验: 解析页签列表缺徽章页签则重建
                        bool resolvedOk = def.inspectorTabsResolved != null &&
                                          def.inspectorTabsResolved.Any(t => t != null && t.GetType() == tabType);
                        if (changed || !resolvedOk)
                        {
                            def.ResolveReferences();
                            rebuilt++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("[HSKFixPack] PawnBadgeWiringFix failed on def " + def.defName + ": " + ex);
                    }
                }
                Log.Message("[HSKFixPack] PawnBadgeWiringFix: races=" + races.Count
                    + " compsAdded=" + addedComps + " deduped=" + deduped + " resolvedRebuilt=" + rebuilt);
            }
            catch (Exception e)
            {
                Log.Error("[HSKFixPack] PawnBadgeWiringFix failed: " + e);
            }
        }
    }
}

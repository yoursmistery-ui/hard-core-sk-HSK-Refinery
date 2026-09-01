// 鼠族HSK拓展 - 西风骑士团武器 体型适配 (1:1 缩放)
//
// 背景: 原版 RimWorld 1.6 的近战武器渲染不按 pawn 的 baseBodySize 缩放
// (PawnRenderNodeWorker.ScaleFor 只乘 drawSize/bodyType/儿童动画), 而 SYS 的
// 武器绘制扩展(CompProperties_WeaponExtention)也只做位置偏移不做缩放。
// 因此鼠族(baseBodySize=0.8)握人类尺寸的武器会显得偏大。
//
// 方案: Harmony Postfix patch PawnRenderNodeWorker.ScaleFor
//   - 命中条件(任中其一):
//     a) 渲染节点贴图路径 node.Props.texPath 前缀在白名单:
//        "Weapon/Favonius"(本mod西风武器) / "Things/Weapon/RK_Crowbar"(撬棍, 2026-08-27 增补);
//     b) [仅手持节点] 持有者主装备 defName 前缀在白名单: "RKHSK_Favonius" / "RK_MeleeWeapon_Crowbar";
//     c) [仅手持节点, 2026-08-27 泛化] 主装备是任意 SurvivalToolsLite 生存工具
//        (thingClass=SurvivalToolsLite.SurvivalTool 或挂 SurvivalToolProperties 扩展,
//         类型全名字符串判断不引用 STL 程序集, 按 ThingDef 缓存判定结果)。
//   - b/c 限定 node.Props.workerClass == PawnRenderNodeWorker_Carried(手持/搬运渲染节点),
//     避免体干/头部节点同样走基类 ScaleFor 时把整个小人一起缩小。
//   - 命中后把原返回的 scale 乘以 pawn.RaceProps.baseBodySize。
//   - 人类 baseBodySize=1.0 -> 不变; 鼠族 0.8 -> 武器 0.8 倍, 与持有者体型 1:1 缩放。
//   - 只影响白名单/生存工具, 不影响其它任何 mod 的武器渲染。
//
// 编译(系统 csc, C#5, 与 MoreInfoFix.cs 相同):
//   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:library
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\netstandard.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\UnityEngine.dll"
//     /r:"<RimWorld>\Mods\Harmony\Current\Assemblies\0Harmony.dll"
//     /out:FavoniusBodyScale.dll FavoniusBodyScale.cs
using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RK_Favonius
{
    [StaticConstructorOnStartup]
    public static class FavoniusBodyScaleInit
    {
        static FavoniusBodyScaleInit()
        {
            try
            {
                var harmony = new Harmony("local.ratkin.clothesweapons.favoniusbodyscale");
                harmony.PatchAll();
            }
            catch (Exception e)
            {
                Log.Error("[鼠族HSK拓展] FavoniusBodyScale patch 初始化失败: " + e);
            }
        }
    }

    /// <summary>
    /// 按持有者 baseBodySize 1:1 缩放白名单武器(西风系列 + 撬棍等人类尺寸工具)。
    /// </summary>
    [HarmonyPatch(typeof(PawnRenderNodeWorker), "ScaleFor")]
    public static class Patch_PawnRenderNodeWorker_ScaleFor
    {
        // 命中条件 a: 渲染节点贴图路径前缀白名单
        static readonly string[] TexPathPrefixes =
        {
            "Weapon/Favonius",           // 西风骑士团武器
            "Things/Weapon/RK_Crowbar",  // 撬棍(2026-08-27 用户要求, 人类尺寸工具需随体型缩放)
        };
        // 命中条件 b: 持有者主装备 defName 前缀白名单(双保险)
        static readonly string[] PrimaryDefPrefixes =
        {
            "RKHSK_Favonius",
            "RK_MeleeWeapon_Crowbar",
        };

        static void Postfix(PawnRenderNode node, PawnDrawParms parms, ref Vector3 __result)
        {
            try
            {
                if (parms.pawn == null || parms.pawn.RaceProps == null)
                    return;
                float s = parms.pawn.RaceProps.baseBodySize;
                if (s <= 0.01f || Mathf.Abs(s - 1f) <= 0.001f)
                    return;

                // 命中条件 a: 节点贴图路径在白名单
                if (node != null && node.Props != null && node.Props.texPath != null)
                {
                    for (int i = 0; i < TexPathPrefixes.Length; i++)
                    {
                        if (node.Props.texPath.StartsWith(TexPathPrefixes[i], StringComparison.Ordinal))
                        {
                            __result *= s;
                            return;
                        }
                    }
                }

                // 命中条件 d: 中世纪大修(MO)盔甲/头盔(2026-08-30 用户要求)。
                //   MO 盔甲走原版 Apparel 渲染网格(人类体型尺寸), 不随鼠族体型缩小,
                //   头盔/板甲在鼠族身上明显偏大; 盔甲节点 node.apparel 非空,
                //   defName 前缀匹配 DankPyon_(MO 全系, 含原作 typo "DankPoyn_")。
                //   身体节点 Worker(Apparel_Body)先调 base.ScaleFor 再返回, 改 base 结果可传导;
                //   头部节点不 override, 直接走本方法。
                if (node != null && node.apparel != null && node.apparel.def != null)
                {
                    string apparelDefName = node.apparel.def.defName;
                    if (apparelDefName != null
                        && (apparelDefName.StartsWith("DankPyon_", StringComparison.Ordinal)
                            || apparelDefName.StartsWith("DankPoyn_", StringComparison.Ordinal)))
                    {
                        __result *= s;
                        return;
                    }
                }

                // 命中条件 b: 主装备 defName 在白名单(仅限手持渲染节点)
                //   1.6 手持武器/搬运物由 PawnRenderNodeWorker_Carried 节点绘制,
                //   体干/头部节点同样走基类 ScaleFor, 不加限制会把整个小人缩小;
                //   2026-08-27 起显式限定 Carried 节点, 只缩放手持物。
                bool weaponNode = node != null && node.Props != null
                    && node.Props.workerClass == typeof(PawnRenderNodeWorker_Carried);
                ThingDef primaryDef = null;
                if (weaponNode && parms.pawn.equipment != null && parms.pawn.equipment.Primary != null)
                {
                    primaryDef = parms.pawn.equipment.Primary.def;
                    string defName = primaryDef.defName;
                    if (defName != null)
                    {
                        for (int i = 0; i < PrimaryDefPrefixes.Length; i++)
                        {
                            if (defName.StartsWith(PrimaryDefPrefixes[i], StringComparison.Ordinal))
                            {
                                __result *= s;
                                return;
                            }
                        }
                    }
                }

                // 命中条件 c: 主装备是任意 SurvivalToolsLite 生存工具(2026-08-27 用户要求,
                // 全部工具随体型适配; 用类型全名字符串判断, 不引用 STL 程序集)
                if (weaponNode && primaryDef != null && IsSurvivalToolDef(primaryDef))
                {
                    __result *= s;
                }
            }
            catch (Exception e)
            {
                // 渲染路径不允许抛异常, 静默兜底
                Log.Warning("[鼠族HSK拓展] ScaleFor patch 异常: " + e.Message);
            }
        }

        // 性能铁律: 渲染高频路径, 每个 ThingDef 只判定一次并缓存(无反射无 LINQ)。
        // 线程安全(2026-08-30 修复): ScaleFor 由 PawnRenderTree.ParallelPreDraw 在 Unity 并行
        // 渲染 Job 中调用(Gilzoide ManagedJobs / PerformanceFish 动态绘制), 多个工作线程会同时进入
        // Postfix。普通 Dictionary 并发写会抛 "Operations that change non-concurrent collections
        // must have exclusive access"(本 bug 根因)。改用 ConcurrentDictionary: 命中路径无锁读,
        // GetOrAdd 保证原子且不损坏结构;factory 用静态缓存委托避免每次调用分配闭包。
        static readonly System.Collections.Concurrent.ConcurrentDictionary<ThingDef, bool> survivalToolDefCache
            = new System.Collections.Concurrent.ConcurrentDictionary<ThingDef, bool>();

        static readonly System.Func<ThingDef, bool> SurvivalToolDefFactory = ComputeIsSurvivalToolDef;

        static bool IsSurvivalToolDef(ThingDef def)
        {
            return survivalToolDefCache.GetOrAdd(def, SurvivalToolDefFactory);
        }

        static bool ComputeIsSurvivalToolDef(ThingDef def)
        {
            if (def.thingClass != null && def.thingClass.FullName == "SurvivalToolsLite.SurvivalTool")
            {
                return true;
            }
            if (def.modExtensions != null)
            {
                for (int i = 0; i < def.modExtensions.Count; i++)
                {
                    DefModExtension ext = def.modExtensions[i];
                    if (ext != null && ext.GetType().FullName == "SurvivalToolsLite.SurvivalToolProperties")
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

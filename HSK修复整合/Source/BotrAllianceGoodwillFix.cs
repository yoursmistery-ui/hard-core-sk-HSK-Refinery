// BOTR(边境拓展HSK)联盟落地修复(2026-09-04)
//
// 症状(每次政治周期刷屏 Player.log):
//   "Tried to use SetRelationDirect for factions which use goodwill.
//    The relation would be overriden by goodwill anyway. faction=红狐军阀, other=卡尤伊斯提亚"
//   栈: WorldComponent_Territories.TickPoliticsSystem → TryFormCoalition → MakeFactionsAllied
//        → RimWorld.Faction.SetRelationDirect
//
// 根因(反编译 BordersOfTheRim.dll + Assembly-CSharp.dll 定位):
//   Faction.SetRelationDirect 在 1.6 对"双方都 HasGoodwill(=非隐藏/非临时)"的派系
//   直接 Log.Error 并 return(关系由 goodwill 阈值派生:Ally=goodwill>=75,Hostile<=-75)。
//   BOTR 的 MakeFactionsAllied(私有静态)逻辑缺陷:
//     ①若双方 HasGoodwill → TryAffectGoodwillWith 抬好感度到 80;
//     ②随后【无条件】RelationKindWith!=Ally 就调 SetRelationDirect(Ally)。
//   当好感度通道被挡(permanentEnemy / defeated / 任务锁好感 / 自然好感上限)时,
//   第①步抬不到阈值,RelationKindWith 仍非 Ally → 落到第②步非法 SetRelationDirect → 报错。
//   对照:兄弟方法 MakeFactionsHostile / RestorePeace 都用 if/else 把 SetRelationDirect
//   限死在"非双 goodwill"分支,唯独 MakeFactionsAllied 漏了这个 guard —— 这是它自己的 bug。
//
// 修复:prefix 接管 MakeFactionsAllied 的"双 goodwill"路径,忠实复刻其善意逻辑(抬好感度到 80、
//   返回 RelationKindWith==Ally),但**不再调用非法 SetRelationDirect**;非双 goodwill 路径
//   交回原方法(其 SetRelationDirect 安全)。行为等价,只是消除报错噪音;
//   当 goodwill 确实无法达阈值时,联盟本就不该形成(不能把死敌强行绑定),跳过正确。
//
// 约束: 纯反射拿 BOTR 类型(不新增程序记引用);C#5 语法;整体 try/catch,BOTR 未装/签名漂移
//   → AccessTools 返 null → 静默跳过,绝不崩整个 StaticConstructorOnStartup 队列(同 FactionalWarBordersLink 范式)。
// 编译: 并入 HSKFixPack.dll(build.ps1)。DLL 改动需冷启动生效。
using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BotrAllianceGoodwillFix
{
    [StaticConstructorOnStartup]
    public static class BotrAllianceGoodwillFixInit
    {
        private const string BotrTerritories = "BordersOfTheRim.WorldComponent_Territories";

        static BotrAllianceGoodwillFixInit()
        {
            try
            {
                Type terType = AccessTools.TypeByName(BotrTerritories);
                if (terType == null) return; // BOTR 未装 → 静默跳过
                MethodInfo target = AccessTools.Method(terType, "MakeFactionsAllied",
                    new Type[] { typeof(Faction), typeof(Faction) });
                if (target == null) return; // 签名漂移 → 静默跳过
                Harmony harmony = new Harmony("local.hskfixpack.botr.alliancegoodwill");
                harmony.Patch(target, prefix: new HarmonyMethod(
                    typeof(BotrAllianceGoodwillFixInit).GetMethod("MakeFactionsAllied_Prefix",
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[BOTR联盟修复] 已挂 WorldComponent_Territories.MakeFactionsAllied(双好感度派系改走好感度落地, 不再误调 SetRelationDirect)");
            }
            catch (Exception e)
            {
                Log.Warning("[BOTR联盟修复] 初始化失败(已降级, 不影响游戏): " + e);
            }
        }

        // 返回 true = 让原方法照常跑(非双 goodwill 路径, 原版 SetRelationDirect 安全);
        // 返回 false = 跳过原方法(双 goodwill 路径, 本 prefix 已完整接管)。
        private static bool MakeFactionsAllied_Prefix(Faction first, Faction second, ref bool __result)
        {
            if (first == null || second == null) return true;
            if (!first.HasGoodwill || !second.HasGoodwill) return true; // 交回原版处理

            // 双 goodwill: 复刻原版善意逻辑但不碰非法 SetRelationDirect。
            if (FactionUtility.HostileTo(first, second))
            {
                __result = false;
                return false;
            }
            int delta = 80 - first.GoodwillWith(second);
            if (delta > 0)
            {
                first.TryAffectGoodwillWith(second, delta, false, false);
            }
            __result = first.RelationKindWith(second) == FactionRelationKind.Ally;
            return false;
        }
    }
}

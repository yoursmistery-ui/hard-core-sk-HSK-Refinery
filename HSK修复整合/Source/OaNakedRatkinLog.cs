// 金鼠族裸体诊断日志(OaNakedRatkinLog, 并入 HSK 修复整合)
//
// 目的(2026-09-05 用户要求): 现场仍出现金鼠族不穿衣服的个体, 需要一条 error 日志
//   帮忙判断"到底是哪些金鼠族被判成裸体、从哪冒出来的"。
//
// 判定落点: RimWorld.PawnApparelGenerator.GenerateStartingApparelFor(Pawn, PawnGenerationRequest)
//   —— 原版给新生成 pawn 装配初始服装的入口。挂 postfix, 在游戏"决定"了这个 pawn 穿什么之后,
//   检查它是不是金鼠族、结果是不是光膀子(躯干无覆盖), 是则 Log.Error 打出可定位信息。
//
// 金鼠族识别: PawnKind defName 前缀 "OA_RK_" 或 派系 defName == "OA_RK_Faction"
//   (金鼠族与鼠族共用 race "Ratkin", 只能靠 kind/派系区分)。
// 裸体判据: 穿戴列表里没有任何覆盖 Torso 的衣物(只戴帽子/眼罩也算裸体)。
//
// 临时探针: 数据到手后整文件删除重编译(正式 DLL 不留周期日志)。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起, 系统 csc, C#5)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace OaNakedRatkinLog
{
    [StaticConstructorOnStartup]
    public static class OaNakedRatkinLogInit
    {
        static OaNakedRatkinLogInit()
        {
            try
            {
                MethodInfo gen = AccessTools.Method(
                    typeof(PawnApparelGenerator), "GenerateStartingApparelFor",
                    new Type[] { typeof(Pawn), typeof(PawnGenerationRequest) });
                if (gen == null)
                {
                    Log.Warning("[HSKFix] PawnApparelGenerator.GenerateStartingApparelFor not found, OA naked log skipped");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.oanakedlog");
                harmony.Patch(
                    gen,
                    postfix: new HarmonyMethod(
                        typeof(OaNakedRatkinLogInit).GetMethod(
                            "Postfix",
                            BindingFlags.Static | BindingFlags.NonPublic)));

                Log.Message("[HSKFix] 金鼠族裸体诊断日志已挂载 (postfix PawnApparelGenerator.GenerateStartingApparelFor)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] OA naked log patch failed: " + e);
            }
        }

        private static void Postfix(Pawn pawn)
        {
            if (pawn == null || pawn.apparel == null)
            {
                return;
            }
            if (!IsOberoniaRatkin(pawn))
            {
                return;
            }
            if (CoversTorso(pawn))
            {
                return; // 穿了盖躯干的衣服, 不算裸体
            }

            string kind = (pawn.kindDef != null) ? pawn.kindDef.defName : "(null)";
            string fac = "(无派系)";
            if (pawn.Faction != null && pawn.Faction.def != null)
            {
                fac = pawn.Faction.def.defName + " / " + pawn.Faction.Name;
            }
            string loc = "(未在地图上)";
            Map map = pawn.MapHeld;
            if (map != null)
            {
                loc = "map#" + map.Index + " @" + pawn.Position.ToString() + " (tile " + map.Tile.tileId + ")";
            }
            int worn = pawn.apparel.WornApparel.Count;

            Log.Error("[HSKFix][金鼠族裸体] 生成后无躯干衣物 -> "
                + pawn.LabelShortCap + " | kind=" + kind
                + " | faction=" + fac
                + " | 穿戴件数=" + worn
                + " | 位置=" + loc
                + " | thingID=" + pawn.thingIDNumber);
        }

        // 金鼠族: kind 前缀 OA_RK_ 或派系 OA_RK_Faction
        private static bool IsOberoniaRatkin(Pawn pawn)
        {
            if (pawn.kindDef != null && pawn.kindDef.defName != null
                && pawn.kindDef.defName.StartsWith("OA_RK_"))
            {
                return true;
            }
            if (pawn.Faction != null && pawn.Faction.def != null
                && pawn.Faction.def.defName == "OA_RK_Faction")
            {
                return true;
            }
            return false;
        }

        // 是否有覆盖躯干的穿戴物
        private static bool CoversTorso(Pawn pawn)
        {
            List<Apparel> worn = pawn.apparel.WornApparel;
            for (int i = 0; i < worn.Count; i++)
            {
                Apparel a = worn[i];
                if (a != null && a.def != null && a.def.apparel != null
                    && a.def.apparel.bodyPartGroups != null
                    && a.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

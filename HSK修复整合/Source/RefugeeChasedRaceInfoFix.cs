using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FacilityCrashFix
{
    // 增强: Core_SK "被追捕的难民"(IncidentWorker_RefugeeChased)求救对话框不显示难民的人种与异种人。
    // HSK 环境下难民可能是任意种族(鼠族/魅狐/美狐/人类等),接受前无法判断是否值得救。
    //
    // 根因(反编译 Core_SK.dll 确认):
    //   SK.IncidentWorker_RefugeeChased.CreateRefugeeText 仅拼 RefugeeChasedInitial 模板
    //   (名字/称号/追捕者/年龄/敌组成分)+ 关系 + 禁止工作 + 特性 + 技能热情,不涉及 race/xenotype。
    //
    // 本补丁: postfix 该私有实例方法,在文本末尾追加【种族】【异种人】两行。
    //   人种   = refugee.def.LabelCap(已含翻译);
    //   异种人 = refugee.genes.XenotypeLabelCap(自动处理自定义异种名/独有异种);
    //           智人种(Baseliner)显示为 "无(智人种)",避免鼠族等 HAR 种族被误标成智人种造成困惑;
    //           genes 为空(极端情况)则跳过该行。
    [StaticConstructorOnStartup]
    public static class RefugeeChasedRaceInfoFix
    {
        static RefugeeChasedRaceInfoFix()
        {
            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(SK.IncidentWorker_RefugeeChased),
                    "CreateRefugeeText",
                    new Type[]
                    {
                        typeof(Pawn),
                        typeof(Faction),
                        typeof(IEnumerable<PawnKindDef>)
                    });
                if (target == null)
                {
                    Log.Error("[HSKFixPack] 未找到 SK.IncidentWorker_RefugeeChased.CreateRefugeeText,跳过被追捕难民人种显示补丁");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.refugeechasedraceinfo");
                harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(
                        typeof(RefugeeChasedRaceInfoFix).GetMethod(
                            "Postfix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFixPack] 被追捕难民人种/异种人显示补丁已挂载");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFixPack] 被追捕难民人种显示补丁应用失败: " + e);
            }
        }

        private static void Postfix(ref string __result, Pawn refugee)
        {
            if (__result == null || refugee == null || refugee.def == null)
            {
                return;
            }
            StringBuilder sb = new StringBuilder(__result);
            sb.AppendLine();
            sb.AppendLine();
            sb.Append("【种族】").Append(refugee.def.LabelCap);
            string xenotype = GetXenotypeLabel(refugee);
            if (xenotype != null)
            {
                sb.AppendLine();
                sb.Append("【异种人】").Append(xenotype);
            }
            __result = sb.ToString();
        }

        private static string GetXenotypeLabel(Pawn pawn)
        {
            try
            {
                Pawn_GeneTracker genes = pawn.genes;
                if (genes == null)
                {
                    return null;
                }
                XenotypeDef xen = genes.Xenotype;
                if (xen == null)
                {
                    return null;
                }
                if (xen == XenotypeDefOf.Baseliner)
                {
                    return "无(智人种)";
                }
                return genes.XenotypeLabelCap;
            }
            catch
            {
                return null;
            }
        }
    }
}

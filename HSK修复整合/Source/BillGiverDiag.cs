// [临时诊断] PotentialBillGiver 组内非 IBillGiver 真凶点名器
// 背景:1.6 红字 "Found non-bill-giver tagged as PotentialBillGiver"(BillUtility.MapBillGivers),
//       组判定 = def.AllRecipes 非空(recipeUsers 引用 ∪ 自身 recipes),实例类非 IBillGiver 即报,常伴 ITab_Bills InvalidCast。
//       全 XML 静态扫必为 0,真凶需运行时按实例类点名。抓到后按 defName 根治(从 recipeUsers 移除 / 补 CompBillGiver / 改 Building 系),然后删除本文件。
// 并入 HSKFixPack.dll。手动 Patch,不用 [HarmonyPatch] 属性(避免被 FacilityCrashFix 的 assembly-wide PatchAll 重复打补丁)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BillGiverDiag
{
    [StaticConstructorOnStartup]
    public static class BillGiverDiagInit
    {
        static BillGiverDiagInit()
        {
            try
            {
                var harmony = new Harmony("local.hskfixpack.billgiverdiag");
                var storageFill = AccessTools.Method(typeof(ITab_Storage), "FillTab");
                if (storageFill != null)
                    harmony.Patch(storageFill,
                        prefix: new HarmonyMethod(typeof(BillGiverDiagInit), "OnStorageFillTab"));
                var billsFill = AccessTools.Method(typeof(ITab_Bills), "FillTab");
                if (billsFill != null)
                    harmony.Patch(billsFill,
                        prefix: new HarmonyMethod(typeof(BillGiverDiagInit), "OnBillsFillTab"));
                Log.Message("[BillGiverDiag] 诊断补丁已加载(HSKFixPack)");
            }
            catch (Exception e)
            {
                Log.Warning("[BillGiverDiag] 加载失败: " + e.Message);
            }
        }

        public static void OnStorageFillTab()
        {
            Diag.Scan("点储存页签");
        }

        public static void OnBillsFillTab()
        {
            // ITab_Bills.FillTab 本身会抛 InvalidCast,prefix 抢在异常前点名
            Diag.Scan("点账单页签");
        }
    }

    public static class Diag
    {
        // def 级缓存:def 是否落在 PotentialBillGiver 组(AllRecipes 非空),避免重复计算
        private static readonly Dictionary<ThingDef, bool> GroupCache = new Dictionary<ThingDef, bool>();
        // 已点名的 def,防刷屏
        private static readonly HashSet<ThingDef> Reported = new HashSet<ThingDef>();

        public static void Scan(string reason)
        {
            try
            {
                foreach (Map map in Find.Maps)
                {
                    foreach (Thing t in map.listerThings.AllThings)
                    {
                        ThingDef def = t.def;
                        if (def == null) continue;
                        bool inGroup;
                        if (!GroupCache.TryGetValue(def, out inGroup))
                        {
                            inGroup = ThingRequestGroup.PotentialBillGiver.Includes(def);
                            GroupCache[def] = inGroup;
                        }
                        if (!inGroup) continue;
                        if (t is IBillGiver) continue;
                        if (Reported.Add(def))
                        {
                            Log.Warning("[BillGiverDiag] 真凶 def=" + def.defName
                                + " | thingClass=" + t.GetType().FullName
                                + " | category=" + def.category
                                + " | map=" + map.ToString()
                                + " | pos=" + t.Position
                                + " | 触发=" + reason);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[BillGiverDiag] 扫描异常: " + e);
            }
        }
    }

    // 低频兜底扫描(每 2400 tick ≈ 40s):覆盖 colonist 自动作业时(WorkGiver_DoBill.ShouldSkip)触发报错、
    // 但玩家没点页签的场景。抓到后应连同本文件一并删除。
    public class GameComp_BillGiverDiag : GameComponent
    {
        private int c;
        public GameComp_BillGiverDiag(Game game) { }
        public override void GameComponentTick()
        {
            if (++c < 2400) return;
            c = 0;
            Diag.Scan("tick兜底");
        }
    }
}

// Factional War(SR.ModRimworld.FactionalWarContinued) × Borders of the Rim 政治边界联动(2026-09-02)
//
// 背景: Factional War 用 FactionUtil.GetHostileFactionPair 从可见人类派系里
// 随机 shuffle 取"第一对 HostileTo"当事件 map 上的混战双方,与地缘无关;
// 打完对 BOTR 的领土边界毫无影响;玩家被波及也只有 FW 自带的旁观者信。
// BOTR(WorldComponent_Territories)有权威边界/领土战争,但只跑自己的 AI 战争循环。
//
// 本桥三个挂载(全部反射目标 mod 的公开/受保护入口,不改本体文件):
//  ① postfix FactionUtil.GetHostileFactionPair —— 优选 BOTR 边界邻接的敌对配对。
//  ② postfix IncidentWorkerFactionWar.TryExecuteWorker —— FW 成功开战后,
//     若双方尚未处于 BOTR 战争,反射 best-effort 调 WorldComponent_Territories.StartWar
//     把战争喂给 BOTR 领土引擎(其 TryCaptureSettlement+RebuildTerritories 驱动边界变化)。
//  ③ 同一 postfix —— 若事件发生在玩家主图,发原版信件提示"边境战事波及我领"。
//
// 容错: 每个补丁整体 try/catch;FW/BOTR 未装或签名漂移 → AccessTools 返 null → 静默跳过,
// 绝不抛异常崩掉 HSKFixPack 的整个 StaticConstructorOnStartup 队列(同 FactionalWarRaidFix 范式)。
// 编译: 并入 HSKFixPack.dll(build.ps1,系统 csc,C#5,BOTR/FW 类型纯反射不新增程序集引用)。
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FactionalWarBordersLink
{
    [StaticConstructorOnStartup]
    public static class FactionalWarBordersLinkInit
    {
        private const string FwType = "SR.ModRimworld.FactionalWar.FactionUtil";
        private const string FwWarWorker = "SR.ModRimworld.FactionalWar.IncidentWorkerFactionWar";
        private const string BotrTerritories = "BordersOfTheRim.WorldComponent_Territories";
        private const string BotrMod = "BordersOfTheRim.BordersOfTheRimMod";

        // 反射句柄(仅应用时解析一次)
        private static Type terType;
        private static MethodInfo mIsDisputed;      // IsDisputedBorder(Faction,Faction)
        private static MethodInfo mBorderContact;   // BorderContactBetween(Faction,Faction)
        private static MethodInfo mAreAtWar;        // AreFactionsAtWar(Faction,Faction)
        private static MethodInfo mStartWar;        // StartWar(...) 私有,8 参
        private static PropertyInfo pModSettings;   // BordersOfTheRimMod.Settings
        private static Type fwWorkerType;
        private static FieldInfo fFaction1;         // _faction1
        private static FieldInfo fFaction2;         // _faction2
        private static bool fwWorkerReady;

        static FactionalWarBordersLinkInit()
        {
            try
            {
                ResolveBotr();
                Harmony harmony = new Harmony("local.hskfixpack.factionalwarborderslink");
                PatchPairing(harmony);
                PatchWarFire(harmony);
            }
            catch (Exception e)
            {
                Log.Error("[FWxBOTR联动] 初始化失败(已降级,不影响游戏): " + e);
            }
        }

        private static void ResolveBotr()
        {
            terType = AccessTools.TypeByName(BotrTerritories);
            if (terType == null) return; // BOTR 未装
            mIsDisputed = AccessTools.Method(terType, "IsDisputedBorder",
                new Type[] { typeof(Faction), typeof(Faction) });
            mBorderContact = AccessTools.Method(terType, "BorderContactBetween",
                new Type[] { typeof(Faction), typeof(Faction) });
            mAreAtWar = AccessTools.Method(terType, "AreFactionsAtWar",
                new Type[] { typeof(Faction), typeof(Faction) });
            mStartWar = AccessTools.Method(terType, "StartWar");
            Type modType = AccessTools.TypeByName(BotrMod);
            if (modType != null) pModSettings = AccessTools.Property(modType, "Settings");

            fwWorkerType = AccessTools.TypeByName(FwWarWorker);
            if (fwWorkerType != null)
            {
                fFaction1 = AccessTools.Field(fwWorkerType, "_faction1");
                fFaction2 = AccessTools.Field(fwWorkerType, "_faction2");
                fwWorkerReady = fFaction1 != null && fFaction2 != null;
            }
        }

        private static void PatchPairing(Harmony harmony)
        {
            Type fu = AccessTools.TypeByName(FwType);
            if (fu == null) return; // FW 未装
            MethodInfo m = AccessTools.Method(fu, "GetHostileFactionPair");
            if (m == null || terType == null) return;
            harmony.Patch(m, postfix: new HarmonyMethod(
                typeof(FactionalWarBordersLinkInit).GetMethod("Pairing_Postfix",
                    BindingFlags.Static | BindingFlags.NonPublic)));
            Log.Message("[FWxBOTR联动] ① 已挂 GetHostileFactionPair(优选边界邻接配对)");
        }

        private static void PatchWarFire(Harmony harmony)
        {
            if (fwWorkerType == null || !fwWorkerReady) return;
            MethodInfo m = AccessTools.Method(fwWorkerType, "TryExecuteWorker");
            if (m == null) return;
            harmony.Patch(m, postfix: new HarmonyMethod(
                typeof(FactionalWarBordersLinkInit).GetMethod("WarFire_Postfix",
                    BindingFlags.Static | BindingFlags.NonPublic)));
            Log.Message("[FWxBOTR联动] ②③ 已挂 IncidentWorkerFactionWar.TryExecuteWorker");
        }

        // ---- BOTR 世界组件取用 ----
        private static object TerritoriesComp()
        {
            if (terType == null || Find.World == null) return null;
            try
            {
                MethodInfo g = AccessTools.Method(typeof(World), "GetComponent");
                if (g != null)
                    return g.MakeGenericMethod(terType).Invoke(Find.World, null);
            }
            catch { }
            return null;
        }

        private static bool BotrBorderAdjacent(Faction a, Faction b)
        {
            object comp = TerritoriesComp();
            if (comp == null) return false;
            try
            {
                if (mIsDisputed != null)
                {
                    object r = mIsDisputed.Invoke(comp, new object[] { a, b });
                    if (r is bool && (bool)r) return true;
                }
                if (mBorderContact != null)
                {
                    object c = mBorderContact.Invoke(comp, new object[] { a, b });
                    if (c != null) return true;
                }
            }
            catch { }
            return false;
        }

        // ① 若 FW 选中的对不是边界邻接,就在候选里换一对"既敌对又邻接"的;找不到保留原结果。
        private static void Pairing_Postfix(List<Faction> candidateFactionList,
            ref Faction faction1, ref Faction faction2)
        {
            try
            {
                if (faction1 == null || faction2 == null) return;
                if (BotrBorderAdjacent(faction1, faction2)) return; // 已邻接,原样
                if (candidateFactionList == null || candidateFactionList.Count < 2) return;
                for (int i = 0; i < candidateFactionList.Count; i++)
                {
                    Faction a = candidateFactionList[i];
                    if (a == null) continue;
                    for (int j = 0; j < candidateFactionList.Count; j++)
                    {
                        if (i == j) continue;
                        Faction b = candidateFactionList[j];
                        if (b == null) continue;
                        if (!a.HostileTo(b)) continue;
                        if (!BotrBorderAdjacent(a, b)) continue;
                        faction1 = a;
                        faction2 = b;
                        return; // 换成邻接对
                    }
                }
                // 无邻接对可换 → 保留 FW 原配对(不阻断事件)
            }
            catch (Exception e)
            {
                Log.Warning("[FWxBOTR联动] ① 配对调整失败(保留 FW 原结果): " + e);
            }
        }

        // ②③ FW 成功开战后:喂 BOTR 战争 + 玩家主图波及提示。
        private static void WarFire_Postfix(object __instance, IncidentParms parms, bool __result)
        {
            try
            {
                if (!__result || parms == null) return;
                Faction f1 = fFaction1.GetValue(__instance) as Faction;
                Faction f2 = fFaction2.GetValue(__instance) as Faction;
                if (f1 == null || f2 == null) return;

                // ② 驱动 BOTR 边界战争(反射 best-effort;签名漂移静默 no-op)
                TryBridgeStartWar(f1, f2, (int)Mathf.Clamp(parms.points, 0f, 5000f));

                // ③ 玩家主图波及提示
                Map map = parms.target as Map;
                if (map != null && Find.AnyPlayerHomeMap != null && map == Find.AnyPlayerHomeMap)
                    NotifyPlayerSpillover(f1, f2);
            }
            catch (Exception e)
            {
                Log.Warning("[FWxBOTR联动] ②③ postfix 异常(已吞,不影响 FW 事件): " + e);
            }
        }

        private static void TryBridgeStartWar(Faction f1, Faction f2, int points)
        {
            object comp = TerritoriesComp();
            if (comp == null || mStartWar == null || pModSettings == null) return;
            try
            {
                if (mAreAtWar != null)
                {
                    object w = mAreAtWar.Invoke(comp, new object[] { f1, f2 });
                    if (w is bool && (bool)w) return; // 已在 BOTR 战争中
                }
                object settings = pModSettings.GetValue(null, null);
                ParameterInfo[] ps = mStartWar.GetParameters();
                object[] args = new object[ps.Length];
                // 观测到真实调用: StartWar(f1, f2, Find.TickManager.TicksGame, settings, [可选...])
                // → 位置感知填:0=进攻方 1=防御方 2=起始tick(int) 3=Settings,其余取默认。
                int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                for (int k = 0; k < ps.Length; k++)
                {
                    ParameterInfo p = ps[k];
                    if (k == 0) args[k] = f1;
                    else if (k == 1) args[k] = f2;
                    else if (k == 2) args[k] = tick;
                    else if (k == 3 && settings != null && p.ParameterType.IsInstanceOfType(settings)) args[k] = settings;
                    else args[k] = p.HasDefaultValue ? p.DefaultValue : DefaultValueOf(p.ParameterType);
                }
                mStartWar.Invoke(comp, args);
                Log.Message("[FWxBOTR联动] ② 已把 " + f1.Name + " vs " + f2.Name + " 的战争喂给 BOTR 领土引擎");
            }
            catch (Exception e)
            {
                Log.Warning("[FWxBOTR联动] ② StartWar 反射降级(仅边界邻接/提示仍生效): " + e);
            }
        }

        private static object DefaultValueOf(Type t)
        {
            if (t.IsValueType) return Activator.CreateInstance(t);
            return null;
        }

        private static void NotifyPlayerSpillover(Faction f1, Faction f2)
        {
            try
            {
                string label = "边境战事波及我领";
                string text = f1.Name + " 与 " + f2.Name
                    + " 在我方领地附近爆发战事。若局势恶化,注意边防守卫与伤员。";
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatSmall);
                Messages.Message(text, MessageTypeDefOf.ThreatSmall);
            }
            catch (Exception e)
            {
                Log.Warning("[FWxBOTR联动] ③ 波及提示失败: " + e);
            }
        }
    }
}

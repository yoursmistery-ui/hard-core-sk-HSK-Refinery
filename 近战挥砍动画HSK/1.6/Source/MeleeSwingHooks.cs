using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using JobEffects;

namespace MeleeSwing
{
    // 复用 Show Me Your Tools 的挥砍动画，但把绘制贴图换成小人当前装备的近战武器本身。
    //
    // 关键坑（本次修复根因）：held 装备这个 Thing 没有 drawer，pawn.equipment.Primary.Graphic
    // 恒为 null —— 武器是靠 def.graphicData(+stuff) 现取图形绘制的，不是靠 item 自身 Graphic。
    // 所以材质必须从 weapon.def.graphicData.Graphic 取，否则取空 → 抑制绘制 → 只剩原版锤击。
    //
    // 注入点：
    //   1) ToolAnimator.DrawActive 前缀：近战 def 且能取到武器图形时，把武器材质塞进静态；
    //      取不到（赤手空拳等）则 return false 跳过整段（宁可不画也不画兜底刀）。
    //   2) JobToolDef.Material 后缀：仅当当前绘制的是本 mod 近战 def 且已备好武器材质时替换。
    //   3) PawnRenderUtility.DrawEquipmentAndApparelExtras 前缀：近战攻击中且 SMYT 正在为该 pawn
    //      画工具时，隐藏其手持武器（避免与 SMYT 的 OverrideToolMods 设置耦合导致双画）。
    [StaticConstructorOnStartup]
    public static class MeleeSwingHooks
    {
        private const string DefChop = "JE_Melee_Chop";
        private const string DefSweep = "JE_Melee_Sweep";
        private const string JobAttack = "AttackMelee";

        // Melee Animation (co.uk.epicguru.meleeanimation) 活跃 → 近战攻击动画交给它的全身骨骼
        // 动画, 本 mod 不挂 JobEffects 挥砍(工作动画HSK 已对 melee 让路), 剑气改挂各子类
        // ApplyMeleeDamageToTarget 命中事件(不能挂基类 TryCastShot —— CE 的 Verb_MeleeAttackCE
        // 重写且不调 base, 基类 Postfix 在 CE 环境永不触发), 保证剑气特效保留(用户要求)。
        private static readonly bool MeleeAnimActive;
        private static readonly FleckDef slashQi;

        // 逐帧、逐 pawn 设置；只在 DrawActive → DrawTool 的同步调用栈内有效（与 SMYT 同线程模型）。
        private static bool meleeActive;
        private static Material weaponMat;

        static MeleeSwingHooks()
        {
            Log.Message("[近战挥砍] static ctor 运行，开始挂载");
            MeleeAnimActive =
                ModLister.GetActiveModWithIdentifier("co.uk.epicguru.meleeanimation", true) != null
                || GenTypes.GetTypeInAnyAssembly("AM.AnimationManager") != null;
            slashQi = DefDatabase<FleckDef>.GetNamedSilentFail("JE_Melee_SlashQi");
            var harmony = new Harmony("local.ratkin.melee.swing");

            if (MeleeAnimActive)
            {
                // 双模 A: Melee Animation 活跃 → 剑气挂在命中判定(ApplyMeleeDamageToTarget)上。
                // 关键坑: 不能只挂基类 Verb_MeleeAttack.TryCastShot —— CE 的 Verb_MeleeAttackCE
                // 完全重写 TryCastShot 且不调 base(), 原版基类 Postfix 在 CE 环境下永不触发。
                // ApplyMeleeDamageToTarget 是 abstract, 所有直接继承的子类都各自实现且命中必然调用,
                // 与 MA(meleeanimation) 的 PatchAll 同一遍历策略, 保证原版/CE/鼠族自定义 Verb 全覆盖。
                int n = PatchSlashOnAllMeleeSubclasses(harmony);
                Log.Message("[近战挥砍] Melee Animation 活跃 → 剑气挂 " + n +
                    " 个 Verb_MeleeAttack 子类的 ApplyMeleeDamageToTarget，跳过 JobEffects 挥砍");
                return;
            }

            try
            {
                Type ta = AccessTools.TypeByName("JobEffects.ToolAnimator");
                if (ta == null) { Log.Warning("[近战挥砍] 未找到 JobEffects.ToolAnimator（SMYT 未加载？），跳过"); return; }

                MethodInfo drawActive = AccessTools.Method(ta, "DrawActive");
                MethodInfo getMat = AccessTools.PropertyGetter(typeof(JobToolDef), "Material");
                if (drawActive == null || getMat == null) { Log.Warning("[近战挥砍] 目标方法缺失，跳过"); return; }

                harmony.Patch(drawActive,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(MeleeSwingHooks), nameof(DrawActivePrefix))));
                harmony.Patch(getMat,
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(MeleeSwingHooks), nameof(MaterialPostfix))));
                harmony.Patch(AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras)),
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(MeleeSwingHooks), nameof(HideWeaponPrefix))));

                Log.Message("[近战挥砍] 已挂载 Show Me Your Tools 挥砍动画，武器贴图=当前装备");
            }
            catch (Exception e)
            {
                Log.Error("[近战挥砍] Harmony patch 失败: " + e);
            }
        }

        // 剑气: 遍历所有程序集中直接继承 Verb_MeleeAttack 的非抽象子类, 给每个子类的
        // ApplyMeleeDamageToTarget 挂 Postfix(命中事件)。策略与 MA(meleeanimation) 的
        // Patch_Verb_MeleeAttack_ApplyMeleeDamageToTarget.PatchAll 一致 —— 这样无论原版
        // (Verb_MeleeAttackDamage / Verb_MeleeApplyHediff)、CE(Verb_MeleeAttackCE)、鼠族
        // (Verb_ChainSword / Verb_GunlanceFiring / Verb_MeleeExplosion), 攻击命中必然迸剑气。


        private static int PatchSlashOnAllMeleeSubclasses(Harmony harmony)
        {
            int count = 0;
            HarmonyMethod postfix = new HarmonyMethod(AccessTools.Method(typeof(MeleeSwingHooks), nameof(HitPostfix)));
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Type[] types;
                try { types = assemblies[a].GetTypes(); }
                catch (ReflectionTypeLoadException) { continue; }
                for (int i = 0; i < types.Length; i++)
                {
                    Type t = types[i];
                    if (t == null || t.IsAbstract || t.BaseType != typeof(Verb_MeleeAttack)) continue;
                    try
                    {
                        MethodInfo mi = AccessTools.Method(t, "ApplyMeleeDamageToTarget",
                            new Type[] { typeof(LocalTargetInfo) }, null);
                        if (mi == null) continue;
                        harmony.Patch(mi, postfix: postfix);
                        count++;
                        if (count <= 8)
                            Log.Message("[近战挥砍] 剑气已挂 " + t.FullName + ".ApplyMeleeDamageToTarget");
                    }
                    catch (Exception e)
                    {
                        Log.Warning("[近战挥砍] 剑气挂 " + t.FullName + " 失败: " + e.Message);
                    }
                }
            }
            return count;
        }

        // 剑气: 命中事件(ApplyMeleeDamageToTarget 返回后)在目标处迸发一道弧光。
        // 方向 = 命中点; 位置 = 目标 DrawPos(武器刃区), 复用 JE_Melee_SlashQi FleckDef。
        private static void HitPostfix(Verb_MeleeAttack __instance, LocalTargetInfo target)
        {
            try
            {
                Pawn pawn = __instance.caster as Pawn;
                if (pawn == null || pawn.Dead || slashQi == null || pawn.Map == null) return;
                if (pawn.equipment == null || pawn.equipment.Primary == null) return;   // 赤手空拳不迸剑气
                Vector3 flat = pawn.DrawPos; flat.y = 0f;
                Vector3 tpos = target.HasThing ? target.Thing.DrawPos : target.Cell.ToVector3(); tpos.y = 0f;
                Vector3 dir = tpos - flat;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
                dir.Normalize();
                Vector3 impact = flat + dir * 0.7f; impact.y = 0f;
                FleckMaker.Static(impact, pawn.Map, slashQi, 2.0f);
            }
            catch { }
        }

        private static bool IsMeleeDef(JobToolDef tool)
        {
            if (tool == null) return false;
            string dn = tool.defName;
            return dn == DefChop || dn == DefSweep;
        }

        // 从武器 ThingDef 的 graphicData 取材质（stuff 武器按 stuff 上色）。取不到返回 null。
        private static Material WeaponMaterial(ThingWithComps weapon)
        {
            if (weapon == null) return null;
            try
            {
                GraphicData gd = weapon.def?.graphicData;
                if (gd == null) return null;
                Graphic g = gd.Graphic;
                Material m = g?.MatSingle;
                if (m == null && gd.texPath != null)
                {
                    Shader sh = gd.shaderType != null ? gd.shaderType.Shader : ShaderDatabase.Cutout;
                    m = MaterialPool.MatFrom(gd.texPath, sh, gd.color);
                }
                return m;
            }
            catch { return null; }
        }

        private static int diagCount;

        // 返回 false → 跳过原 DrawActive（近战 def 但取不到武器图形时，宁可不画也不画兜底刀）。
        private static bool DrawActivePrefix(Pawn pawn, JobToolDef tool)
        {
            meleeActive = false;
            weaponMat = null;
            if (!IsMeleeDef(tool)) return true;   // 非本 mod 近战 def：交回原逻辑

            if (pawn == null || pawn.Dead) return false;
            Material m = WeaponMaterial(pawn.equipment?.Primary);
            bool ok = m != null && m.mainTexture != null;
            if (diagCount < 5)
            {
                diagCount++;
                Log.Message("[近战挥砍] DrawActive 命中近战 def=" + tool.defName + " 武器=" +
                    (pawn.equipment?.Primary?.def?.defName ?? "null") + " 材质取到=" + ok);
            }
            if (!ok) return false;  // 无武器/取不到贴图：不画

            weaponMat = m;
            meleeActive = true;
            return true;
        }

        private static void MaterialPostfix(JobToolDef __instance, ref Material __result)
        {
            if (!meleeActive || weaponMat == null) return;
            if (!IsMeleeDef(__instance)) return;
            __result = weaponMat;
        }

        // 近战攻击中、且 SMYT 正在为该 pawn 画工具（HasActiveTool 已内含"被 cap 掉则报无工具"）时，
        // 隐藏其手持武器，只画服饰 worn-extras —— 这样只有那把会挥的武器，不会双画。
        private static bool HideWeaponPrefix(Pawn pawn)
        {
            if (pawn == null) return true;
            try
            {
                JobDef jobDef = pawn.CurJob?.def;
                if (jobDef == null || jobDef.defName != JobAttack) return true;
                if (!ToolAnimator.HasActiveTool(pawn)) return true;   // SMYT 没在画 → 别动，正常显示武器
                if (pawn.apparel != null)
                {
                    List<Apparel> worn = pawn.apparel.WornApparel;
                    for (int i = 0; i < worn.Count; i++)
                    {
                        try { worn[i].DrawWornExtras(); } catch { }
                    }
                }
                return false;   // 跳过手持武器绘制
            }
            catch { return true; }
        }
    }
}

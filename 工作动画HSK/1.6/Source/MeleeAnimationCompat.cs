using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace JobEffects
{
    /// <summary>
    /// Melee Animation (Epicguru, packageId co.uk.epicguru.meleeanimation) interop.
    ///
    /// Melee Animation draws and animates a pawn's equipped melee weapon via its per-pawn
    /// <c>AM.Idle.IdleControllerComp</c> (idle/move/flavour weapon animations). When one of OUR
    /// animated tools is on screen for that pawn, we want Melee Animation to stay out of the way
    /// entirely — otherwise its weapon animation fights / double-draws over our tool.
    ///
    /// Rather than Harmony-patch AM internals, we use AM's OWN public extension point:
    ///   <c>public static readonly List&lt;IdleControllerDrawDelegate&gt; IdleControllerComp.ShouldDrawAdditional</c>
    /// AM invokes every delegate in this list from <c>ShouldBeActive</c>; a delegate that sets
    /// <c>shouldBeActive = false</c> makes AM clear its animation and skip the weapon for that pawn.
    /// We also set <c>doDefaultDraw = false</c> so AM doesn't ask vanilla to draw the weapon instead
    /// (the pawn is mid-work, no weapon should show).
    ///
    /// PERFORMANCE — read before touching this file.
    /// <c>ShouldBeActive</c> is called from <c>IdleControllerComp.CompTick()</c>, i.e. once per
    /// humanlike-with-the-comp PER TICK, on every map, regardless of the camera. On a 120-pawn colony
    /// at 3x that is ~21,600 invocations/second — roughly two orders of magnitude hotter than this
    /// mod's per-frame render postfix. AM also invokes us BEFORE its own <c>GetMeleeWeapon()</c>
    /// check, so unarmed pawns reach us too. Consequences, both handled below:
    ///   * The body must be trivially cheap. It previously recovered the Pawn through a cached
    ///     <c>FieldInfo.GetValue</c> — an unnecessary Mono runtime-invoke, because
    ///     <c>Verse.ThingComp.parent</c> is a PUBLIC field. It's a plain field read now.
    ///   * When our own feature is switched off the delegate is REMOVED from AM's list outright
    ///     (see <see cref="SyncRegistration"/>) rather than early-returning, so a disabled feature
    ///     costs exactly nothing instead of "nearly nothing, 21,600 times a second".
    ///
    /// All reflection, all guarded — a no-op when Melee Animation isn't loaded, and any failure
    /// leaves AM behaving exactly as normal.
    /// </summary>
    public static class MeleeAnimationCompat
    {
        // True when Melee Animation is loaded. The whole hook is skipped otherwise.
        public static readonly bool Active =
            ModLister.GetActiveModWithIdentifier("co.uk.epicguru.meleeanimation", true) != null;

        private static IList hookList;    // AM's public static List<IdleControllerDrawDelegate>
        private static Delegate hook;     // our delegate instance, bound once
        private static bool registered;   // true while `hook` is present in `hookList`
        private static bool resolved;     // one-time reflection has run
        private static Type compTypeF;    // AM.Idle.IdleControllerComp (compat use)
        private static MethodInfo clearAnim; // AM IdleControllerComp.ClearAnimation() (public)

        public static void Apply()
        {
            if (!Active || resolved) return;
            resolved = true;
            try
            {
                compTypeF = AccessTools.TypeByName("AM.Idle.IdleControllerComp");
                Type compType = compTypeF;
                Type delType = AccessTools.TypeByName("AM.Idle.IdleControllerDrawDelegate");
                if (compType == null || delType == null)
                {
                    Log.Warning("[Show Me Your Tools] Melee Animation compat: AM idle types not found (fork drift?) — interop off");
                    return;
                }

                FieldInfo listField = AccessTools.Field(compType, "ShouldDrawAdditional");
                if (!(listField?.GetValue(null) is IList list))
                {
                    Log.Warning("[Show Me Your Tools] Melee Animation compat: ShouldDrawAdditional list not found — interop off");
                    return;
                }
                hookList = list;
                clearAnim = AccessTools.Method(compType, "ClearAnimation");

                // Bind our static method to AM's delegate type. Relaxed delegate binding lets the
                // delegate's IdleControllerComp parameter target our less-derived ThingComp param.
                MethodInfo mi = AccessTools.Method(typeof(MeleeAnimationCompat), nameof(ShouldDrawHook));
                hook = Delegate.CreateDelegate(delType, mi);

                SyncRegistration();

                // Render-frame HARD kill (belt & braces for the tick-time delegate above). The
                // delegate only runs from AM's CompTick / ShouldBeActive; while the game is PAUSED
                // (no ticks) or if the delegate list got mutated/lost, an idle weapon animation
                // created before work started survives and AM keeps drawing the real equipped
                // tool over our animated one — the "two pickaxes" desync. PreDraw is the per-frame
                // chokepoint AM itself consults right before rendering, so killing the animation
                // here covers every window the tick path can miss.
                MethodInfo preDraw = AccessTools.Method(compType, "PreDraw");
                if (preDraw != null && clearAnim != null)
                {
                    Harmony hLocal = new Harmony("meathax.JobEffects.compat");
                    hLocal.Patch(preDraw, prefix: new HarmonyMethod(
                        AccessTools.Method(typeof(MeleeAnimationCompat), nameof(PreDrawSuppress))));
                    Log.Message("[Show Me Your Tools] Melee Animation compat: tick delegate + render PreDraw guard installed");
                    // NOTE(2026-09-07): 曾尝试"每帧驱动 PreDraw"补死链, 但实测干扰 SMYT 工具
                    // 动画路径(工具也没动画了)→ 已回退, 此路暂弃。
                }
                else
                {
                    Log.Warning("[Show Me Your Tools] Melee Animation compat: PreDraw/ClearAnimation missing — only tick delegate active");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Show Me Your Tools] Melee Animation compat hook failed: " + e.Message);
            }

            PatchWorldMapLeak();
        }

        // ==================== 星球(世界)地图泄漏修复(2026-09-04) ====================
        // 现象: 近战动画的武器/手部网格出现在星球(世界)地图上, 图层错误。
        // 根因(反编译 zAnimationMod.dll 实证):
        //   1) AM 的 AnimationManager 是 MapComponent, vanilla 的 Map.MapUpdate 对
        //      MapComponentUtility.MapComponentUpdate 不做 WorldRendererUtility.DrawingMap 门控
        //      (只门控 DrawDynamicThings / MapComponentOnGUI), 所以切到星球视图后 AM 仍每帧
        //      执行 DrawAll → Graphics.DrawMesh 提交绘制;
        //   2) AnimRenderer.Camera 字段在整个 DLL 中没有任何赋值(恒为 null), 以 null 相机调用
        //      Graphics.DrawMesh 会被当前帧的所有活跃相机渲染 —— 星球相机也画, 于是网格被
        //      投影到星球画面上(世界地图上飘武器+白点)。
        // 修两层:
        //   A) MapComponentUpdate Prefix: 星球视图(!DrawingMap)整帧跳过 —— 根治 + 省性能;
        //   B) transpiler: 把 AM 绘制方法里"恒 null 的 Camera 字段/ldnull 相机参数"替换为
        //      Find.Camera(地图相机), 4 参无相机重载改走绑定地图相机的 helper —— 堵住
        //      1.6 背景星球模式(地图边缘外露星球, DrawingMap 仍为 true)时的残留泄漏。
        //      AM 动画编辑器的预览走局部相机变量显式传参, 不在以下 4 个类里, 不受影响。
        private static bool AnimUpdateGate() => RimWorld.Planet.WorldRendererUtility.DrawingMap;

        // 替换 AnimRenderer.Camera(恒 null)读取: 参数取 object 是为了 IL 层面直接替 ldfld
        // (栈上原有实例引用转交作参数), 返回地图相机。
        public static Camera AnimCamera(object renderer) => Find.Camera;

        // 替换 4 参 Graphics.DrawMesh(mesh, matrix, mat, layer): 显式绑定地图相机。
        public static void AnimDrawMeshCam(Mesh mesh, Matrix4x4 matrix, Material mat, int layer)
            => Graphics.DrawMesh(mesh, matrix, mat, layer, Find.Camera);

        private static void PatchWorldMapLeak()
        {
            if (!Active) return;
            try
            {
                Harmony h = new Harmony("meathax.JobEffects.compat");

                Type mgr = AccessTools.TypeByName("AM.AnimationManager");
                MethodInfo upd = mgr != null ? AccessTools.Method(mgr, "MapComponentUpdate") : null;
                if (upd != null)
                {
                    h.Patch(upd, prefix: new HarmonyMethod(
                        AccessTools.Method(typeof(MeleeAnimationCompat), nameof(AnimUpdateGate))));
                    Log.Message("[Show Me Your Tools] Melee Animation compat: planet-view gate installed on AnimationManager.MapComponentUpdate");
                }
                else
                {
                    Log.Warning("[Show Me Your Tools] Melee Animation compat: AnimationManager.MapComponentUpdate not found — camera binding only");
                }

                var transpiler = new HarmonyMethod(
                    AccessTools.Method(typeof(MeleeAnimationCompat), nameof(BindDrawCamera_IL)));
                int patched = 0;
                // 只限定 4 个反编译实证的绘制类(AnimRenderer 主体+阴影, PartWithSweep 挥砍轨迹,
                // GrabUtility 钩爪绳索, HeadInstance 掉落头阴影)。
                string[] typeNames = { "AM.AnimRenderer", "AM.PartWithSweep", "AM.GrabUtility", "AM.HeadInstance" };
                foreach (string tn in typeNames)
                {
                    Type t = AccessTools.TypeByName(tn);
                    if (t == null) continue;
                    foreach (MethodInfo m in AccessTools.GetDeclaredMethods(t))
                    {
                        try { h.Patch(m, transpiler: transpiler); patched++; }
                        catch { }
                    }
                }
                Log.Message("[Show Me Your Tools] Melee Animation compat: camera-binding transpiler on " + patched + " methods");
            }
            catch (Exception e)
            {
                Log.Warning("[Show Me Your Tools] Melee Animation world-map leak fix failed: " + e.Message);
            }
        }

        // IL 改写规则(全部栈平衡):
        //   1) ldfld AnimRenderer::Camera(恒 null) → call AnimCamera(object);
        //   2) 4 参 Graphics.DrawMesh → 改调 AnimDrawMeshCam(同签名, 绑定 Find.Camera);
        //   3) 7 参 Graphics.DrawMesh 的相机槽为 ldnull → 改 call Find.get_Camera。
        //      ldnull 定位: 从调用点向前回退到上一条 store/call/branch 边界, 窗口内恰好
        //      一个 ldnull 才替换, 否则保持原样(宁可不修不误伤)。
        private static IEnumerable<CodeInstruction> BindDrawCamera_IL(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo animCam = AccessTools.Method(typeof(MeleeAnimationCompat), nameof(AnimCamera));
            MethodInfo drawCam = AccessTools.Method(typeof(MeleeAnimationCompat), nameof(AnimDrawMeshCam));
            MethodInfo findCam = AccessTools.PropertyGetter(typeof(Find), nameof(Find.Camera));

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction ci = codes[i];
                // 规则 1: 读取恒 null 的 Camera 实例字段 → 换成取地图相机(实例引用转作参数)
                if (ci.opcode == OpCodes.Ldfld && ci.operand is FieldInfo ff
                    && ff.Name == "Camera" && ff.FieldType == typeof(Camera))
                {
                    ci.opcode = OpCodes.Call;
                    ci.operand = animCam;
                    continue;
                }
                if ((ci.opcode == OpCodes.Call || ci.opcode == OpCodes.Callvirt)
                    && ci.operand is MethodInfo mi && mi.DeclaringType == typeof(Graphics) && mi.Name == "DrawMesh")
                {
                    System.Reflection.ParameterInfo[] pars = mi.GetParameters();
                    if (pars.Length == 4)
                    {
                        // 规则 2: 无相机重载 → helper(同参个数同类型, 直接换调用目标)
                        ci.operand = drawCam;
                    }
                    else if (pars.Length == 7 && pars[4].ParameterType == typeof(Camera))
                    {
                        // 规则 3: 回退找本调用的参数压栈段, 恰有一个 ldnull(相机槽)才替换
                        int j = i - 1;
                        for (; j >= 0; j--)
                        {
                            OpCode op = codes[j].opcode;
                            if (op == OpCodes.Call || op == OpCodes.Callvirt
                                || op == OpCodes.Stsfld || op == OpCodes.Pop
                                || op.Name.StartsWith("stloc", StringComparison.Ordinal)
                                || (op.Name.Length > 1 && op.Name[0] == 'b'))   // br*/beq*/bge*/blt* 等全部跳转
                                break;
                        }
                        int ldnullIdx = -1, ldnullCount = 0;
                        for (int k = j + 1; k < i; k++)
                        {
                            if (codes[k].opcode == OpCodes.Ldnull) { ldnullCount++; ldnullIdx = k; }
                        }
                        if (ldnullCount == 1)
                        {
                            codes[ldnullIdx].opcode = OpCodes.Call;
                            codes[ldnullIdx].operand = findCam;
                        }
                    }
                }
            }
            return codes;
        }

        /// <summary>
        /// Add/remove our delegate from AM's list to match the current settings, so that with tool
        /// animation (or the "override other tool mods" option) switched off we are not in AM's
        /// per-tick iteration at all. Idempotent; called at init and from Mod.WriteSettings.
        ///
        /// Mutating AM's list is safe here: AM only enumerates it from CompTick (tick phase) and
        /// PreDraw (render phase), both inside Unity's Update, while settings are written from
        /// OnGUI. The two never interleave, so this can't invalidate an in-flight enumerator.
        /// </summary>
        public static void SyncRegistration()
        {
            if (hookList == null || hook == null) return;
            bool want = JobEffectsSettings.AnimatedTools && JobEffectsSettings.OverrideToolMods;
            if (want == registered) return;
            try
            {
                if (want) hookList.Add(hook);
                else hookList.Remove(hook);
                registered = want;
            }
            catch (Exception e)
            {
                Log.Warning("[Show Me Your Tools] Melee Animation compat (de)registration failed: " + e.Message);
            }
        }

        // The exact window the equipped weapon must stay hidden: while SMYT's tool is being animated
        // (HasActiveTool) OR still lingering at the hip after the job (IsHolstering — holster hold +
        // fade, and the en-route belt carry). This mirrors Patch_DrawEquipment_OverrideToolMods'
        // vanilla-path gate verbatim so the two hide-paths can never disagree. Previously AM's
        // suppression only checked HasActiveTool, so during the linger window AM re-materialised the
        // equipped melee weapon and it floated beside the still-visible animated tool (the "floating
        // pickaxe after a swing" desync). IsHolstering is cheap (a settings bool + PawnStates.Peek).
        private static bool ToolStillVisible(Pawn pawn)
            => ToolAnimator.HasActiveTool(pawn) || ToolAnimator.IsHolstering(pawn);

        // Render-time hard kill — see Apply(). Runs once per rendered pawn per frame; every line
        // shares the tick delegate's budget. Clearing (not just skipping) the animation matters:
        // AM's RenderPawnAt draws via AnimRenderer lookup, so a live CurrentAnimation would still
        // paint the weapon even with PreDraw skipped.
        public static bool PreDrawSuppress(object __instance)
        {
            try
            {
                if (!JobEffectsSettings.AnimatedTools || !JobEffectsSettings.OverrideToolMods) return true;
                if (!(__instance is ThingComp comp)) return true;
                if (!(comp.parent is Pawn pawn)) return true;
                if (!ToolStillVisible(pawn)) return true;
                clearAnim?.Invoke(__instance, null);   // no-op when no animation is live
                return false;
            }
            catch { return true; }
        }

        // Signature-compatible with AM.Idle.IdleControllerDrawDelegate
        // (void(IdleControllerComp, ref bool shouldBeActive, ref bool doDefaultDraw)).
        // Suppress AM's weapon animation/draw for a pawn while we're showing an animated tool.
        //
        // Hot: per humanlike, per tick. Every line here is on that budget — the ladder is ordered
        // static bool -> field read -> type check -> ToolAnimator's array-indexed job gate.
        public static void ShouldDrawHook(ThingComp comp, ref bool shouldBeActive, ref bool doDefaultDraw)
        {
            if (!shouldBeActive) return;   // already suppressed by another delegate
            // Belt-and-braces: SyncRegistration should already have removed us, but a settings change
            // that never routes through WriteSettings must not leave us suppressing weapons.
            if (!JobEffectsSettings.AnimatedTools || !JobEffectsSettings.OverrideToolMods) return;
            try
            {
                // ThingComp.parent is a public field — plain read, no reflection.
                if (!(comp.parent is Pawn pawn)) return;
                if (ToolStillVisible(pawn))
                {
                    shouldBeActive = false;
                    doDefaultDraw = false;
                }
            }
            catch { }
        }
    }
}

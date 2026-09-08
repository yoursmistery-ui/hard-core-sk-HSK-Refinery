// zz.AM.RatkinPerfPatch.dll — 近战动画HSK 性能补丁(独立 Harmony 补丁,不改原 zAnimationMod.dll)
// 2026-09-05 by ratkin
//
// 解决"动画小人一多就卡"的四板斧(P1/P2 档位经用户确认;P3 2026-09-06 增补;P4 2026-09-06 增补):
//  另: P5/P6(2026-09-06 增补的诊断探针/CSV 开关)采完数据后已于 2026-09-07 整体移除。
//  P1【扫描节流 v3·200 tick 保底+触发率精确补偿+和平短路】MapPawnProcessor.Tick 每
//      ScanTickInterval(默认20)全图扫 AllPawnsSpawned,单次 15~30ms(Analyzer 实测摊薄
//      0.76ms/tick,战斗时单次 100~125ms 尖峰)。v3 三层:
//      ① 有效间隔保底 200 tick(取 max,玩家调大仍生效);
//      ② 通过时把 Settings.ScanTickInterval 同步改写为有效间隔——内部 MTB 掷骰概率
//        = interval/(MTB×60),与扫描间隔同源,改写后单位时间触发率与原版设置完全
//        一致(纯降低检查频率,不削减功能触发次数);
//      ③ 和平短路:map.attackTargetsCache.TargetsHostileToColony 为空时自动处决/
//        擒抱不存在任何潜在目标(GetPotentialTargets 只取敌对/狂暴/无阵营),整次
//        扫描直接跳过——和平期该组件成本归零,行为无损。
//      附带:原先每 tick 的 GetScanTickInterval 反射查询改为缓存 FieldInfo。
//  P2【Yayo 双动画协同】检测到 Yayo's Animation(com.yayo.yayoani.continued)启用
//      且其 CombatAnim=True 时,把 AM 的闲暇武器挥动画(HandleStartingFlavourAnim)
//      整体短路(等效闲暇动画关闭),消除双动画系统叠加的闲暇小人;
//      不影响 AM 的攻击/处决/决斗/移动动画本体,Yayo 本体分毫不动。
//  P3【GetAnimator 免锁】渲染热路径锁消除(详见下)。
//
// 只对 AM 侧做手脚;补丁可整体移除,不残留任何状态。加载即生效,无需额外配置。
// 实现:对 zAnimationMod 全部字符串反射访问(AccessTools),编译期零依赖;
//       本文件为 C#5 兼容语法,系统 csc.exe 直接编译(与 HSK修复整合 同路径)。

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AM.RatkinPerfPatch
{
	[StaticConstructorOnStartup]
	public static class RatkinPerfMain
	{
		public static Harmony HarmonyInstance;

		public static bool YayoActiveWithCombatAnim;

		public static int MinScanInterval = 200;

		// AM 类型名(始终用字符串,避免编译期强引用)
		public const string CoreType = "AM.Core";
		public const string MapPawnProcessorType = "AM.Processing.MapPawnProcessor";
		private const string IdleControllerCompType = "AM.Idle.IdleControllerComp";
		private const string PatchMasterType = "AM.Patches.PatchMaster";

		static RatkinPerfMain()
		{
			try
			{
				HarmonyInstance = new Harmony("local.ratkin.melee.perfpatch");
				ApplyPatches();
				EvaluateYayo();
				Log.Message("[AM.Perf] Perf patch loaded. ScanIntervalGuard=" + MinScanInterval + " YayoCombatAnim=" + YayoActiveWithCombatAnim);
			}
			catch (Exception e)
			{
				Log.Error("[AM.Perf] Failed to init perf patch: " + e);
			}
		}

		private static void ApplyPatches()
		{
			// P1: MapPawnProcessor.Tick 节流
			Type mppType = AccessTools.TypeByName(MapPawnProcessorType);
			MethodBase tick = mppType != null ? AccessTools.Method(mppType, "Tick", new Type[0], null) : null;
			if (tick != null)
			{
				HarmonyInstance.Patch(tick, prefix: new HarmonyMethod(typeof(Patch_ScanThrottle), "Prefix"));
				Log.Message("[AM.Perf] Patched " + MapPawnProcessorType + ".Tick");
			}
			else
			{
				Log.Error("[AM.Perf] FAILED to find " + MapPawnProcessorType + ".Tick");
			}

			// P2: IdleControllerComp.HandleStartingFlavourAnim 协同
			Type iccType = AccessTools.TypeByName(IdleControllerCompType);
			MethodBase flavour = iccType != null ? AccessTools.Method(iccType, "HandleStartingFlavourAnim", null, null) : null;
			if (flavour != null)
			{
				HarmonyInstance.Patch(flavour, prefix: new HarmonyMethod(typeof(Patch_IdleFlavour), "Prefix"));
				Log.Message("[AM.Perf] Patched " + IdleControllerCompType + ".HandleStartingFlavourAnim");
			}
			else
			{
				Log.Error("[AM.Perf] FAILED to find " + IdleControllerCompType + ".HandleStartingFlavourAnim");
			}

			// P3: PatchMaster.GetAnimator 免锁直查(渲染热路径,每帧×每小人)
			Type pmType = AccessTools.TypeByName(PatchMasterType);
			MethodBase getAnim = pmType != null ? AccessTools.Method(pmType, "GetAnimator") : null;
			if (getAnim != null)
			{
				HarmonyInstance.Patch(getAnim, prefix: new HarmonyMethod(typeof(Patch_GetAnimatorLockFree), "Prefix"));
				Log.Message("[AM.Perf] Patched " + PatchMasterType + ".GetAnimator (lock-free)");
			}
			else
			{
				Log.Error("[AM.Perf] FAILED to find " + PatchMasterType + ".GetAnimator");
			}

			// P5(2026-09-06 已移除): 曾置 MapPawnProcessor.LogPerformanceToDesktop=true 向桌面
			// 写扫描剖析 CSV。已采完(307 行,avg 0.12ms / max 0.96ms,attackers≤10——扫描非热点),
			// 按"采完即关"移除,不再写 CSV。
		}

		// 读取 AM.Core.Settings.ScanTickInterval(反射;失败返回默认 5)。
		public static int GetScanTickInterval()
		{
			try
			{
				Type coreType = AccessTools.TypeByName(CoreType);
				if (coreType == null)
				{
					return 5;
				}
				FieldInfo settingsField = coreType.GetField("Settings", BindingFlags.Public | BindingFlags.Static);
				if (settingsField == null)
				{
					return 5;
				}
				object settings = settingsField.GetValue(null);
				if (settings == null)
				{
					return 5;
				}
				FieldInfo f = settings.GetType().GetField("ScanTickInterval", BindingFlags.Public | BindingFlags.Instance);
				if (f == null)
				{
					return 5;
				}
				object v = f.GetValue(settings);
				if (v is int)
				{
					return (int)v;
				}
			}
			catch
			{
			}
			return 5;
		}

		// 检测 Yayo 是否启用,且其 ModSettings 中 CombatAnim==true(纯只读)。
		private static void EvaluateYayo()
		{
			try
			{
				bool isYayoActive = ModsConfig.IsActive("com.yayo.yayoani.continued");
				bool combatAnim = false;
				if (isYayoActive)
				{
					combatAnim = FindYayoCombatAnim();
				}
				YayoActiveWithCombatAnim = isYayoActive && combatAnim;
			}
			catch (Exception e)
			{
				Log.Warning("[AM.Perf] Failed to evaluate Yayo settings: " + e);
				YayoActiveWithCombatAnim = false;
			}
		}

		// 从 Yayo 的 ModSettings 实例反射读取 CombatAnim bool 字段;任何失败返回 false(最保守=不拦)。
		// 注:1.6 的 Verse.Mod 没有公开 Settings 属性(反编译确认),实例存于私有字段 modSettings,反射读取。
		private static bool FindYayoCombatAnim()
		{
			try
			{
				FieldInfo modSettingsField = AccessTools.Field(typeof(Mod), "modSettings");
				if (modSettingsField == null)
				{
					return false;
				}
				foreach (Mod m in LoadedModManager.ModHandles)
				{
					if (m == null)
					{
						continue;
					}
					object settings = modSettingsField.GetValue(m);
					if (settings == null)
					{
						continue;
					}
					Type t = settings.GetType();
					string fullName = t.FullName;
					if (fullName != null && fullName.StartsWith("yayoAni", StringComparison.Ordinal))
					{
						FieldInfo f = AccessTools.Field(t, "CombatAnim");
						if (f != null)
						{
							object v = f.GetValue(settings);
							if (v is bool)
							{
								return (bool)v;
							}
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}
	}

	// ── P1 扫描节流 v3(间隔保底 + 触发率补偿 + 和平短路)─────────
	// MapPawnProcessor.Tick 每 tick 被 MapComponentTick 调用,内部自门控:
	//   if (TicksAbs % Settings.ScanTickInterval != 0) return;
	// 之后 CompileListOfAttackers 全图扫 AllPawnsSpawned + MTB 掷骰,单次 15~30ms,
	// 战斗期(过骰者多)单次可到 100ms+。v3 Prefix 三步:
	//   ① 有效间隔 eff = max(200, 玩家设置),未到点直接跳过整次扫描;
	//   ② 到点时把 Settings.ScanTickInterval 同步改写为 eff——内部处决/擒抱掷骰
	//     Rand.MTBEventOccurs(mtb, 60, interval) 的概率正比 interval,扫描间隔与
	//     概率基数同步放大后,单位时间触发率与原版设置严格一致(不削功能);
	//   ③ map.attackTargetsCache.TargetsHostileToColony 为空 → 自动处决/擒抱的
	//     GetPotentialTargets(敌对派系/狂暴精神/无阵营人类)必为空 → 整次跳过。
	// 反射目标(Core.Settings 实例、ScanTickInterval、processor.map)全部缓存,
	// 初始化失败时放行原实现(等同无补丁),只降级一次。
	public static class Patch_ScanThrottle
	{
		internal static object settingsInst;
		internal static FieldInfo intervalField;
		private static FieldInfo mapField;
		private static bool ready;
		private static bool failed;

		// Settings 实例为 null(AM.Core 静态构造尚未跑)时静默重试;字段缺失才永久放弃。
		private static void TryInit()
		{
			if (ready || failed)
			{
				return;
			}
			try
			{
				if (settingsInst == null)
				{
					Type coreType = AccessTools.TypeByName(RatkinPerfMain.CoreType);
					FieldInfo sf = coreType != null ? coreType.GetField("Settings", BindingFlags.Public | BindingFlags.Static) : null;
					settingsInst = sf != null ? sf.GetValue(null) : null;
					if (settingsInst == null)
					{
						return; // AM 尚未初始化,下个 tick 重试
					}
				}
				if (intervalField == null)
				{
					FieldInfo f = settingsInst.GetType().GetField("ScanTickInterval", BindingFlags.Public | BindingFlags.Instance);
					if (f == null)
					{
						failed = true;
						Log.Warning("[AM.Perf] P1 v3 init failed: ScanTickInterval 字段不存在,扫描节流退回原实现");
						return;
					}
					intervalField = f;
				}
				if (mapField == null)
				{
					Type mppType = AccessTools.TypeByName(RatkinPerfMain.MapPawnProcessorType);
					FieldInfo mf = mppType != null ? AccessTools.Field(mppType, "map") : null;
					if (mf == null)
					{
						failed = true;
						Log.Warning("[AM.Perf] P1 v3 init failed: MapPawnProcessor.map 字段不存在,扫描节流退回原实现");
						return;
					}
					mapField = mf;
				}
				ready = true;
			}
			catch (Exception e)
			{
				failed = true;
				Log.Warning("[AM.Perf] P1 v3 init exception, 扫描节流退回原实现: " + e);
			}
		}

		public static bool Prefix(object __instance)
		{
			if (!ready)
			{
				TryInit();
				if (!ready)
				{
					return true;
				}
			}
			int cur = (int)intervalField.GetValue(settingsInst);
			int eff = cur < RatkinPerfMain.MinScanInterval ? RatkinPerfMain.MinScanInterval : cur;
			if (GenTicks.TicksAbs % eff != 0)
			{
				return false;
			}
			if (cur != eff)
			{
				intervalField.SetValue(settingsInst, eff); // MTB 概率基数与扫描间隔同步,触发率无损
			}
			try
			{
				Map map = mapField.GetValue(__instance) as Map;
				if (map != null && map.attackTargetsCache.TargetsHostileToColony.Count == 0)
				{
					return false;
				}
			}
			catch
			{
			}
			return true;
		}
	}

	// ── P2 Yayo 双动画协同 ───────────────────────────────────────
	// IdleControllerComp.HandleStartingFlavourAnim(ItemTweakData) 是
	// "站立持械随机挥舞"闲暇动画的唯一触发点(内部 check FlavourMTB>0)。
	// Yayo+CombatAnim 双开时短路之。
	public static class Patch_IdleFlavour
	{
		public static bool Prefix()
		{
			if (RatkinPerfMain.YayoActiveWithCombatAnim)
			{
				return false;
			}
			return true;
		}
	}

	// ── P3 渲染热路径免锁动画器查询(2026-09-06 增补) ────────────
	// PatchMaster.GetAnimator 是"每帧 × 每个渲染小人"的查询口(RenderPawnAt 前缀、
	// Pawn.DrawGUIOverlay 前缀、InvisibilityUtility.IsPsychologicallyInvisible 前缀
	// 都经它),原实现 = lock(全局锁) + 单入口缓存 + 字典查询。两个问题:
	//  ① 1.6 并行渲染(PawnRenderTree.ParallelPreDraw 系列)下多个 worker 同时过这把
	//     锁,把本应并行的渲染查询串行化;
	//  ② 字典本体 AnimRenderer.pawnToRenderer 的写入(RegisterInt/DestroyNew)并不持
	//     该锁,锁对字典没有实际保护作用,纯属开销。
	// 本补丁:反射取 pawnToRenderer 引用(static readonly,实例永不替换),用非泛型
	//     IDictionary 索引器一次哈希直查;无锁、无共享可变缓存字段。字典写入只发生
	//     在主线程 Update/Tick 阶段(动画开始/结束),渲染阶段对其纯只读,并发安全。
	// 兜底:初始化失败或查询行为异常时 ready=false,前缀放行走原实现,只降级一次。
	// 回退:本补丁可直接删除,不残留任何状态。
	public static class Patch_GetAnimatorLockFree
	{
		private static System.Collections.IDictionary map;

		private static bool ready;

		static Patch_GetAnimatorLockFree()
		{
			try
			{
				Type t = AccessTools.TypeByName("AM.AnimRenderer");
				FieldInfo f = t != null ? AccessTools.Field(t, "pawnToRenderer") : null;
				map = f != null ? f.GetValue(null) as System.Collections.IDictionary : null;
				ready = map != null;
			}
			catch (Exception e)
			{
				Log.Warning("[AM.Perf] P3 init failed, keep original GetAnimator: " + e);
				map = null;
				ready = false;
			}
		}

		public static bool Prefix(Pawn pawn, ref object __result)
		{
			if (!ready)
			{
				return true;
			}
			if (pawn == null)
			{
				__result = null;
				return false;
			}
			try
			{
				// Dictionary 的显式 IDictionary 索引器按 .NET/Mono 语义未命中返回 null;
				// 若运行时行为有异则自禁用回退原实现。
				__result = map[pawn];
				return false;
			}
			catch (Exception inner)
			{
				Log.Warning("[AM.Perf] P3 lookup failed, keep original GetAnimator: " + inner);
				ready = false;
				__result = null;
				return false;
			}
		}
	}
	}

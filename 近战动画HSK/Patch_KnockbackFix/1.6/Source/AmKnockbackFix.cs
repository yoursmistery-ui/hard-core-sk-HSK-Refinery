// zz.AM.KnockbackFix.dll — 近战动画HSK 击退飞行器"零飞行时间"除零修复 + 向西击退落点修复
// 2026-09-11 by ratkin
//
// ---------------------------------------------------------------------------
// 【玩家实测日志】
//   Exception in EnsureInitialized for 'AM_KnockbackFlyer3978168' at cell (-2147483648, 0, -2147483648):
//   System.NullReferenceException: Object reference not set to an instance of an object
//     at Verse.Thing.set_Position (Verse.IntVec3 value)
//     - TRANSPILER CombatExtended.HarmonyCE.Harmony_Thing_Position:Transpiler
//     at RimWorld.PawnFlyer.RecomputePosition ()
//     - PREFIX OskarPotocki.VEF.Abilities.VanillaExpandedFramework_PawnFlyer_RecomputePosition_Patch:Prefix
//     at RimWorld.PawnFlyer.DynamicDrawPhaseAt (...)
//     at PerformanceFish.Rendering.DynamicDrawManagerPatches+DrawDynamicThingsPatch.EnsureInitialized (...)
//   + AM_KnockbackFlyer3978168 tried to de-register out of bounds at (-2147483648, 0, -2147483648)
//   + Tried to get valid region out of bounds at (-2147483648, 0, -2147483648)
//   + Cannot deregister drawable AM_KnockbackFlyer3978168 while drawing is in progress.
//   玩家可见后果:被"踹飞(Punt)"的单位有概率卡住 / 消失 / 无法行动。
//
// ---------------------------------------------------------------------------
// 【根因】反编译 AM.Grappling.KnockbackFlyer + 原版 RimWorld.PawnFlyer 后逐行比对:
//
//  1) zAnimationMod 自身有两个缺陷叠加,只在"向西击退"时同时命中:
//
//     a. KnockbackFlyer.GetEndCell → GetCellsFromTo(迭代器)的循环条件写错了方向:
//          bool hor = from.x != to.x;
//          if (hor) { dir = Clamp(to.x - from.x, -1, 1); x = from.x + dir;
//                     while (x <= to.x) { yield return new IntVec3(x, from.y, from.z); x += dir; } }
//          //       ^^^^^^^^ 只对 to.x > from.x(向东)成立
//        向西击退时 to.x < from.x → x = from.x - 1 恒 > to.x → 循环体一次都不执行
//        → GetEndCell 直接返回初始值 result = victim.Position(原地)。
//        (PuntPawnWorker: direction = flag ? IntVec3.East : IntVec3.West, flag 取自动画事件 Right 且受 MirrorHorizontal 翻转,
//         所以"向左/被镜像"的那一半击退必然踩中。)
//
//     b. KnockbackFlyer.SpawnSetup 用自建字段 StartPos/EndPos 的距离覆盖基类的飞行时长:
//          ticksFlightTime = (int)((StartPos.ToFlat() - EndPos.ToFlat()).magnitude * 3f);
//        由于 (a) 使 EndPos == StartPos(零位移) → magnitude == 0 → ticksFlightTime = 0。
//        它覆盖掉了原版 PawnFlyer.SpawnSetup 里的安全下限
//          a = Max(flightDistance, 1f) / def.pawnFlyer.flightSpeed; a = Max(a, flightDurationMin);
//        (Flyers.xml 里写的 flightDurationMin/flightSpeed 在这里被无视,注释也自认"被 C# 覆盖")。
//
//  2) 原版 PawnFlyer.RecomputePosition 于是除零:
//        float t = (float)ticksFlying / (float)ticksFlightTime;   // 0f / 0 = NaN
//        groundPos = Vector3.Lerp(startVec, DestinationPos, t);   // NaN 向量
//        base.Position = groundPos.ToIntVec3();                   // IntVec3.Invalid
//     float NaN 转 int 在 Mono 上得到 int.MinValue == -2147483648,与日志里的
//     (-2147483648, 0, -2147483648) 完全吻合 —— 这就是那串坐标的来源。
//
//  3) Position 被写成无效值后,ThingGrid 反注册/注册与 Region 查询全部越界报错;
//     并且会有一条不经过 RespawnPawn 的 Destroy 路径把 flyer(连同 innerContainer 里的 pawn)
//     直接销毁 —— 这就是"单位卡住/消失/不动"。
//
//  放大器(不是元凶,不建议动):
//   * PerformanceFish — Draw 阶段强制 EnsureInitialized → DynamicDrawPhase → RecomputePosition,
//     把本该在 Tick 里做的位置重算提前到绘制采样时,且它的 drawable 缓存里还留着已销毁的 flyer,
//     所以才会出现 "Cannot deregister drawable ... while drawing is in progress"。
//   * CombatExtended  — 对 Thing.set_Position 挂了 Transpiler,那条 NullReferenceException 就发生在被改写后的 setter 里。
//   * VEF             — 对 PawnFlyer.RecomputePosition 挂 Prefix、对 PawnFlyer.MakeFlyer 挂 Transpiler。
//
// ---------------------------------------------------------------------------
// 【修法】三层,不改 AM 原始 dll、不改任何 XML 数值:
//
//   P1【根因·防崩】KnockbackFlyer.SpawnSetup 之后 —— ticksFlightTime <= 0 就抬到 1。
//       抬到 1 之后:  tick0: positionLastComputedTick == ticksFlying(0 == 0),不算,位置保持有效;
//                     tick1: ticksFlying(0) >= ticksFlightTime(1) 不成立 → ticksFlying += 1;
//                     下一个 tick 才 RespawnPawn + Destroy,正常落地(落地逻辑不动)。
//
//   P2【兜底·防崩】原版 PawnFlyer.RecomputePosition 之前 —— 任何来源的 ticksFlightTime <= 0 都就地抬到 1。
//       正常 flyer(原版跳跃默认 120 tick)读取一次即返回,零副作用;
//       即使将来别的 mod 又造出一个 0 飞行时间的 PawnFlyer,t 也永远不可能是 NaN。
//
//   P3【功能·2026-09-11 玩家确认后追加】KnockbackFlyer.GetEndCell 之前置 —— 接管落点计算,
//       把上面 (1a) 那个方向缺陷本身修掉:沿 direction 逐格推进,撞到阻挡物即停在前一格。
//       于是"向西击退"不再原地落下,也能真的把目标踹出去(与向东行为对齐)。
//       ⚠ 顺带修正:原实现是"先赋值 result = cell 再判 IsSolid",若第一格就撞墙,落点会被设成
//         那个实心格(目标被放进墙/建筑里,这本身也是"单位卡住"的来源之一);本实现改为停在前一格。
//         代价:向东击退若终点原本是实心格,距离会比原来少 1 格(从墙格退到墙前格),这是有意的修正。
//       ⚠ "第一格就撞墙"时落点仍等于起点(零位移),此时由 P1 兜底:原地落地 + 眩晕,不会崩。
//
// 补丁可整体移除:删掉 Patch_KnockbackFix 目录 + LoadFolders.xml 里对应一行即可,不残留任何状态。
// 实现:对 zAnimationMod 全部字符串反射访问(AccessTools.TypeByName),编译期零依赖该 dll;
//       本文件为 C#5 兼容语法,可直接用系统 csc.exe 编译(与 Patch_CleanFeatures / Patch_RatkinPerf 同路径)。

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AM.KnockbackFix
{
	[StaticConstructorOnStartup]
	public static class KnockbackFixMain
	{
		public static Harmony HarmonyInstance;

		private const string KnockbackFlyerType = "AM.Grappling.KnockbackFlyer";

		private const string HarmonyId = "local.ratkin.melee.knockbackfix";

		// 原版 PawnFlyer.ticksFlightTime (protected int)。
		// 注:这里不用 AccessTools.FieldRef<T,F> —— 它是"返回 ref"的委托,而 ref 返回是 C# 7 特性,
		//     系统 csc.exe(v4.0.30319 = C# 5)编译会直接报 CS0648 "不是个受支持的类型"。
		//     改用 FieldInfo 反射:调用点只有 spawn 一次 + RecomputePosition 每帧每 flyer 一次,
		//     而同屏 PawnFlyer 通常 0~3 个,开销可忽略。
		private static FieldInfo TicksFlightTimeField;

		// 同类警告只打印前若干次,防异常状态下每帧刷屏。
		private static int clampLogCount;

		private const int MaxClampLogs = 3;

		static KnockbackFixMain()
		{
			try
			{
				HarmonyInstance = new Harmony(HarmonyId);
				ApplyPatches();
			}
			catch (Exception e)
			{
				Log.Error("[AM.KBFix] Failed to init knockback fix: " + e);
			}
		}

		private static void ApplyPatches()
		{
			// 绑定原版字段(不依赖 zAnimationMod,只依赖 Assembly-CSharp)
			TicksFlightTimeField = AccessTools.Field(typeof(PawnFlyer), "ticksFlightTime");
			if (TicksFlightTimeField == null)
			{
				Log.Error("[AM.KBFix] PawnFlyer.ticksFlightTime not found, abort (no patch applied)");
				return;
			}

			// P1【根因】AM.Grappling.KnockbackFlyer.SpawnSetup(Map, bool)
			try
			{
				Type kbType = AccessTools.TypeByName(KnockbackFlyerType);
				MethodBase spawnSetup = kbType != null
					? AccessTools.Method(kbType, "SpawnSetup", new Type[] { typeof(Map), typeof(bool) }, null)
					: null;
				if (spawnSetup == null)
				{
					Log.Warning("[AM.KBFix] P1 skipped: " + KnockbackFlyerType
						+ ".SpawnSetup(Map,bool) not found (AM 版本变动?)");
				}
				else
				{
					HarmonyInstance.Patch(spawnSetup,
						postfix: new HarmonyMethod(typeof(Patch_KnockbackSpawnSetup), "Postfix"));
					Log.Message("[AM.KBFix] P1 patched " + KnockbackFlyerType + ".SpawnSetup (flight time floor)");
				}
			}
			catch (Exception e1)
			{
				Log.Warning("[AM.KBFix] P1 init failed: " + e1);
			}

			// P2【兜底】RimWorld.PawnFlyer.RecomputePosition(private)
			try
			{
				MethodBase recompute = AccessTools.Method(typeof(PawnFlyer), "RecomputePosition", new Type[0], null);
				if (recompute == null)
				{
					Log.Warning("[AM.KBFix] P2 skipped: RimWorld.PawnFlyer.RecomputePosition not found");
				}
				else
				{
					HarmonyInstance.Patch(recompute,
						prefix: new HarmonyMethod(typeof(Patch_RecomputePositionGuard), "Prefix"));
					Log.Message("[AM.KBFix] P2 patched RimWorld.PawnFlyer.RecomputePosition (NaN guard)");
				}
			}
			catch (Exception e2)
			{
				Log.Warning("[AM.KBFix] P2 init failed: " + e2);
			}

			// P3【功能】AM.Grappling.KnockbackFlyer.GetEndCell(Pawn victim, in IntVec3 direction, int maxRange)
			// 注:原方法第三个参数是 `in IntVec3`(IL 上就是 IntVec3&),C#5 没有 in 关键字,
			//     patch 侧声明成 ref IntVec3 即可对上;本 patch 只读不改它。
			try
			{
				Type kbType = AccessTools.TypeByName(KnockbackFlyerType);
				MethodBase getEndCell = kbType != null ? AccessTools.Method(kbType, "GetEndCell") : null;
				if (getEndCell == null)
				{
					Log.Warning("[AM.KBFix] P3 skipped: " + KnockbackFlyerType
						+ ".GetEndCell not found (AM 版本变动?)");
				}
				else
				{
					HarmonyInstance.Patch(getEndCell,
						prefix: new HarmonyMethod(typeof(Patch_GetEndCell), "Prefix"));
					Log.Message("[AM.KBFix] P3 patched " + KnockbackFlyerType + ".GetEndCell (direction fix)");
				}
			}
			catch (Exception e3)
			{
				Log.Warning("[AM.KBFix] P3 init failed, keep original GetEndCell: " + e3);
			}
		}

		// P1/P2 共用:把非法的飞行时长抬到 1 tick,保证 RecomputePosition 里的 t 永远有定义。
		internal static void ClampFlightTime(PawnFlyer flyer)
		{
			if (flyer == null || TicksFlightTimeField == null)
			{
				return;
			}
			if ((int)TicksFlightTimeField.GetValue(flyer) > 0)
			{
				return;
			}
			TicksFlightTimeField.SetValue(flyer, 1);
			if (clampLogCount < MaxClampLogs)
			{
				clampLogCount++;
				string defName = flyer.def != null ? flyer.def.defName : "?";
				Log.Warning("[AM.KBFix] Zero flight time on " + flyer.GetType().Name + " (def=" + defName
					+ ") clamped to 1 tick to avoid NaN Position. See zz.AM.KnockbackFix header.");
			}
		}

		// P3 用:接管后的落点计算。
		// 语义 = "从 victim 出发, 沿 direction 逐格前进, 撞到阻挡物就停在前一格, 最多 maxRange 格"。
		// 对照 AM 原实现:同样的逐格推进, 但原实现的迭代器循环条件只对"向东"成立(向西一次都不进),
		// 且原实现先赋值后判阻挡(会把落点定在实心格上)。
		internal static IntVec3 ComputeEndCell(Pawn victim, IntVec3 direction, int maxRange)
		{
			if (victim == null)
			{
				return IntVec3.Invalid;
			}
			if (!victim.Spawned || victim.Map == null)
			{
				return victim.Position;
			}
			Map map = victim.Map;
			IntVec3 from = victim.Position;
			IntVec3 result = from;
			for (int i = 1; i <= maxRange; i++)
			{
				IntVec3 cell = from + direction * i;
				if (IsSolid(victim, cell, map))
				{
					break;
				}
				result = cell;
			}
			return result;
		}

		// 对齐 AM 的 IsSolid 语义:越界 或 该格有能阻挡该 pawn 的东西。
		internal static bool IsSolid(Pawn pawn, IntVec3 cell, Map map)
		{
			if (cell.x < 0 || cell.z < 0 || cell.x >= map.info.Size.x || cell.z >= map.info.Size.z)
			{
				return true;
			}
			List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
			for (int i = 0; i < things.Count; i++)
			{
				if (things[i].BlocksPawn(pawn))
				{
					return true;
				}
			}
			return false;
		}
	}

	// P1: AM.Grappling.KnockbackFlyer.SpawnSetup 之后置
	internal static class Patch_KnockbackSpawnSetup
	{
		private static void Postfix(object __instance)
		{
			KnockbackFixMain.ClampFlightTime(__instance as PawnFlyer);
		}
	}

	// P2: RimWorld.PawnFlyer.RecomputePosition 之前置
	internal static class Patch_RecomputePositionGuard
	{
		private static void Prefix(PawnFlyer __instance)
		{
			KnockbackFixMain.ClampFlightTime(__instance);
		}
	}

	// P3: AM.Grappling.KnockbackFlyer.GetEndCell 之前置 —— 完整接管落点计算(返回 false 跳过原方法)
	internal static class Patch_GetEndCell
	{
		private static bool Prefix(Pawn victim, ref IntVec3 direction, int maxRange, ref IntVec3 __result)
		{
			__result = KnockbackFixMain.ComputeEndCell(victim, direction, maxRange);
			return false;
		}
	}
}

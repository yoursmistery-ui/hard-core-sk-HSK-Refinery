// 污染统一 - 裂化装置沥青副产组件。
// 参考 Rimatomics 核废料生成逻辑：设备运行期间持续累积，定期在设备旁吐出沥青块废物。
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PollutionUnify
{
	public class CompProperties_AsphaltProducer : CompProperties
	{
		// 每次吐出的沥青数量
		public int stackSize = 5;

		// 每运行多少游戏天吐一次
		public float daysPerSpawn = 1.5f;

		// 产出物 defName
		public string waste = "Asphalt";

		public CompProperties_AsphaltProducer()
		{
			compClass = typeof(CompAsphaltProducer);
		}
	}

	public class CompAsphaltProducer : ThingComp
	{
		// 已累计的运行天数进度
		private float progress;

		public CompProperties_AsphaltProducer Props
		{
			get { return (CompProperties_AsphaltProducer)props; }
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref progress, "progress", 0f);
		}

		// CompTickRare 每 250 tick 调用一次（250/60000 = 1/240 游戏天）
		public override void CompTickRare()
		{
			base.CompTickRare();
			if (!IsRunning())
			{
				return;
			}

			progress += 250f / 60000f;
			if (progress < Props.daysPerSpawn)
			{
				return;
			}

			progress = 0f;
			SpawnWaste();
		}

		private bool IsRunning()
		{
			CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();
			if (power != null && !power.PowerOn)
			{
				return false;
			}

			CompFlickable flick = parent.TryGetComp<CompFlickable>();
			if (flick != null && !flick.SwitchIsOn)
			{
				return false;
			}

			return true;
		}

		private void SpawnWaste()
		{
			ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(Props.waste);
			if (def == null)
			{
				return;
			}

			Map map = parent.Map;
			IntVec3? cell = FindSpawnCell();
			if (cell == null || map == null)
			{
				return;
			}

			Thing asphalt = ThingMaker.MakeThing(def);
			asphalt.stackCount = Props.stackSize;
			GenSpawn.Spawn(asphalt, cell.Value, map);
		}

		private IntVec3? FindSpawnCell()
		{
			Map map = parent.Map;
			if (map == null)
			{
				return null;
			}

			CellRect occupied = parent.OccupiedRect();
			List<IntVec3> candidates = new List<IntVec3>();
			foreach (IntVec3 c in occupied.ExpandedBy(1))
			{
				if (occupied.Contains(c))
				{
					continue;
				}

				if (!c.InBounds(map) || c.Fogged(map))
				{
					continue;
				}

				if (!c.Walkable(map) || c.GetEdifice(map) != null)
				{
					continue;
				}

				if (c.GetFirstPawn(map) != null)
				{
					continue;
				}

				candidates.Add(c);
			}

			if (candidates.Count == 0)
			{
				return null;
			}

			return candidates.RandomElement();
		}
	}
}
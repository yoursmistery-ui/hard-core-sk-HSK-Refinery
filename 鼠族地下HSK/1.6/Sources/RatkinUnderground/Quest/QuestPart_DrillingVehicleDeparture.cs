using RimWorld;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RatkinUnderground
{
    public class QuestPart_DrillingVehicleDeparture : QuestPart
    {
        public RKU_DrillingVehicleInEnemyMap drillingVehicle;
        public string inSignal; // 钻机离开信号
        public string winSignal; // 任务成功信号
        public Faction faction; // 添加派系引用

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            if (signal.tag == inSignal)
            {
                StartDrillingVehicleDeparture();
            }
        }

        private void StartDrillingVehicleDeparture()
        {
            RKU_DrillingVehicleInEnemyMap targetDrillingVehicle = drillingVehicle;
            
            // 如果直接引用为null，尝试从QuestGen.slate获取钻机引用
            if (targetDrillingVehicle == null)
            {
                targetDrillingVehicle = QuestGen.slate.Get<RKU_DrillingVehicleInEnemyMap>("drillingVehicle");
            }
            
            // 如果slate中也没有，尝试通过派系查找钻机
            if (targetDrillingVehicle == null && faction != null)
            {
                Map map = Find.AnyPlayerHomeMap;
                if (map != null)
                {
                    var allDrillingVehicles = map.listerThings.ThingsOfDef(ThingDef.Named("RKU_DrillingVehicleInEnemyMap"));

                    targetDrillingVehicle = allDrillingVehicles
                        .OfType<RKU_DrillingVehicleInEnemyMap>()
                        .FirstOrDefault(d => d.Faction == faction);
                }
            }

            if (targetDrillingVehicle != null && targetDrillingVehicle.Spawned)
            {
                var passengers = targetDrillingVehicle.GetDirectlyHeldThings();
                
                // 在钻地机位置留下持续的灰尘效果
                for (int i = 0; i < 15; i++)
                {
                    IntVec3 offset = new IntVec3(Rand.Range(-1, 2), 0, Rand.Range(-1, 2));
                    FleckMaker.ThrowDustPuffThick((targetDrillingVehicle.Position + offset).ToVector3(), targetDrillingVehicle.Map, 2f, Color.gray);
                }
                
                if (targetDrillingVehicle.Map != null && targetDrillingVehicle.Position.IsValid)
                {
                    RKU_DigDust dust = (RKU_DigDust)ThingMaker.MakeThing(ThingDef.Named("RKU_DigDust"));
                    if (dust != null)
                    {
                        Vector3 centerPos = targetDrillingVehicle.Position.ToVector3() + new Vector3(0.5f, 0, 0.5f);
                        dust.exactPosition = centerPos;
                        GenSpawn.Spawn(dust, targetDrillingVehicle.Position, targetDrillingVehicle.Map);
                    }
                }
                
                // 在钻机消失位置生成电台和蓝图（校验 def / CanSpawn / 异常，避免改科技印刷品的 mod 在 Spawn 链里循环报错）
                Map rewardMap = targetDrillingVehicle.Map;
                IntVec3 rewardOrigin = targetDrillingVehicle.Position;
                if (rewardMap != null && rewardOrigin.IsValid && rewardOrigin.InBounds(rewardMap))
                {
                    Thing radio = DefOfs.RKU_Radio != null ? ThingMaker.MakeThing(DefOfs.RKU_Radio) : null;
                    IntVec3 radioPosition = rewardOrigin + new IntVec3(1, 0, 0);
                    if (!radioPosition.InBounds(rewardMap))
                    {
                        radioPosition = rewardOrigin;
                    }
                    if (radio != null && TrySpawnThingSafely(radio, rewardMap, radioPosition))
                    {
                        radio.SetFaction(Faction.OfPlayer);
                    }

                    ThingDef techprintDef = DefDatabase<ThingDef>.GetNamedSilentFail("Techprint_RKU_UndergroundGuerrillaEquipments");
                    if (techprintDef != null)
                    {
                        Thing researchReward = null;
                        try
                        {
                            researchReward = ThingMaker.MakeThing(techprintDef);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"蓝图生成失败: {ex.Message}");
                        }
                        if (researchReward != null)
                        {
                            TrySpawnThingSafely(researchReward, rewardMap, rewardOrigin);
                        }
                    }
                }

                targetDrillingVehicle.DeSpawn();

                // 发送信件
                string title = "RKU_RadioLeftTitle".Translate();
                string message = "RKU_RadioLeftMessage".Translate();
                
                
                ChoiceLetter letter = LetterMaker.MakeLetter(
                    title,
                    message,
                    LetterDefOf.PositiveEvent
                );
                Find.LetterStack.ReceiveLetter(letter);
                // 发送任务成功信号
                if (!string.IsNullOrEmpty(winSignal))
                {
                    Find.SignalManager.SendSignal(new Signal(winSignal));
                }
            }
            else
            {
                Log.Error("[QuestPart_DrillingVehicleDeparture] 无法找到有效的钻机车辆");
            }
        }

        private static bool TrySpawnThingSafely(Thing thing, Map map, IntVec3 preferredCell)
        {
            if (thing == null || map == null || thing.Destroyed) return false;
            Rot4 rot = thing.def.defaultPlacingRot;
            foreach (IntVec3 cell in CellsPreferredFirst(preferredCell, map))
            {
                if (thing.Destroyed) return false;
                if (!GenSpawn.CanSpawnAt(thing.def, cell, map, rot)) continue;
                try
                {
                    GenSpawn.Spawn(thing, cell, map);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Warning($"[QuestPart_DrillingVehicleDeparture] 生成 {thing.def.defName} 失败，已跳过: {ex.Message}");
                    if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
                    return false;
                }
            }
            if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            return false;
        }

        private static IEnumerable<IntVec3> CellsPreferredFirst(IntVec3 origin, Map map)
        {
            if (origin.InBounds(map)) yield return origin;
            foreach (IntVec3 c in GenAdj.CellsAdjacent8Way(new TargetInfo(origin,map)))
            {
                if (c.InBounds(map)) yield return c;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref drillingVehicle, "drillingVehicle");
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref winSignal, "winSignal");
            Scribe_References.Look(ref faction, "faction");
        }
    }
}
// 电力适配间房间判定(RoomRolePowerRoom,并入 HSK 修复整合)
//
// 需求(2026-08-29): 新增房间角色"电力适配间"——封闭房间内集中放置
// 发电设施与电池,合计 >= 2 台即判定,每台 100 分。
//
// 识别口径(通用识别,不硬编码 defName,自动覆盖原版与各 mod 的电力建筑):
// - 发电设施: 挂 CompPowerTrader 且 PowerConsumption < 0(1.6 私有字段
//   basePowerConsumption 的公共读取口,含研究升级系数)。
//   CompPowerPlant 及其全部子类(太阳能/风能/地热/化工发电机等)均继承
//   CompPowerTrader,发电机一律按负功率申报;
// - 电池: 挂 CompPowerBattery(含其子类,如 SK 裂变电池)。
// - 变压器/开关/用电器等耗电件不计分;单台闲置物不触发(合计>=2 才算),
//   避免"库房里一块电池就把卧室变成电力适配间"。
//
// 其余实现说明同 RoomRoleIndoorPool: 原版仅对 ProperRoom 打分,此处再显式
// 排除 PsychologicallyOutdoors;delta 按增量语义返回;事件驱动的线性扫描。

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RoomRolePowerRoom
{
    public class RoomRoleWorker_PowerRoom : RoomRoleWorker
    {
        private const float ScorePerUnit = 100f;
        private const int MinUnits = 2;

        public override float GetScore(Room room)
        {
            if (room.PsychologicallyOutdoors || room.CellCount == 0)
            {
                return 0f;
            }
            int units = 0;
            List<Thing> things = room.ContainedAndAdjacentThings;
            for (int i = 0; i < things.Count; i++)
            {
                if (!(things[i] is Building))
                {
                    continue;
                }
                Thing thing = things[i];
                CompPowerTrader trader = thing.TryGetComp<CompPowerTrader>();
                if (trader != null)
                {
                    // 1.6 起 basePowerConsumption 为私有,公共读取口是 PowerConsumption
                    // (含研究升级系数),原版 CompPowerPlant 同样以 < 0 判发电。
                    if (trader.Props.PowerConsumption < 0)
                    {
                        units++;
                    }
                }
                else if (thing.TryGetComp<CompPowerBattery>() != null)
                {
                    units++;
                }
            }
            if (units < MinUnits)
            {
                return 0f;
            }
            return units * ScorePerUnit;
        }

        public override float GetScoreDeltaIfBuildingPlaced(Room room, ThingDef buildingDef)
        {
            if (buildingDef != null && IsPowerUnitDef(buildingDef))
            {
                return ScorePerUnit;
            }
            return 0f;
        }

        private static bool IsPowerUnitDef(ThingDef def)
        {
            List<CompProperties> comps = def.comps;
            if (comps == null)
            {
                return false;
            }
            for (int i = 0; i < comps.Count; i++)
            {
                CompProperties props = comps[i];
                if (props is CompProperties_Battery)
                {
                    return true;
                }
                CompProperties_Power power = props as CompProperties_Power;
                if (power != null && power.PowerConsumption < 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

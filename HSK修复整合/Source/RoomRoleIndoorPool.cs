// 室内游泳池房间判定(RoomRoleIndoorPool,并入 HSK 修复整合)
//
// 需求(2026-08-29): 新增房间角色"室内游泳池"——封闭房间内出现
// 卫生分类"水池娱乐"里的建筑即判定: 游泳池(DBHSwimmingPool,权重150)、
// 热水浴缸(HotTub,权重100)。
//
// 实现说明:
// - 原版与环境内各 mod 的 RoomRoleWorker 均为硬编码,没有可参数化的通用
//   worker,故自建本类,由 Defs/RoomRoleDefs 下的 RoomRoleDef 引用。
// - DBH def 用 GetNamedSilentFail 按名解析: 未装 DBH 时得分恒 0,角色闲置
//   无害,不需要 FindMod 门控。
// - 原版 UpdateRoomStatsAndRole 只对 ProperRoom(封闭、RegionCount<=60)打分,
//   这里再显式排除 PsychologicallyOutdoors,保证"室内"语义(围墙无顶的大空间
//   心理上仍算室外,不算室内游泳池)。
// - GetScoreDeltaIfBuildingPlaced 按原版 Room.GetRoomRoleIfBuildingPlaced 的
//   语义返回"增量"(原版按 GetScore+delta 取 MaxBy),摆放预览能实时反映角色。
//
// 性能: GetScore 仅在房间 statsAndRoleDirty 时触发(事件驱动),单次为房间内
// 物件线性扫描 + 两次字典查询,无 tick 开销。

using System.Collections.Generic;
using Verse;

namespace RoomRoleIndoorPool
{
    public class RoomRoleWorker_IndoorPool : RoomRoleWorker
    {
        private const float PoolScore = 150f;
        private const float HotTubScore = 100f;

        public override float GetScore(Room room)
        {
            if (room.PsychologicallyOutdoors || room.CellCount == 0)
            {
                return 0f;
            }
            ThingDef poolDef = DefDatabase<ThingDef>.GetNamedSilentFail("DBHSwimmingPool");
            ThingDef hotTubDef = DefDatabase<ThingDef>.GetNamedSilentFail("HotTub");
            if (poolDef == null && hotTubDef == null)
            {
                return 0f;
            }
            float score = 0f;
            List<Thing> things = room.ContainedAndAdjacentThings;
            for (int i = 0; i < things.Count; i++)
            {
                ThingDef def = things[i].def;
                if (def == poolDef)
                {
                    score += PoolScore;
                }
                else if (def == hotTubDef)
                {
                    score += HotTubScore;
                }
            }
            return score;
        }

        public override float GetScoreDeltaIfBuildingPlaced(Room room, ThingDef buildingDef)
        {
            if (buildingDef == DefDatabase<ThingDef>.GetNamedSilentFail("DBHSwimmingPool"))
            {
                return PoolScore;
            }
            if (buildingDef == DefDatabase<ThingDef>.GetNamedSilentFail("HotTub"))
            {
                return HotTubScore;
            }
            return 0f;
        }
    }
}

// DBHWaterCapPacer.cs — Dubs Bad Hygiene 管网容量重算 分帧化(常驻,无配置页)
//
// 问题(2026-09-06 [TP2] 实测): MapComponent_Hygiene.MapComponentTick 在 UpdateCapacities
// 置位时(任何水用户 Register/Deregister 都会置位),**一个 tick 内遍历全部管网×全部水井**
// 调 UpdateCap();每个 UpdateCap 对该井 GridArea 逐格做 ValueAt 求和(IceWaterCapacityFrom /
// DeepWater/WaterCapacityFrom,O(格)),多井大泵场景单 tick 32~148ms 一记(本会话三次)。
//
// 修法: 前缀吞 CompWaterInlet.UpdateCap 与其子类 CompWaterInletCryo.UpdateCap,
// 每口井只在 (TicksGame % 120 == (thingIDNumber*31) & 119) 的那 tick 跑原方法,
// 全量刷新摊到 ≤120 tick(~1~2 游戏分钟)完成,单 tick 代价亚毫秒。
// 容量值最多陈旧 120 tick(冰/水网格本身变化极慢,行为不可感知);
// SpawnSetup 的首次 UpdateCap 最多延迟 120 tick,无影响。
// 反射拿不到类型(DBH 未装)则不接管。
//
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5;build.ps1 已引 BadHygiene.dll)。
using System;
using HarmonyLib;
using Verse;

namespace LocationGeneratorPacer
{
    [StaticConstructorOnStartup]
    public static class DbhWaterCapPacerInit
    {
        private const int SpreadTicks = 120;

        static DbhWaterCapPacerInit()
        {
            try
            {
                Type baseInlet = AccessTools.TypeByName("DubsBadHygiene.CompWaterInlet");
                Type cryoInlet = AccessTools.TypeByName("DubsBadHygiene.CompWaterInletCryo");
                if (baseInlet == null)
                {
                    return;                            // 未装 DBH,静默跳过
                }
                Harmony h = new Harmony("local.ratkin.hskfix.dbhwatcap");
                HarmonyMethod pre = new HarmonyMethod(typeof(DbhWaterCapPacer), "Prefix");
                int patched = 0;
                if (AccessTools.Method(baseInlet, "UpdateCap") != null) { h.Patch(AccessTools.Method(baseInlet, "UpdateCap"), pre); patched++; }
                if (cryoInlet != null && AccessTools.Method(cryoInlet, "UpdateCap") != null) { h.Patch(AccessTools.Method(cryoInlet, "UpdateCap"), pre); patched++; }
                if (patched > 0)
                {
                    Log.Message("[HSKFix] DBH 水井容量重算已分帧(每井 " + SpreadTicks + "t 一次,原一 tick 全井重扫的 " + "32~148ms 停顿应消失)");
                }
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] DBHWaterCapPacer 挂载失败: " + e);
            }
        }
    }

    public static class DbhWaterCapPacer
    {
        internal const int SpreadTicks = 120;

        public static bool Prefix(ThingComp __instance)
        {
            Thing parent = __instance.parent;
            int id = parent != null ? parent.thingIDNumber : 0;
            return Find.TickManager.TicksGame % SpreadTicks == ((id * 31) & (SpreadTicks - 1));
        }
    }
}

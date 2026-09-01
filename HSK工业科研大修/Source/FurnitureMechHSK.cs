using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

// ============================================================================
//  家具机制两件套(MO DLL 依赖项的等价复刻, 零 MO DLL)
// ----------------------------------------------------------------------------
//  ① 冰柜 IceChest —— 等价 MO 的 CompMeltable + CompIceBoxFill
//     MO 原机制: 往冰柜里填冰块, 冰块按室温慢慢融化, 融化期间持续向外抽热制冷;
//                冰块耗尽即失效。
//     我们: 直接用原版 CompProperties_Refuelable 的燃料槽装冰块(RK_IceBlock),
//           本 comp 只负责两件 MO 里在 DLL 中的事——
//             a) 冰块消耗速率随室温升高而加快(热天化得快);
//             b) 制冷输出随余量线性衰减(快没冰时制冷变弱)。
//           制冷本体仍交给原版 CompProperties_HeatPusher(heatPerSecond 负值),
//           不重复造轮子, 也不与 HSK 的温度体系抢实现。
//
//  ② 毛皮床 FurBed —— 等价 MO 的 Comp_BedCureHediff
//     MO 原机制: 睡在毛皮床上可治愈低温症。
//     我们: CompTickRare 低频轮询床上睡眠者, 命中目标 hediff 则治愈。
//           只在该建筑真的有人睡时才做 O(1) 检查, 无人即早退。
//
//  性能(AGENTS §9): 两者都用 CompTickRare(250 tick 一次), 且均先做"有没有人/
//      有没有燃料"的早退判断, 空转时开销接近零; 无全局组件、无反射缓存。
//
//  ⚠ 本文件为 **C#5 兼容写法**(与 BlueprintUnlockHSK.cs / PetroInfraHSK.cs 同规约):
//    build.ps1 用的是 Framework64\v4.0.30319\csc.exe, 默认语言版本为 C#5,
//    因此禁止 $"" 字符串插值、=> 表达式体成员、?. null 条件、内联 out var、局部函数。
//    字符串一律 string.Format, 属性一律 get { return ...; }, 判空一律显式 if。
// ============================================================================
namespace BlueprintUnlockHSK
{
    // ---------------- ① 冰柜 ----------------

    public class CompProperties_IceChestHSK : CompProperties
    {
        // 基准消耗速率(每天消耗的冰块数), 与 def 里 Refuelable.fuelConsumptionRate 保持一致
        public float baseConsumptionRate = 0.6f;
        // 室温每高于 baseMeltTemp 一度, 每天额外融化的冰块数
        public float meltPerDegreePerDay = 0.35f;
        // 低于该温度只按基准速率缓慢消耗
        public float baseMeltTemp = 0f;

        public CompProperties_IceChestHSK()
        {
            compClass = typeof(CompIceChestHSK);
        }
    }

    /// <summary>
    /// 只做一件 ASF 没做的事: 让冰块的消耗速率随室温升高。
    /// 制冷本体与"无冰即停"都由 AdaptiveStorage.Extension 的 temperature
    /// + requiresFuel 原生处理, 这里不复刻。
    /// </summary>
    public class CompIceChestHSK : ThingComp
    {
        private CompRefuelable fuel;

        public CompProperties_IceChestHSK Props
        {
            get { return (CompProperties_IceChestHSK)props; }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            fuel = parent.GetComp<CompRefuelable>();
        }

        public override void CompTickRare()
        {
            // 无冰/未生成时早退: 空转开销接近零
            if (fuel == null || !parent.Spawned) return;

            float over = parent.AmbientTemperature - Props.baseMeltTemp;
            float rate = Props.baseConsumptionRate +
                         (over > 0f ? Props.meltPerDegreePerDay * over : 0f);

            // 直接改写 Refuelable 的消耗速率: 天热化得快, 天冷几乎不化
            if (fuel.Props != null)
                fuel.Props.fuelConsumptionRate = rate;
        }

        public override string CompInspectStringExtra()
        {
            if (fuel == null) return null;
            string s = string.Format("冰块余量: {0:F0}%", fuel.FuelPercentOfMax * 100f);
            if (!parent.Spawned) return s;

            float over = parent.AmbientTemperature - Props.baseMeltTemp;
            if (over > 0f)
                s += string.Format(" (室温高于{0:F0}°C, 融化加快)", Props.baseMeltTemp);
            else
                s += " (天寒, 几乎不化)";
            return s;
        }
    }

    // ---------------- ② 毛皮床治愈低温症 ----------------

    public class CompProperties_FurBedCureHSK : CompProperties
    {
        // 要治愈的 hediff(默认低温症; MO 原版即治愈 hypothermia)
        public List<string> cureHediffs = new List<string> { "Hypothermia" };
        // 每次治愈的心跳间隔(CompTickRare 次数, 250 tick/次)
        public int intervalTicks = 4;   // ≈1000 tick 检查一次
        public string cureMessageKey;   // 可空: 不弹提示

        public CompProperties_FurBedCureHSK()
        {
            compClass = typeof(CompFurBedCureHSK);
        }

        // 注: 不能在这里 override ConfigErrors() —— 那是 Def / DefModExtension 的方法,
        //     CompProperties 基类并没有它(强行 override 会报 CS0115)。
        //     配置校验改为运行时宽容处理: cureHediffs 为空/null 时 CompTickRare 直接早退,
        //     毛皮床退化成普通床, 不会抛错。
    }

    public class CompFurBedCureHSK : ThingComp
    {
        private int tickCounter;

        public CompProperties_FurBedCureHSK Props
        {
            get { return (CompProperties_FurBedCureHSK)props; }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref tickCounter, "furBedCureCounter", 0);
        }

        public override void CompTickRare()
        {
            // 1.6 起 ThingComp 没有可重写的 ExposeData(), 持久化走 PostExposeData
            if (++tickCounter < Props.intervalTicks) return;
            tickCounter = 0;

            // 配置宽容: 未配置治愈目标则整个 comp 不生效, 毛皮床退化成普通床
            if (Props.cureHediffs == null || Props.cureHediffs.Count == 0) return;

            Building_Bed bed = parent as Building_Bed;
            if (bed == null) return;

            // 无人睡则早退(1.6 的 CurOccupants 是 IEnumerable<Pawn>, 只能 foreach)
            IEnumerable<Pawn> sleepers = bed.CurOccupants;
            if (sleepers == null) return;

            foreach (Pawn p in sleepers)
            {
                if (p == null || p.health == null || p.health.hediffSet == null) continue;

                foreach (string defName in Props.cureHediffs)
                {
                    Hediff h = p.health.hediffSet.GetFirstHediffOfDef(
                        DefDatabase<HediffDef>.GetNamedSilentFail(defName), false);
                    if (h == null) continue;

                    p.health.RemoveHediff(h);
                    if (!Props.cureMessageKey.NullOrEmpty())
                    {
                        Messages.Message(Props.cureMessageKey.Translate(p.Named("PAWN")),
                            p, MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

// =====================================================================
//  石化工业线基础设施 (并入 HSK工业科研大修) — 断水停机
//  namespace 复用现有 BlueprintUnlockHSK, 与同 Assemblies 内其它补丁共存。
//  本文件为 C#5 兼容写法 (无 $"" 插值 / 无 ?. / 无表达式体成员 / 无内联 out var)。
//  1.6 API 对齐: ThingWithComps.AllComps / GetComp<T>() ; CompRefuelable.Fuel ;
//                 CompFlickable.SwitchIsOn ; Building_WorkTable.BillStack.AnyShouldDoNow ;
//                 CompHeatPusher.enabled (powered 走 CompPowerTrader) 。
// =====================================================================

namespace BlueprintUnlockHSK
{
    // ---------- 静态反射封装: 安全读取 DubsBadHygiene.CompWaterStorage ----------
    public static class DBHWater
    {
        private static bool _resolved;
        private static Type _waterCompType;
        private static Type _pipeType;
        private static FieldInfo _fWaterStorage;
        private static PropertyInfo _pProps;
        private static FieldInfo _fCapOnProps;
        private static FieldInfo _fPipeNetRef;
        private static FieldInfo _fDrawOverlay;

        public static bool Available
        {
            get
            {
                Resolve();
                return _waterCompType != null && _fWaterStorage != null;
            }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            _waterCompType = AccessTools.TypeByName("DubsBadHygiene.CompWaterStorage");
            if (_waterCompType != null)
            {
                _fWaterStorage = AccessTools.Field(_waterCompType, "WaterStorage");
                _pProps = AccessTools.Property(_waterCompType, "Props");
                _fDrawOverlay = AccessTools.Field(_waterCompType, "DrawOverlay");
                if (_pProps != null)
                {
                    Type propsType = _pProps.PropertyType;
                    _fCapOnProps = AccessTools.Field(propsType, "WaterStorageCap");
                }
            }
            _pipeType = AccessTools.TypeByName("DubsBadHygiene.CompPipe");
            _fPipeNetRef = (_pipeType != null) ? AccessTools.Field(_pipeType, "pipeNetRef") : null;
        }

        private static ThingComp GetWaterComp(Thing t)
        {
            Resolve();
            if (_waterCompType == null) return null;
            ThingWithComps twc = t as ThingWithComps;
            if (twc == null) return null;
            List<ThingComp> comps = twc.AllComps;
            if (comps == null) return null;
            for (int i = 0; i < comps.Count; i++)
                if (comps[i] != null && _waterCompType.IsInstanceOfType(comps[i])) return comps[i];
            return null;
        }

        // 是否已接入水管管网(pipeNetRef 由 DBH 在管网重建时写入; DBH 缺失/无水管 comp 时返回 true 不拦)
        public static bool PipeConnected(Thing t)
        {
            Resolve();
            if (_pipeType == null || _fPipeNetRef == null) return true;
            ThingWithComps twc = t as ThingWithComps;
            if (twc == null) return true;
            List<ThingComp> comps = twc.AllComps;
            if (comps == null) return true;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] != null && _pipeType.IsInstanceOfType(comps[i]))
                    return _fPipeNetRef.GetValue(comps[i]) != null;
            }
            return true;
        }

        // 选中时触发 DBH 自带水位条(CompWaterStorage.PostDraw 画完即复位, 每帧重置)
        public static void PulseWaterBar(Thing t)
        {
            Resolve();
            if (_fDrawOverlay == null) return;
            ThingComp wc = GetWaterComp(t);
            if (wc != null) _fDrawOverlay.SetValue(wc, true);
        }

        // 返回 false 表示无 DBH 水箱 (视为不受水限制)
        public static bool TryGetLevel(Thing t, out float cur, out float cap)
        {
            cur = 0f;
            cap = 0f;
            ThingComp wc = GetWaterComp(t);
            if (wc == null) return false;
            cur = (float)_fWaterStorage.GetValue(wc);
            object props = (_pProps != null) ? _pProps.GetValue(wc, null) : null;
            if (props != null && _fCapOnProps != null) cap = (float)_fCapOnProps.GetValue(props);
            else cap = cur;
            return true;
        }

        // 从水箱抽走 amount, 返回实际抽到的量
        public static float Draw(Thing t, float amount)
        {
            ThingComp wc = GetWaterComp(t);
            if (wc == null) return 0f;
            float cur = (float)_fWaterStorage.GetValue(wc);
            float take = Mathf.Min(cur, amount);
            if (take < 0f) take = 0f;
            _fWaterStorage.SetValue(wc, cur - take);
            return take;
        }
    }

    // ---------- 组件属性 ----------
    public class CompProperties_NeedsWater : CompProperties
    {
        public float litersPerWorkSecond = 0.5f; // 满负荷工作时每秒耗水(L)
        public int checkInterval = 150;          // 抽水位检查间隔(tick), 低频
        public float resumeFraction = 0.1f;      // 停机后水位回升到容量该比例才恢复

        public CompProperties_NeedsWater()
        {
            compClass = typeof(CompNeedsWater);
        }
    }

    // ---------- 需要用水才能运转 ----------
    public class CompNeedsWater : ThingComp
    {
        private bool _autoSwitchedOff;   // 是否由本组件把锅炉/发电机自动关了
        private bool outOfWater;

        public bool OutOfWater
        {
            get { return outOfWater; }
        }

        public CompProperties_NeedsWater PropsW
        {
            get { return props as CompProperties_NeedsWater; }
        }

        public static CompNeedsWater Of(Thing t)
        {
            ThingWithComps twc = t as ThingWithComps;
            if (twc == null) return null;
            return twc.GetComp<CompNeedsWater>();
        }

        public static bool IsOutOfWater(Thing t)
        {
            CompNeedsWater nw = Of(t);
            return (nw != null) && nw.outOfWater;
        }

        // 装置当前是否"在干活"(决定是否耗水)
        private bool WorkingNow()
        {
            Building b = parent as Building;
            if (b == null) return false;
            CompPowerTrader pt = b.GetComp<CompPowerTrader>();
            if (pt != null && !pt.PowerOn) return false;
            CompFlickable fl = b.GetComp<CompFlickable>();
            if (fl != null && !fl.SwitchIsOn) return false;
            CompBreakdownable bd = b.GetComp<CompBreakdownable>();
            if (bd != null && bd.BrokenDown) return false;

            // 无燃料时不耗水(SK.CompFueled 继承 CompRefuelable, 燃料为 0 也可能 PowerOn)
            CompRefuelable rf = b.GetComp<CompRefuelable>();
            if (rf != null && !rf.HasFuel) return false;

            Building_WorkTable table = b as Building_WorkTable;
            if (table != null)
                return table.BillStack != null && table.BillStack.AnyShouldDoNow;

            // 发电机: CompPowerTrader + CompPowerPlant 通电即视为在烧
            CompPowerPlant plant = b.GetComp<CompPowerPlant>();
            if (plant != null && pt != null && pt.PowerOn) return true;

            // 锅炉: CompHeatPusher.enabled + 通电
            CompHeatPusher heat = b.GetComp<CompHeatPusher>();
            if (heat != null && heat.enabled && pt != null && pt.PowerOn) return true;

            // 兜底: 通电即算运行
            return pt != null && pt.PowerOn;
        }

        public override void CompTick()
        {
            base.CompTick();
            CompProperties_NeedsWater p = PropsW;
            if (p == null) return;
            if (!parent.IsHashIntervalTick(p.checkInterval)) return;

            float cur;
            float cap;
            bool hasTank = DBHWater.TryGetLevel(parent, out cur, out cap);
            if (!hasTank)
            {
                outOfWater = false;
                return;
            }

            if (!outOfWater)
            {
                if (WorkingNow())
                {
                    float seconds = p.checkInterval / 60f;
                    DBHWater.Draw(parent, p.litersPerWorkSecond * seconds);
                    if (cur <= 0.01f)
                    {
                        outOfWater = true;
                        HandleOutOfWaterStarted();
                    }
                }
            }
            else
            {
                if (cap > 0f && cur >= cap * p.resumeFraction)
                {
                    outOfWater = false;
                    HandleRecovered();
                }
            }
        }

        private void HandleOutOfWaterStarted()
        {
            // 工作台由 Harmony 前缀拦截发单, 无需关开关; 锅炉/发电机自动 Switch off。
            Building_WorkTable table = parent as Building_WorkTable;
            if (table != null) return;
            CompFlickable fl = parent.GetComp<CompFlickable>();
            if (fl != null && fl.SwitchIsOn)
            {
                fl.DoFlick();
                _autoSwitchedOff = true;
            }
        }

        private void HandleRecovered()
        {
            if (_autoSwitchedOff)
            {
                CompFlickable fl = parent.GetComp<CompFlickable>();
                if (fl != null && !fl.SwitchIsOn) fl.DoFlick();
                _autoSwitchedOff = false;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref outOfWater, "outOfWater", false);
            Scribe_Values.Look(ref _autoSwitchedOff, "autoSwitchedOff", false);
        }

        public override string CompInspectStringExtra()
        {
            if (!DBHWater.Available) return null;
            float cur;
            float cap;
            if (!DBHWater.TryGetLevel(parent, out cur, out cap)) return null;
            string text = "用水量: " + cur.ToString("F0") + " / " + cap.ToString("F0") + " L";
            if (!DBHWater.PipeConnected(parent)) text = text + "\n未连接水管: 需水井/水泵与铜管管网供水";
            if (outOfWater) text = text + "\n缺水, 已停止运行";
            return text;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (Find.Selector != null && Find.Selector.IsSelected(parent))
                DBHWater.PulseWaterBar(parent);
        }
    }

    // ---------- Harmony: 工作台缺水时不再派发加工任务 ----------
    [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing")]
    public static class Patch_NeedsWaterBlocksWork
    {
        static bool Prefix(Thing thing, ref Job __result)
        {
            if (CompNeedsWater.IsOutOfWater(thing))
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}

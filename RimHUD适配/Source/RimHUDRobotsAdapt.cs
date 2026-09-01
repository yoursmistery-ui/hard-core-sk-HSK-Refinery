// RimHUD 适配 · Misc Robots(robots.misc)独立本地 mod —— defClass 静态方法
//
// 背景: RimHUD 通过 ExternalWidgetDef 子类(CustomBarDef / CustomValueDef 等)支持第三方
// mod 自定义 HUD 部件: def 的 defClass 指向本程序集内静态类,RimHUD 在 def 加载期
// (ResolveReferences → InitializeV1 → GetHandler)用 Harmony AccessTools.Method 反射解析,
// 方法名固定为 GetParameters(参数 typeof(Pawn)),运行时以 FastInvokeHandler 调用。
// 方法签名必须与 RimHUD 期望精确一致: 返回类型精确匹配 System.ValueTuple 泛型
// (否则抛 "has unexpected return type"),因此每个 def 必须用独立的 defClass 类
// (同名 GetParameters 不能共用一个类)。
//
// 适配内容(Misc Robots 机器人):
//   1) 电量条(RK_HR_Battery, CustomBarDef): 机器人的电量实际就是 Pawn_NeedsTracker.rest
//      (反编译 AIRobot.X2_PawnColumnWorker_Charge.DoCell 确认: 充电列直接读 rest.CurLevelPercentage),
//      原版 HUD 只把 rest 显示成"休息"条,本部件以"电量"标签 + 充电中提示展示;
//   2) 状态(RK_HR_Status, CustomValueDef): 充电中 / 睡眠模式 / 工作中,反射读取
//      AIRobot.X2_AIRobot 实例字段(rechargeStation / isSleepModeActive),不编译期引用 AIRobot.dll。
//
// 非机器人 pawn: 电量返回 fill=-1(BarWidget 不绘制),状态返回 null(ValueWidget 不绘制),
// 与 RimHUD 内置部件对不适用 pawn 的"空行"行为一致。
//
// 编译(系统 csc, C#5, 见 build_dll.cmd):
//   csc /nologo /target:library /r:Assembly-CSharp.dll /r:UnityEngine.CoreModule.dll /r:netstandard.dll
//       /out:..\Assemblies\RimHUDRobotsAdapt.dll RimHUDRobotsAdapt.cs
// 注: 本程序集不引用 RimHUD.dll(只被 RimHUD 反射调用)与 AIRobot.dll(类型判定/字段访问全反射),
//     因此加载顺序无关,未装 RimHUD 时也零副作用。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimHUDRobotsAdapt
{
    /// <summary>电量条(RK_HR_Battery)defClass: (label, value, fill, thresholds, tooltip, onHover, onClick)。</summary>
    public static class BatteryWidget
    {
        public static ValueTuple<string, string, float, float[], Func<string>, Action, Action> GetParameters(Pawn pawn)
        {
            if (!Robots.IsRobot(pawn) || pawn.needs == null || pawn.needs.rest == null)
            {
                return new ValueTuple<string, string, float, float[], Func<string>, Action, Action>(null, null, -1f, null, null, null, null);
            }

            var rest = pawn.needs.rest;
            float percent = rest.CurLevelPercentage;

            string tooltip = "RKHR_BatteryLabel".Translate() + ": " + percent.ToStringPercent();
            if (Robots.IsCharging(pawn)) { tooltip += "\n" + "RKHR_StatusCharging".Translate(); }
            string tooltipText = tooltip;

            return new ValueTuple<string, string, float, float[], Func<string>, Action, Action>(
                "RKHR_BatteryLabel".Translate(),
                percent.ToStringPercent(),
                percent,
                null,
                () => tooltipText,
                null,
                null);
        }
    }

    /// <summary>状态(RK_HR_Status)defClass: (label, value, tooltip, onHover, onClick)。</summary>
    public static class StatusWidget
    {
        public static ValueTuple<string, string, Func<string>, Action, Action> GetParameters(Pawn pawn)
        {
            if (!Robots.IsRobot(pawn))
            {
                return new ValueTuple<string, string, Func<string>, Action, Action>(null, null, null, null, null);
            }

            string value;
            if (Robots.IsCharging(pawn)) { value = "RKHR_StatusCharging".Translate(); }
            else if (Robots.IsSleepMode(pawn)) { value = "RKHR_StatusSleeping".Translate(); }
            else { value = "RKHR_StatusWorking".Translate(); }
            string valueText = value;

            return new ValueTuple<string, string, Func<string>, Action, Action>(
                "RKHR_StatusLabel".Translate(),
                valueText,
                () => valueText,
                null,
                null);
        }
    }

    /// <summary>专精(RK_HR_VSE_Expertise, Vanilla Skills Expanded)defClass: (label, value, tooltip, onHover, onClick)。
    /// 反射 VSE.dll(VSE.ExpertiseTrackers.Expertise(pawn) → ExpertiseTracker.AllExpertise →
    /// List&lt;ExpertiseRecord&gt;,每条含 def(ExpertiseDef)/Level/LevelDescriptor),不编译期引用 VSE.dll;
    /// 专精名称走 Verse.Def.LabelCap(吃 VSE 自己的 DefInjected 汉化)。非 VSE pawn / 无专精时返回 null(不绘制)。</summary>
    public static class VSEExpertiseWidget
    {
        private const string VseAssemblyName = "VSE";
        private const string TrackersTypeName = "VSE.ExpertiseTrackers";
        private const string TrackerTypeName = "VSE.ExpertiseTracker";
        private const string RecordTypeName = "VSE.ExpertiseRecord";

        private static Type _trackersType;
        private static Type _trackerType;
        private static Type _recordType;
        private static MethodInfo _getTrackerMethod;
        private static PropertyInfo _allExpertiseProperty;
        private static FieldInfo _recordDefField;
        private static PropertyInfo _recordLevelProperty;
        private static PropertyInfo _recordDescriptorProperty;

        private static bool EnsureLoaded()
        {
            if (_trackersType != null) { return true; }
            try
            {
                Assembly vse = null;
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == VseAssemblyName) { vse = assembly; break; }
                }
                if (vse == null) { return false; }

                _trackersType = vse.GetType(TrackersTypeName);
                _trackerType = vse.GetType(TrackerTypeName);
                _recordType = vse.GetType(RecordTypeName);
                if (_trackersType == null || _trackerType == null || _recordType == null) { return false; }

                _getTrackerMethod = _trackersType.GetMethod("Expertise", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Pawn) }, null);
                if (_getTrackerMethod == null || !_getTrackerMethod.IsStatic) { return false; }

                _allExpertiseProperty = _trackerType.GetProperty("AllExpertise", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _recordDefField = _recordType.GetField("def", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _recordLevelProperty = _recordType.GetProperty("Level", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _recordDescriptorProperty = _recordType.GetProperty("LevelDescriptor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                return _allExpertiseProperty != null && _recordDefField != null && _recordLevelProperty != null && _recordDescriptorProperty != null;
            }
            catch (Exception)
            {
                _trackersType = null;
                return false;
            }
        }

        public static ValueTuple<string, string, Func<string>, Action, Action> GetParameters(Pawn pawn)
        {
            if (pawn == null || pawn.skills == null || !EnsureLoaded())
            {
                return new ValueTuple<string, string, Func<string>, Action, Action>(null, null, null, null, null);
            }

            object tracker;
            try { tracker = _getTrackerMethod.Invoke(null, new object[] { pawn }); }
            catch (Exception) { return new ValueTuple<string, string, Func<string>, Action, Action>(null, null, null, null, null); }
            if (tracker == null) { return new ValueTuple<string, string, Func<string>, Action, Action>(null, null, null, null, null); }

            IList list = _allExpertiseProperty.GetValue(tracker, null) as IList;
            if (list == null || list.Count == 0) { return new ValueTuple<string, string, Func<string>, Action, Action>(null, null, null, null, null); }

            var names = new List<string>();
            var lines = new List<string>();
            foreach (object record in list)
            {
                if (record == null) { continue; }
                string name = null;
                try
                {
                    object def = _recordDefField.GetValue(record);
                    if (def is Def) { name = ((Def)def).LabelCap; }
                }
                catch (Exception) { }
                if (string.IsNullOrEmpty(name)) { name = "?"; }

                string levelDescriptor = null;
                int level = 0;
                try
                {
                    object lv = _recordLevelProperty.GetValue(record, null);
                    if (lv is int) { level = (int)lv; }
                    object desc = _recordDescriptorProperty.GetValue(record, null);
                    if (desc is string) { levelDescriptor = (string)desc; }
                }
                catch (Exception) { }

                names.Add(name);
                if (!string.IsNullOrEmpty(levelDescriptor)) { lines.Add(name + " — " + levelDescriptor); }
                else if (level > 0) { lines.Add(name + " (" + level + ")"); }
                else { lines.Add(name); }
            }
            if (names.Count == 0) { return new ValueTuple<string, string, Func<string>, Action, Action>(null, null, null, null, null); }

            string value = names.Count == 1 ? names[0] : "RKHR_VSE_ExpertiseCount".Translate(names.Count).ToString();
            string tooltipText = string.Join("\n", lines.ToArray());

            return new ValueTuple<string, string, Func<string>, Action, Action>(
                "RKHR_VSE_ExpertiseLabel".Translate(),
                value,
                () => tooltipText,
                null,
                null);
        }
    }

    /// <summary>护甲厚度(RK_HR_Armor, Combat Extended 兼容)defClass: CustomWidgetDef 自绘部件。
    /// 遍历 pawn 穿着护甲,取每件 ArmorRating_Sharp/Blunt/Heat(原版 1.6 StatDefOf;CE 下由
    /// CombatExtended 的 StatWorker 计算成护甲厚度,即 CE 角色卡护甲面板同源数值),求和三值;
    /// 绘制参考 RPG Style Inventory 装备栏的「图标 + 数值标注」: 锐/钝/热 三枚小图标各跟数值,
    /// tooltip 逐件列出「名称: 锐 x / 钝 x / 热 x」(TooltipHandler,原版 API)。
    /// 不编译期引用 CE.dll;CE / 非 CE 通用(非 CE 显示原版护甲评级)。
    /// GetMaxHeight 引用 RimHUD Active.Pawn 判定有无护甲: 无护甲返回 0(行折叠,不留白行)。
    /// 图标 Textures/RimHUD/RKHR_ArmorSharp/Blunt/Heat.png(取自 RPG Style Inventory 的
    /// Sandy_Armor*_Icon,复制到本 mod 自包含,不依赖 RPG 是否启用)。</summary>
    public static class CEArmorWidget
    {
        private const float IconSize = 20f;
        private const float IconTextGap = 3f;
        private const float GroupGap = 14f;

        private static Texture2D _sharpIcon;
        private static Texture2D _bluntIcon;
        private static Texture2D _heatIcon;

        private static Texture2D SharpIcon
        {
            get
            {
                if (_sharpIcon == null) { _sharpIcon = ContentFinder<Texture2D>.Get("RimHUD/RKHR_ArmorSharp", false); }
                return _sharpIcon;
            }
        }

        private static Texture2D BluntIcon
        {
            get
            {
                if (_bluntIcon == null) { _bluntIcon = ContentFinder<Texture2D>.Get("RimHUD/RKHR_ArmorBlunt", false); }
                return _bluntIcon;
            }
        }

        private static Texture2D HeatIcon
        {
            get
            {
                if (_heatIcon == null) { _heatIcon = ContentFinder<Texture2D>.Get("RimHUD/RKHR_ArmorHeat", false); }
                return _heatIcon;
            }
        }

        private static bool HasArmor(Pawn pawn)
        {
            return pawn != null && pawn.apparel != null && pawn.apparel.WornApparel != null && pawn.apparel.WornApparel.Count > 0;
        }

        public static float GetMaxHeight()
        {
            try
            {
                if (HasArmor(RimHUD.Interface.Hud.Models.Active.Pawn)) { return Text.LineHeight; }
            }
            catch (Exception) { }
            return 0f;
        }

        public static bool OnDraw(Pawn pawn, Rect rect)
        {
            if (!HasArmor(pawn)) { return false; }

            int sharp = 0;
            int blunt = 0;
            int heat = 0;
            var lines = new List<string>();

            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel == null || apparel.def == null) { continue; }

                int s = (int)Math.Round(apparel.GetStatValue(StatDefOf.ArmorRating_Sharp));
                int b = (int)Math.Round(apparel.GetStatValue(StatDefOf.ArmorRating_Blunt));
                int h = (int)Math.Round(apparel.GetStatValue(StatDefOf.ArmorRating_Heat));

                sharp += s;
                blunt += b;
                heat += h;

                lines.Add(apparel.LabelCap + ": 锐 " + s + " / 钝 " + b + " / 热 " + h);
            }

            if (lines.Count == 0) { return false; }

            float y = rect.y + (rect.height - IconSize) / 2f;
            float x = rect.x;

            DrawGroup(ref x, y, SharpIcon, sharp);
            x += GroupGap;
            DrawGroup(ref x, y, BluntIcon, blunt);
            x += GroupGap;
            DrawGroup(ref x, y, HeatIcon, heat);

            TooltipHandler.TipRegion(rect, "RKHR_ArmorTooltipTitle".Translate() + "\n" + string.Join("\n", lines.ToArray()));
            return true;
        }

        private static void DrawGroup(ref float x, float y, Texture2D icon, int value)
        {
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(x, y, IconSize, IconSize), icon);
                x += IconSize + IconTextGap;
            }

            string text = value.ToString();
            float width = Text.CalcSize(text).x + 2f;
            Widgets.Label(new Rect(x, y, width, IconSize), text);
            x += width + IconTextGap;
        }
    }

    /// <summary>Misc Robots 机器人类型判定与字段反射(不编译期引用 AIRobot.dll)。
    /// 类型用 AppDomain 程序集扫描解析(不用 GenTypes,避免依赖 Verse.Log/UnityEngine,
    /// 离线可测且加载顺序无关);字段名/类型来自对 AIRobot.dll 的 IL 反编译。</summary>
    internal static class Robots
    {
        private const string RobotAssemblyName = "AIRobot";
        private const string RobotTypeName = "AIRobot.X2_AIRobot";
        private const string RechargeStationFieldName = "rechargeStation";
        private const string SleepModeFieldName = "isSleepModeActive";

        private static Type _robotType;
        private static FieldInfo _rechargeStationField;
        private static FieldInfo _sleepModeField;

        private static Type RobotType
        {
            get
            {
                if (_robotType == null)
                {
                    try
                    {
                        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (assembly.GetName().Name == RobotAssemblyName)
                            {
                                _robotType = assembly.GetType(RobotTypeName);
                                break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        _robotType = null;
                    }
                }
                return _robotType;
            }
        }

        private static FieldInfo RechargeStationField
        {
            get
            {
                if (_rechargeStationField == null && RobotType != null)
                {
                    _rechargeStationField = RobotType.GetField(RechargeStationFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                return _rechargeStationField;
            }
        }

        private static FieldInfo SleepModeField
        {
            get
            {
                if (_sleepModeField == null && RobotType != null)
                {
                    _sleepModeField = RobotType.GetField(SleepModeFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                return _sleepModeField;
            }
        }

        public static bool IsRobot(Pawn pawn)
        {
            return RobotType != null && pawn != null && RobotType.IsInstanceOfType(pawn);
        }

        public static bool IsCharging(Pawn pawn)
        {
            if (RechargeStationField == null) { return false; }
            return RechargeStationField.GetValue(pawn) != null;
        }

        public static bool IsSleepMode(Pawn pawn)
        {
            if (SleepModeField == null) { return false; }
            object value = SleepModeField.GetValue(pawn);
            return value is bool && (bool)value;
        }
    }
}

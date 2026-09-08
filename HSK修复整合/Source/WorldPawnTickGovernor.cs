// WorldPawnTickGovernor.cs — 世界 pawn 冻结/降频调速器
//
// 定位过程(2026-09-05 [WPP] + 存档核对): WorldPawnsTick 的全部开销来自**别的派系在途商队的成员**
// (BOTR 的 FactionTradeCaravan,商队护卫+驮兽)。这些 pawn 每 tick 跑 Pawn.Tick,每 15 tick 再跑整条
// Pawn.TickInterval(needs 从商队库存扣饭、心情、伤病、衰老、records…),而玩家对它们的全部感知就是
// "在世界地图上走"和"能不能被袭击"。BOTR 战争结算本身已是骰子模型且 VisibleRaidForce 不引用 pawn,
// 冻结不影响战争。
//
// ⚠ 热路径铁律(2026-09-05 OOM 实录): 本文件补丁挂在 Pawn.Tick / TickInterval / NeedsTracker.TickInterval /
//   MindState.TickInterval 上,全游戏每个 pawn 每 tick 都过。绝不允许调用 Thing.Spawned / Destroyed / Map /
//   LabelShortCap / ThingID —— `Thing.Spawned` 失败分支会先拼 $"Thing {ThingID} ..." 再 ErrorOnce,
//   地图索引重建窗口里就是每 pawn 每次一个新字符串,直接压穿 Mono 堆。
//   判定只用字段级读取: `pawn.holdingOwner`(地图上的 pawn 恒为 null)→ `Owner as Caravan`。
//
// 配置: 选项 → Mod 设置 → HSK修复整合 - 世界pawn降频
// 编译: 并入 HSKFixPack.dll(系统 csc,C#5)。手动 harmony.Patch(理由见 BotrDecisionTickThrottle.cs)。
using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace WorldPawnTickGovernor
{
    public class GovSettings : ModSettings
    {
        public int npcCaravanMode = 2;          // 0关 1只砍需求心情 2整只冻结
        public bool intervalEnabled = false;
        public int intervalTicks = 30;
        public bool intervalAffectsPlayerCaravan = false;
        public bool equipVerbEnabled = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref npcCaravanMode, "wptgNpcCaravanMode", 2);
            Scribe_Values.Look(ref intervalEnabled, "wptgIntervalEnabled", false);
            Scribe_Values.Look(ref intervalTicks, "wptgIntervalTicks", 30);
            Scribe_Values.Look(ref intervalAffectsPlayerCaravan, "wptgIntervalAffectsPlayerCaravan", false);
            Scribe_Values.Look(ref equipVerbEnabled, "wptgEquipVerbEnabled", false);
            base.ExposeData();
        }
    }

    public class GovMod : Mod
    {
        public static GovSettings settings;

        public GovMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<GovSettings>();
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect.ContractedBy(12f));
            list.Label("C) 非玩家派系在途商队成员(按商队归属判定,自家商队与押运俘虏不受影响)");
            int before = settings.npcCaravanMode;
            bool m2 = before == 2;
            bool m1 = before == 1;
            bool m0 = before == 0;
            list.CheckboxLabeled("整只冻结: 不再 Tick(默认,最省)", ref m2,
                "跳过 Pawn.Tick 与 Pawn.TickInterval。落地被交战/贸易/事件取用时,原版地图 tick 照常接管。");
            list.CheckboxLabeled("只砍需求与心情: 保留伤病/装备/基因/年龄推进", ref m1,
                "只跳过 NeedsTracker.TickInterval(不再从商队库存扣饭)与 MindState.TickInterval;伤病仍会恶化、仍会在路上死。");
            list.CheckboxLabeled("关闭(完全原版行为)", ref m0, null);
            if (m2 && before != 2) { settings.npcCaravanMode = 2; }
            else if (m1 && before != 1) { settings.npcCaravanMode = 1; }
            else if (m0 && before != 0) { settings.npcCaravanMode = 0; }
            list.Label("  副作用: 商队库存食物不再减少、心情不推进" + (settings.npcCaravanMode == 2 ? "; 整只冻结还会定格伤病与年龄" : ""));
            list.Gap();
            list.Label("A) 其余世界 pawn 的 TickInterval 降频(原版粒度 15 tick)");
            list.CheckboxLabeled("启用降频", ref settings.intervalEnabled,
                "未生成到地图上的 pawn 攒够设定 tick 才跑一次 TickInterval,按 delta 结算,不会少算需求/时间。");
            float f = settings.intervalTicks;
            settings.intervalTicks = (int)list.SliderLabeled("粒度: " + settings.intervalTicks + " tick", f, 15f, 90f, 1f,
                tooltip: "15 = 与原版一致。越大越省,但需求/心情/伤病推进越跳跃。建议先试 30~45。");
            if (settings.intervalTicks < 15) settings.intervalTicks = 15;
            if (settings.intervalTicks > 240) settings.intervalTicks = 240;
            list.CheckboxLabeled("  也包括玩家自己的商队", ref settings.intervalAffectsPlayerCaravan,
                "不勾则只影响闲散世界 NPC;玩家商队在途时的开销要勾上才降。");
            list.Gap();
            list.Label("B) 跳过在途 pawn 的武器 verb 心跳(实验性)");
            list.CheckboxLabeled("启用", ref settings.equipVerbEnabled,
                "未生成 pawn 仍逐 tick tick 每件武器的 VerbsTick(基本只减冷却)。跳过可省这部分,代价是世界层武器冷却不再衰减。");
            list.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "HSK修复整合 - 世界pawn降频";
        }
    }

    [StaticConstructorOnStartup]
    public static class GovInit
    {
        static readonly Dictionary<int, int> pending = new Dictionary<int, int>();
        static int lastTicks = -1;
        static bool announced;

        static GovInit()
        {
            try
            {
                Harmony h = new Harmony("local.ratkin.hskfix.worldpawntickgovernor");
                h.Patch(AccessTools.Method(typeof(Pawn), "Tick"),
                    new HarmonyMethod(typeof(GovInit), "Tick_Pre"), null, null, null);
                h.Patch(AccessTools.Method(typeof(Pawn), "TickInterval", new Type[] { typeof(int) }),
                    new HarmonyMethod(typeof(GovInit), "Int_Pre"), null, null, null);
                h.Patch(AccessTools.Method(typeof(Pawn_NeedsTracker), "NeedsTrackerTickInterval", new Type[] { typeof(int) }),
                    new HarmonyMethod(typeof(GovInit), "Needs_Pre"), null, null, null);
                h.Patch(AccessTools.Method(typeof(Pawn_MindState), "MindStateTickInterval", new Type[] { typeof(int) }),
                    new HarmonyMethod(typeof(GovInit), "Mind_Pre"), null, null, null);
                h.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), "EquipmentTrackerTick"),
                    new HarmonyMethod(typeof(GovInit), "Equip_Pre"), null, null, null);
                GovSettings s = GovMod.settings;
            }
            catch (Exception e)
            {
                Log.Error("[WPTG] 挂载失败: " + e);
            }
        }

        // 只允许字段级读取: 地图上的 pawn 的 holdingOwner 恒为 null,一次判空即短路
        static Caravan Carrier(Pawn p)
        {
            if (p == null) return null;
            ThingOwner owner = p.holdingOwner;
            if (owner == null) return null;
            return owner.Owner as Caravan;
        }

        // 0 = 正常, 1 = 只砍需求/心情, 2 = 整只冻结
        static int ModeOf(Pawn p)
        {
            GovSettings s = GovMod.settings;
            if (s == null || s.npcCaravanMode <= 0) return 0;
            Caravan c = Carrier(p);
            if (c == null || c.IsPlayerControlled) return 0;         // 非商队 / 自家商队(含押运俘虏)不动
            if (!announced)
            {
                announced = true;
            }
            return s.npcCaravanMode;
        }

        static bool Tick_Pre(Pawn __instance)
        {
            GovSettings s = GovMod.settings;
            if (s == null || s.npcCaravanMode == 0) return true;      // 关闭时零成本
            return ModeOf(__instance) != 2;
        }

        static bool Needs_Pre(Pawn __instance)
        {
            GovSettings s = GovMod.settings;
            if (s == null || s.npcCaravanMode == 0) return true;
            return ModeOf(__instance) == 0;
        }

        static bool Mind_Pre(Pawn __instance)
        {
            GovSettings s = GovMod.settings;
            if (s == null || s.npcCaravanMode == 0) return true;
            return ModeOf(__instance) == 0;
        }

        static bool Int_Pre(Pawn __instance, ref int delta)
        {
            GovSettings s = GovMod.settings;
            if (s == null) return true;
            if (s.npcCaravanMode == 2 && ModeOf(__instance) == 2)
            {
                int fid = __instance.thingIDNumber;
                if (pending.ContainsKey(fid)) pending.Remove(fid);
                return false;
            }
            int id = __instance.thingIDNumber;
            int pend;
            bool has = pending.TryGetValue(id, out pend);
            int now = Find.TickManager.TicksGame;
            if (now < lastTicks && pending.Count > 0) pending.Clear();     // 新档/读档 → 清账
            lastTicks = now;
            bool intervalOn = s.intervalEnabled && s.intervalTicks > 15;
            if (!intervalOn || !EligibleInterval(__instance, s))
            {
                if (has)
                {
                    pending.Remove(id);
                    delta += pend;                                         // 关掉后把攒下的时间补回,不丢 tick
                }
                return true;
            }
            pend += delta;
            if (pend < s.intervalTicks)
            {
                pending[id] = pend;
                return false;
            }
            if (has) pending.Remove(id);
            delta = pend;
            return true;
        }

        static bool EligibleInterval(Pawn p, GovSettings s)
        {
            // 只作用于世界 pawn(未生成)。不能用 Thing.Spawned(见文件头铁律),
            // 改用 WorldPawns 的 HashSet 查询 —— 只在 A 开启时才会走到。
            if (Find.WorldPawns == null || !Find.WorldPawns.Contains(p)) return false;
            Caravan c = Carrier(p);
            if (c == null) return true;                                    // 闲散世界 NPC
            return c.IsPlayerControlled ? s.intervalAffectsPlayerCaravan : true;
        }

        static bool Equip_Pre(Pawn_EquipmentTracker __instance)
        {
            GovSettings s = GovMod.settings;
            if (s == null || !s.equipVerbEnabled) return true;
            Caravan c = Carrier(__instance.pawn);
            return c == null;                                              // 在商队里(未生成) → 跳过武器 verb
        }
    }
}

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

// =====================================================================
//  炼油油泥累积槽 (v3, 2026-08-29) — 并入 HSK工业科研大修
//  挂在常减压蒸馏塔上: 装置每 rare tick 按 progressPerRareTick 累积"油泥进度",
//  满 maxProgress 记 1 份入内部槽; 内部槽满 maxStored 份 → 把塔上所有账单挂起(停机),
//  直到玩家用"清理油泥"按钮把内部槽倒成地上的炼油油泥物品(=原版毒废包属性)。
//
//  设计取舍:
//   - 不自动往地上吐物品(否则炼油塔周围会被油泥糊满, 且无法表达"槽满"这一约束);
//   - 停机用 Bill.suspended(1.6 原版"暂停"开关), 不碰电源/开关, 玩家能一眼看懂;
//   - 只挂起"本组件自己挂起过"的账单, 玩家手动暂停的账单不会被我们擅自恢复;
//   - §9 性能: 全部逻辑在 CompTickRare(250 ticks 一次), 前置短路(非玩家/未生成/无 wasteDef)直接 return。
//
//  C#5 兼容(无插值/无 ?./无表达式体成员)。
// =====================================================================

namespace BlueprintUnlockHSK
{
    public class CompProperties_WasteAccumulator : CompProperties
    {
        public ThingDef wasteDef;              // 产生的污染物 ThingDef
        public int stackSize = 1;              // 1 份 = 几个物品
        public float progressPerRareTick = 0.4f;
        public float maxProgress = 100f;
        public int maxStored = 25;             // 内部槽容量(份)

        public CompProperties_WasteAccumulator()
        {
            compClass = typeof(CompWasteAccumulator);
        }
    }

    public class CompWasteAccumulator : ThingComp
    {
        private float progress;
        private int stored;
        private readonly List<Bill> suspendedByUs = new List<Bill>();

        private CompProperties_WasteAccumulator P
        {
            get { return props as CompProperties_WasteAccumulator; }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            CompProperties_WasteAccumulator p = P;
            if (p == null || p.wasteDef == null) return;
            Building b = parent as Building;
            if (b == null || !b.Spawned || b.Faction != Faction.OfPlayer) return;

            bool full = stored >= p.maxStored;
            if (full)
            {
                SuspendAllBills(b);
                return;
            }
            // 槽位腾出来了: 只恢复本组件挂起的账单
            ResumeSuspendedBills();

            if (!WorkingNow(b)) return;
            progress += p.progressPerRareTick;
            while (progress >= p.maxProgress)
            {
                progress -= p.maxProgress;
                stored++;
                if (stored >= p.maxStored)
                {
                    SuspendAllBills(b);
                    Messages.Message("炼油油泥累积槽已满, 蒸馏塔已停机, 需要清理。", b, MessageTypeDefOf.CautionInput);
                    break;
                }
            }
        }

        // 装置当前是否在干活(有账单且未被挂起/断电/熄灭/故障)
        private static bool WorkingNow(Building b)
        {
            Building_WorkTable table = b as Building_WorkTable;
            if (table != null) return table.BillStack != null && table.BillStack.AnyShouldDoNow;

            CompPowerTrader pt = b.GetComp<CompPowerTrader>();
            if (pt != null && !pt.PowerOn) return false;
            CompFlickable fl = b.GetComp<CompFlickable>();
            if (fl != null && !fl.SwitchIsOn) return false;
            CompBreakdownable bd = b.GetComp<CompBreakdownable>();
            if (bd != null && bd.BrokenDown) return false;
            return true;
        }

        private void SuspendAllBills(Building b)
        {
            Building_WorkTable table = b as Building_WorkTable;
            if (table == null || table.BillStack == null) return;
            List<Bill> bills = table.BillStack.Bills;
            if (bills == null) return;
            for (int i = 0; i < bills.Count; i++)
            {
                Bill b2 = bills[i];
                if (b2 == null || b2.suspended) continue;   // 玩家已暂停的不接管
                b2.suspended = true;
                if (!suspendedByUs.Contains(b2)) suspendedByUs.Add(b2);
            }
        }

        private void ResumeSuspendedBills()
        {
            for (int i = suspendedByUs.Count - 1; i >= 0; i--)
            {
                Bill b2 = suspendedByUs[i];
                if (b2 != null) b2.suspended = false;
                suspendedByUs.RemoveAt(i);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref progress, "wasteProgress", 0f);
            Scribe_Values.Look(ref stored, "wasteStored", 0);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            // 与毒废包/精炼厂"破坏即泄污"语义对齐: 被击毁/熔毁时内部槽不许凭空消失。
            // 玩家主动拆除(Deconstruct/Refund/Vanish/FailConstruction/Cancel)视为已回收, 不洒。
            if (GasHazards.FriendlyRemoval(mode)) return;
            CompProperties_WasteAccumulator p = P;
            if (p == null || p.wasteDef == null || stored <= 0) return;
            Map m = (parent.MapHeld != null) ? parent.MapHeld : previousMap;
            if (m == null) return;
            SpawnWaste(m, parent.PositionHeld, p, stored * p.stackSize);
        }

        public override string CompInspectStringExtra()
        {
            CompProperties_WasteAccumulator p = P;
            if (p == null || p.wasteDef == null) return null;
            int pct = (p.maxProgress > 0f)
                ? Mathf.Clamp(Mathf.RoundToInt(progress / p.maxProgress * 100f), 0, 100)
                : 0;
            string tail = (stored >= p.maxStored) ? " —— 槽满, 装置已停机" : string.Empty;
            return "油泥累积槽: " + stored + " / " + p.maxStored + " 份 (下一份 " + pct + "%)" + tail;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
            CompProperties_WasteAccumulator p = P;
            if (p == null || p.wasteDef == null) yield break;
            Building b = parent as Building;
            if (b == null || !b.Spawned || b.Faction != Faction.OfPlayer) yield break;
            if (stored <= 0) yield break;

            Command_Action clear = new Command_Action();
            clear.defaultLabel = "清理油泥";
            clear.defaultDesc = "把累积槽里的 " + (stored * p.stackSize) + " 份炼油油泥倒到装置旁的空地上, 由小人搬运去原子化器/冻结或直接丢弃。";
            clear.icon = ResolveWasteIcon(p.wasteDef);
            clear.action = delegate { Clear(b, p); };
            yield return clear;
        }

        private void Clear(Building b, CompProperties_WasteAccumulator p)
        {
            int total = stored * p.stackSize;
            stored = 0;
            progress = 0f;
            ResumeSuspendedBills();
            if (total <= 0) return;
            int landed = SpawnWaste(b.Map, b.Position, p, total);
            if (landed >= total)
                Messages.Message("已清出 " + landed + " 份炼油油泥。", b, MessageTypeDefOf.NeutralEvent);
            else
            {
                // 落不进的量退回累积槽(不凭空消失), 让玩家先腾地方
                stored += (total - landed) / Mathf.Max(1, p.stackSize);
                Messages.Message("装置周围空地不足, 只清出 " + landed + " 份, 剩余仍在槽内。", b, MessageTypeDefOf.CautionInput);
            }
        }

        // 按 stackLimit 分堆落地, 返回真正落进世界的数量(落不进的不计)
        private static int SpawnWaste(Map map, IntVec3 root, CompProperties_WasteAccumulator p, int total)
        {
            if (map == null || p == null || p.wasteDef == null || total <= 0) return 0;
            int limit = Mathf.Max(1, p.wasteDef.stackLimit);
            int left = total, landed = 0;
            int tries = 0;
            while (left > 0 && tries < 40)
            {
                tries++;
                IntVec3 drop;
                if (!CellFinder.TryFindRandomCellNear(root, map, 4,
                    (IntVec3 x) => x.Standable(map) && x.GetEdifice(map) == null, out drop)) continue;
                Thing t = ThingMaker.MakeThing(p.wasteDef);
                t.stackCount = Mathf.Min(left, limit);
                if (GenPlace.TryPlaceThing(t, drop, map, ThingPlaceMode.Direct))
                    landed += t.stackCount;
                left -= t.stackCount;
            }
            return landed;
        }

        private static Texture2D ResolveWasteIcon(ThingDef wasteDef)
        {
            if (wasteDef == null || wasteDef.graphicData == null) return null;
            string path = wasteDef.graphicData.texPath;
            if (wasteDef.graphicData.graphicClass == typeof(Graphic_StackCount))
            {
                // Graphic_StackCount 的图放在同名子目录下的 <末段>_a/_b/_c.png
                int slash = path.LastIndexOf('/');
                string last = (slash >= 0) ? path.Substring(slash + 1) : path;
                path = path + "/" + last + "_a";
            }
            return ContentFinder<Texture2D>.Get(path, false);
        }
    }
}

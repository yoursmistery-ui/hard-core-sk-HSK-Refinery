using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RatkinUnderground
{
    public class RKU_DrillingVehicleCargo : RKU_DrillingVehicle, IThingHolder
    {
        private const int MaxPassengers = 2;
        private static Dictionary<string, List<Thing>> cargoStorage = new Dictionary<string, List<Thing>>();
        private static bool cargoStorageSaved = false;

        public int enterPawns = 0;

        public int maxPassengers
        {
            get { return MaxPassengers; }
        }

        public RKU_DrillingVehicleCargo()
        {
            passengers = new ThingOwner<Pawn>(this);
        }

        protected override void Tick()
        {
            base.Tick();

            // 检查货物容器中是否有pawn或建筑，如果有则移除并提示玩家
            var cargoContainer = GetCargoContainer();
            if (cargoContainer != null && cargoContainer.Count > 0)
            {
                List<Pawn> pawnsToRemove = new List<Pawn>();
                List<Thing> buildingsToRemove = new List<Thing>();
                foreach (Thing thing in cargoContainer)
                {
                    if (thing is Pawn pawn && pawn != null)
                    {
                        pawnsToRemove.Add(pawn);
                    }
                    else
                    {
                        // 检查是否是建筑或最小化的建筑
                        bool isBuilding = false;
                        if (thing.def.building != null)
                        {
                            isBuilding = true;
                        }
                        else if (thing is MinifiedThing minifiedThing)
                        {
                            if (minifiedThing.InnerThing?.def.building != null)
                            {
                                isBuilding = true;
                            }
                        }
                        
                        if (isBuilding)
                        {
                            buildingsToRemove.Add(thing);
                        }
                    }
                }

                foreach (Pawn pawn in pawnsToRemove)
                {
                    cargoContainer.Remove(pawn);
                    if (Map != null && Position.IsValid)
                    {
                        GenSpawn.Spawn(pawn, Position, Map);
                        Messages.Message("RKU_PawnInCargo".Translate(pawn.LabelShort), MessageTypeDefOf.CautionInput, false);
                    }
                }

                foreach (Thing building in buildingsToRemove)
                {
                    cargoContainer.Remove(building);
                    if (Map != null && Position.IsValid)
                    {
                        GenSpawn.Spawn(building, Position, Map);
                        Messages.Message("RKU_BuildingInCargo".Translate(building.LabelShort), MessageTypeDefOf.CautionInput, false);
                    }
                }
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            // 返回乘客列表，pawn应该进入乘客列表而不是货物容器
            return base.GetDirectlyHeldThings();
        }

        public ThingOwner GetCargoContainer()
        {
            return this.GetComp<CompTransporter>().innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetParentHolder()
        {
            return null;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (gizmo is Command_Action command && command.defaultLabel == "RKU.Drill".Translate())
                {
                    Command_Action modifiedCommand = new Command_Action
                    {
                        defaultLabel = command.defaultLabel,
                        defaultDesc = command.defaultDesc,
                        icon = command.icon,
                        action = () =>
                        {
                            command.action();
                        }
                    };
                    yield return modifiedCommand;
                }
                else
                {
                    yield return gizmo;
                }
            }

            CompTransporter compTransporter = this.GetComp<CompTransporter>();
            yield return new Command_Action
            {
                defaultLabel = "RKU_UnloadItems".Translate(),
                defaultDesc = "RKU_UnloadItemsDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter"),
                action = () =>
                {
                    Find.WindowStack.Add(new Dialog_LoadDrillingCargo(this, compTransporter));
                }
            };
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            
            ThingOwner container = GetCargoContainer();
            
            if (map == null || !Position.IsValid)
            {
                return;
            }

            if (cargoStorage == null)
            {
                cargoStorage = new Dictionary<string, List<Thing>>();
            }

            string cargoKey = $"{map.uniqueID}_{Position.x}_{Position.y}_{Position.z}";
            if (cargoStorage.TryGetValue(cargoKey, out List<Thing> savedCargo))
            {
                if (container != null && savedCargo != null)
                {
                    foreach (Thing savedItem in savedCargo)
                    {
                        if (savedItem != null && !savedItem.Destroyed)
                        {
                            container.TryAddOrTransfer(savedItem);
                        }
                    }
                }
                cargoStorage.Remove(cargoKey);
            }
        }

        public void PrepareCargoForDrilling()
        {
            ThingOwner container = GetCargoContainer();
            if (container != null && container.Count > 0)
            {
                // 保存货物到静态存储中，使用地图ID+位置作为复合键
                string cargoKey = $"{Map.uniqueID}_{Position.x}_{Position.y}_{Position.z}";
                List<Thing> cargoToSave = new List<Thing>(container);

                cargoStorage[cargoKey] = cargoToSave;
                container.Clear();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            ThingOwner cargoContainer = GetCargoContainer();
            if (cargoContainer != null && cargoContainer.Count > 0)
            {
                cargoContainer.Clear();
            }
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                if (!cargoStorageSaved)
                {
                    Scribe_Collections.Look(ref cargoStorage, "cargoStorage", LookMode.Value, LookMode.Deep);
                    cargoStorageSaved = true;
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Collections.Look(ref cargoStorage, "cargoStorage", LookMode.Value, LookMode.Deep);
                cargoStorageSaved = false;
            }
            Scribe_Values.Look(ref enterPawns, "enterPawns", 0);
        }

        private class Dialog_LoadDrillingCargo : Window
        {
            private RKU_DrillingVehicleCargo vehicle;
            private CompTransporter compTransporter;
            private List<TransferableOneWay> transferables;
            private TransferableOneWayWidget itemsTransfer;
            private float lastMassFlashTime = -9999f;
            private bool massUsageDirty = true;
            private float cachedMassUsage;
            private const float TitleRectHeight = 35f;
            private const float BottomAreaHeight = 55f;
            private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);
            private float MaxCargoMass => compTransporter?.Props?.massCapacity ?? 1000f;

            public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

            protected override float Margin => 0f;

            private float MassCapacity => MaxCargoMass;

            private float MassUsage
            {
                get
                {
                    if (massUsageDirty)
                    {
                        massUsageDirty = false;
                        cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false);
                    }
                    return cachedMassUsage;
                }
            }

            public Dialog_LoadDrillingCargo(RKU_DrillingVehicleCargo vehicle, CompTransporter compTransporter)
            {
                this.vehicle = vehicle;
                this.compTransporter = compTransporter;
                forcePause = true;
                absorbInputAroundWindow = true;
            }

            public override void PostOpen()
            {
                base.PostOpen();
                CalculateAndRecacheTransferables();
            }

            public override void DoWindowContents(Rect inRect)
            {
                Rect rect = new Rect(0f, 0f, inRect.width, 35f);
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "CommandLoadTransporter".Translate());
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;

                // 显示重量信息
                CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(MassUsage, MassCapacity, "", 0f, "", default((float days, float tillRot)), default((ThingDef food, float perDay)), "", 0f, "", MassUsage, MassCapacity, ""), null, vehicle.Map.Tile, null, lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), lerpMassColor: false);
                inRect.yMin += 52f;

                inRect.yMin += 67f;
                Widgets.DrawMenuSection(inRect);
                inRect = inRect.ContractedBy(17f);
                Widgets.BeginGroup(inRect);
                Rect rect2 = inRect.AtZero();
                DoBottomButtons(rect2);
                Rect inRect2 = rect2;
                inRect2.yMax -= 59f;
                bool anythingChanged = false;
                itemsTransfer.OnGUI(inRect2, out anythingChanged);
                if (anythingChanged)
                {
                    CountToTransferChanged();
                }
                Widgets.EndGroup();
            }

            public override bool CausesMessageBackground()
            {
                return true;
            }

            private void AddToTransferables(Thing t)
            {
                TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferableOneWay == null)
                {
                    transferableOneWay = new TransferableOneWay();
                    transferables.Add(transferableOneWay);
                }
                if (transferableOneWay.things.Contains(t))
                {
                    Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
                    return;
                }
                transferableOneWay.things.Add(t);
            }

            private void DoBottomButtons(Rect rect)
            {
                Rect rect2 = new Rect(rect.width / 2f - BottomButtonSize.x / 2f, rect.height - 55f, BottomButtonSize.x, BottomButtonSize.y);
                if (Widgets.ButtonText(rect2, "AcceptButton".Translate()))
                {
                    if (TryAccept())
                    {
                        Close(doCloseSound: false);
                    }
                }

                if (Widgets.ButtonText(new Rect(rect2.x - 10f - BottomButtonSize.x, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate()))
                {
                    CalculateAndRecacheTransferables();
                }

                if (Widgets.ButtonText(new Rect(rect2.xMax + 10f, rect2.y, BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate()))
                {
                    Close();
                }
            }

            private void CalculateAndRecacheTransferables()
            {
                transferables = new List<TransferableOneWay>();
                AddItemsToTransferables();
                itemsTransfer = new TransferableOneWayWidget(transferables.Where((TransferableOneWay x) => x.ThingDef.category == ThingCategory.Item && x.ThingDef != ThingDef.Named("Corpse_Human")), null, null, "FormCaravanColonyThingCountTip".Translate(), drawMass: true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, includePawnsMassInMassUsage: false, () => MassCapacity - MassUsage, 0f, ignoreSpawnedCorpseGearAndInventoryMass: false, vehicle.Map.Tile, drawMarketValue: true, drawEquippedWeapon: false, drawItemNutrition: true, drawForagedFoodPerDay: false, drawDaysUntilRot: true);
                CountToTransferChanged();
            }

            private bool TryAccept()
            {
                if (!CheckForErrors())
                {
                    return false;
                }

                ThingOwner cargo = compTransporter.innerContainer;

                // 清空并重新初始化 leftToLoad
                if (compTransporter.leftToLoad == null)
                {
                    compTransporter.leftToLoad = new List<TransferableOneWay>();
                }
                else
                {
                    compTransporter.leftToLoad.Clear();
                }

                // 处理每个物品类型的转移
                foreach (TransferableOneWay transferable in transferables)
                {
                    if (!transferable.HasAnyThing)
                        continue;

                    int targetCount = transferable.CountToTransfer;
                    
                    // 统计当前容器中的数量
                    int currentCountInCargo = GetCurrentCountInCargo(cargo, transferable.ThingDef);
                    
                    // 统计正在被pawn搬运的数量
                    int beingHauledCount = GetThingsBeingHauledCount(transferable.ThingDef);
                    
                    // 计算剩余需要装载的数量
                    // 剩余需要 = 目标数量 - 容器中已有 - 正在搬运
                    int remainingToLoad = targetCount - currentCountInCargo - beingHauledCount;

                    if (remainingToLoad > 0)
                    {
                        // 需要装载，设置 leftToLoad
                        // CountToTransfer 直接设置为剩余需要装载的数量
                        // 这样每次装载时，SubtractFromToLoadList 会自动减少它
                        compTransporter.AddToTheToLoadList(transferable, remainingToLoad);
                        Log.Message($"[LoadDrillingCargo] TryAccept - 设置 leftToLoad: 物品: {transferable.ThingDef?.label ?? "null"}, 目标数量: {targetCount}, 容器中已有: {currentCountInCargo}, 正在搬运: {beingHauledCount}, 剩余需要: {remainingToLoad}");
                    }
                    else if (remainingToLoad < 0)
                    {
                        // 需要卸载
                        UnloadItems(transferable, -remainingToLoad, cargo);
                    }
                }

                return true;
            }

            /// <summary>
            /// 统计容器中指定物品类型的数量
            /// </summary>
            private int GetCurrentCountInCargo(ThingOwner cargo, ThingDef thingDef)
            {
                int count = 0;
                for (int i = 0; i < cargo.Count; i++)
                {
                    Thing thing = cargo[i];
                    if (thing.def == thingDef)
                    {
                        count += thing.stackCount;
                    }
                }
                return count;
            }

            /// <summary>
            /// 装载物品到容器
            /// </summary>
            private void LoadItems(TransferableOneWay transferable, int remainingToLoad, ThingOwner cargo)
            {
                Dictionary<Thing, int> allocatedCounts = GetActiveHaulingTasks(transferable.ThingDef);
                
                foreach (Thing thing in transferable.things)
                {
                    if (remainingToLoad <= 0)
                        break;
                    
                    if (!thing.Spawned || IsThingBeingHauled(thing))
                        continue;

                    int allocatedCount = allocatedCounts.TryGetValue(thing, 0);
                    int availableCount = thing.stackCount - allocatedCount;
                    
                    if (availableCount <= 0)
                        continue; 

                    int amountToLoad = Mathf.Min(remainingToLoad, availableCount);
                    if (amountToLoad > 0)
                    {
                        Thing splitThing = thing.SplitOff(amountToLoad);
                        if (splitThing != null)
                        {
                            int actuallyAdded = cargo.TryAddOrTransfer(splitThing, splitThing.stackCount, canMergeWithExistingStacks: true);
                            remainingToLoad -= actuallyAdded;
                            
                            if (splitThing.stackCount > 0 && !splitThing.Destroyed)
                            {
                                thing.TryAbsorbStack(splitThing, respectStackLimit: false);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// 从容器卸载物品
            /// </summary>
            private void UnloadItems(TransferableOneWay transferable, int remainingToUnload, ThingOwner cargo)
            {
                List<Thing> thingsToUnload = new List<Thing>();
                for (int i = 0; i < cargo.Count; i++)
                {
                    Thing thing = cargo[i];
                    if (thing.def == transferable.ThingDef)
                    {
                        thingsToUnload.Add(thing);
                    }
                }

                foreach (Thing thing in thingsToUnload)
                {
                    if (remainingToUnload <= 0)
                        break;

                    int amountToUnload = Mathf.Min(remainingToUnload, thing.stackCount);
                    if (amountToUnload > 0)
                    {
                        Thing splitThing = thing.SplitOff(amountToUnload);
                        if (splitThing != null)
                        {
                            if (GenPlace.TryPlaceThing(splitThing, vehicle.Position, vehicle.Map, ThingPlaceMode.Near))
                            {
                                remainingToUnload -= amountToUnload;
                            }
                            else
                            {
                                // 放置失败，将物品合并回去
                                if (splitThing.stackCount > 0 && !splitThing.Destroyed)
                                {
                                    thing.TryAbsorbStack(splitThing, respectStackLimit: false);
                                }
                            }
                        }
                    }
                }
            }

            private bool CheckForErrors()
            {
                if (MassUsage > MassCapacity)
                {
                    FlashMass();
                    Messages.Message("TransportersMassUsageExceedsMassCapacity".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                    return false;
                }

                return true;
            }

            private void AddItemsToTransferables()
            {
                // 先统计容器内物品的数量
                Dictionary<ThingDef, int> cargoItemCounts = new Dictionary<ThingDef, int>();
                ThingOwner cargo = compTransporter.innerContainer;
                if (cargo != null)
                {
                    foreach (Thing item in cargo)
                    {
                        if (!cargoItemCounts.ContainsKey(item.def))
                        {
                            cargoItemCounts[item.def] = 0;
                        }
                        cargoItemCounts[item.def] += item.stackCount;
                    }
                }

                // 添加地图上的物品到transferables
                foreach (Thing item in vehicle.Map.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item && t.Spawned && t.Position.InBounds(vehicle.Map)))
                {
                    AddToTransferables(item);
                }

                // 为每个transferable设置初始countToTransfer为容器内对应物品的数量
                foreach (TransferableOneWay transferable in transferables)
                {
                    if (transferable.HasAnyThing && cargoItemCounts.TryGetValue(transferable.ThingDef, out int cargoCount))
                    {
                        // 使用反射设置countToTransfer，因为setter是protected的
                        var countToTransferField = typeof(TransferableOneWay).GetField("countToTransfer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (countToTransferField != null)
                        {
                            countToTransferField.SetValue(transferable, cargoCount);
                        }
                        transferable.EditBuffer = cargoCount.ToStringCached();
                    }
                }

                // 对于容器内有但地图上没有的物品，创建一个空的transferable来显示
                foreach (var kvp in cargoItemCounts)
                {
                    var existingTransferable = transferables.FirstOrDefault(t => t.ThingDef == kvp.Key);
                    if (existingTransferable == null)
                    {
                        // 地图上没有这个物品，创建一个空的transferable来显示容器内数量
                        var transferable = new TransferableOneWay();
                        var virtualThing = ThingMaker.MakeThing(kvp.Key);
                        virtualThing.stackCount = 0; // 虚拟对象，stackCount设为0
                        transferable.things.Add(virtualThing);
                        transferables.Add(transferable);
                        
                        var countToTransferField = typeof(TransferableOneWay).GetField("countToTransfer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (countToTransferField != null)
                        {
                            countToTransferField.SetValue(transferable, kvp.Value);
                        }
                        transferable.EditBuffer = kvp.Value.ToStringCached();
                    }
                }
            }

            private void FlashMass()
            {
                lastMassFlashTime = Time.time;
            }

            private void CountToTransferChanged()
            {
                massUsageDirty = true;
            }

            /// <summary>
            /// 获取所有正在搬运到当前钻机的任务信息
            /// 返回字典：物品 -> 已分配数量
            /// </summary>
            private Dictionary<Thing, int> GetActiveHaulingTasks(ThingDef filterDef = null)
            {
                Dictionary<Thing, int> result = new Dictionary<Thing, int>();
                if (vehicle?.Map == null)
                    return result;

                IReadOnlyList<Pawn> allPawns = vehicle.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    Pawn pawn = allPawns[i];
                    if (pawn.CurJobDef == DefOfs.RKU_LoadDrillingCargo && pawn.CurJob != null)
                    {
                        LocalTargetInfo vehicleTarget = pawn.CurJob.GetTarget(TargetIndex.B);
                        if (vehicleTarget.Thing == vehicle)
                        {
                            Thing itemToLoad = pawn.CurJob.GetTarget(TargetIndex.A).Thing;
                            if (itemToLoad != null && (filterDef == null || itemToLoad.def == filterDef))
                            {
                                // 优先使用实际携带的数量，否则使用job.count
                                Thing carriedThing = pawn.carryTracker.CarriedThing;
                                int allocatedCount = (carriedThing != null && carriedThing.def == itemToLoad.def) 
                                    ? carriedThing.stackCount 
                                    : pawn.CurJob.count;

                                int existing = result.TryGetValue(itemToLoad, 0);
                                result[itemToLoad] = existing + allocatedCount;
                            }
                        }
                    }
                }
                return result;
            }

            /// <summary>
            /// 统计正在被pawn搬运的指定物品类型的总数量
            /// </summary>
            private int GetThingsBeingHauledCount(ThingDef thingDef)
            {
                Dictionary<Thing, int> haulingTasks = GetActiveHaulingTasks(thingDef);
                int count = 0;
                foreach (var kvp in haulingTasks)
                {
                    count += kvp.Value;
                }
                return count;
            }

            /// <summary>
            /// 检查指定物品是否正在被pawn搬运到钻机
            /// </summary>
            private bool IsThingBeingHauled(Thing thing)
            {
                if (vehicle?.Map == null || thing == null)
                    return false;

                // 检查是否是job目标或已在carryTracker中
                IReadOnlyList<Pawn> allPawns = vehicle.Map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < allPawns.Count; i++)
                {
                    Pawn pawn = allPawns[i];
                    if (pawn.CurJobDef == DefOfs.RKU_LoadDrillingCargo && pawn.CurJob != null)
                    {
                        LocalTargetInfo vehicleTarget = pawn.CurJob.GetTarget(TargetIndex.B);
                        if (vehicleTarget.Thing == vehicle)
                        {
                            LocalTargetInfo itemTarget = pawn.CurJob.GetTarget(TargetIndex.A);
                            if (itemTarget.Thing == thing || pawn.carryTracker.CarriedThing == thing)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}


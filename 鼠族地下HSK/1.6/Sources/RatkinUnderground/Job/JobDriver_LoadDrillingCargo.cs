using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RatkinUnderground
{
    public class WorkGiver_LoadDrillingCargo : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Building building in pawn.Map.listerBuildings.allBuildingsColonist)
            {
                if (building is RKU_DrillingVehicleCargo vehicle &&
                    pawn.CanReach(vehicle, PathEndMode.Touch, Danger.None))
                {
                    CompTransporter compTransporter = vehicle.GetComp<CompTransporter>();
                    if (compTransporter != null && compTransporter.LoadingInProgressOrReadyToLaunch)
                    {
                        yield return vehicle;
                    }
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (!(thing is RKU_DrillingVehicleCargo vehicle))
                return false;

            if (!pawn.CanReach(vehicle, PathEndMode.Touch, Danger.None))
                return false;

            CompTransporter compTransporter = vehicle.GetComp<CompTransporter>();
            if (compTransporter == null || !compTransporter.LoadingInProgressOrReadyToLaunch)
                return false;

            if (compTransporter.leftToLoad == null || compTransporter.leftToLoad.Count == 0)
                return false;

            Dictionary<TransferableOneWay, int> alreadyLoading = GetAlreadyLoadingCounts(pawn, vehicle, compTransporter);

            bool hasJob = false;
            foreach (TransferableOneWay transferable in compTransporter.leftToLoad)
            {
                int countToTransfer = transferable.CountToTransfer;
                if (countToTransfer <= 0 || !transferable.HasAnyThing)
                    continue;

                int alreadyLoadingCount = alreadyLoading.TryGetValue(transferable, 0);
                int remainingToLoad = Mathf.Max(0, countToTransfer - alreadyLoadingCount);

                if (remainingToLoad <= 0)
                    continue;

                foreach (Thing thingToLoad in transferable.things)
                {
                    if (thingToLoad.Spawned &&
                        thingToLoad.Position.InBounds(pawn.Map) &&
                        pawn.CanReserve(thingToLoad) &&
                        pawn.CanReach(thingToLoad, PathEndMode.Touch, Danger.None))
                    {
                        hasJob = true;
                        break;
                    }
                }
                if (hasJob)
                    break;
            }

            return hasJob;
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            if (!(thing is RKU_DrillingVehicleCargo vehicle))
                return null;

            CompTransporter compTransporter = vehicle.GetComp<CompTransporter>();
            if (compTransporter == null || !compTransporter.LoadingInProgressOrReadyToLaunch)
                return null;

            if (compTransporter.leftToLoad == null || compTransporter.leftToLoad.Count == 0)
                return null;

            Dictionary<TransferableOneWay, int> alreadyLoading = GetAlreadyLoadingCounts(pawn, vehicle, compTransporter);
            return FindBestItemToLoad(pawn, vehicle, compTransporter, alreadyLoading);
        }

        /// <summary>
        /// 统计每个TransferableOneWay正在被其他pawn搬运的数量
        /// </summary>
        private Dictionary<TransferableOneWay, int> GetAlreadyLoadingCounts(Pawn currentPawn, RKU_DrillingVehicleCargo vehicle, CompTransporter compTransporter)
        {
            Dictionary<TransferableOneWay, int> alreadyLoading = new Dictionary<TransferableOneWay, int>();
            IReadOnlyList<Pawn> allPawns = currentPawn.Map.mapPawns.AllPawnsSpawned;
            
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn otherPawn = allPawns[i];
                if (otherPawn == currentPawn || otherPawn.CurJobDef != DefOfs.RKU_LoadDrillingCargo || otherPawn.CurJob == null)
                    continue;
                
                LocalTargetInfo vehicleTarget = otherPawn.CurJob.GetTarget(TargetIndex.B);
                if (vehicleTarget.Thing != vehicle)
                    continue;
                
                Thing itemToLoad = otherPawn.CurJob.GetTarget(TargetIndex.A).Thing;
                if (itemToLoad == null)
                    continue;
                
                Thing carriedThing = otherPawn.carryTracker.CarriedThing;
                int allocatedCount = (carriedThing != null && carriedThing.def == itemToLoad.def) 
                    ? carriedThing.stackCount 
                    : otherPawn.CurJob.count;
                
                foreach (TransferableOneWay transferable in compTransporter.leftToLoad)
                {
                    if (transferable.things.Contains(itemToLoad))
                    {
                        int allocated = alreadyLoading.TryGetValue(transferable, 0);
                        alreadyLoading[transferable] = allocated + allocatedCount;
                        break;
                    }
                }
            }
            
            return alreadyLoading;
        }

        /// <summary>
        /// 查找最近的需要装载的物品
        /// </summary>
        private Job FindBestItemToLoad(Pawn pawn, RKU_DrillingVehicleCargo vehicle, CompTransporter compTransporter, Dictionary<TransferableOneWay, int> alreadyLoading)
        {
            Thing bestItem = null;
            float bestDist = float.MaxValue;
            int bestAmount = 0;

            foreach (TransferableOneWay transferable in compTransporter.leftToLoad)
            {
                int countToTransfer = transferable.CountToTransfer;
                if (countToTransfer <= 0 || !transferable.HasAnyThing)
                    continue;
                
                int alreadyLoadingCount = alreadyLoading.TryGetValue(transferable, 0);
                int remainingToLoad = Mathf.Max(0, countToTransfer - alreadyLoadingCount);
                
                if (remainingToLoad <= 0)
                    continue;

                foreach (Thing item in transferable.things)
                {
                    if (!item.Spawned || 
                        !item.Position.InBounds(pawn.Map) ||
                        !pawn.CanReserve(item) ||
                        !pawn.CanReach(item, PathEndMode.Touch, Danger.None))
                        continue;

                    int amountToLoad = Mathf.Min(remainingToLoad, item.stackCount);
                    if (amountToLoad > 0)
                    {
                        float dist = pawn.Position.DistanceTo(item.Position);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestItem = item;
                            bestAmount = amountToLoad;
                        }
                    }
                }
            }

            if (bestItem != null)
            {
                Job job = JobMaker.MakeJob(DefOfs.RKU_LoadDrillingCargo, bestItem, vehicle);
                job.count = bestAmount;
                return job;
            }

            return null;
        }
    }

    public class JobDriver_LoadDrillingCargo : JobDriver
    {
        private const TargetIndex ItemToLoadIndex = TargetIndex.A;
        private const TargetIndex VehicleIndex = TargetIndex.B;

        protected Thing ItemToLoad => job.GetTarget(ItemToLoadIndex).Thing;
        protected RKU_DrillingVehicleCargo Vehicle => (RKU_DrillingVehicleCargo)job.GetTarget(VehicleIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(ItemToLoad, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(ItemToLoadIndex);
            this.FailOnDestroyedOrNull(VehicleIndex);

            yield return Toils_Goto.GotoThing(ItemToLoadIndex, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(ItemToLoadIndex);
            yield return Toils_Goto.GotoThing(VehicleIndex, PathEndMode.Touch);

            Toil loadToil = new Toil();
            loadToil.initAction = () =>
            {
                Thing carriedThing = pawn.carryTracker.CarriedThing;
                if (carriedThing != null)
                {
                    CompTransporter compTransporter = Vehicle.GetComp<CompTransporter>();
                    if (compTransporter == null)
                        return;

                    int amountToLoad = Mathf.Min(job.count, carriedThing.stackCount);
                    
                    if (amountToLoad > 0)
                    {
                        Thing splitThing = carriedThing.SplitOff(amountToLoad);
                        if (splitThing == null)
                            return;

                        compTransporter.innerContainer.TryAddOrTransfer(splitThing, splitThing.stackCount, canMergeWithExistingStacks: true);
                    }

                    if (carriedThing.stackCount == 0)
                    {
                        pawn.carryTracker.innerContainer.Remove(carriedThing);
                    }
                }
            };
            loadToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return loadToil;
        }
    }
}

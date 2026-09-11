using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RatkinUnderground
{
    public class JobDriver_RescueAndEnterDrillingVehicle : JobDriver
    {
        private const TargetIndex PawnInd = TargetIndex.A;
        private const TargetIndex VehicleInd = TargetIndex.B;

        protected Pawn PawnToRescue => job.GetTarget(PawnInd).Pawn;
        protected RKU_DrillingVehicle Vehicle => job.GetTarget(VehicleInd).Thing as RKU_DrillingVehicle;
        protected RKU_DrillingVehicleInEnemyMap VehicleInEnemyMap => job.GetTarget(VehicleInd).Thing as RKU_DrillingVehicleInEnemyMap;
        protected Thing VehicleThing => job.GetTarget(VehicleInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(PawnToRescue, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(VehicleThing, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(PawnInd);
            this.FailOnDestroyedOrNull(VehicleInd);
            this.FailOnSomeonePhysicallyInteracting(PawnInd);
            this.FailOn(() => PawnToRescue != null && !PawnToRescue.Downed && !(PawnToRescue.IsColonyMech));

            // 前往倒地pawn
            yield return Toils_Goto.GotoThing(PawnInd, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(PawnInd);
            pawn.jobs.curJob.count = 1;
            yield return Toils_Haul.StartCarryThing(PawnInd);


            Toil toil = Toils_Goto.GotoCell(VehicleInd, PathEndMode.Touch);

            yield return toil;

            // 等待进入钻机
            yield return Toils_General.Wait(60).WithProgressBarToilDelay(VehicleInd);

            // 最终进入钻机
            Toil enterVehicleToil = new Toil
            {
                initAction = () =>
                {
                    Pawn carriedPawn = pawn.carryTracker.CarriedThing as Pawn;

                    if (Vehicle != null)
                    {
                        // 首先将被救援pawn放入钻机
                        if (carriedPawn != null)
                        {
                            Vehicle.AddPassenger(carriedPawn);
                            pawn.carryTracker.innerContainer.Remove(carriedPawn);
                        }

                        // 然后救援者自己进入钻机
                        if (Vehicle is IThingHolder thingHolder)
                        {
                            pawn.DeSpawnOrDeselect();
                            thingHolder.GetDirectlyHeldThings().TryAddOrTransfer(pawn);
                        }
                    }
                    else if (VehicleInEnemyMap != null)
                    {
                        if (carriedPawn != null)
                        {
                            VehicleInEnemyMap.AddPassenger(carriedPawn);
                            pawn.carryTracker.innerContainer.Remove(carriedPawn);
                        }

                        if (VehicleInEnemyMap is IThingHolder thingHolder)
                        {
                            pawn.DeSpawnOrDeselect();
                            thingHolder.GetDirectlyHeldThings().TryAddOrTransfer(pawn);
                        }
                    }
                    else
                    {
                        Log.Message($"[RKU_Rescue] 进入钻机失败 - 钻机为空或类型不支持");
                    }
                }
            };
            enterVehicleToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enterVehicleToil;
        }
    }
}
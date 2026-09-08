using HarmonyLib;
using RimWorld;
using Verse;

namespace Defaults.BabyFeeding
{
    [HarmonyPatchCategory("BabyFeeding")]
    [HarmonyPatch(typeof(PregnancyUtility))]
    [HarmonyPatch(nameof(PregnancyUtility.ApplyBirthOutcome))]
    [HarmonyPatchMod("Ludeon.RimWorld.Biotech")]
    public static class Patch_PregnancyUtility
    {
        public static void Postfix(Thing birtherThing, Thing __result)
        {
            if (__result is Pawn baby)
            {
                foreach (Pawn feeder in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
                {
                    if (feeder != baby && feeder.RaceProps.Humanlike && !ChildcareUtility.CanSuckle(feeder, out _) && !feeder.IsWorkTypeDisabledByAge(WorkTypeDefOf.Childcare, out _))
                    {
                        AutofeedMode mode;
                        if (feeder == birtherThing)
                        {
                            mode = Settings.Get<BabyFeedingOptions>(Settings.BABY_FEEDING).BirtherParent;
                        }
                        else
                        {
                            bool isParent = baby.GetMother() == feeder || baby.GetFather() == feeder;
                            bool isLactating = feeder.health.hediffSet.HasHediff(HediffDefOf.Lactating);
                            mode = PatchUtility_PregnancyUtility.GetMode(isParent, isLactating);
                        }
                        if (mode != AutofeedMode.Childcare)
                        {
                            baby.mindState.SetAutofeeder(feeder, mode);
                        }
                    }
                }
            }
        }
    }

    public static class PatchUtility_PregnancyUtility
    {
        public static AutofeedMode GetMode(bool isParent, bool isLactating)
        {
            BabyFeedingOptions options = Settings.Get<BabyFeedingOptions>(Settings.BABY_FEEDING);
            return isParent
                ? isLactating ? options.ParentLactating : options.ParentNonlactating
                : isLactating ? options.NonparentLactating : options.NonparentNonlactating;
        }
    }
}

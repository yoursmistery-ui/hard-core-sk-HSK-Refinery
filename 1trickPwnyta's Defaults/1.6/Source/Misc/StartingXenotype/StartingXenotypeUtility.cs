using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

namespace Defaults.Misc.StartingXenotype
{
    public static class StartingXenotypeUtility
    {
        public static void InitializeStartingPawns()
        {
            for (int i = 0; i < Find.GameInitData.startingAndOptionalPawns.Count; i++)
            {
                InitializeStartingPawn(i);
            }
        }

        public static void InitializeStartingPawn(int index)
        {
            StartingXenotypeOptions options = Settings.Get<StartingXenotypeOptions>(Settings.STARTING_XENOTYPE_OPTIONS);
            object[] args;
            switch (options.Option)
            {
                case StartingXenotypeOption.AnyNonArchite:
                    args = new object[]
                    {
                            index,
                            null,
                            null,
                            DefDatabase<XenotypeDef>.AllDefsListForReading.Where(x => !x.Archite && x != XenotypeDefOf.Baseliner).ToList(),
                            0.5f,
                            (Func<PawnGenerationRequest, bool>)(r => r.ForcedXenotype != null || r.ForcedCustomXenotype != null),
                            null,
                            false
                    };
                    break;
                case StartingXenotypeOption.XenotypeDef:
                    args = new object[]
                    {
                            index,
                            options.XenotypeDef,
                            null,
                            null,
                            0f,
                            (Func<PawnGenerationRequest, bool>)(r => r.ForcedXenotype != options.XenotypeDef),
                            null,
                            false
                    };
                    break;
                case StartingXenotypeOption.CustomXenotype:
                    args = new object[]
                    {
                            index,
                            null,
                            options.CustomXenotype,
                            null,
                            0f,
                            (Func<PawnGenerationRequest, bool>)(r => r.ForcedCustomXenotype != options.CustomXenotype),
                            null,
                            false
                    };
                    break;
                default:
                    throw new Exception("Invalid starting xenotype option: " + options.Option);
            }
            typeof(CharacterCardUtility).Method("SetupGenerationRequest").Invoke(null, args);
            StartingPawnUtility.RandomizePawn(index);
        }
    }
}

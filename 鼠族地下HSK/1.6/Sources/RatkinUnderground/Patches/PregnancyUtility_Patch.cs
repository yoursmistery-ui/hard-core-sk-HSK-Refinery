using HarmonyLib;
using RimWorld;
using Verse;

namespace RatkinUnderground.Patches;

/// 怀孕时因技能限制导致的问题
[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
public static class PawnGenerator_GeneratePawn_Patch
{
    /// <summary>
    /// </summary>
    static void Prefix(ref PawnGenerationRequest request, ref PawnKindDef __state)
    {
        __state = null;
        
        if (request.KindDef != null && request.AllowedDevelopmentalStages.Newborn())
        {
            PawnKindDef kindDef = request.KindDef;
            if (kindDef.defName.StartsWith("RKU_") && kindDef.skills != null && kindDef.skills.Count > 0)
            {
                __state = kindDef;
                request.KindDef = PawnKindDefOf.Colonist;
            }
        }
    }

    static void Postfix(ref PawnGenerationRequest request, PawnKindDef __state)
    {
        if (__state != null && request.KindDef == PawnKindDefOf.Colonist)
        {
            request.KindDef = __state;
        }
    }
}


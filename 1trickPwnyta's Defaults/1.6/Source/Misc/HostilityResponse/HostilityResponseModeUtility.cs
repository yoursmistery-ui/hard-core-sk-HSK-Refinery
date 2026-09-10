using RimWorld;
using Verse;

namespace Defaults.Misc.HostilityResponse
{
    public static class HostilityResponseModeUtility
    {
        public static void SetHostilityResponseMode(Pawn pawn, Pawn_PlayerSettings settings)
        {
            if (settings != null && pawn.IsColonist)
            {
                settings.hostilityResponse = Settings.GetValue<HostilityResponseMode>(Settings.HOSTILITY_RESPONSE);
                if (pawn.WorkTagIsDisabled(WorkTags.Violent) && settings.hostilityResponse == HostilityResponseMode.Attack)
                {
                    settings.hostilityResponse = HostilityResponseMode.Flee;
                }
            }
        }
    }
}

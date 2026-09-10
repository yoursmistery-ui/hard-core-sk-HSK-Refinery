using RimWorld;
using System;
using Verse;

namespace Defaults
{
    public enum PawnType
    {
        AdultColonist,
        ChildColonist,
        Slave,
        Guest,
        Mech,
        Animal,
        Ghoul
    }

    public static class PawnTypeUtility
    {
        public static string GetLabel(this PawnType pawnType)
        {
            switch (pawnType)
            {
                case PawnType.AdultColonist: return "Defaults_AdultColonists".Translate();
                case PawnType.ChildColonist: return "Defaults_ChildColonists".Translate();
                case PawnType.Slave: return "Defaults_Slaves".Translate();
                case PawnType.Guest: return "Defaults_Guests".Translate();
                case PawnType.Mech: return "Defaults_Mechs".Translate();
                case PawnType.Animal: return "Defaults_Animals".Translate();
                case PawnType.Ghoul: return "Defaults_Ghouls".Translate();
                default: throw new ArgumentException("Unknown pawn type: " + pawnType);
            }
        }

        public static bool IsActive(this PawnType pawnType)
        {
            switch (pawnType)
            {
                case PawnType.ChildColonist: return ModsConfig.BiotechActive;
                case PawnType.Slave: return ModsConfig.IdeologyActive;
                case PawnType.Mech: return ModsConfig.BiotechActive;
                case PawnType.Ghoul: return ModsConfig.AnomalyActive;
                default: return true;
            }
        }

        public static PawnType? GetPawnType(Pawn pawn)
        {
            if (pawn.IsFreeNonSlaveColonist && !pawn.IsQuestLodger() && pawn.ageTracker.Adult)
            {
                return PawnType.AdultColonist;
            }
            else if (pawn.IsFreeNonSlaveColonist && !pawn.IsQuestLodger() && !pawn.ageTracker.Adult)
            {
                return PawnType.ChildColonist;
            }
            else if (pawn.IsSlaveOfColony)
            {
                return PawnType.Slave;
            }
            else if (pawn.IsQuestLodger())
            {
                return PawnType.Guest;
            }
            else if (pawn.IsColonyMech)
            {
                return PawnType.Mech;
            }
            else if (pawn.IsAnimal && ((pawn.Faction != null && pawn.Faction.IsPlayer) || (pawn.HostFaction != null && pawn.HostFaction.IsPlayer)))
            {
                return PawnType.Animal;
            }
            else if (pawn.IsColonySubhuman)
            {
                return PawnType.Ghoul;
            }
            return null;
        }
    }
}

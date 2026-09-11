using UnityEngine;
using Verse;

namespace RatkinUnderground
{
    public class RKU_ModSettings : ModSettings
    {
        public bool showOnlyCurrentDialogueMessages = true;
        public bool allowRescueMechs = true;
        public int finalBattleMaxDeaths = 500;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref showOnlyCurrentDialogueMessages, "showOnlyCurrentDialogueMessages", true);
            Scribe_Values.Look(ref allowRescueMechs, "allowRescueMechs", true);
            Scribe_Values.Look(ref finalBattleMaxDeaths, "finalBattleMaxDeaths", 500);
        }
    }
}

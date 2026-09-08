using Verse;

namespace Defaults.Storyteller
{
    public class NoPauseOptions : IExposable
    {
        public bool NoPause = false;
        public bool HalfSpeed = false;

        public void ExposeData()
        {
            Scribe_Values.Look(ref NoPause, "NoPause", false);
            Scribe_Values.Look(ref HalfSpeed, "HalfSpeed", false);
        }
    }
}

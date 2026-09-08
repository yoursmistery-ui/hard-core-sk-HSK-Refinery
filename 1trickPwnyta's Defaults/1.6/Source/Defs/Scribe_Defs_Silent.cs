using Verse;

namespace Defaults.Defs
{
    public static class Scribe_Defs_Silent
    {
        public static void Look<T>(ref T value, string label) where T : Def, new()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                string defName = value?.defName ?? "null";
                Scribe_Values.Look(ref defName, label, "null");
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                value = DefDatabase<T>.GetNamedSilentFail(ScribeExtractor.ValueFromNode(Scribe.loader.curXmlParent[label], "null"));
            }
        }
    }
}

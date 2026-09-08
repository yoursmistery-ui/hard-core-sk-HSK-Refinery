using Defaults.Defs;
using RimWorld;
using Verse;

namespace Defaults.Misc.StartingXenotype
{
    public class StartingXenotypeOptions : IExposable
    {
        public StartingXenotypeOption Option = StartingXenotypeOption.XenotypeDef;
        public XenotypeDef XenotypeDef = XenotypeDefOf.Baseliner;
        public CustomXenotype CustomXenotype = null;
        private string customXenotypeName;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Option, "Option", StartingXenotypeOption.XenotypeDef);
            Scribe_Defs_Silent.Look(ref XenotypeDef, "XenotypeDef");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && Option == StartingXenotypeOption.XenotypeDef && XenotypeDef == null)
            {
                XenotypeDef = XenotypeDefOf.Baseliner;
            }
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                customXenotypeName = CustomXenotype?.fileName;
            }
            Scribe_Values.Look(ref customXenotypeName, "CustomXenotype");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && customXenotypeName != null)
            {
                CustomXenotype = CharacterCardUtility.CustomXenotypesForReading.FirstOrDefault(c => c.fileName == customXenotypeName);
                if (CustomXenotype == null)
                {
                    Option = StartingXenotypeOption.XenotypeDef;
                    XenotypeDef = XenotypeDefOf.Baseliner;
                }
            }
        }
    }

    public enum StartingXenotypeOption
    {
        AnyNonArchite,
        XenotypeDef,
        CustomXenotype
    }
}

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.AllowedAreas
{
    public class AllowedArea : IExposable, ILoadReferenceable, IRenameable
    {
        public static string FindUnusedName()
        {
            List<AllowedArea> allowedAreas = Settings.Get<List<AllowedArea>>(Settings.ALLOWED_AREAS);
            string name = string.Empty;
            for (int i = 1; i == 1 || allowedAreas.Any(a => a.name == name); i++)
            {
                name = "AreaDefaultLabel".Translate(i);
            }
            return name;
        }

        public AllowedArea()
        {
        }

        public AllowedArea(string name)
        {
            this.name = name;
        }

        public string name;
        public Color color = Color.HSVToRGB(Rand.Value, Rand.Value, 0.5f);
        public bool full;

        public string RenamableLabel { get => name; set => name = value; }

        public string BaseLabel => name;

        public string InspectLabel => name;

        public override string ToString() => name;

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref color, "color");
            Scribe_Values.Look(ref full, "full");
        }

        public string GetUniqueLoadID() => "Defaults_AllowedArea_" + name;
    }
}

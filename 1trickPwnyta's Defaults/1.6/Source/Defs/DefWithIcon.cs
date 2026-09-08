using UnityEngine;
using Verse;

namespace Defaults.Defs
{
    public abstract class DefWithIcon : Def
    {
        private Texture2D icon;

        public string iconPath;

        public Texture2D Icon
        {
            get
            {
                if (icon == null)
                {
                    if (iconPath != null)
                    {
                        icon = ContentFinder<Texture2D>.Get(iconPath);
                    }
                }
                return icon;
            }
        }
    }
}

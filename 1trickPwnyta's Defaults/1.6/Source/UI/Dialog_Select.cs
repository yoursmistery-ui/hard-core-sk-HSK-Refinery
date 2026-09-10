using UnityEngine;
using Verse;

namespace Defaults.UI
{
    public abstract class Dialog_Select : Dialog_Input
    {
        public Dialog_Select(TaggedString prompt, string title = null, bool forceInput = false, bool destructive = false) : base(title, prompt, forceInput, destructive)
        {
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(600f, 500f);
    }
}

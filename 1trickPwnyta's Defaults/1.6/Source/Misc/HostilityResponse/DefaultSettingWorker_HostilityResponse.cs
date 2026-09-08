using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.Misc.HostilityResponse
{
    public class DefaultSettingWorker_HostilityResponse : DefaultSettingWorker_Dropdown<HostilityResponseMode?>
    {
        protected override HostilityResponseMode? Default => HostilityResponseMode.Flee;

        public override string Key => Settings.HOSTILITY_RESPONSE;

        protected override IEnumerable<HostilityResponseMode?> Options
        {
            get
            {
                foreach (HostilityResponseMode value in Enum.GetValues(typeof(HostilityResponseMode)))
                {
                    yield return value;
                }
            }
        }

        protected override Texture2D GetIcon(HostilityResponseMode? option) => option.Value.GetIcon();

        protected override TaggedString GetText(HostilityResponseMode? option) => option.Value.GetLabel();

        protected override TaggedString GetTip(HostilityResponseMode? option) => "Defaults_HostilityReponseTip".Translate() + "\n\n" + "HostilityResponseCurrentMode".Translate() + ": " + GetText(option);

        public DefaultSettingWorker_HostilityResponse(DefaultSettingDef def) : base(def)
        {
        }

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }
    }
}

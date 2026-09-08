using Defaults.Defs;
using Defaults.Workers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Medicine
{
    [StaticConstructorOnStartup]
    public abstract class DefaultSettingWorker_DefaultCareFor : DefaultSettingWorker_Segmented<MedicalCareCategory?>
    {
        private static readonly Texture2D[] careTextures = new[]
        {
            ContentFinder<Texture2D>.Get("UI/Icons/Medical/NoCare"),
            ContentFinder<Texture2D>.Get("UI/Icons/Medical/NoMeds"),
            ThingDefOf.MedicineHerbal.uiIcon,
            ThingDefOf.MedicineIndustrial.uiIcon,
            ThingDefOf.MedicineUltratech.uiIcon,
        };

        public DefaultSettingWorker_DefaultCareFor(DefaultSettingDef def) : base(def)
        {
        }

        protected override IEnumerable<MedicalCareCategory?> Options => Enum.GetValues(typeof(MedicalCareCategory)).Cast<MedicalCareCategory?>();

        protected override void ExposeSetting()
        {
            Scribe_Values.Look(ref setting, Key, Default);
        }

        protected override Texture2D GetIcon(MedicalCareCategory? option) => careTextures[(int)option];

        protected override TaggedString GetTip(MedicalCareCategory? option) => option.Value.GetLabel().CapitalizeFirst();
    }
}

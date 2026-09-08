using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using static Verse.Widgets;

namespace Defaults.AllowedAreas
{
    public class Dialog_ColorPickerAllowedArea : Dialog_ColorPickerBase
    {
        private readonly AllowedArea area;

        public Dialog_ColorPickerAllowedArea(AllowedArea area) : base(ColorComponents.None, ColorComponents.None)
        {
            this.area = area;
            color = oldColor = area.color;
        }

        protected override bool ShowDarklight => false;

        protected override Color DefaultColor => oldColor;

        protected override List<Color> PickableColors => typeof(Dialog_AllowedAreaColorPicker).Field("colors").GetValue(null) as List<Color>;

        protected override float ForcedColorValue => 0.5f;

        protected override bool ShowColorTemperatureBar => false;

        protected override void SaveColor(Color color)
        {
            area.color = color;
        }
    }
}

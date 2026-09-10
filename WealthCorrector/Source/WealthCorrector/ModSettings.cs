using UnityEngine;
using Verse;
using System.IO;
using System.Reflection;
using System;

namespace cn.zhuzijun.WealthCorrector
{
    public class ZModSettings : ModSettings
    {
        public static float wealthItemsRate = 100f;
        public static float wealthBuildingsRate = 100f;
        public static float wealthFloorsRate = 100f;
        public static float wealthPawnsRate = 100f;
        public static bool wealthIgnorePocketMap = true;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref wealthItemsRate, "wealthItemsRate", 100f, true);
            Scribe_Values.Look(ref wealthBuildingsRate, "wealthBuildingsRate", 100f, true);
            Scribe_Values.Look(ref wealthFloorsRate, "wealthFloorsRate", 100f, true);
            Scribe_Values.Look(ref wealthPawnsRate, "wealthPawnsRate", 100f, true);
            Scribe_Values.Look(ref wealthIgnorePocketMap, "wealthIgnorePocketMap", true, true);


        }

        string buffer1;
        string buffer2;
        string buffer3;
        string buffer4;
        public void DoWindowContents(Rect inRect)
        {
            var list = new Listing_Standard()
            {
                ColumnWidth = inRect.width
            };
            list.Begin(inRect);
            list.Label("wealthItemsRate".Translate());
            list.TextFieldNumeric(ref wealthItemsRate, ref buffer1, 0, 1000);

            list.Label("wealthBuildingsRate".Translate());
            list.TextFieldNumeric(ref wealthBuildingsRate, ref buffer2, 0, 1000);

            list.Label("wealthFloorsRate".Translate());
            list.TextFieldNumeric(ref wealthFloorsRate, ref buffer3, 0, 1000);

            list.Label("wealthPawnsRate".Translate());
            list.TextFieldNumeric(ref wealthPawnsRate, ref buffer4, 0, 1000);

            list.CheckboxLabeled("wealthIgnorePocketMap".Translate(),ref wealthIgnorePocketMap, "wealthIgnorePocketMapTips".Translate());

            list.End();
        }

    }
    public class ZMod : Mod
    {
        public static ZModSettings settings = new ZModSettings();

        public ZMod(ModContentPack content) : base(content)
        {
            Pack = content;
            settings = GetSettings<ZModSettings>();
        }

        public ModContentPack Pack { get; }

        public override string SettingsCategory() => Pack.Name;

        public override void DoSettingsWindowContents(Rect inRect) => settings.DoWindowContents(inRect);
    }
}

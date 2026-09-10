using Defaults.Defs;
using Defaults.UI;
using Defaults.Workers;
using RimWorld;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.Misc.StartingXenotype
{
    public class DefaultSettingWorker_StartingXenotype : DefaultSettingWorker<StartingXenotypeOptions>
    {
        public DefaultSettingWorker_StartingXenotype(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.STARTING_XENOTYPE_OPTIONS;

        protected override StartingXenotypeOptions Default => new StartingXenotypeOptions();

        protected override void DoWidget(Rect rect)
        {
            string text = "AnyNonArchite".Translate().CapitalizeFirst();
            Texture2D icon = null;
            TipSignal? tooltip = null;
            if (setting.Option == StartingXenotypeOption.XenotypeDef)
            {
                text = setting.XenotypeDef.LabelCap;
                icon = setting.XenotypeDef.Icon;
                tooltip = setting.XenotypeDef.descriptionShort ?? setting.XenotypeDef.description;
            }
            if (setting.Option == StartingXenotypeOption.CustomXenotype)
            {
                if (CharacterCardUtility.CustomXenotypesForReading.Contains(setting.CustomXenotype))
                {
                    text = $"{setting.CustomXenotype.name.CapitalizeFirst()} ({"Custom".Translate()})";
                    icon = setting.CustomXenotype.IconDef.Icon;
                }
                else
                {
                    setting.Option = StartingXenotypeOption.XenotypeDef;
                    setting.XenotypeDef = XenotypeDefOf.Baseliner;
                }
            }
            float minWidth = Text.CalcSize(text).x + rect.height + 16f;

            rect = rect.RightPartPixels(Mathf.Max(minWidth, 175f));
            if (UIUtility.DoImageTextButton(rect, icon, text))
            {
                DoMenu();
            }
            if (tooltip.HasValue)
            {
                TooltipHandler.TipRegion(rect, tooltip.Value);
            }
        }

        private void DoMenu()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>
            {
                new FloatMenuOption("AnyNonArchite".Translate(), () =>
                {
                    setting.Option = StartingXenotypeOption.AnyNonArchite;
                    setting.XenotypeDef = null;
                    setting.CustomXenotype = null;
                })
            };

            list.AddRange(DefDatabase<XenotypeDef>.AllDefsListForReading.OrderByDescending(x => x.displayPriority).Select(x => new FloatMenuOption(
                x.LabelCap,
                () =>
                {
                    setting.Option = StartingXenotypeOption.XenotypeDef;
                    setting.XenotypeDef = x;
                    setting.CustomXenotype = null;
                },
                x.Icon,
                XenotypeDef.IconColor,
                mouseoverGuiAction: r => TooltipHandler.TipRegion(r, x.descriptionShort ?? x.description),
                extraPartWidth: 24f,
                extraPartOnGUI: r => Widgets.InfoCardButton(r.x, r.y + 3f, x),
                extraPartRightJustified: true
            )));

            list.AddRange(CharacterCardUtility.CustomXenotypesForReading.Select(x => new FloatMenuOption(
                $"{x.name.CapitalizeFirst()} ({"Custom".Translate()})",
                () =>
                {
                    setting.Option = StartingXenotypeOption.CustomXenotype;
                    setting.XenotypeDef = null;
                    setting.CustomXenotype = x;
                },
                x.IconDef.Icon,
                XenotypeDef.IconColor,
                extraPartWidth: 24f,
                extraPartOnGUI: r =>
                {
                    if (Widgets.ButtonImage(new Rect(r.x, r.y + (r.height - r.width) / 2f, r.width, r.width), TexButton.Delete, Color.white))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmDelete".Translate(x.name.CapitalizeFirst()), () =>
                        {
                            string path = GenFilePaths.AbsFilePathForXenotype(x.name);
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                                CharacterCardUtility.cachedCustomXenotypes = null;
                            }
                        }, true));
                        return true;
                    }
                    return false;
                },
                extraPartRightJustified: true
            )));

            Find.WindowStack.Add(new FloatMenu(list));
        }

        public override void PreLoadSetting()
        {
            // Force CharacterCardUtility to load custom xenotypes before we load settings
            _ = CharacterCardUtility.CustomXenotypesForReading;
        }

        protected override void ExposeSetting()
        {
            Scribe_Deep.Look(ref setting, Settings.STARTING_XENOTYPE_OPTIONS);
        }
    }
}

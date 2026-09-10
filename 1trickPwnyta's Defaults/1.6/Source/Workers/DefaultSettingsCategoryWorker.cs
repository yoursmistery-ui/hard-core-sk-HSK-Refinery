using Defaults.Defs;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Defaults.Workers
{
    public abstract class DefaultSettingsCategoryWorker
    {
        private static readonly Color buttonColor = new Color(0.2f, 0.2f, 0.2f);
        private static readonly float buttonIconSize = 50f;
        private static readonly float buttonPadding = 10f;
        private static readonly Color disabledColor = Color.black.WithAlpha(0.5f);

        public DefaultSettingsCategoryDef def;
        public bool disabled;

        public static T GetWorker<T>() where T : DefaultSettingsCategoryWorker => DefDatabase<DefaultSettingsCategoryDef>.AllDefsListForReading.First(d => d.Worker is T).Worker as T;

        public DefaultSettingsCategoryWorker(DefaultSettingsCategoryDef def)
        {
            this.def = def;
            ResetCategorySettings(false);
        }

        protected virtual string DataPrefix => def.defName + ".";

        public bool WasEnabledAtStartup { get; private set; } = true;

        public virtual Texture2D Icon => def.Icon;

        public virtual IEnumerable<FloatMenuOption> FloatMenuOptions
        {
            get
            {
                if (def.canDisable)
                {
                    yield return new FloatMenuOption((disabled ? "Defaults_Enable" : "Defaults_Disable").Translate(), () =>
                    {
                        disabled = !disabled;
                    });
                }
            }
        }

        public virtual bool GetSetting<T>(string key, out T value)
        {
            if (GetCategorySetting(key, out object o))
            {
                value = (T)o;
                return true;
            }

            foreach (DefaultSettingDef def in def.DefaultSettings)
            {
                if (def.Worker.Key == key)
                {
                    if (def.Worker is DefaultSettingWorker<T> worker)
                    {
                        value = worker.setting;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        protected virtual bool GetCategorySetting(string key, out object value)
        {
            value = default;
            return false;
        }

        public virtual bool SetSetting<T>(string key, T value)
        {
            if (SetCategorySetting(key, value))
            {
                return true;
            }

            foreach (DefaultSettingDef def in def.DefaultSettings)
            {
                if (def.Worker.Key == key)
                {
                    (def.Worker as DefaultSettingWorker<T>).setting = value;
                    return true;
                }
            }

            return false;
        }

        protected virtual bool SetCategorySetting(string key, object value) => false;

        public virtual void HandleNewDefs(IEnumerable<Def> defs)
        {
        }

        protected virtual void PreLoadCategory() { }

        public void PreLoad()
        {
            PreLoadCategory();
            foreach (DefaultSettingDef def in def.DefaultSettings)
            {
                def.Worker.PreLoadSetting();
            }
        }

        protected virtual void ExposeCategorySettings()
        {
        }

        protected virtual void PostExposeData()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref disabled, DataPrefix + "disabled");
            ExposeCategorySettings();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ResetCategorySettings(false);
                WasEnabledAtStartup = !disabled;
            }
            foreach (DefaultSettingDef def in def.DefaultSettings)
            {
                def.Worker.ExposeData();
            }
            PostExposeData();
        }

        protected virtual void ResetCategorySettings(bool forced)
        {
        }

        public void ResetSettings()
        {
            disabled = false;
            ResetCategorySettings(true);
            foreach (DefaultSettingDef def in def.DefaultSettings)
            {
                def.Worker.ResetSetting(true);
            }
        }

        public abstract void OpenSettings();

        public virtual float AdditionalSettingsDialogWidth => 500f;

        public void DoButton(Rect rect)
        {
            Widgets.DrawRectFast(rect, buttonColor);
            Widgets.DrawHighlightIfMouseover(rect);
            Rect iconRect = new Rect(rect.x + (rect.width - buttonIconSize) / 2, rect.y + buttonPadding, buttonIconSize, buttonIconSize);
            Widgets.DrawTextureFitted(iconRect, Icon, 1f);
            Rect labelRect = new Rect(rect.x + buttonPadding, rect.y + buttonPadding + buttonIconSize + buttonPadding, rect.width - buttonPadding * 2, rect.height - buttonPadding - buttonIconSize - buttonPadding - buttonPadding);
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(labelRect, def.LabelCap);
            Text.Anchor = default;
            TaggedString tip = def.description + (disabled
                ? "\n\n" + "Defaults_SettingsCategoryDisabled".Translate(def.LabelCap).Colorize(ColoredText.WarningColor)
                : string.Empty
            );
            TooltipHandler.TipRegion(rect, tip);
            MouseoverSounds.DoRegion(rect);
            if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseUp)
            {
                if (Event.current.button == 0)
                {
                    OpenSettings();
                }
                else if (Event.current.button == 1)
                {
                    List<FloatMenuOption> options = FloatMenuOptions.ToList();
                    if (options.Any())
                    {
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                }
            }
            if (disabled)
            {
                Widgets.DrawRectFast(rect, disabledColor);
            }
        }

        public virtual void Notify_FirstSpawnAnywhere(Pawn pawn)
        {
            foreach (DefaultSettingDef def in def.DefaultSettings)
            {
                def.Worker.Notify_FirstSpawnAnywhere(pawn);
            }
        }

        public virtual void Notify_FirstSpawnOnMap(Pawn pawn, Map map)
        {
            foreach (DefaultSettingDef def in def.DefaultSettings)
            {
                def.Worker.Notify_FirstSpawnOnMap(pawn, map);
            }
        }
    }
}

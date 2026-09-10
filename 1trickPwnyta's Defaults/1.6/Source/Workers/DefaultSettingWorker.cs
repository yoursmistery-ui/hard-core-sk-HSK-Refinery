using Defaults.Defs;
using UnityEngine;
using Verse;

namespace Defaults.Workers
{
    public interface IDefaultSettingWorker
    {
        string Key { get; }

        bool RenderLast { get; }

        DefaultSettingDef Def { set; }

        void PreLoadSetting();

        void ExposeData();

        void ResetSetting(bool forced);

        void DoSetting(Rect rect);

        void Notify_FirstSpawnAnywhere(Pawn pawn);

        void Notify_FirstSpawnOnMap(Pawn pawn, Map map);
    }

    public abstract class DefaultSettingWorker<T> : IDefaultSettingWorker
    {
        public DefaultSettingDef def;
        public T setting;

        public DefaultSettingWorker(DefaultSettingDef def)
        {
            this.def = def;
            ResetSetting(false);
        }

        public abstract string Key { get; }

        public virtual bool RenderLast => false;

        public DefaultSettingDef Def
        {
            set => def = value;
        }

        protected abstract void DoWidget(Rect rect);

        public void DoSetting(Rect rect)
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(rect, def.LabelCap);
            }
            DoWidget(rect);
        }

        public virtual void PreLoadSetting() { }

        protected abstract void ExposeSetting();

        public void ExposeData()
        {
            ExposeSetting();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ResetSetting(false);
            }
        }

        protected abstract T Default { get; }

        public void ResetSetting(bool forced)
        {
            if (forced || setting == null)
            {
                setting = Default;
            }
        }

        public virtual void Notify_FirstSpawnAnywhere(Pawn pawn) { }

        public virtual void Notify_FirstSpawnOnMap(Pawn pawn, Map map) { }
    }
}

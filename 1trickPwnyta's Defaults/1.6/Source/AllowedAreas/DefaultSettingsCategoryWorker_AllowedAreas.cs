using Defaults.Defs;
using Defaults.Workers;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Defaults.AllowedAreas
{
    public class DefaultSettingsCategoryWorker_AllowedAreas : DefaultSettingsCategoryWorker
    {
        private static AllowedArea homeArea;

        public static AllowedArea HomeArea
        {
            get
            {
                if (homeArea == null)
                {
                    homeArea = new AllowedArea()
                    {
                        name = "Home".Translate(),
                        color = new Color(0.3f, 0.3f, 0.9f)
                    };
                }
                return homeArea;
            }
        }

        private List<AllowedArea> defaultAllowedAreas;
        private Dictionary<PawnType, AllowedArea> defaultPawnAllowedAreas;

        private List<PawnType> allowedPawnWorkingList;
        private List<AllowedArea> allowedAreaWorkingList;

        public DefaultSettingsCategoryWorker_AllowedAreas(DefaultSettingsCategoryDef def) : base(def)
        {
        }

        public override void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_AllowedAreas(def));
        }

        protected override bool GetCategorySetting(string key, out object value)
        {
            switch (key)
            {
                case Settings.ALLOWED_AREAS:
                    value = defaultAllowedAreas;
                    return true;
                case Settings.ALLOWED_AREAS_PAWN:
                    value = defaultPawnAllowedAreas;
                    return true;
                default:
                    return base.GetCategorySetting(key, out value);
            }
        }

        protected override bool SetCategorySetting(string key, object value)
        {
            switch (key)
            {
                case Settings.ALLOWED_AREAS:
                    defaultAllowedAreas = value as List<AllowedArea>;
                    return true;
                case Settings.ALLOWED_AREAS_PAWN:
                    defaultPawnAllowedAreas = value as Dictionary<PawnType, AllowedArea>;
                    return true;
                default:
                    return base.SetCategorySetting(key, value);
            }
        }

        protected override void ResetCategorySettings(bool forced)
        {
            if (forced || defaultAllowedAreas == null)
            {
                defaultAllowedAreas = new List<AllowedArea>() { new AllowedArea("AreaDefaultLabel".Translate(1)) };
            }
            if (forced || defaultPawnAllowedAreas == null)
            {
                defaultPawnAllowedAreas = new Dictionary<PawnType, AllowedArea>();
            }
        }

        protected override void ExposeCategorySettings()
        {
            Scribe_Deep.Look(ref homeArea, "homeArea");
            Scribe_Collections.Look(ref defaultAllowedAreas, "defaultAllowedAreas", LookMode.Deep);
            Scribe_Collections.Look(ref defaultPawnAllowedAreas, "defaultPawnAllowedAreas", LookMode.Value, LookMode.Reference, ref allowedPawnWorkingList, ref allowedAreaWorkingList);
        }

        public override void Notify_FirstSpawnOnMap(Pawn pawn, Map map)
        {
            base.Notify_FirstSpawnOnMap(pawn, map);
            AllowedAreaUtility.SetDefaultAllowedArea(pawn);
        }
    }
}

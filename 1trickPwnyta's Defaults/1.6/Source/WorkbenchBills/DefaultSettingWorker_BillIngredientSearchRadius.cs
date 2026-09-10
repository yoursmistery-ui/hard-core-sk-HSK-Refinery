using Defaults.Defs;
using Defaults.Workers;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class DefaultSettingWorker_BillIngredientSearchRadius : DefaultSettingWorker_Slider<float>
    {
        public DefaultSettingWorker_BillIngredientSearchRadius(DefaultSettingDef def) : base(def)
        {
        }

        public override string Key => Settings.BILL_INGREDIENT_SEARCH_RADIUS;

        protected override float Min => 3f;

        protected override float Max => 100f;

        protected override float? Default => 100f;

        protected override float GetNumber(float value) => value;

        protected override float GetValue(float value) => value;

        protected override string MiddleLabel => setting < Max ? setting.Value.ToString("F0") : "Unlimited".TranslateSimple();
    }
}

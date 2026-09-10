using RimWorld;
using Verse;

namespace Defaults.WorkbenchBills
{
    public class BillTemplate : IExposable, IRenameable
    {
        public string name;
        public bool use = true;
        public bool locked = false;
        public RecipeDef recipe;
        public ThingFilter ingredientFilter;
        public float ingredientSearchRadius = DefaultsSettings.DefaultBillIngredientSearchRadius;
        public IntRange allowedSkillRange = DefaultsSettings.DefaultBillAllowedSkillRange;
        public bool slavesOnly = false;
        public bool mechsOnly = false;
        public bool nonMechsOnly = false;
        public BillRepeatModeDef repeatMode = BillRepeatModeDefOf.RepeatCount;
        public int repeatCount = 1;
        public BillStoreModeDef storeMode = DefaultsSettings.DefaultBillStoreMode;
        public int targetCount = 10;
        public bool pauseWhenSatisfied = false;
        public int unpauseWhenYouHave = 5;
        public bool includeEquipped = false;
        public bool includeTainted = false;
        public FloatRange hpRange = FloatRange.ZeroToOne;
        public QualityRange qualityRange = QualityRange.All;
        public bool limitToAllowedStuff = false;

        public string RenamableLabel { get => name; set => name = value; }

        public string BaseLabel => RenamableLabel;

        public string InspectLabel => RenamableLabel;

        private BillTemplate() { }

        public BillTemplate(RecipeDef recipe)
        {
            name = recipe.LabelCap;
            this.recipe = recipe;
            ingredientFilter = new ThingFilter();
            if (recipe.fixedIngredientFilter != null)
            {
                ingredientFilter.CopyAllowancesFrom(recipe.fixedIngredientFilter);
            }
        }

        public Bill ToBill()
        {
            // Disable bill constructor patch since we will be setting those parameters here and don't want to patch over any values set by another mod (such as EndlessGrowth)
            Patch_Bill_Production_ctor.Enabled = false;
            Bill bill = recipe.MakeNewBill();
            Patch_Bill_Production_ctor.Enabled = true;

            bill.ingredientFilter.CopyAllowancesFrom(ingredientFilter);
            bill.ingredientSearchRadius = ingredientSearchRadius;

            bill.allowedSkillRange.min = allowedSkillRange.min;
            // To support mods that increase max skill level, count 20 as unlimited and therefore do not change the max
            if (allowedSkillRange.max < 20)
            {
                bill.allowedSkillRange.max = allowedSkillRange.max;
            }

            if (slavesOnly)
            {
                bill.SetAnySlaveRestriction();
            }
            if (mechsOnly)
            {
                bill.SetAnyMechRestriction();
            }
            if (nonMechsOnly)
            {
                bill.SetAnyNonMechRestriction();
            }
            Bill_Production productionBill = bill as Bill_Production;
            if (productionBill != null)
            {
                productionBill.repeatMode = repeatMode;
                productionBill.repeatCount = repeatCount;
                productionBill.SetStoreMode(storeMode);
                productionBill.targetCount = targetCount;
                productionBill.pauseWhenSatisfied = pauseWhenSatisfied;
                productionBill.unpauseWhenYouHave = unpauseWhenYouHave;
                productionBill.includeEquipped = includeEquipped;
                productionBill.includeTainted = includeTainted;
                productionBill.hpRange = hpRange;
                productionBill.qualityRange = qualityRange;
                productionBill.limitToAllowedStuff = limitToAllowedStuff;
            }
            
            return bill;
        }

        public static BillTemplate FromBill(Bill bill)
        {
            BillTemplate template = new BillTemplate(bill.recipe);
            template.ingredientFilter = new ThingFilter();
            template.ingredientFilter.CopyAllowancesFrom(bill.ingredientFilter);
            template.ingredientSearchRadius = bill.ingredientSearchRadius;
            template.allowedSkillRange = bill.allowedSkillRange;
            template.slavesOnly = bill.SlavesOnly;
            template.mechsOnly = bill.MechsOnly;
            template.nonMechsOnly = bill.NonMechsOnly;
            Bill_Production productionBill = bill as Bill_Production;
            if (productionBill != null)
            {
                template.repeatMode = productionBill.repeatMode;
                template.repeatCount = productionBill.repeatCount;
                template.storeMode = productionBill.GetStoreMode();
                if (template.storeMode == BillStoreModeDefOf.SpecificStockpile)
                {
                    template.storeMode = BillStoreModeDefOf.BestStockpile;
                }
                template.targetCount = productionBill.targetCount;
                template.pauseWhenSatisfied = productionBill.pauseWhenSatisfied;
                template.unpauseWhenYouHave = productionBill.unpauseWhenYouHave;
                template.includeEquipped = productionBill.includeEquipped;
                template.includeTainted = productionBill.includeTainted;
                template.hpRange = productionBill.hpRange;
                template.qualityRange = productionBill.qualityRange;
                template.limitToAllowedStuff = productionBill.limitToAllowedStuff;
            }

            return template;
        }

        public void SetAnyPawnRestriction()
        {
            slavesOnly = false;
            mechsOnly = false;
            nonMechsOnly = false;
        }

        public void SetAnySlaveRestriction()
        {
            slavesOnly = true;
            mechsOnly = false;
            nonMechsOnly = false;
        }

        public void SetAnyMechRestriction()
        {
            slavesOnly = false;
            mechsOnly = true;
            nonMechsOnly = false;
        }

        public void SetAnyNonMechRestriction()
        {
            slavesOnly = false;
            mechsOnly = false;
            nonMechsOnly = true;
        }

        public BillTemplate Clone()
        {
            BillTemplate clone = new BillTemplate(recipe);
            clone.use = use;
            clone.locked = locked;
            clone.ingredientFilter = new ThingFilter();
            clone.ingredientFilter.CopyAllowancesFrom(ingredientFilter);
            clone.ingredientSearchRadius = ingredientSearchRadius;
            clone.allowedSkillRange = allowedSkillRange;
            clone.slavesOnly = slavesOnly;
            clone.mechsOnly = mechsOnly;
            clone.nonMechsOnly = nonMechsOnly;
            clone.repeatMode = repeatMode;
            clone.repeatCount = repeatCount;
            clone.storeMode = storeMode;
            clone.targetCount = targetCount;
            clone.pauseWhenSatisfied = pauseWhenSatisfied;
            clone.unpauseWhenYouHave = unpauseWhenYouHave;
            clone.includeEquipped = includeEquipped;
            clone.includeTainted = includeTainted;
            clone.hpRange = hpRange;
            clone.qualityRange = qualityRange;
            clone.limitToAllowedStuff = limitToAllowedStuff;
            return clone;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref use, "use", true);
            Scribe_Values.Look(ref locked, "locked", false);
            Scribe_Defs.Look(ref recipe, "recipe");
            Scribe_Deep.Look(ref ingredientFilter, "ingredientFilter");
            Scribe_Values.Look(ref ingredientSearchRadius, "ingredientSearchRadius");
            Scribe_Values.Look(ref allowedSkillRange, "allowedSkillRange");
            Scribe_Values.Look(ref slavesOnly, "slavesOnly");
            Scribe_Values.Look(ref mechsOnly, "mechsOnly");
            Scribe_Values.Look(ref nonMechsOnly, "nonMechsOnly");
            Scribe_Defs.Look(ref repeatMode, "repeatMode");
            Scribe_Values.Look(ref repeatCount, "repeatCount");
            Scribe_Defs.Look(ref storeMode, "storeMode");
            Scribe_Values.Look(ref targetCount, "targetCount");
            Scribe_Values.Look(ref pauseWhenSatisfied, "pauseWhenSatisfied");
            Scribe_Values.Look(ref unpauseWhenYouHave, "unpauseWhenYouHave");
            Scribe_Values.Look(ref includeEquipped, "includeEquipped");
            Scribe_Values.Look(ref includeTainted, "includeTainted");
            Scribe_Values.Look(ref hpRange, "hpRange");
            Scribe_Values.Look(ref qualityRange, "qualityRange");
            Scribe_Values.Look(ref limitToAllowedStuff, "limitToAllowedStuff");
        }
    }
}

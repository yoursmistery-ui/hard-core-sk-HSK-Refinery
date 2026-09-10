using Defaults.Compatibility;
using Defaults.Defs;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
        public float ingredientSearchRadius = Settings.GetValue<float>(Settings.BILL_INGREDIENT_SEARCH_RADIUS);
        public IntRange allowedSkillRange = Settings.GetValue<IntRange>(Settings.BILL_ALLOWED_SKILL_RANGE);
        public bool slavesOnly = false;
        public bool mechsOnly = false;
        public bool nonMechsOnly = false;
        public BillRepeatModeDef repeatMode = BillRepeatModeDefOf.RepeatCount;
        public int repeatCount = 1;
        public BillStoreModeDef storeMode = Settings.Get<BillStoreModeDef>(Settings.BILL_STORE_MODE);
        public int targetCount = 10;
        public bool pauseWhenSatisfied = false;
        public int unpauseWhenYouHave = 5;
        public bool includeEquipped = false;
        public bool includeTainted = false;
        public FloatRange hpRange = FloatRange.ZeroToOne;
        public QualityRange qualityRange = QualityRange.All;
        public bool limitToAllowedStuff = false;
        public BetterWorkbenchOptions betterWorkbenchOptions = new BetterWorkbenchOptions();

        public string RenamableLabel { get => name; set => name = value; }

        public string BaseLabel => RenamableLabel;

        public string InspectLabel => RenamableLabel;

        private BillTemplate()
        {
            if (ingredientSearchRadius >= 100f)
            {
                ingredientSearchRadius = 999f;
            }
        }

        public BillTemplate(RecipeDef recipe) : this()
        {
            name = recipe.LabelCap;
            this.recipe = recipe;
            ingredientFilter = new ThingFilter();
            if (recipe.defaultIngredientFilter != null)
            {
                ingredientFilter.CopyAllowancesFrom(recipe.defaultIngredientFilter);
            }
            else
            {
                ingredientFilter.CopyAllowancesFrom(recipe.fixedIngredientFilter);
            }
        }

        public Bill ToBill()
        {
            // Disable bill constructor patch since we will be setting those parameters here and don't want to patch over any values set by another mod (such as EndlessGrowth)
            Patch_Bill_Production_ctor.enabled = false;
            Bill bill = recipe.MakeNewBill();
            Patch_Bill_Production_ctor.enabled = true;

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
            if (bill is Bill_Production productionBill)
            {
                productionBill.RenamableLabel = name;
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
                ModCompatibilityUtility_BetterWorkbench.ApplyBetterWorkbenchOptions(betterWorkbenchOptions, productionBill);
            }

            return bill;
        }

        public static BillTemplate FromBill(Bill bill)
        {
            BillTemplate template = new BillTemplate(bill.recipe) { ingredientFilter = new ThingFilter() };
            template.ingredientFilter.CopyAllowancesFrom(bill.ingredientFilter);
            template.ingredientSearchRadius = bill.ingredientSearchRadius;
            template.allowedSkillRange = bill.allowedSkillRange;
            template.slavesOnly = bill.SlavesOnly;
            template.mechsOnly = bill.MechsOnly;
            template.nonMechsOnly = bill.NonMechsOnly;
            if (bill is Bill_Production productionBill)
            {
                template.name = productionBill.RenamableLabel;
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
                ModCompatibilityUtility_BetterWorkbench.SetBetterWorkbenchOptions(template.betterWorkbenchOptions, productionBill);
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
            BillTemplate clone = new BillTemplate(recipe)
            {
                use = use,
                locked = locked,
                ingredientFilter = new ThingFilter(),
                ingredientSearchRadius = ingredientSearchRadius,
                allowedSkillRange = allowedSkillRange,
                slavesOnly = slavesOnly,
                mechsOnly = mechsOnly,
                nonMechsOnly = nonMechsOnly,
                repeatMode = repeatMode,
                repeatCount = repeatCount,
                storeMode = storeMode,
                targetCount = targetCount,
                pauseWhenSatisfied = pauseWhenSatisfied,
                unpauseWhenYouHave = unpauseWhenYouHave,
                includeEquipped = includeEquipped,
                includeTainted = includeTainted,
                hpRange = hpRange,
                qualityRange = qualityRange,
                limitToAllowedStuff = limitToAllowedStuff,
                betterWorkbenchOptions = betterWorkbenchOptions.Clone()
            };
            clone.ingredientFilter.CopyAllowancesFrom(ingredientFilter);
            return clone;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref use, "use", true);
            Scribe_Values.Look(ref locked, "locked", false);
            Scribe_Defs_Silent.Look(ref recipe, "recipe");
            Scribe_Deep.Look(ref ingredientFilter, "ingredientFilter");
            Scribe_Values.Look(ref ingredientSearchRadius, "ingredientSearchRadius");
            Scribe_Values.Look(ref allowedSkillRange, "allowedSkillRange");
            Scribe_Values.Look(ref slavesOnly, "slavesOnly");
            Scribe_Values.Look(ref mechsOnly, "mechsOnly");
            Scribe_Values.Look(ref nonMechsOnly, "nonMechsOnly");
            Scribe_Defs_Silent.Look(ref repeatMode, "repeatMode");
            Scribe_Values.Look(ref repeatCount, "repeatCount");
            Scribe_Defs_Silent.Look(ref storeMode, "storeMode");
            Scribe_Values.Look(ref targetCount, "targetCount");
            Scribe_Values.Look(ref pauseWhenSatisfied, "pauseWhenSatisfied");
            Scribe_Values.Look(ref unpauseWhenYouHave, "unpauseWhenYouHave");
            Scribe_Values.Look(ref includeEquipped, "includeEquipped");
            Scribe_Values.Look(ref includeTainted, "includeTainted");
            Scribe_Values.Look(ref hpRange, "hpRange");
            Scribe_Values.Look(ref qualityRange, "qualityRange");
            Scribe_Values.Look(ref limitToAllowedStuff, "limitToAllowedStuff");
            ModCompatibilityUtility_BetterWorkbench.WriteBetterWorkbenchOptions(ref betterWorkbenchOptions);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (repeatMode == null)
                {
                    repeatMode = BillRepeatModeDefOf.RepeatCount;
                }
                if (storeMode == null)
                {
                    storeMode = BillStoreModeDefOf.BestStockpile;
                }

                // Clean up ThingDefs and SpecialThingFilterDefs that were added when they shouldn't have been
                List<SpecialThingFilterDef> disallowedSpecialFilters = typeof(ThingFilter).Field("disallowedSpecialFilters").GetValue(ingredientFilter) as List<SpecialThingFilterDef>;
                HashSet<SpecialThingFilterDef> allSpecialThingFilters = recipe.GetAllSpecialThingFilterDefs().ToHashSet();
                disallowedSpecialFilters.RemoveWhere(f => (!f.configurable && f.allowedByDefault) || !allSpecialThingFilters.Contains(f));
                foreach (ThingDef def in ingredientFilter.AllowedThingDefs.ToList())
                {
                    if (def != null && !recipe.fixedIngredientFilter.Allows(def))
                    {
                        ingredientFilter.SetAllow(def, false);
                    }
                }
            }
        }
    }
}

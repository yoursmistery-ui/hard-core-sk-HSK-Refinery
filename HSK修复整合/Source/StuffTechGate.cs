// 材料科技档硬门控(StuffTechGate,并入 HSK 修复整合)
//
// 规则(2026-09-02 用户确认): 材料档 ≥ 物品档——太空物品必须太空档(A级)材料,
// 工业物品可用工业档(B级)及以上,中世纪及以下物品不限材料;金属按物品档取最低档。
// 仅门控 Fabric / Metallic 两类 stuff(皮革 E~A+ 是品质分级、木质等其他类别不参与);
// 远程武器与 Ludeon 原版(Core/DLC)物品不参与。
//
// 机制依据(反编译 RimWorld 1.6): recipeMaker 自动配方的 fixedIngredientFilter 由
// RecipeDefGenerator.SetIngredients -> ThingFilter.SetAllowAllWhoCanMake 生成,只按
// stuff 大类放行;defaultIngredientFilter 只是账单默认(可被玩家勾选放开),纯 XML 无法
// 对自动配方硬门控。故在 defs 全部加载后统一收紧 fixedIngredientFilter + ingredients[].filter
// + defaultIngredientFilter——账单 UI 只能在 fixedIngredientFilter 边界内勾选,实现硬门控。
// 材料档位来自 Defs/StuffTechTiers.xml(缺省回退 stuff 自身 techLevel,均无则放行并告警)。
//
// 启动期一次性执行,无 Tick 开销。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace HSKFixPack
{
    public class StuffTechTierEntry
    {
        public string stuff;
        public TechLevel tier;
    }

    public class StuffTechTierListDef : Def
    {
        public List<StuffTechTierEntry> entries = new List<StuffTechTierEntry>();
    }

    [StaticConstructorOnStartup]
    public static class StuffTechGate
    {
        private static readonly HashSet<string> GatedCategories = new HashSet<string> { "Fabric", "Metallic" };

        private static readonly HashSet<string> LudeonMods = new HashSet<string>
            { "Core", "Royalty", "Ideology", "Biotech", "Anomaly", "Odyssey" };

        static StuffTechGate()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Log.Error("[HSKFixPack] 材料科技档门控失败: " + e);
            }
        }

        private static void Run()
        {
            var tierByStuff = new Dictionary<string, TechLevel>();
            var listDef = DefDatabase<StuffTechTierListDef>.GetNamed("RK_StuffTechTiers", false);
            if (listDef != null && listDef.entries != null)
            {
                foreach (var e in listDef.entries)
                {
                    if (!string.IsNullOrEmpty(e.stuff))
                    {
                        tierByStuff[e.stuff] = e.tier;
                    }
                }
            }

            var gated = new List<ThingDef>();
            var unknown = new List<string>();
            foreach (var t in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var sp = t.stuffProps;
                var cats = sp != null ? sp.categories : null;
                if (cats == null || !cats.Any(c => c != null && GatedCategories.Contains(c.defName)))
                {
                    continue;
                }
                if (tierByStuff.ContainsKey(t.defName) || t.techLevel != TechLevel.Undefined)
                {
                    gated.Add(t);
                }
                else
                {
                    unknown.Add(t.defName);
                }
            }
            foreach (var u in unknown)
            {
                Log.Warning("[HSKFixPack] 材料科技档门控: " + u + " 无档位(映射表与 def 均未定义),不参与门控(放行)");
            }

            int recipeCount = 0;
            var gatedItems = new List<string>();
            foreach (var r in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (r.products.Count != 1)
                {
                    continue;
                }
                var def = r.products[0].thingDef;
                if (def == null || !def.MadeFromStuff || def.stuffCategories == null)
                {
                    continue;
                }
                if (!def.stuffCategories.Any(c => GatedCategories.Contains(c.defName)))
                {
                    continue;
                }
                var pack = def.modContentPack;
                if (pack == null || pack.IsCoreMod || LudeonMods.Contains(pack.Name))
                {
                    continue;
                }
                if (!def.IsApparel && !IsMeleeWeapon(def))
                {
                    continue;
                }
                var itemTier = ResolveItemTier(def, r);
                if (itemTier == TechLevel.Undefined || itemTier <= TechLevel.Neolithic)
                {
                    continue;
                }

                bool changed = false;
                foreach (var s in gated)
                {
                    TechLevel st;
                    TechLevel mapped;
                    if (tierByStuff.TryGetValue(s.defName, out mapped))
                    {
                        st = mapped;
                    }
                    else
                    {
                        st = s.techLevel;
                    }
                    if (st >= itemTier)
                    {
                        continue;
                    }
                    bool touched = false;
                    if (r.fixedIngredientFilter != null && r.fixedIngredientFilter.Allows(s))
                    {
                        r.fixedIngredientFilter.SetAllow(s, false);
                        touched = true;
                    }
                    if (r.ingredients != null)
                    {
                        foreach (var ing in r.ingredients)
                        {
                            if (ing != null && ing.filter != null && ing.filter.Allows(s))
                            {
                                ing.filter.SetAllow(s, false);
                                touched = true;
                            }
                        }
                    }
                    if (r.defaultIngredientFilter != null && r.defaultIngredientFilter.Allows(s))
                    {
                        r.defaultIngredientFilter.SetAllow(s, false);
                        touched = true;
                    }
                    if (touched)
                    {
                        changed = true;
                    }
                }
                if (changed)
                {
                    recipeCount++;
                    gatedItems.Add(def.defName);
                }
            }

            var sb = new StringBuilder();
            sb.Append("[HSKFixPack] 材料科技档门控完成: 门控材料 ").Append(gated.Count);
            sb.Append(" 种, 收紧配方 ").Append(recipeCount).Append(" 条");
            if (unknown.Count > 0)
            {
                sb.Append(", 无档位放行 ").Append(unknown.Count).Append(" 种(见上方告警)");
            }
            Log.Message(sb.ToString());
        }

        private static TechLevel ResolveItemTier(ThingDef def, RecipeDef r)
        {
            if (def.techLevel != TechLevel.Undefined)
            {
                return def.techLevel;
            }
            if (r.researchPrerequisite != null && r.researchPrerequisite.techLevel != TechLevel.Undefined)
            {
                return r.researchPrerequisite.techLevel;
            }
            if (r.researchPrerequisites != null)
            {
                foreach (var rp in r.researchPrerequisites)
                {
                    if (rp != null && rp.techLevel != TechLevel.Undefined)
                    {
                        return rp.techLevel;
                    }
                }
            }
            return TechLevel.Undefined;
        }

        private static bool IsMeleeWeapon(ThingDef def)
        {
            bool ranged = def.Verbs != null && def.Verbs.Any(v => v != null && v.defaultProjectile != null);
            if (ranged)
            {
                return false;
            }
            return (def.tools != null && def.tools.Count > 0)
                || (def.weaponTags != null && def.weaponTags.Count > 0);
        }
    }
}

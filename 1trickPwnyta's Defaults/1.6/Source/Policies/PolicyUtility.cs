using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Defaults.Policies
{
    public struct PolicyParms
    {
        public string NameKey;
        public IList VanillaPolicies;
        public IList DefaultPolicies;
    }

    public static class PolicyUtility
    {
        private static readonly Dictionary<Type, PolicyParms> policyParms = new Dictionary<Type, PolicyParms>()
        {
            {
                typeof(ApparelPolicy), new PolicyParms()
                {
                    NameKey = "ApparelPolicy",
                    VanillaPolicies = VanillaPolicyStore.VanillaApparelPolicies,
                    DefaultPolicies = Settings.Get<List<ApparelPolicy>>(Settings.POLICIES_APPAREL)
                }
            },
            {
                typeof(FoodPolicy), new PolicyParms()
                {
                    NameKey = "FoodPolicy",
                    VanillaPolicies = VanillaPolicyStore.VanillaFoodPolicies,
                    DefaultPolicies = Settings.Get<List<FoodPolicy>>(Settings.POLICIES_FOOD)
                }
            },
            {
                typeof(DrugPolicy), new PolicyParms()
                {
                    NameKey = "DrugPolicy",
                    VanillaPolicies = VanillaPolicyStore.VanillaDrugPolicies,
                    DefaultPolicies = Settings.Get<List<DrugPolicy>>(Settings.POLICIES_DRUG)
                }
            },
            {
                typeof(ReadingPolicy), new PolicyParms()
                {
                    NameKey = "ReadingPolicy",
                    VanillaPolicies = VanillaPolicyStore.VanillaReadingPolicies,
                    DefaultPolicies = Settings.Get<List<ReadingPolicy>>(Settings.POLICIES_READING)
                }
            }
        };

        public static bool IsLocked(this Policy policy) => !Settings.Get<HashSet<Policy>>(Settings.UNLOCKED_POLICIES).Contains(policy);

        public static void SetLocked(this Policy policy, bool value)
        {
            if (policy.IsLockable())
            {
                HashSet<Policy> unlockedPolicies = Settings.Get<HashSet<Policy>>(Settings.UNLOCKED_POLICIES);
                if (value)
                {
                    unlockedPolicies.Remove(policy);
                }
                else
                {
                    unlockedPolicies.Add(policy);
                }
            }
        }

        public static bool IsLockable(this Policy policy) => policy is ApparelPolicy || policy is FoodPolicy || policy is ReadingPolicy;

        public static T NewPolicy<T>(Dialog_ManagePolicies<T> dialog, string name = null) where T : Policy => (T)NewPolicy(typeof(T), dialog, name);

        public static Policy NewPolicy(Type type, Window dialog, string name = null)
        {
            IList defaultPolicies = (IList)dialog.GetType().Method("GetPolicies").Invoke(dialog, new object[] { });
            name = GetAvailableNameForPolicy(name, policyParms[type].NameKey, defaultPolicies);
            Policy policy = (Policy)dialog.GetType().Method("CreateNewPolicy").Invoke(dialog, new object[] { });
            policy.label = name;
            return policy;
        }

        public static T NewDefaultPolicy<T>(string name = null) where T : Policy
        {
            List<T> defaultPolicies = GetDefaultPolicies<T>();
            name = GetAvailableNameForPolicy(name, policyParms[typeof(T)].NameKey, defaultPolicies);
            T policy = (T)Activator.CreateInstance(typeof(T), new object[] { 0, name });
            defaultPolicies.Add(policy);
            return policy;
        }

        public static Policy NewDefaultPolicy(Type type, string name = null)
        {
            IList defaultPolicies = policyParms[type].DefaultPolicies;
            name = GetAvailableNameForPolicy(name, policyParms[type].NameKey, defaultPolicies);
            Policy policy = (Policy)Activator.CreateInstance(type, new object[] { 0, name });
            defaultPolicies.Add(policy);
            return policy;
        }

        public static List<T> GetVanillaPolicies<T>() => GetVanillaPolicies(typeof(T)) as List<T>;

        public static IList GetVanillaPolicies(Type type) => policyParms[type].VanillaPolicies;

        public static List<T> GetDefaultPolicies<T>() => GetDefaultPolicies(typeof(T)) as List<T>;

        public static IList GetDefaultPolicies(Type type) => policyParms[type].DefaultPolicies;

        private static string GetAvailableNameForPolicy(string preferredName, string nameKey, IList existingPolicies)
        {
            string name = preferredName;
            int i = existingPolicies.Count + 1;
            while (name == null || existingPolicies.Cast<Policy>().Any(p => p.label == name))
            {
                name = nameKey.Translate() + " " + i++;
            }
            return name;
        }

        public static bool IsGamePolicyDialog(this Window window) =>
            window.GetType() == typeof(Dialog_ManageApparelPolicies)
            || window.GetType() == typeof(Dialog_ManageFoodPolicies)
            || window.GetType() == typeof(Dialog_ManageDrugPolicies)
            || window.GetType() == typeof(Dialog_ManageReadingPolicies);

        public static T GetDefaultPolicy<T>(this Game game) where T : Policy =>
            typeof(T) == typeof(ApparelPolicy) ? game.outfitDatabase.DefaultOutfit() as T
            : typeof(T) == typeof(FoodPolicy) ? game.foodRestrictionDatabase.DefaultFoodRestriction() as T
            : typeof(T) == typeof(DrugPolicy) ? game.drugPolicyDatabase.DefaultDrugPolicy() as T
            : typeof(T) == typeof(ReadingPolicy) ? game.readingPolicyDatabase.DefaultReadingPolicy() as T
            : throw new Exception("Invalid policy type: " + typeof(T));

        public static List<T> GetPolicies<T>(this Game game) where T : Policy =>
            typeof(T) == typeof(ApparelPolicy) ? game.outfitDatabase.AllOutfits as List<T>
            : typeof(T) == typeof(FoodPolicy) ? game.foodRestrictionDatabase.AllFoodRestrictions as List<T>
            : typeof(T) == typeof(DrugPolicy) ? game.drugPolicyDatabase.AllPolicies as List<T>
            : typeof(T) == typeof(ReadingPolicy) ? game.readingPolicyDatabase.AllReadingPolicies as List<T>
            : throw new Exception("Invalid policy type: " + typeof(T));

        public static T GetCurrentPolicy<T>(this Pawn pawn) where T : Policy =>
            typeof(T) == typeof(ApparelPolicy) ? pawn.outfits?.CurrentApparelPolicy as T
            : typeof(T) == typeof(FoodPolicy) ? pawn.foodRestriction?.CurrentFoodPolicy as T
            : typeof(T) == typeof(DrugPolicy) ? pawn.drugs?.CurrentPolicy as T
            : typeof(T) == typeof(ReadingPolicy) ? pawn.reading?.CurrentPolicy as T
            : throw new Exception("Invalid policy type: " + typeof(T));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "hard to follow")]
        public static void SetCurrentPolicy<T>(this Pawn pawn, T policy) where T : Policy
        {
            if (typeof(T) == typeof(ApparelPolicy)) pawn.outfits.CurrentApparelPolicy = policy as ApparelPolicy;
            else if (typeof(T) == typeof(FoodPolicy)) pawn.foodRestriction.CurrentFoodPolicy = policy as FoodPolicy;
            else if (typeof(T) == typeof(DrugPolicy)) pawn.drugs.CurrentPolicy = policy as DrugPolicy;
            else if (typeof(T) == typeof(ReadingPolicy)) pawn.reading.CurrentPolicy = policy as ReadingPolicy;
            else throw new Exception("Invalid policy type: " + typeof(T));
        }

        public static void SetAllDefaultPolicies(this Pawn pawn, PawnType? previousPawnType = null)
        {
            SetDefaultPolicy<ApparelPolicy>(pawn, previousPawnType);
            SetDefaultPolicy<FoodPolicy>(pawn, previousPawnType);
            SetDefaultPolicy<DrugPolicy>(pawn, previousPawnType);
            SetDefaultPolicy<ReadingPolicy>(pawn, previousPawnType);
        }

        public static void SetDefaultPolicy<T>(this Pawn pawn, PawnType? previousPawnType = null) where T : Policy
        {
            if (pawn.GetCurrentPolicy<T>() != null)
            {
                DefaultPolicyAssignments assignments = Settings.Get<DefaultPolicyAssignments>(Settings.POLICY_ASSIGNMENTS);

                PawnType? pawnType = PawnTypeUtility.GetPawnType(pawn);
                if (pawnType.HasValue && assignments.PolicyAssignments.Keys.Contains(pawnType.Value))
                {
                    if (pawnType == PawnType.Guest && typeof(T) == typeof(ApparelPolicy))
                    {
                        return;
                    }
                    if (previousPawnType.HasValue)
                    {
                        T previousDefaultPolicy = assignments.PolicyAssignments[previousPawnType.Value].GetPolicy<T>();
                        if (previousDefaultPolicy != null)
                        {
                            if (pawn.GetCurrentPolicy<T>().RenamableLabel != previousDefaultPolicy.RenamableLabel)
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (pawn.GetCurrentPolicy<T>() != Current.Game.GetDefaultPolicy<T>())
                            {
                                return;
                            }
                        }
                    }
                    T newPolicy = Current.Game.GetPolicies<T>().FirstOrDefault(p => p.RenamableLabel == assignments.PolicyAssignments[pawnType.Value].GetPolicy<T>()?.RenamableLabel) ?? Current.Game.GetDefaultPolicy<T>();
                    pawn.SetCurrentPolicy(newPolicy);
                }
            }
        }
    }
}

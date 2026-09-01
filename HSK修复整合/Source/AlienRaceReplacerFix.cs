// HAR replacerDict 重复键兜底(AlienRaceReplacerFix, 并入 HSK 修复整合)
//
// 问题: 启动刷 "Could not execute post-long-event action. Exception:
//   System.ArgumentException: An item with the same key has already been added.
//   Key: 17479",栈在 AlienRace.ThingDef_AlienRace.<ResolveReferences>b__1_1
//   (ThingDef_AlienRace.cs:283)。这是 HAR 官方已知 bug(erdelf/AlienRaces
//   issue #118,2026-05-04,状态 open): ResolveReferences 被二次执行时,
//   thoughtSettings.replacerDict.Add(original.shortHash, replacer) 对同 key
//   重复 Add 抛 ArgumentException —— 并非 XML 里有重复 replacerList
//   (Unified.xml 最终态只有 Ratkin 一条,已核实)。
//
//   ResolveReferences 二次执行的触发源: Hot reload Defs 或某些 mod 会再次
//   ResolveAllReferences;本环境启动流程(MVCF/Prepatcher 等)下稳定复现。
//   replacerDict 是每种族 ThoughtSettings 的实例字段,二次执行时 dict 里
//   已含上次 Add 的同 key 条目 → 抛异常。
//
// 方案: 前缀补丁在 b__1_1 执行前清理 —— 遍历 replacerList,把 replacerDict
//   中已存在的 key 先 Remove 再放行原方法,使二次执行变成幂等(行为同首次
//   执行一致,replacerThoughts HashSet.Add 天然幂等,无需处理)。仅当该种族的
//   thoughtSettings 有 replacerList 时才清理,零副作用。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
//   需引用 AlienRace.dll(Mods/AlienRaces/1.6/Assemblies)。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AlienRaceReplacerFix
{
    [StaticConstructorOnStartup]
    public static class AlienRaceReplacerFixInit
    {
        static AlienRaceReplacerFixInit()
        {
            try
            {
                // AlienRace 程序集与类型(动态查找,不硬引用)
                Assembly har = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "AlienRace")
                    {
                        har = asm;
                        break;
                    }
                }
                if (har == null)
                {
                    Log.Warning("[HSKFix] AlienRace.dll not loaded, replacer fix skipped");
                    return;
                }
                Type thingDefType = har.GetType("AlienRace.ThingDef_AlienRace");
                if (thingDefType == null)
                {
                    Log.Warning("[HSKFix] AlienRace.ThingDef_AlienRace not found, replacer fix skipped");
                    return;
                }

                // 目标: 内嵌 lambda <ResolveReferences>b__1_1
                // 捕获 this 的 lambda 直接放在 ThingDef_AlienRace 自身
                // (反编译部署版确认: 该类型有实例方法 <ResolveReferences>b__1_1,
                //  无 display class),先查类型自身,再兜底查嵌套类型。
                MethodInfo target = thingDefType.GetMethod(
                    "<ResolveReferences>b__1_1",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (target == null)
                {
                    foreach (Type nested in thingDefType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        MethodInfo m = nested.GetMethod(
                            "<ResolveReferences>b__1_1",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        if (m != null)
                        {
                            target = m;
                            break;
                        }
                    }
                }
                if (target == null)
                {
                    Log.Warning("[HSKFix] HAR <ResolveReferences>b__1_1 not found, replacer fix skipped");
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.alienracereplacer");
                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(AlienRaceReplacerFixInit).GetMethod(
                            "Prefix",
                            BindingFlags.Static | BindingFlags.NonPublic)));
                Log.Message("[HSKFix] patched HAR ThingDef_AlienRace <ResolveReferences>b__1_1 (replacerDict dup-key fallback)");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] AlienRace replacer fix failed: " + e);
            }
        }

        // 前缀: 清理 replacerDict 中已存在的 key,使二次执行幂等。
        // 返回 false 不适用(永远放行原方法)。
        private static bool Prefix(object __instance)
        {
            try
            {
                // 反射读 alienRace -> thoughtSettings -> replacerList / replacerDict
                object alienRace = AccessTools.Field(__instance.GetType(), "alienRace").GetValue(__instance);
                if (alienRace == null)
                {
                    return true;
                }
                object thoughtSettings = AccessTools.Field(alienRace.GetType(), "thoughtSettings").GetValue(alienRace);
                if (thoughtSettings == null)
                {
                    return true;
                }
                System.Collections.IList replacerList = (System.Collections.IList)AccessTools.Field(
                    thoughtSettings.GetType(), "replacerList").GetValue(thoughtSettings);
                if (replacerList == null || replacerList.Count == 0)
                {
                    return true;
                }
                System.Collections.IDictionary replacerDict = (System.Collections.IDictionary)AccessTools.Field(
                    thoughtSettings.GetType(), "replacerDict").GetValue(thoughtSettings);
                if (replacerDict == null)
                {
                    return true;
                }

                foreach (object replacer in replacerList)
                {
                    if (replacer == null)
                    {
                        continue;
                    }
                    object original = AccessTools.Field(replacer.GetType(), "original").GetValue(replacer);
                    if (original == null)
                    {
                        continue;
                    }
                    int key = (ushort)AccessTools.Field(original.GetType(), "shortHash").GetValue(original);
                    replacerDict.Remove(key);
                }
            }
            catch (Exception e)
            {
                // 清理失败不阻断原方法(保持原行为,最多复现原报错)
                Log.Warning("[HSKFix] AlienRace replacer cleanup failed: " + e);
            }
            return true;
        }
    }
}

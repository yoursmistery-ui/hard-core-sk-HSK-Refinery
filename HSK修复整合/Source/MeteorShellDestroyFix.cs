// HSK 陨石外壳自毁修复(并入 HSK 修复整合)
//
// 背景: Core_SK 的 SK.Events.Meteor(陨石,defName=Meteor)在落地后通过
//   Tick→PodOpen() 释放内部可挖掘矿块(GenPlace.TryPlaceThing 把矿块放到自身
//   所在格),但外壳自身不会销毁 —— 而它的 thingClass 不是 Mineable 也没有
//   mineable=true,于是开盒后留下一个"没有开采图标"、永远不可挖掘的陨石壳。
//
// Meteor.Tick 逻辑(IL 已解码): 每 tick age++; 当 soundPlayed==false 且
//   age>info.openDelay 时,置 soundPlayed=true 并调用一次 PodOpen();
//   此后只自增 age,再也不做任何事(外壳永存)。
//
// 方案: 对 Meteor.Tick 挂 Harmony 后置。原方法执行后若 soundPlayed==true,
//   说明本 tick 已开盒(矿块已释放),随即销毁外壳(Vanish),清除多余的空壳。
//   Destroyed 判空保证幂等,避免对已销毁对象重复调用 Destroy 抛错。
//
// 编译: 并入 HSKFixPack.dll(与其余 Source/*.cs 一起,系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace MeteorShellDestroyFix
{
    [StaticConstructorOnStartup]
    public static class MeteorShellDestroyFixInit
    {
        static MeteorShellDestroyFixInit()
        {
            try
            {
                Type meteor = AccessTools.TypeByName("SK.Events.Meteor");
                if (meteor == null)
                {
                    return;
                }
                MethodInfo tick = meteor.GetMethod("Tick", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (tick == null)
                {
                    return;
                }

                Harmony harmony = new Harmony("local.hskfixpack.meteorshelldestroy");
                harmony.Patch(
                    tick,
                    postfix: new HarmonyMethod(
                        typeof(MeteorShellDestroyFixInit).GetMethod(
                            "Postfix",
                            BindingFlags.Static | BindingFlags.NonPublic)));

                Log.Message("[HSKFix] patched SK.Events.Meteor.Tick: destroy opened shell to remove un-mineable meteor");
            }
            catch (Exception e)
            {
                Log.Error("[HSKFix] Meteor shell destroy fix failed: " + e);
            }
        }

        private static void Postfix(object __instance, bool ___soundPlayed)
        {
            if (__instance == null || !___soundPlayed)
            {
                return;
            }
            Thing meteor = __instance as Thing;
            if (meteor == null || meteor.Destroyed)
            {
                return;
            }
            meteor.Destroy();
        }
    }
}
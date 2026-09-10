// 修复 Need Bar Overflow(AmCh.NeedBarOverflow)开启「美观值溢出」等需求溢出后,
// 实时值标线超过 100% 触发原版刷屏日志:
//   "Beauty drawing bar percent > 1 : 1.2"
//
// 根因(1.6.4871 反编译确认):Need.DrawBarInstantMarkerAt 开头有原版校验
//   if (pct > 1f) { Log.ErrorOnce(def + " drawing bar percent > 1 : " + pct, 6932178); }
// 而 NeedBarOverflow 的 Need.DrawOnGUI 前缀把未钳制的实时百分比(美观值溢出后可达 1.2)
// 直接传给该方法画「实时值标线」,于是每次打开需求面板都刷一条 ErrorOnce。
// 原版自身调用方的 pct 恒 ≤1,该校验只是防御;标线即使 >1 也照画
// (位置 = barRect.x + width*pct,超出条尾,基本被 GUI 裁剪不可见)。
//
// 修复:给 Need.DrawBarInstantMarkerAt 加 Harmony 前缀,把 pct 钳制回 [0,1]。
// 溢出填充条本身由 NeedBarOverflow 自己绘制,不受影响;本补丁只消除日志报错,
// 对标线位置的影响是超过 100% 时标线停在条尾(原位置本就不可见),纯日志修复。
// 未装 NeedBarOverflow 时本补丁是零开销空转(原版调用方从不传 >1)。
//
// 编译:并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace NeedBarOverflowMarkerFix
{
    [StaticConstructorOnStartup]
    public static class NeedBarOverflowMarkerFixInit
    {
        static NeedBarOverflowMarkerFixInit()
        {
            try
            {
                Harmony harmony = new Harmony("local.hskfixpack.needbaroverflowmarkerfix");
                harmony.Patch(AccessTools.Method(typeof(Need), "DrawBarInstantMarkerAt"),
                    prefix: new HarmonyMethod(typeof(NeedBarOverflowMarkerFixInit), "Prefix"));
                Log.Message("[HSKFix] patched Need.DrawBarInstantMarkerAt: clamp marker pct to [0,1] (Need Bar Overflow log spam fixed)");
            }
            catch (Exception e)
            {
                Log.Warning("[HSKFix] NeedBarOverflowMarkerFix patch failed: " + e);
            }
        }

        private static void Prefix(ref float pct)
        {
            if (pct > 1f || pct < 0f)
            {
                pct = Mathf.Clamp01(pct);
            }
        }
    }
}

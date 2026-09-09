using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XenotypeRaceLeakGuardHSK
{
    // 异种人跨种族泄漏守卫。
    //
    // 根因(反编译 Assembly-CSharp 确认):
    //   Verse.PawnGenerator.XenotypesAvailableFor(kind, factionDef, faction) 组池顺序为
    //     ① kind.useFactionXenotypes 为真时,把 faction.def.xenotypeSet **整池**灌进来
    //     ② 加 ideo meme 池
    //     ③ 加 kind.xenotypeSet
    //     ④ 余量给 Baseliner
    //   全程**不校验"该异种是否允许用在 kind.race 上"**。GetXenotypeForGeneratedPawn
    //   按权重抽完就直接 SetXenotype,并用异种自带的 nameMaker 取名。
    //
    // 实例: Core_SK 的 SK.IncidentWorker_RefugeeChased 用人类职位(Refugee/SpaceRefugee,
    //   useFactionXenotypes 默认 true)配一个随机敌对派系生成难民;若被挑中的是美狐派系
    //   (美狐HSK拓展/1.6/Patches/98b_美狐派系异种人池.xml 给它的池是纯美狐异种、合计 1.000、
    //   无 Baseliner 余量),就会生成"人类身体 + 美狐异种 + 美狐基因 + 美狐名字器"的混合体。
    //
    // 本补丁: postfix GetXenotypeForGeneratedPawn,借用 HAR 自己的判定
    //   AlienRace.RaceRestrictionSettings.CanUseXenotype(xenotype, race) —— 它已实现
    //   "异种被任一种族列入 raceRestriction/xenotypeList 即进全局 xenotypeRestricted,
    //   只有 whiteXenotypeList 含它的种族能用"(非 AlienRace 的 race 一律 false)。
    //   判定不通过就回退 Baseliner。原版不受限异种(智人种/天选者/尼安德特等)原样放行,
    //   真正的本族成员(如美狐 kind 的 race=Alien_Miho)在自己的白名单内,不受影响。
    //
    // 用反射调 HAR 而不加编译期引用: 未装 AlienRaces 时静默不生效,不给整合包添硬依赖。
    [StaticConstructorOnStartup]
    public static class XenotypeRaceLeakGuardFix
    {
        // 缓存反射结果;canUseXenotype 为 null 表示 HAR 不可用,守卫整体退化为不干预。
        private static MethodInfo canUseXenotype;

        private static bool resolved;

        static XenotypeRaceLeakGuardFix()
        {
            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(PawnGenerator), "GetXenotypeForGeneratedPawn",
                    new Type[] { typeof(PawnGenerationRequest) });
                if (target == null)
                {
                    return;
                }
                Harmony harmony = new Harmony("local.hskfixpack.xenotyperaceleakguard");
                harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(typeof(XenotypeRaceLeakGuardFix).GetMethod(
                        "Postfix", BindingFlags.Static | BindingFlags.NonPublic)));
            }
            catch (Exception)
            {
            }
        }

        private static void Postfix(PawnGenerationRequest request, ref XenotypeDef __result)
        {
            if (__result == null)
            {
                return;
            }
            if (!ModsConfig.BiotechActive)
            {
                return;
            }
            if ((object)request.KindDef == null)
            {
                return;
            }
            ThingDef race = request.KindDef.race;
            if (race == null)
            {
                return;
            }
            MethodInfo mi = Resolve();
            if (mi == null)
            {
                return; // 未装 HAR,拿不到种族白名单机制,不干预
            }
            bool allowed;
            try
            {
                object raw = mi.Invoke(null, new object[] { __result, race });
                if (!(raw is bool))
                {
                    return;
                }
                allowed = (bool)raw;
            }
            catch (Exception)
            {
                return; // HAR 内部异常时不改写生成结果
            }
            if (allowed)
            {
                return;
            }
            XenotypeDef baseliner = XenotypeDefOf.Baseliner;
            if (baseliner == null)
            {
                return;
            }
            __result = baseliner;
        }

        private static MethodInfo Resolve()
        {
            if (resolved)
            {
                return canUseXenotype;
            }
            resolved = true;
            try
            {
                Type restrictionType = AccessTools.TypeByName("AlienRace.RaceRestrictionSettings");
                if (restrictionType == null)
                {
                    return null;
                }
                canUseXenotype = AccessTools.Method(
                    restrictionType, "CanUseXenotype",
                    new Type[] { typeof(XenotypeDef), typeof(ThingDef) });
            }
            catch (Exception)
            {
                canUseXenotype = null;
            }
            return canUseXenotype;
        }
    }
}

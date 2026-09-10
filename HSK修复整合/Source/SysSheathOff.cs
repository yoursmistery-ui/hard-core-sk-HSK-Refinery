// SYS 背负武器/刀鞘绘制关闭(2026-09-08;用户报"待机小人两把武器")
//
// 症状: 未征召的小人待机时,视野里出现两把同样的近战武器——手里一把(近战动画 AM 的
// idle 动画在画)+ 腰/背侧一把。工具(锤/镐)不双画,武器(单手/双手)都双画。
//
// 根因(反编译 RatkinRaceHSK/1.6/Assemblies/SYS.dll,反编译件 _tmp/sys_decomp/SYS/):
//   SYS.DrawEquipmentAndApparelExtrasPatch.Postfix 挂在
//   PawnRenderUtility.DrawEquipmentAndApparelExtras 之后,主手武器带 SYS.CompSheath 时再画一次:
//     - CarryWeaponOpenly(pawn)(征召 / alwaysShowWeapon) → SheathOnlyGraphic(刀鞘本体)
//     - 否则(未征召待机)且站立 → FullGraphic = 整把武器(fullGraphicData,如
//       Item_Weapon/RK_Dao_Full;没配则回退武器自身 Graphic)
//   未征召待机时原版因 CarryWeaponOpenly=false 根本不画武器,于是可见两把 =
//   AM 手里那把 + SYS 背负那把。工具 def 没有 CompSheath ⇒ 只有武器双画,与现象完全吻合。
//
// 修复: 给该 Postfix 加前缀 return false,彻底关掉背负/刀鞘绘制(用户选择"背负武器全关")。
//   两份 SYS.dll 各自 PatchAll(RatkinRaceHSK 与 美狐HSK拓展 miho.fortifiedoutremer 各带一份,
//   sha 不同),必须遍历所有已加载程序集逐个压——只压一份的话另一份照画。
//
// 门控: 全部按名反射,不编译期引用 SYS.dll;未装相关 mod 时命中 0、零副作用。
// 日志前缀 [SysSheathOff]。编译: 并入 HSKFixPack.dll(系统 csc,C#5)。
using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace SysSheathOff
{
    [StaticConstructorOnStartup]
    public static class SysSheathOffInit
    {
        private const string TargetTypeName = "SYS.DrawEquipmentAndApparelExtrasPatch";

        static SysSheathOffInit()
        {
            try
            {
                Harmony h = new Harmony("local.hskfixpack.sysoff");
                MethodInfo prefix = typeof(SysSheathOffInit).GetMethod("NoSheath");
                int hit = 0;

                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < asms.Length; i++)
                {
                    Type t = null;
                    try { t = asms[i].GetType(TargetTypeName); } catch { }
                    if (t == null) continue;

                    MethodInfo post = AccessTools.Method(t, "Postfix");
                    if (post == null) continue;

                    h.Patch(post, new HarmonyMethod(prefix), null);
                    hit++;
                }

                if (hit > 0)
                    Log.Message("[SysSheathOff] 背负武器/刀鞘绘制已关闭 (SYS 程序集命中 " + hit + " 份)");
            }
            catch (Exception e)
            {
                Log.Error("[SysSheathOff] 挂载失败: " + e);
            }
        }

        // 前缀返回 false → SYS 的背负/刀鞘 Postfix 整体不执行。
        public static bool NoSheath()
        {
            return false;
        }
    }
}

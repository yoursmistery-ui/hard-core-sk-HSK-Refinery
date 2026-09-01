using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace QuestBoardHSK
{
    /// <summary>
    /// 任务页"通缉令"主窗(2026-09-01):取代 SW 原版 MainTabWindow_Warrants。
    /// 1.6/Defs/MainButtonDefs/MainButtons.xml 的 tabWindowClass 已指到这里,
    /// SW 原版主窗类(SW_Warrants 按钮唯一入口)永远不会被实例化 → 旧界面已删除。
    /// 打开后立即推自己的 Dialog_BountyBoard(海报版查榜)并关闭自身。
    /// 若本类写错/未编译进 DLL,XML 加载 tabWindowClass 时即报 "Could not find a type",启动即见红字错误。
    /// </summary>
    public class MainTabWindow_BountyBoardHSK : MainTabWindow
    {
        public override void DoWindowContents(Rect inRect)
        {
            // 已有海报版/发单窗在栈上则不再重复推(防递归)
            if (Find.WindowStack.Windows.Any(w => w is Dialog_BountyBoard || w is Dialog_IssueWarrantHSK))
            {
                Close();
                return;
            }
            Find.WindowStack.Add(new Dialog_BountyBoard());
            Close();
        }
    }
}

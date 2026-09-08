using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Defaults.WorkbenchBills
{
    [HarmonyPatchCategory("WorkbenchBills")]
    [HarmonyPatch(typeof(Dialog_BillConfig))]
    [HarmonyPatch(nameof(Dialog_BillConfig.DoWindowContents))]
    public static class Patch_Dialog_BillConfig
    {
        public static void Postfix(Rect inRect, Bill_Production ___bill)
        {
            if (!Settings.GetValue<bool>(Settings.HIDE_SETASDEFAULT))
            {
                Rect saveAsDefaultRect = new Rect(inRect.x + (inRect.width - 34f) / 3 - 24f, inRect.y + 50f, 24f, 24f);
                if (Widgets.ButtonImage(saveAsDefaultRect, TexButton.Save, true, "Defaults_SaveNewDefaultBill".Translate()))
                {
                    foreach (WorkbenchBillStore store in Settings.Get<List<WorkbenchBillStore>>(Settings.WORKBENCH_BILLS).Where(s => s.workbenchGroup.Contains(((Thing)___bill.billStack.billGiver).def)))
                    {
                        store.bills.Add(BillTemplate.FromBill(___bill));
                    }
                    DefaultsMod.SaveSettings();
                    Messages.Message("Defaults_SaveBillConfirmed".Translate(), MessageTypeDefOf.PositiveEvent, false);
                }
            }
        }
    }
}

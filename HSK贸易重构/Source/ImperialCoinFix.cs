// 鼠族HSK拓展 - 帝国银币货币改造
//
// 需求(用户 2026-08-19 创建, 2026-08-21 更新为双货币定版):
//   - 双货币: 帝国银币(SilverCoin)= 唯一普通结算货币; 帝国金币(GoldCoin)= 第二高面值货币
//     (1 金令 = 35 银令, 与游戏内金矿:银矿价值比一致)。金币是「一般等价物」——每个商人都能交易,
//     支付时银币不足自动用金币按 35 抵付(智能凑整找零, 见 ImperialDualCurrency)。
//   - 银矿(Silver)回归纯材料,与货币完全分离: 银矿=银矿,银币=银币,两者价格 1:1;
//   - 金币只由帝国(及个别势力)少量携带流通,并非所有商人都持有金币(用户 2026-08-21 要求)。
//
// 原版把"钱"硬编码绑定在 ThingDefOf.Silver 上(1.6.4871 全量扫描确认,
// 21 处 ldsfld Verse.ThingDef Silver 的字段引用,全部是货币用途):
//   贸易结算/货币行/商人资金/资源面板/送礼/乞丐/任务报酬/价格类型...
// 而银矿(Silver)在本环境(Core_SK + Minerals)同时也是可挖矿石/冶炼材料,
// 两者共用同一 def 无法分离。
//
// 本 DLL 用 Harmony transpiler 把方法 IL 中的 "ldsfld Verse.ThingDef Silver"
// 全部替换为 "ldsfld Verse.ThingDef SilverCoin"(仅对 ThingDefOf.Silver 这一个字段生效),
// 使游戏的所有货币逻辑改用帝国银币结算,银矿(Silver)彻底退出货币体系。
// 不做任何代码级"银矿不可挖"的修改 —— 挖矿产出仍是 Silver(纯材料)。
//
// 绑定方式: 静态构造器反射扫描 Assembly-CSharp 全部方法,
// 命中即给该(方法, 替换token)组合挂 transpiler 一次(Harmony 要求 key 唯一,
// 相同的 (method, index) 重复 Patch 会抛 ArgumentException)。
// 因此无需硬编码方法名,游戏版本更新新增/移动引用也能自动覆盖。
//
// 编译(系统 csc, C#5):
//   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /target:library
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"
//     /r:"<RimWorld>\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll"
//     /r:"<RimWorld>\Mods\Harmony\Current\Assemblies\0Harmony.dll"
//     /out:ImperialCoinFix.dll ImperialCoinFix.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RKImperialCoin
{
    [StaticConstructorOnStartup]
    public static class ImperialCoinFixInit
    {
        // 自定义货币 def 字段(替换目标): 由静态构造器从 DefDatabase 加载,
        // 不能放 ThingDefOf(SilverCoin 是 Core_SK DLCModule 的 XML def, 非原版 ThingDefOf 字段)
        public static ThingDef SilverCoinDef;

        private static readonly FieldInfo SilverField =
            AccessTools.Field(typeof(ThingDefOf), "Silver");
        private static readonly FieldInfo SilverCoinField =
            AccessTools.Field(typeof(ImperialCoinFixInit), "SilverCoinDef");

        static ImperialCoinFixInit()
        {
            try
            {
                if (SilverField == null)
                {
                    Log.Warning("[RKImperialCoin] ThingDefOf.Silver field missing, skip");
                    return;
                }

                // defs 已加载完毕(StaticConstructorOnStartup 在 def 加载后执行), 从 DefDatabase 取银币
                SilverCoinDef = DefDatabase<ThingDef>.GetNamedSilentFail("SilverCoin");
                if (SilverCoinDef == null)
                {
                    Log.Warning("[RKImperialCoin] SilverCoin def not found (Core_SK DLCModule missing?), skip");
                    return;
                }

                // 扫描所有已加载程序集(游戏主程序集 + 第三方 mod), 凡用 ThingDefOf.Silver 造银的
                // 一律改为 SilverCoin -> 修 GoExplore 的「发现宝物」等 mod 产原矿的问题
                int patched = 0;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm == null) continue;
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException rtle) { types = rtle.Types; }
                    catch { continue; }
                    if (types == null) continue;
                    foreach (Type t in types)
                    {
                        if (t == null) continue;
                        try
                        {
                            foreach (MethodBase m in AllMethods(t))
                            {
                                if (m == null) continue;
                                if (MethodUsesField(m, SilverField))
                                {
                                    try
                                    {
                                        _harmony.Patch(m, transpiler: new HarmonyMethod(typeof(ImperialCoinFixInit), "Transpiler"));
                                        patched++;
                                    }
                                    catch (Exception e)
                                    {
                                        Log.Warning("[RKImperialCoin] patch fail " + t.FullName + "::" + m.Name + ": " + e.Message);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning("[RKImperialCoin] skip type " + t.FullName + ": " + e.Message);
                        }
                    }
                }
                Log.Message("[RKImperialCoin] Imperial silver coin currency patch applied to " + patched + " methods (all assemblies)");
            }
            catch (Exception e)
            {
                Log.Error("[RKImperialCoin] init error: " + e);
            }
        }

        private static readonly Harmony _harmony = new Harmony("local.imperialcoin");

        private static IEnumerable<MethodBase> AllMethods(Type t)
        {
            const BindingFlags f = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (MethodInfo m in t.GetMethods(f)) yield return m;
            foreach (MethodBase m in t.GetConstructors(f).Cast<MethodBase>()) yield return m;
        }

        private static bool MethodUsesField(MethodBase m, FieldInfo f)
        {
            try
            {
                MethodBody mb = m.GetMethodBody();
                if (mb == null) return false;
                byte[] il = mb.GetILAsByteArray();
                if (il == null) return false;
                Module mod = m.Module;
                int j = 0;
                while (j < il.Length)
                {
                    byte b = il[j];
                    if (b == 0x7E || b == 0x7B || b == 0x7C || b == 0x80)
                    {
                        if (j + 5 <= il.Length)
                        {
                            int tok = BitConverter.ToInt32(il, j + 1);
                            try
                            {
                                FieldInfo rf = mod.ResolveField(tok);
                                if (rf == f) return true;
                            }
                            catch { }
                            j += 5;
                            continue;
                        }
                    }
                    j++;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // 把 IL 中的 ldsfld ThingDefOf.Silver 替换为 ldsfld ImperialCoinFixInit.SilverCoinDef
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldsfld && ins.operand as FieldInfo == SilverField)
                {
                    yield return new CodeInstruction(OpCodes.Ldsfld, SilverCoinField) { labels = ins.labels };
                }
                else
                {
                    yield return ins;
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 双货币结算(2026-08-20 新增, 2026-08-21 定版):
    // 用户确认保留双货币 —— 帝国银币为唯一普通结算货币, 帝国金币为第二高面值货币
    // (一般等价物)。1 金令 = 35 银令(对应游戏内金矿:银矿 35:1), 支付时银币不足
    // 自动用金币整枚折算并智能凑整找零; 金币在可贸易品列表中排最后(商品页最后一栏)。
    //
    // 机制(基于 1.6.9438 反编译确认的结算顺序):
    //   TradeDeal.TryExecute 非赠礼流程:
    //     [1] 检查 CurrencyTradeable.CountPostDealFor(Colony) < 0 → 拒绝(玩家付不起)
    //     [2] Ideology 检查(卖器官/奴隶)
    //     [3] UpdateCurrencyCount()  → 货币行转移量 = -净差额(银令)
    //     [4] LimitCurrencyCountToFunds() → 钳制到双方持有量
    //     [5] 遍历 ResolveTrade() 实物转移
    //
    // 补丁点:
    //   a. Tradeable.CountPostDealFor postfix: [1] 检查时玩家银令不足, 但金令×35 能补上
    //      → 返回 0 视为付得起(金令也不够时保持负值, 交易被拒)。
    //   b. TradeDeal.LimitCurrencyCountToFunds prefix: [4] 前执行 —— 此时 Ideology 检查已过、
    //      ResolveTrade 未开始, 失败无副作用。用 ComputePayment 算精确应付(金令抵大额、
    //      只付必要银令并智能找零, 不多付银令), 扣对应枚数金令实物给商人
    //      (GiveSoldThingToTrader, 兼容商队/地图/轨道), 并把货币行银令转移量改为精确应付银。
    //   c. TradeUI.DrawTradeableRow postfix: 货币行上叠加显示 "金币:xxx 银币:xxx"。
    //   d. Dialog_Trade.DoWindowContents postfix: 贸易窗口底部双方金币/银币图标+数量实时预览。
    //   e. TradeDeal.AllTradeables getter postfix: getter 返回 tradeables 字段引用,
    //      直接把金币(金令)那一条可贸易品移到列表末尾 → 金币出现在商品页最后一栏。
    // ══════════════════════════════════════════════════════════════════════
    [StaticConstructorOnStartup]
    public static class ImperialDualCurrency
    {
        public const int ExchangeRate = 35; // 1 金令 = 35 银令(进制, 对应游戏内金矿:银矿价值比 35:1, 用户 2026-08-20 要求)

        private static ThingDef silverCoinDef;
        private static ThingDef goldCoinDef;

        static ImperialDualCurrency()
        {
            try
            {
                silverCoinDef = DefDatabase<ThingDef>.GetNamedSilentFail("SilverCoin");
                goldCoinDef = DefDatabase<ThingDef>.GetNamedSilentFail("GoldCoin");
                if (silverCoinDef == null || goldCoinDef == null)
                {
                    Log.Warning("[RKImperialCoin] DualCurrency: SilverCoin/GoldCoin def missing, skip");
                    return;
                }

                Harmony h = new Harmony("local.imperialcoin.dual");
                h.Patch(AccessTools.Method(typeof(Tradeable), "CountPostDealFor"),
                    postfix: new HarmonyMethod(typeof(ImperialDualCurrency), "CountPostDealFor_Postfix"));
                h.Patch(AccessTools.Method(typeof(TradeDeal), "LimitCurrencyCountToFunds"),
                    prefix: new HarmonyMethod(typeof(ImperialDualCurrency), "LimitCurrencyCount_Prefix"));
                h.Patch(AccessTools.Method(typeof(TradeUI), "DrawTradeableRow"),
                    postfix: new HarmonyMethod(typeof(ImperialDualCurrency), "DrawTradeableRow_Postfix"));
                h.Patch(AccessTools.Method(typeof(Dialog_Trade), "DoWindowContents"),
                    postfix: new HarmonyMethod(typeof(ImperialDualCurrency), "DrawTradeBottom_Postfix"));
                // 金币在商品页排最后一栏: getter 返回 tradeables 字段引用(postfix 直接改该列表)
                h.Patch(AccessTools.PropertyGetter(typeof(TradeDeal), "AllTradeables"),
                    postfix: new HarmonyMethod(typeof(ImperialDualCurrency), "TradeDeal_AllTradeables_Postfix"));
                Log.Message("[RKImperialCoin] Dual currency patches applied (1 gold coin = " + ExchangeRate + " silver coins)");
            }
            catch (Exception e)
            {
                Log.Error("[RKImperialCoin] DualCurrency init error: " + e);
            }
        }

        // 玩家侧本次交易可用的物品(与货币行银令来源一致: 商队库存 / 信标范围 / 定居点商队)
        private static IEnumerable<Thing> PlayerTradeableThings()
        {
            if (TradeSession.trader == null || TradeSession.playerNegotiator == null) return null;
            return TradeSession.trader.ColonyThingsWillingToBuy(TradeSession.playerNegotiator);
        }

        private static int PlayerGoldCount()
        {
            if (goldCoinDef == null || TradeSession.trader == null) return 0;
            int n = 0;
            try
            {
                IEnumerable<Thing> src = PlayerTradeableThings();
                if (src == null) return 0;
                foreach (Thing t in src)
                {
                    if (t != null && !t.Destroyed && t.def == goldCoinDef) n += t.stackCount;
                }
            }
            catch { }
            return n;
        }

        // a. 检查关: 玩家付不起但金令×10 能补上 → 视为付得起
        public static void CountPostDealFor_Postfix(ref int __result, Tradeable __instance, Transactor trans)
        {
            if (__result >= 0 || trans != Transactor.Colony) return;
            if (__instance == null || !__instance.IsCurrency) return;
            if (TradeSession.giftMode) return;
            try
            {
                int deficit = -__result;
                if ((long)PlayerGoldCount() * ExchangeRate >= deficit) __result = 0;
            }
            catch { }
        }

        // 计算玩家应付的金/银枚数: 优先用金币抵大额, 只付必要的银, 尽量不多付银(智能凑整/找零)
        // need=玩家应付银额(净), haveSilver=玩家现有银, haveGold=玩家现有金币(1金=10银)。
        // 例: 买11银商品, 有1金+3银 → 付1金+1银(=11), 保留2银。
        public static void ComputePayment(int need, int haveSilver, int haveGold,
            out int silverPay, out int goldPay)
        {
            if (haveSilver >= need)
            {
                silverPay = need;
                goldPay = 0;
                return;
            }
            int deficit = need - haveSilver;
            int goldNeeded = (deficit + ExchangeRate - 1) / ExchangeRate; // ceil(缺口/10)
            int goldUse = Math.Min(goldNeeded, haveGold);
            int covered = goldUse * ExchangeRate;                         // 金币能抵的银额
            int remaining = deficit - covered;
            if (remaining > 0)
            {
                // 金币不足(通常已被 CountPostDealFor 拒绝, 兜底): 付光所有银
                silverPay = haveSilver;
                goldPay = goldUse;
                return;
            }
            int overshoot = -remaining;                                   // 金币多抵了 overshoot 银
            // 用多抵的部分给玩家留银(找零): 保留 min(overshoot, 现有银) 的银不付
            int keepSilver = Math.Min(overshoot, haveSilver);
            silverPay = Math.Max(0, haveSilver - keepSilver);
            goldPay = goldUse;
            // 精确性兜底: 若仍多付则扣银(极端金币溢出时可能多付未找零, 属可接受边缘)
            int totalVal = goldPay * ExchangeRate + silverPay;
            if (totalVal > need)
            {
                int drop = totalVal - need;
                silverPay = Math.Max(0, silverPay - drop);
            }
        }

        // 统计某货币行在给定一方持有的总额(没有该商品行则 0)
        public static int CountHeld(ThingDef def, Transactor side)
        {
            try
            {
                TradeDeal deal = TradeSession.deal;
                if (deal == null || def == null) return 0;
                List<Tradeable> all = deal.AllTradeables;
                if (all == null) return 0;
                foreach (Tradeable t in all)
                {
                    if (t != null && t.ThingDef == def) return t.CountHeldBy(side);
                }
            }
            catch { }
            return 0;
        }

        // b. 结算关: 银令不够 → 扣玩家金令实物给商人, 并把银行转移量改为精确的应付银(找零留银)
        public static void LimitCurrencyCount_Prefix(TradeDeal __instance)
        {
            if (TradeSession.giftMode) return;
            if (silverCoinDef == null || goldCoinDef == null) return;
            try
            {
                Tradeable cur = (__instance != null) ? __instance.CurrencyTradeable : null;
                if (cur == null) return;
                if (cur.ThingDef != silverCoinDef) return; // 只处理银令货币行(威望行跳过)
                int need = cur.CountToTransferToDestination;
                if (need <= 0) return;
                int have = cur.CountHeldBy(Transactor.Colony);
                int deficit = need - have;
                int goldHave = PlayerGoldCount();
                if (deficit <= 0 || goldHave <= 0) return;

                // 精确支付方案
                int silverPay, goldPay;
                ComputePayment(need, have, goldHave, out silverPay, out goldPay);
                if (goldPay <= 0) return;

                // 找玩家金令实物(枚举本次交易可用物品)
                List<Thing> goldThings = null;
                IEnumerable<Thing> src = PlayerTradeableThings();
                if (src != null)
                {
                    foreach (Thing t in src)
                    {
                        if (t != null && !t.Destroyed && t.def == goldCoinDef)
                        {
                            if (goldThings == null) goldThings = new List<Thing>();
                            goldThings.Add(t);
                        }
                    }
                }
                if (goldThings == null) return;

                // 扣 goldPay 枚金令实物给商人(整枚)
                int remaining = goldPay;
                foreach (Thing gt in goldThings)
                {
                    if (remaining <= 0 || gt == null || gt.Destroyed) continue;
                    int take = Math.Min(remaining, gt.stackCount);
                    remaining -= take;
                    try
                    {
                        if (gt.Spawned)
                        {
                            Thing split = gt.SplitOff(take);
                            if (split != null)
                                TradeSession.trader.GiveSoldThingToTrader(split, take, TradeSession.playerNegotiator);
                        }
                        else
                        {
                            TradeSession.trader.GiveSoldThingToTrader(gt, take, TradeSession.playerNegotiator);
                        }
                    }
                    catch { }
                }

                // 银行转移量改为精确应付银(而非付光所有银) → 达成找零留银
                cur.ForceToSource(-silverPay);
            }
            catch { }
        }

        // c. 交易窗口货币行: 显示 "金币:xxx 银币:xxx"(画在货币行中间的空白调整区)
        public static void DrawTradeableRow_Postfix(Rect rect, Tradeable trad)
        {
            if (trad == null || !trad.IsCurrency) return;
            if (TradeSession.giftMode) return;
            if (trad.ThingDef != silverCoinDef) return;
            try
            {
                int silver = trad.CountHeldBy(Transactor.Colony);
                int gold = PlayerGoldCount();
                Rect r = new Rect(rect.x + rect.width - 415f + 8f, rect.y + 5f, 230f, 20f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(r, "金币: " + gold + "  银币: " + silver);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            catch { }
        }

        // d. 贸易窗口底部钱币 HUD: 双方金币/银币持有量 + 玩家实时应付预览(icon+数量)
        //   挂在 Dialog_Trade.DoWindowContents postfix, 每帧重算 → 实时。
        public static void DrawTradeBottom_Postfix(Rect inRect)
        {
            try
            {
                if (TradeSession.giftMode) return;
                if (silverCoinDef == null || goldCoinDef == null) return;
                TradeDeal deal = TradeSession.deal;
                if (deal == null) return;
                Tradeable cur = deal.CurrencyTradeable;
                if (cur == null) return;

                int pSilver = cur.CountHeldBy(Transactor.Colony);
                int pGold = CountHeld(goldCoinDef, Transactor.Colony);
                int tSilver = cur.CountHeldBy(Transactor.Trader);
                int tGold = CountHeld(goldCoinDef, Transactor.Trader);

                // 玩家实时应付预览(与结算同一套计算 → 所见即所得)
                int need = cur.CountToTransferToDestination;
                int sp, gp;
                ComputePayment(need, pSilver, pGold, out sp, out gp);

                float bottom = inRect.yMax;
                float y = bottom - 58f;
                const float pw = 96f;
                DrawCoinPanel(new Rect(inRect.xMin + 14f, y, pw, 52f),
                    true, pGold, gp, pSilver, sp);
                DrawCoinPanel(new Rect(inRect.xMax - 14f - pw, y, pw, 52f),
                    false, tGold, -1, tSilver, -1);
            }
            catch { }
        }

        // 画一个半透明钱币面板: 两行(金/银), 每行 [icon] 持有 [应付款]
        // pay>=0 时显示 "付", 否则只显示持有
        private static void DrawCoinPanel(Rect rect, bool isPlayer, int gold, int goldPay,
            int silver, int silverPay)
        {
            Rect bg = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            Widgets.DrawBox(bg);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(bg.x, bg.y, bg.width, 2f), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(bg.x, bg.yMax - 2f, bg.width, 2f), BaseContent.WhiteTex);

            // 标题
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 1f, 1f, 0.75f);
            Widgets.Label(new Rect(bg.x + 6f, bg.y + 2f, bg.width - 12f, 12f),
                isPlayer ? "我方" : "对方");
            GUI.color = Color.white;

            DrawCoinLine(bg.x + 6f, bg.y + 15f, goldCoinDef, gold, goldPay);
            DrawCoinLine(bg.x + 6f, bg.y + 33f, silverCoinDef, silver, silverPay);
        }

        private static void DrawCoinLine(float x, float y, ThingDef def, int held, int pay)
        {
            Texture2D icon = (def != null) ? def.uiIcon : null;
            if (icon != null)
                GUI.DrawTexture(new Rect(x, y, 16f, 16f), icon);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (pay >= 0)
                Widgets.Label(new Rect(x + 20f, y - 2f, 74f, 20f), held + " 付" + pay);
            else
                Widgets.Label(new Rect(x + 20f, y - 2f, 74f, 20f), held.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        // e. 金币(金令)在可贸易品中的位置 → 商品列表最后(用户要求"金币在可贸易品的最后一栏")。
        //    getter 内部是 "return tradeables"(反编译确认返回字段引用), postfix 直接改该列表即实时生效。
        //    若最后一条已经是金币则跳过(已在最后), 其余情况找到金币条目后移到末尾。
        public static void TradeDeal_AllTradeables_Postfix(ref List<Tradeable> __result)
        {
            try
            {
                if (__result == null || __result.Count < 2) return;
                Tradeable last = __result[__result.Count - 1];
                if (last != null && last.ThingDef == goldCoinDef) return;
                for (int i = 0; i < __result.Count; i++)
                {
                    Tradeable t = __result[i];
                    if (t != null && t.ThingDef == goldCoinDef)
                    {
                        __result.RemoveAt(i);
                        __result.Add(t);
                        return;
                    }
                }
            }
            catch { }
        }
    }

    // 资源分类显示排序(2026-08-20 用户要求, 不再绘制独立钱币浮层 UI,
    // 银币/金币直接通过资源条 Coins 分类展示)
    // 机制: ResourceReadout 构造器 postfix, 把 Coins 分类移到 RootThingCategories 最前。
    [StaticConstructorOnStartup]
    public static class ImperialCoinUI
    {
        static ImperialCoinUI()
        {
            // 资源条分类排序: 把 Coins 分类移到 RootThingCategories 最前(用户要求分类排最上方)
            try
            {
                Harmony h2 = new Harmony("local.imperialcoin.resourcereadout");
                h2.Patch(AccessTools.Constructor(typeof(ResourceReadout)),
                    postfix: new HarmonyMethod(typeof(ImperialCoinUI), "ResourceReadout_CtorPostfix"));
            }
            catch (Exception e)
            {
                // 静默
            }
        }

        // ResourceReadout 构造器 postfix: 把 Coins 分类移到 RootThingCategories 首位
        // (RootThingCategories 顺序 = DefDatabase.AllDefs 顺序 = 加载顺序, 原版不排序;
        //  只有 resourceReadoutRoot=true 的分类进列表。Coins 由 DLCModule 定义, 加载早于本 mod, 默认在靠后位置。)
        public static void ResourceReadout_CtorPostfix(object __instance)
        {
            try
            {
                if (__instance == null) return;
                FieldInfo f = AccessTools.Field(typeof(ResourceReadout), "RootThingCategories");
                if (f == null) return;
                var list = f.GetValue(__instance) as System.Collections.IList;
                if (list == null || list.Count < 2) return;
                object coins = null;
                foreach (var item in list)
                {
                    ThingCategoryDef cat = item as ThingCategoryDef;
                    if (cat != null && cat.defName == "Coins") { coins = item; break; }
                }
                if (coins == null) return;
                if (list[0] == coins) return; // 已在首位
                list.Remove(coins);
                list.Insert(0, coins);
            }
            catch (Exception e)
            {
                // 静默
            }
        }
    }
}

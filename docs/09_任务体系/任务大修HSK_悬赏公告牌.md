# 任务大修HSK · 悬赏公告牌(QuestBoardHSK)

> 2026-08-28 实装。mod: `任务大修HSK`(`local.hsk.questoverhaul`),工作区 `C:\Personal\Project\ratkin-patch\任务大修HSK\`,部署 `...\RimWorld\Mods\任务大修HSK\`(双目录已同步)。

## 一、功能闭环

悬赏架子 = 鼠族家具拓展的**公告牌(`RKFC_RL_Board`,2×1 木作,Woody 40)**。选中公告牌 → gizmo **查看悬赏榜** → `Dialog_BountyBoard` 弹窗:

- 左列:悬赏列表,**可接 / 已接·已发** 两页签;搜索框 + 派系筛选(FloatMenu)+ 人种筛选(FloatMenu,Biotech 异种人 label);行 = 类型图标 + 目标名 + 派系族色名 + 人种 + 赏金额。
- 右栏:**v5 通缉令海报卡**(`Dialog_BountyBoard.DrawPoster` 全自绘,不再走 `Warrant.Draw`):纸底色+派系色边框角标、类型徽章、旋转印章倒计时、照片框头像(PortraitsCache/动物/物件贴图)、生死双酬筹码、实物加成清单、口信纸条;**接受即生成原版任务**(`DoAcceptAction` 内 `GenerateQuestAndMakeAvailable`,autoAccept)→ 出现在**任务主标签页**,交付走 SW 原有商队/运输舱 gizmo。
- 顶栏:**打开任务页** 按钮(`Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Quests)`)。

## 二、文件清单

| 文件 | 说明 |
|---|---|
| `1.6/Source/QuestBoardHSK/QuestBoardHSK.csproj` | SDK 风格,**net48**(SimpleWarrants.dll 是 net48,net472 引用会被 MSB3274 拒绝);引用 Assembly-CSharp/UnityEngine.CoreModule/IMGUIModule/TextRenderingModule + `..\..\Assemblies\SimpleWarrants.dll`(注意项目嵌套在 Source/QuestBoardHSK 子目录,是两层 `..\..`) |
| `CompBountyBoard.cs` | `CompProperties_BountyBoard`+`CompBountyBoard`:仅 `CompGetGizmosExtra` 出 gizmo(icon=`UI/Warrants/TargetWarrants`),`WarrantsManager.Instance` 为空或非玩家派系时不出;**零 tick 逻辑** |
| `Dialog_BountyBoard.cs` | 弹窗(1120×700,v5 海报版)。数据源 = `WarrantsManager.Instance.availableWarrants/acceptedWarrants/createdWarrants/takenWarrants`(全 public);页签 可接/已接·已发/无线电。四类通缉识别:`Warrant_Pawn`(人类抓捕+动物猎杀,`Pawn.RaceProps.Animal` 区分图标 IconCapture/IconDeath)、`Warrant_TameAnimal`(icon=`UI/Designators/Tame`,`AnimalRace.label`)、`Warrant_Artifact`(icon=IconRetrieve)。人种取 `pawn.genes.Xenotype.label`(ModLister Biotech 门控)。列表按"签名+30 tick"缓存(§八);海报实物清单走 `BountyRewards.GetPackage`(UI 缓存) |
| `HarmonyPatches.cs` | 全部 Harmony 补丁:SW 生成限流(GenerateRandomWarrant prefix/PopulateWarrants cap+捕获)、商队夹带(TryOpenComms postfix)、接收入口(通讯台/公告牌 GetFloatMenuOptions postfix)、实物发放(`Warrant.GiveReward` 基类 postfix + `TransportersArrivalAction_ReturnWarrant.Arrived` postfix,见附注 §六/§十) |
| `BountyRewards.cs` | 悬赏实物加成(2026-08-30):FNV-1a(loadID) 种子确定性生成,海报展示与商队/运输舱实发同种子零持久化;分档 35~50%;含驯服单不发银的上游 bug 修复。详见附注 §十 |
| `1.6/Defs/ThingSetMakerDefs/RK_悬赏实物奖励池.xml` | `ThingSetMaker_MarketValue` ×4:`RK_BountyReward_Ingots`(锭)/`_Arms`(枪械近战,含鼠族刀)/`_Gear`(护甲衣物)/`_Sundries`(零件药品杂货);defName 均对全环境核过 |
| `1.6/Patches/03_悬赏公告牌.xml` | 双层 `PatchOperationConditional`:外层门控 `RKFC_RL_Board` 存在(未装鼠族家具拓展时零操作),内层区分有无 comps 节点分别 Add。**已用 `_tmp/sim_bounty_board_patch.py` 模拟双情形验证 PASS** |
| `Languages/*/Keyed/RK_Bounty_Keys.xml` | UI 文案 `RK_Bounty.*` ×53 键(中英 + 简体中文副本三目录对齐) |

## 三、RimThemesLite(skyarkhangel.RimThemesLite)适配

该 mod 在 **Widgets/GUI 层全局 Harmony 接管**(ButtonTextWorker/ButtonTextSubtle/Label/TextField/DrawWindowBackground/DrawTexture/字体样式 + `ColorsSubstitution` 原版颜色值精确映射),主题 meta 键 = 原版颜色字段路径(`Widgets.WindowBGFillColor`、`MenuSectionBG*`、`OptionSelected/Unselected*`、`GenUI.MouseoverColor`、`FloatMenuOption.*`)。

∴ 适配正解 = **弹窗全部用原版 Widgets + 原版颜色常量表达,不发明自定义色**:
- 行背景/选中态:`Widgets.DrawOptionBackground(row, selected)`(走 Option*Fill/Border 四色,主题全映射);悬停 `GenUI.MouseoverColor`。
- 次要文字:`ColoredText.SubtleGrayColor`;赏金额:`ColoredText.CurrencyColor`;派系名:语义色 `Faction.Color` 保留。
- 窗口背景/按钮/输入框/FloatMenu:原生路径,自动被主题接管。
- ⚠ 反编译确认:1.6 原版**没有** `ColoredText.TextColor`/`SubUnitGreyColor`,别用;有 `SubtleGrayColor`/`CurrencyColor`。

## 四、SimpleWarrants API 速查(反编译 `_tmp/sw_decomp/`)

- `WarrantsManager`(public, GameComponent):`Instance`、`availableWarrants/acceptedWarrants/createdWarrants/takenWarrants/postponedWarrants`(List\<Warrant\>)。
- `Warrant`(abstract, public):`issuer/thing/message/status/createdTick/tickToBeCompleted`、`MaxRewardValue()`、`Draw(Rect, doAcceptAndDeclineButtons=true, doCompensateWarrantButton=false)`、`DoAcceptAction()`(子类覆写内含 `GenerateQuestAndMakeAvailable(SW_DefOf.SW_Warrant_*)`)。
- `Warrant_Pawn`:`reason/rewardForDead/rewardForLiving/Pawn(things 解 Corpse)`;`Warrant_TameAnimal`:`Reward/AnimalRace`;`Warrant_Artifact`:`reward`。
- 罪名/口信 RulePack(`SW_WantedFor`/`SW_Messages`)在**本 mod Defs 内**,汉化走 `DefInjected/RulePackDef`(索引式 `rulePack.rulesStrings.N`,顺序须与 defs 严格一致)。

## 五、迭代史与坑(外挂)

详见 [任务大修HSK_悬赏公告牌_迭代附注.md](任务大修HSK_悬赏公告牌_迭代附注.md)(§5 SW API 之前的迭代记录、§六无线电、§七限流与期限、§八四期优化、§九五期性能降频、§十 v5 海报+实物奖励、并行会话冲突处置、构建/API 坑)。

## 六、悬赏体系六期(2026-08-31):科技门槛/预付/关系倍率

**规则核心 `BountyRules.cs`**(新增,四枚 Harmony 补丁在 `HarmonyPatches.cs`):

- **生成端科技门槛**(`GenerateRandomWarrant` postfix):目标必须来自**世界NPC派系**(humanlike/未灭/可见/非玩家),且与发布方科技档**相差 ≤1 级**;违规销毁临时 pawn 返回 null 封死"原始时代悬赏极致时代天网"。
- **奖励重写**(`AssignRewards` postfix,`private static` 无重载):`赏金 = 角色身价 × 科技惩罚 × 关系倍率`;科技惩罚=目标档**低于本方 ×0.5**;关系倍率=关系 0 最低 ×1.0、+100→×1.5、−100→×1.4(V 形中间线性);SW 形状保留(30% 概率尸体酬金=活捉价 30~70%,财富缩放按其设置)。
- **发出端**(`TryAddWarrant` prefix):目标档 > 我方+1 级**拦截**;预付手续费按被通缉方科技档 500(原始)~5000(极致/极超),**越级(目标>我方档)费用×2**;`Utils.AllPlayerSilver()` 核余额、`Warrant.Pay` 扣银,不足弹窗拦截;交付酬劳仍由 SW 在完成时收取。
- **发单激怒**(`Warrant_Pawn.OnCreate` prefix 覆盖 SW 固定 -80):激怒值 = 12 + (派系领袖+40, 王室头衔 seniority/25) + 身价/150,clamp 12~110。
- **受雇成功率**(`Warrant_Pawn.SuccessChance` postfix,2026-08-31):玩家发布的悬赏由 NPC 派系执行,成功率随**被通缉方科技档**线性 66%(原始)→33%(极致)(perLevel=0.0825,两端 clamp);仅覆盖派系目标,动物/无派系目标保留 SW 原公式(赏金/身价);`AcceptChance` 刻意不动——赏金越高 NPC 接单越快,保持激励。海报卡对 `issuer==OfPlayer` 的单显示「受雇成功率」行,<50% 红字。
- **海报行**:被通缉方(派系名·科技档)/我方关系(数值·倍率,负值红)/科技惩罚(×0.5 红字)/受雇成功率(仅我方发单)。
- **任务名重写**(DefInjected QuestScriptDef ×2 目录):悬赏令:通缉/猎杀/寻回/驯捕;描述补 `[victim_faction_name]`(pawn 派系变量,与 `[asker_faction_name]` 同机制)。
- **API 坑**:SW `SimpleWarrantsMod`(Mod 壳类)是 **internal**——`Settings.warrantRewardScaling` 走 `AccessTools.TypeByName` 反射读一次缓存;1.6 `ResearchManager.IsFinished` 不存在,用 `ResearchProjectDef.IsFinished`;`Utils.AllPlayerSilver()`/`Warrant.Pay` 为 public 可直接复用。玩家科技档 = 派系基线与已完成研究最高 `techLevel` 取大(按游戏日缓存)。

## 七、悬赏成败事件故事化 + 入口修复 + 执行进度信(2026-08-31)

→ 已外挂:**`docs/09_任务体系/任务大修HSK_悬赏故事化七期.md`**(七期:成败故事信实现细节、PatchAll 入口缺失教训、script def 正则改法;八期:接单/追踪/接火进度故事信与变体扩充)。

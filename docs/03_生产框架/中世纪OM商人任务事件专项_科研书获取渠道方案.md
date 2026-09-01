# 中世纪OM 商人/任务/事件专项 — 科研书获取渠道整合方案

> 2026-08-27。MO 未整合盘点的 §8 深化专项(总盘点见 `中世纪OM未整合内容清单与优化评估.md`)。
> 目标: 吸收 MO「商人/任务/事件」的**获取渠道设计**,强化蓝图书(科研书)的获取玩法;**不搬 MO 本体**。
> 数据源: MO 1.6.2.2 本地副本 `workshop\content\294100\3219596926\1.6\` 实读 + `HSK工业科研大修` 现有蓝图补丁实读 + 部署 `Mods`/`ModsConfig.xml` 核查。

---

## 0. 环境硬约束(核查结论,影响一切方案)

| 项 | 实测结论 |
|---|---|
| MO 本体 | **未启用**。`ModsConfig.xml` 的 `vitech.medievaloverhaul` 实际 = `HMC Vile's Pre-Industrial Overhaul`(Vile 复用同名 packageId),非 DankPyon MO。 |
| VEF Core | **已装且激活**(2026-08-27 晚二次核实修正)。工坊 `content\294100\2023507013`(Vanilla Expanded Framework)直接加载——1.6 工坊 mod 不在 Mods 目录,首查只扫 Mods 导致误报;Unified 导出含 VEF 类引用 169 处/VFEC 节点 10 个,`KCSG`/`VEF QuestNode` 均可用。 |
| 直接后果(修正) | VEF 激活 → KCSG `GenStep_CustomStructureGen`/`StructureLayoutDef`/`SymbolDef` 与 `VEF.Storyteller.QuestNode_GetFaction` **可用**。仍不可用的只剩: MO DLL 类(Building_QuestScanner / Building_Lootable / GameComponent_QuestFinder / IncidentWorker_* / RaidStrategyWorker_MedievalSiege)与 MO 派系/生物 def(MO 已订阅**未勾选**: Unified 仅 4 处 MayRequire 残留引用, 0 个 DankPyon def 实体)。 |
| 可用底座 | VEF(KCSG 结构生成/VEF QuestNode)+ 原版 QuestScriptDef/SitePart/QuestNode(`QuestNode_GetFaction` 原版也存在)+ 自研 `BlueprintUnlockHSK.dll` / 新 `SchematicGateHSK.dll`(蓝图书获取/图纸门禁)+ 原版钩子:`SitePart_ItemStash`、古Complex 战利池 `MapGen_AncientComplexRoomLoot_Default`、`Reward_ItemsStandard`、考古 Artifacts 池、DLC 宠幸(Royalty/Ideology/Biotech/Odyssey 全启用)。 |

---

## 1. MO「获取侧」全拆解(实读)

### 1.1 商人侧(纯 XML 可参考,定义在 `Defs/TraderKindDefs/`)

5 个 TraderKindDef(Base_Medieval / Caravan_Medieval / Caravan_Alchemy / Caravan_Soren / Visitor_Medieval),挂四大贵族派系。书籍售卖实测(学者商队 Caravan_Medieval):

| stockGenerator | 标签/对象 | countRange | 备注 |
|---|---|---|---|
| StockGenerator_MarketValue | `DankPyon_Schematic`(图纸) | **1~5** | 高价值稀有 |
| StockGenerator_MarketValue | `DankPyon_Treatise`(专著) | 2~5 | |
| StockGenerator_MarketValue | `DankPyon_Tale`(传说书) | 3~5 | |
| StockGenerator_MarketValue | `DankPyon_Book`(泛书籍) | 1~3 | |
| StockGenerator_SingleDef | `DankPyon_Paper` | 50~250 | |
| StockGenerator_BuyTradeTag ×4 | 上述四类**全回购** | — | 玩家可倒卖 |

**机制要点**: 全部走 tradeTag + 市场价值抽档,零 DLL。你的蓝图书 ThingDef 挂一个 `BlueprintBook` tradeTag 即可同法复用。

### 1.2 任务侧(`Defs/QuestScriptDefs/` + `Sites/Parts/` + `MapGeneration/`)

| 任务 | 生成方式 | 依赖链 |
|---|---|---|
| 小废墟清剿 `DankPyon_OpportunitySite_SmallRuin` | 自动生成(weight 1.5,minPoints 350,4~8 天过期) | SitePart 标签 → **VEF QuestNode_GetFaction**(固定强盗派系)→ KCSG 结构布局(废墟地图含预置 Lootable 架/箱/藏宝图/书)→ 奖励=钱+好感+宠幸+**营地战利品 addCampLootReward** |
| 中废墟/蛇窟清剿 | 同上(weight/points 递增) | 同上 + MO 怪物(蛇/龙) |
| 邪教 1/2/3 级 | **weight=0,不自动生成**——用知识之书 CultBook 道具触发(祭坛×5→邪教宝珠×9 收集链) | MO 派系+DLL 扫描链 |
| Alp 月蚀 | 注释停用 | — |

**机制要点**: 脚本本体是**标准原版 QuestNode_Sequence 编排**,可抄结构;卡点在 site part 的地图生成(KCSG)与守卫派系(VEF/DLL)。奖励语义「任务=货币,站点内 loot=实物书源」是双层设计。

### 1.3 藏宝图→探索工作台链(最有价值的「主动获取」设计)

```
[获取线索] 废墟 Lootable 架/箱/碎纸堆 + 强盗掉落 + 商人
     ↓  DankPyon_TornNote_Hideout(堆3,150银)
[解码]   探索工作台 DankPyon_QuestFinder(3×3,Building_QuestScanner,DLL)
     ↓  链接件升级: 探险家档案柜(可存9张图)/古基座/古球/信鸽站, 0-1-5-9 件解锁 1~3 级
[生成]   GameComponent_QuestFinder 长距离扫描 → 世界地图三级藏身处(26 方向布局)
     ↓
[收获]   藏身处=全部储物建筑的 Lootable 变体 → 搜出 三级知识书/图纸/大量物资
```

### 1.4 事件侧(`Defs/Storyteller/`)

稀有巨兽路过 ×4(Daer/龙/狮鹫/巨魔,baseChance 0.7,minRefireDays 30)/ 哥雷姆陨石(≥500 点)/ 矿井虫灾 / 中世纪围城 RaidStrategy。**全部 workerClass=DLL + 引用 MO 生物/派系 → 对本环境整体不可移植,亦与书无关**。你已有的 Cybranian 陨石/老人注入点承担了同类职能。

## 2. 现状对照:你的蓝图书获取渠道(BlueprintUnlockHSK)

| 渠道 | 现状 | MO 对照差距 |
|---|---|---|
| 商人卖书 | ✅ 7 类 TraderKindDef 注入 `StockGenerator_BlueprintTrader`(按派系 techLevel 分档,中世纪/工业卖全套但稀有,太空/极致约半数) | 无"专精学者商人"人设/权重;缺书类**回购**(玩家倒卖/换书回路) |
| 任务奖励 | ✅ 注入 `Reward_ItemsStandard`(weight 0.5 低概率)+ 物品贮藏藏宝处(DLL) | 无"清剿→站点实物 loot"第二层;书是**附带奖励**而非**目标物** |
| 世界探索 | ✅ GoExplore 奖励注入 | 无"线索→定向解锁"链 |
| 随机事件 | ✅ Cybranian 盟友礼物(25%)/陨石/老人 | 同上 |
| 玩法结构 | **全部被动**(刷商人/刷任务碰运气) | MO 的藏宝图链=**主动探索支柱**,恰好是你体系缺的一腿 |

## 3. 分阶段落地建议(全部不引 MO DLL / 不依赖 VEF)

### P1 学者商人 + 书市回购(纯 XML,~0.5 天) — 立即可做

- 新建 1~2 个 TraderKindDef(如"流浪学者""古籍商"),挂**原版 Outlander 友好派系**(不改派系体系),stockGenerators 复用现成 `BlueprintUnlockHSK.StockGenerator_BlueprintTrader`(自动按档位)+ `StockGenerator_SilverFrac` 等基础件;权重可仿 MO: 书商多卖、普通商少卖。
- 给蓝图书 ThingDef 补 tradeTag(如 `RK_BlueprintBook`),商人加 `StockGenerator_MarketValue` + `StockGenerator_BuyTradeTag` 实现**回购倒卖**(仿 MO 四层标签结构)。
- 平衡参照: MO 图纸 1~5/专著 2~5/传说 3~5 本每商队。
- 风险: 与现有 7 类注入叠加后书供应过量 → `StockGenerator_BlueprintTrader` 已有"该档已读集合"过滤,复用时把学者商人**权重调高但数量收紧**即可。

### P2 藏宝图→定向挖宝链(自研 DLL 扩展,~3-5 天) — 核心玩法支柱

- ① 新物品「藏宝图残片」(`RK_TreasureMapFragment`,堆叠,低成本 XML),投放: 并入 `ThingSetMaker_BlueprintReward` 同池(任务/贮藏掉)+ 学者商人偶尔出售 + 世界物件搜刮。
- ② `BlueprintUnlockHSK.dll` 扩展 `CompUseEffect_UseMap`: 使用消耗残片 N 张(1/3/5 对应三档)→ 在附近生成**原版 SitePart 组合**的世界站点(推荐 `BanditCamp 风格=原版 ItemStash + Manhunters/Hostiles`)+ 任务信。
- ③ 站点地图生成: 轻量档用原版 ItemStash 埋箱(把箱内容物 SetMaker 换成参数化 `ThingSetMaker_BlueprintReward`, 高档=必出指定系列卷册); 因 VEF 可用, 可升级 KCSG `GenStep_CustomStructureGen`+自建 StructureLayout(用原版/HSK 家具 symbol 重绘, 不引 MO def)做建筑级废墟
- ④ 可选升级件(轻量仿 MO): 一座「探秘桌」工作台(普通 WorkTable + bill"解读藏宝图",消耗残片+工时,**替代** MO 的 QuestScanner DLL——用原版 recipe 框架就能做,不需要链接件体系;链接件分级玩法建议砍掉,性价比低)。
- 验收: 清档测 1/3/5 残片三档站点、奖励必含对应档位书、与 RimQuest 并存不炸。

### P3 「废墟清剿」原版化任务脚本(纯 XML + P2 产物,~2-3 天)

- 抄 MO `Script_SmallRuin` 节点编排: `VEF.Storyteller.QuestNode_GetFaction` **可直接沿用**(factionDef 指原版 Pirate), 无需换血;SitePart 自建轻量版;废墟地图用 KCSG + 自建 StructureLayout 重绘原版家具废墟(不引 MO def)。
- 奖励双层仿 MO: `QuestNode_GiveRewards`(钱/好感/宠幸)+ 站点实物=蓝图书/残片(P2 的 SetMaker)。
- `rootSelectionWeight` 0.8~1.5、minPoints 350 起,4~8 天过期,直接抄 MO 实测值。
- 与 RimQuest 并存: 都走标准生成器,任务池自然叠加,无冲突面。

### P4 明确缓做/放弃(⛔/🅿)

| 项 | 原因 | 处置 |
|---|---|---|
| 邪教三级 CultBook 收集链 | 卡 MO 派系+DLL 扫描+26 向布局 | 🅿 若 P2 反响好,可把"宝珠×9"改成"残片收集"变体重做 |
| 中世纪围城 RaidStrategy | MO DLL 攻城 AI,CE 下另需适配 | ⛔(CE/HSK 已有自己的袭击体系) |
| 巨兽路过/哥雷姆陨石/虫灾 | 卡 MO 生物(§7)+DLL worker | ⛔(Cybranian 渠道已覆盖"随机书来源"职能) |
| Base_Medieval 等 MO 派系商人 | 卡四大贵族派系(§8 整体) | ⛔(P1 用原版派系学者商人替代其职能) |

## 4. 建议执行顺序

P1(半天见效,纯 XML)→ P2(核心支柱,DLL 小扩展)→ P3(玩法闭环,纯 XML)。
> 三段做完 = MO 获取侧三条腿(商人/任务/主动探索)全部吸收完毕,零 MO DLL(VEF 已在场, KCSG 可选用),全部收在 `HSK工业科研大修` 一个 mod 内。

## 5. 落地铁律提醒

- 补丁一律 xpath 存在性门控(AGENTS.md §3,1.6 Operation 级 MayRequire 失效);新 TraderKindDef/SitePartDef 用 Add 到本 mod 自己的 Defs,不打别人补丁。
- 新增研究(若给探秘桌挂科研)按 §6 六档补 SK `AdvancedResearchExtension` 硬门槛。
- 新 Def 补 DefInjected `<LanguageData>`(§4);DLL 改动注意 Tick 逻辑性能铁律(§9,扫描/站点生成走事件驱动)。
- 工作区改完**立刻双目录同步**(§1),DLL 重编译走 `build.ps1`。

---

*数值以游戏内实测为准;MO 参数抄自其 1.6 xml 原文。*

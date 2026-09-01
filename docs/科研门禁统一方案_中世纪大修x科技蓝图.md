# HSK 科研门禁统一方案(中世纪大修 × 科技蓝图与逆向工程)

> 方案日期: 2026-08-26。状态: **方案设计(待确认)**。
> ⚠️ **已废弃(2026-08-26 用户新约束)**: 本方案 v1 的「抽取 MO 科研解锁功能再合并成新 mod」路线已不采用。用户确认: 不搬 MO 研究树、科研大修为长期独立 mod 只做科研修改、书架/设施归 hsk工业大修、重点为任务机制 + 书架阅读获取渠道。**修订版见 [`docs/科研大修方案_v2.md`](科研大修方案_v2.md)**,本文档保留作 MO 科研机制参考(§二 现状盘点仍有效)。
> 目标: 把中世纪大修(Medieval Overhaul,MO)的「科研解锁功能(图纸 Schematic 门禁)」独立成 mod,再与本地「科技蓝图与逆向工程HSK」整合成一个新 mod,并在此基础上设计 HSK 科研门禁的统一方案。
> 相关文档: [`docs/中世纪大修内容梳理.md`](中世纪大修内容梳理.md)、[`docs/ProcessorFramework框架梳理.md`](ProcessorFramework框架梳理.md)、`科技蓝图与逆向工程HSK/科技配置/` 五份既有方案。

---

## 一、背景与目标

### 1.1 为什么要做

MO(中世纪大修)整合入 HSK+Vile 时,其内部自带一套**科研解锁机制(图纸门禁)**,与本地已成熟运行的「科技蓝图与逆向工程HSK」**功能高度重叠**:

| | MO RequiredSchematic | 科技蓝图 BlueprintTargetExtension |
|---|---|---|
| 门禁载体 | ResearchProjectDef 的 DefModExtension | 蓝图 ThingDef 的 DefModExtension |
| 物品 | 图纸(Schematic),书籍体系 BookBase | 蓝图(Blueprint),ResourceBase |
| 解锁动作 | 书架阅读(不消耗,1 张即解锁) | 直接使用(消耗,多张累加科研进度) |
| 研究进度 | 无加成 | 每张加科研点(250~1000) |
| 获取渠道 | 废墟/藏身处搜刮 + 学者商人 + 探索任务 | 商人/掉落/5 路事件任务投放 |
| 门控范围 | 18 个 MO 中世纪研究 | 43 个 HSK 各档节点 |
| UI | 研究窗口超链接标注 | 顶栏进度条 + 节点角标 |
| 硬依赖 | MO 本体(研究树/书架/藏身处) | 仅 Harmony + Core SK |

**结论**: 两套机制语义相同("需要图纸才能研究")但实现分裂。整合后若都保留,会出现:同一个研究(如原版 Greatbow)被 MO 的 Biotech 模式图纸与科技蓝图的门控**同时/重复门控**、两套 UI 互相干扰、两套存档状态互不知晓。必须统一。

### 1.2 目标

1. **抽取**: MO 的科研解锁功能(RequiredSchematic 门禁 + 图纸 + 相关 DLL 补丁)独立成单独 mod,与 MO 本体解耦(不依赖探索桌/藏身处/怪物/论文体系)。
2. **整合**: 独立出的机制与「科技蓝图与逆向工程HSK」合并成一个新 mod,一套门禁机制、一套配置、一套 UI、一套存档。
3. **重设计**: 在统一机制上,结合 MO 的「图纸稀有古物」叙事与科技蓝图的「分级/银行/动态提权/任务核心」机制,设计新的科研门禁方案。

---

## 二、现状盘点(2026-08-26 实测)

### 2.1 中世纪大修侧(科研解锁功能全貌)

**门禁标记**: `MedievalOverhaul.RequiredSchematic`(DefModExtension,唯一字段 `<schematicDef>`),挂 ResearchProjectDef 上。**恒 1 张、不消耗**(Biotech 模式除外)。

**源码内 14 个门禁研究**(全在 DankPyon_MedievalResearchTab):

| defName | label | cost | 图纸 |
|---|---|---|---|
| DankPyon_Crossbow | crossbow | 400 | Schematic_Crossbow |
| DankPyon_HeavyCrossbow | arbalest | 700 | Schematic_HeavyCrossbow |
| DankPyon_RepeaterBallista | ballista repeater | 2500 | Schematic_BallistaRepeater |
| DankPyon_Plasteel | Mithril | 4000 | Schematic_Steel(秘银) |
| DankPyon_Tar | tar | 400 | Schematic_Tar |
| DankPyon_Gunpowder | gunpowder | 1000 | Schematic_Gunpowder |
| DankPyon_WarBow | war bow | 700 | Schematic_WarBow |
| DankPyon_MilitaryPolearms / NoblePolearms | 军事/贵族长杆 | 600/1000 | Schematic_Military/NoblePolearms |
| DankPyon_MilitaryMaces / NobleMaces | 军事/贵族锤矛 | 600/1000 | Schematic_Military/NobleMaces |
| DankPyon_MilitaryBlades | 军事刀剑 | 600 | Schematic_MilitaryBlades |
| DankPyon_IntermediateCooking / AdvancedCooking | 中级/高级烹饪 | 400/800 | Schematic_Intermediate/AdvancedCooking |

**补丁注入 4 个**(Biotech 模式 active 分支): 原版 `LongBlades`→NobleBlades、`Greatbow`→GreatBow(另 2 个在 1.5 版 Change_ResearchProjectDef 内)。

**图纸物品**: `DankPyon_SchematicBase`(ParentName=BookBase)+ 16 具体图纸 + 通用 `DankPyon_Schematic`(BookOutcomeProperties_GainResearch 按 tab 随机)。comp = CompProperties_Book + `MedievalOverhaul.BookOutcomeProperties_GainResearchDefinable`(精确指定研究)。分类 `DankPyon_Schematics`(parent=Books)。**无制造配方**,来源 = 废墟战利品架/藏身处三级书卷/学者商人(StockGenerator_MarketValue 卖 1~5 个)/探索桌任务。

**DLL 侧相关类**: `RequiredSchematic`、`ResearchProjectDef_CanBeResearchedAt`/`CanStartNow` 补丁(HasBook 判定)、`HasBook`/`PlayerHasSchematic`、`BookOutcomeProperties_GainResearchDefinable`、`MainTabWindow_Research_DrawUnlockableHyperlinks_Patch`、`Building_Bookcase`、`PatchOperation_ToggleSettings`(biotechSchematic 双模式开关)。

**可砍依赖**: 探索桌/藏身处/废墟搜刮建筑/怪物/论文(CompPropertiesUseEffect_LearnSkillImproved/RecipeWorker_MakeSkillBook/TreatiseSkill)/技能书 5 连补丁/CompQuality_SetQuality —— 均与"图纸门禁"本体无关。

**强耦合**: 18 个门禁研究的前置链穿过 MO 的 RusticFurniture/Smithing/Alchemy/Engineering 等 40+ MO 研究;DankPyon_Plasteel 等引用 `DankPyon_AdvancedResearchBench` 研究台。

### 2.2 科技蓝图与逆向工程 HSK 侧(机制全貌)

**配置**: 蓝图 ThingDef 挂 `BlueprintUnlockHSK.BlueprintTargetExtension`(targetTech/tier/researchPoints/unlockCount 四字段),当前 43 个蓝图覆盖 43 个节点。

**门控判定**: `Patch_CanStartNow` Prefix——未解锁且前置未满足时 CanStartNow=false。

**使用流程**(CompUseEffect_Blueprint): ①前置未研究→提示不消耗;②已研究→科研值存入存储池(空间不足不消耗);③未研究→加科研进度+计数,累计 unlockCount 张解锁。

**科研进度银行**: `BlueprintUnlockTracker`(WorldComponent,存档 useCounts/unlocked/storedPoints),上限随已解锁/完成最高档门控科技动态扩容 500/1000/2000/4000/8000,读档钳制。

**分级(v3.3 确认)**: tier1 中世纪 250/1 张、tier2 工业 400/1、tier3 太空 500/2、tier4 极致 700/3、tier5 超凡 1000/4。

**动态提权**: `BlueprintStuckMonitor`(600 ticks 节流 + 事件驱动惰性重建),stuck 非空时 PickRandomBlueprint 80% 偏向卡住节点。

**UI**: 顶栏「全览」右侧进度条(280×26,悬停看档位/全部门控状态)+ 每个门控节点上「已收集 X/Y」角标(RTSK.Node.Draw Postfix)。

**投放(5 路)**: 商人(StockGenerator,按派系科技等级限售)、敌人掉落(原版 DLL)、研发台自制、RimQuest 任务(物品贮藏/匪徒营地)、Go Explore(失落之城/监狱营/拦截消息)、Cybranian 事件(陨石/老人)。

**源码**: `Source/BlueprintUnlockHSK.cs` 1131 行 C#5(build.ps1 csc 编译)。

---

## 三、核心设计:统一科研门禁框架

### 3.1 架构决策(为什么以科技蓝图为底座)

| 维度 | 选择 | 理由 |
|---|---|---|
| 机制底座 | **科技蓝图 BlueprintUnlockHSK 机制** | 功能更全(分级/银行/动态提权/UI),已在本环境实机运行验证,配置全 Defs 驱动无需重编译 |
| MO RequiredSchematic | **转换并淘汰** | 不保留第二套门禁;MO 门禁研究改为挂统一蓝图 |
| 图纸形态 | **统一为蓝图(直接使用)** | 书架阅读仪式感代价高(依赖书架/阅读 Job),直接使用更直观、与任务投放契合 |
| MO 研究树 | **保留独立 tab,门禁接入统一机制** | MO 的中世纪研究树自成体系,并入 HSK 13 tab 需重排坐标/前置,风险大(见 §5) |
| 双存档 | **统一 WorldComponent 存档** | 科技蓝图已有 BlueprintUnlockTracker,MO 侧无独立存档(靠 Def 标记),无迁移负担 |

### 3.2 新 mod 结构

```
新 mod(拟名: HSK 科研蓝图与门控 / local.ratkin.researchgates)
├─ 1.6/Assemblies/ResearchGateHSK.dll      ← 科技蓝图 BlueprintUnlockHSK.cs 重构而来(含 MO 机制接管)
├─ 1.6/Defs/
│   ├─ ThingDefs_BlueprintGates.xml        ← 43 既有 + MO 18(转换) + 新增(见 §4.3)
│   ├─ ThingDefs_Categories.xml            ← TechnologyBlueprints + DankPyon_Schematics 合并
│   ├─ (MO 迁移) Schematic 转换补丁输出    ← RequiredSchematic → BlueprintTargetExtension 的映射定义
│   └─ (MO 迁移) 学者商人/书架等抽取       ← 可选
├─ Patches/
│   ├─ 99_ModAssistant标记.xml
│   ├─ BlueprintUnlock_ClearTechprints.xml ← 清原版 techprint 门控(扩至 MO 节点)
│   ├─ BlueprintUnlock_EventIntegration.xml
│   ├─ BlueprintUnlock_Traders.xml
│   └─ MO_RequiredSchematic_转换.xml       ← 新: 把 MO 门禁研究挂上统一蓝图(存在性门控,未装 MO 自动跳过)
└─ Source/ResearchGateHSK.cs               ← 统一源码
```

**关键原则**: 所有门控配置 = Defs 驱动(BlueprintTargetExtension),增删节点不重编译;对 MO 的接入全部 xpath 存在性门控(未装 MO 时新补丁静默跳过,MO 缺失的研究蓝图惰性无效)。

### 3.3 门禁判定合并(单一路径)

统一后只有一条判定链(科技蓝图现有,补 MO 兼容):

```
CanStartNow = 统一蓝图判定(IsUnlocked && PrereqsFinished)
    └─ IsUnlocked 来源:
         ① 蓝图使用累计 unlockCount 张(科技蓝图路径)
         ② (MO 研究) MO 图纸阅读/分析标记 → 转换为 IsUnlocked(兼容层)
```

- 对 MO 研究: 新补丁 `MO_RequiredSchematic_转换.xml` 在 MO 存在时,把 MO 18 个研究的 RequiredSchematic 门禁**替换**为统一 BlueprintTargetExtension(不删 MO 原扩展,而是让统一判定优先;或直接 Add 对应蓝图 ThingDef 到 MO 研究)。
- **重复门控去重**: 原版 Greatbow/LongBlades 若被 MO(Biotech 模式图纸)与科技蓝图(Blueprint_Medieval_Greatbow 等)同时门控 → 统一后只走蓝图路径,MO 的 Schematic 补丁由 MO 整合层关闭(`biotechSchematic` 设置或补丁移除)。

### 3.4 分级与数值(沿用科技蓝图 v3.3,微调)

| tier | 时代 | 每张科研值 | 解锁张数 | 市场价 | 说明 |
|---|---|---|---|---|---|
| 1 | 中世纪 | 250 | **1** | 250 | MO 中世纪研究归此档(图纸感弱化,门槛=前置链本身) |
| 2 | 工业 | 400 | 1 | 400 | 预留(当前无工业门控) |
| 3 | 太空 | 500 | 2 | 700 | 既有 Spacer 节点 |
| 4 | 极致 | 700 | 3 | 1200 | 既有 Ultra 节点 |
| 5 | 超凡 | 1000 | 4 | 2000 | 既有 Archotech 节点 |

MO 研究统一按 **tier1(250/1)** 处理——MO 图纸原本"1 张即解锁、不消耗",对齐后蓝图为消耗品但只需 1 张,体验接近且与统一机制一致。

### 3.5 科研进度银行(保留,MO 研究接入)

- 存储池上限 500/1000/2000/4000/8000 动态扩容规则不变。
- MO 研究"已研究→蓝图存银行"同样生效(机制通用)。
- 读档钳制保留(历史存档超限截断)。

### 3.6 UI(合并两边的强项)

| 元素 | 来源 | 合并后 |
|---|---|---|
| 顶栏进度条(存储 X/Y) | 科技蓝图 | 保留 |
| 节点「X/Y」角标 | 科技蓝图 | 保留(覆盖 MO 研究节点) |
| 研究窗口「需图纸」超链接标注 | MO(MainTabWindow_Research_DrawUnlockableHyperlinks_Patch) | **移植进新 DLL**(对 MO 研究节点也显示"需蓝图",提示来源) |
| 蓝图使用反馈(消息/信件) | 科技蓝图 | 保留 |

### 3.7 投放渠道(合并 5+3 路)

| 渠道 | 现状 | 合并后 |
|---|---|---|
| 商人(按派系科技等级限售) | 科技蓝图 | 保留;MO 学者商人如整合 MO 本体则额外生效 |
| 敌人掉落 | 科技蓝图 | 保留 |
| RimQuest 任务(物品贮藏/匪徒) | 科技蓝图 | 保留 |
| Go Explore 三站点 | 科技蓝图 | 保留 |
| Cybranian 事件(陨石/老人) | 科技蓝图 | 保留 |
| **MO 废墟/藏身处战利品** | MO | 仅 MO 整合时生效(蓝图加入 MO 战利品池,补丁门控) |
| **MO 学者商人** | MO | 仅 MO 整合时生效(可并入统一 StockGenerator) |
| **MO 探索桌任务** | MO | 仅 MO 整合时生效(任务奖励池注入统一蓝图) |

统一规则: 动态提权(卡节点 80% 偏向)对**所有渠道**生效——MO 战利品/商人同样偏向当前卡住的 MO 研究蓝图。

---

## 四、门控节点全量方案

### 4.1 门控原则(吸收 MO 设计理念 + 科技蓝图既有规则)

1. **MO 原则**: 门禁适合「时代终局 / 跨时代门槛 / 高级装备」;不适合中前期链条科技与基础设施(会卡死)。
2. **科技蓝图规则**: 石器时代不门控;中世纪只卡跨时代/高级装备/终端工艺,不卡基础链(不门控 Craft_B1/研究台/蒸汽水轮沼铁/采矿)。
3. **新增规则(统一后)**: MO 研究中世纪门禁(弩/火药/秘银/贵族武器等)恰好符合"跨时代门槛/高级装备",与科技蓝图中世纪组同档;MO 的原版研究改动(LongBlades/Greatbow 前置改挂 MO 链)若整合 MO 则保留其门禁,若不整合 MO 则不动。

### 4.2 去重与冲突处理(关键)

| 冲突 | 现状 | 处理 |
|---|---|---|
| 原版 Greatbow | 科技蓝图门控(Blueprint_Medieval_Greatbow,tier1) + MO Biotech 模式门控(DankPyon_Schematic_GreatBow) | **统一走科技蓝图**;MO 整合层关闭 biotechSchematic 模式(该模式依赖 Biotech 分析物,HSK 环境非默认) |
| 原版 LongBlades | MO Biotech 模式门控(NobleBlades 图纸) | 统一后:科技蓝图未门控 LongBlades → **保留 MO 门禁语义**,改为蓝图门控(新增 Blueprint_LongBlades,tier1)或随 MO 中世纪化方案定 |
| MO 的 DankPyon_Crossbow 等 14 个 | MO RequiredSchematic | 全部转换: 每个 MO 研究 ↔ 一张统一蓝图(Blueprint_MO_<defName>,tier1,250/1) |
| 蓝图/图纸 defName 冲突 | — | MO 图纸(DankPyon_Schematic_*)与科技蓝图(Blueprint_*)前缀不同,无冲突;合并分类时 TechnologyBlueprints 为主,DankPyon_Schematics 分类由 MO 整合层保留 |

### 4.3 门控节点清单(合并后,约 43 + 18 = 61,去重后 ~58)

**A. 科技蓝图既有 43(保持不变)** —— 中世纪 7(PlateArmor/Metallurgy_B5/Steel_B3/RKHSK_Tools_Medieval3/Medicine_B2/Greatbow/Ballista_B1)+ 太空 9 + 极致 22 + 超凡 5(含 Stargate_F1)。

**B. MO 门禁 18 个(转换,新增 18 张蓝图)**:

| MO 研究 defName | label | 蓝图 | tier | 科研值/张 | 张数 |
|---|---|---|---|---|---|
| DankPyon_Crossbow | 弩 | Blueprint_MO_Crossbow | 1 | 250 | 1 |
| DankPyon_HeavyCrossbow | 重弩 | Blueprint_MO_HeavyCrossbow | 1 | 250 | 1 |
| DankPyon_RepeaterBallista | 连发弩炮 | Blueprint_MO_RepeaterBallista | 1 | 250 | 1 |
| DankPyon_Plasteel | 秘银 | Blueprint_MO_Mithril | 1 | 250 | 1 |
| DankPyon_Tar | 焦油 | Blueprint_MO_Tar | 1 | 250 | 1 |
| DankPyon_Gunpowder | 火药 | Blueprint_MO_Gunpowder | 1 | 250 | 1 |
| DankPyon_WarBow | 战弓 | Blueprint_MO_WarBow | 1 | 250 | 1 |
| DankPyon_MilitaryPolearms / NoblePolearms | 军事/贵族长杆 | Blueprint_MO_MilPole / NoblePole | 1 | 250 | 1 |
| DankPyon_MilitaryMaces / NobleMaces | 军事/贵族锤矛 | Blueprint_MO_MilMace / NobleMace | 1 | 250 | 1 |
| DankPyon_MilitaryBlades | 军事刀剑 | Blueprint_MO_MilBlades | 1 | 250 | 1 |
| DankPyon_IntermediateCooking / AdvancedCooking | 中/高级烹饪 | Blueprint_MO_MidCook / AdvCook | 1 | 250 | 1 |
| (MO Biotech 模式注入,可选) LongBlades | 长剑 | Blueprint_LongBlades | 1 | 250 | 1 |

> 全部蓝图对缺失目标**惰性无效**(BlueprintGateDatabase.AllGated 用 GetNamedSilentFail),未装 MO 时这些蓝图因目标研究不存在而自动不生成/不投放,零报错。

### 4.4 科研体系配套(整合 MO 研究树的处理)

见 §5。核心: MO 的中世纪研究树**保留独立 tab**(DankPyon_MedievalResearchTab),不与 HSK 13 tab 合并;统一门禁机制通过转换补丁对 MO 研究生效,UI 角标/进度条自然覆盖。

---

## 五、MO 研究树接入策略(方案取舍)

### 5.1 三条候选路线

| 路线 | 做法 | 优点 | 风险 |
|---|---|---|---|
| **A. 独立 tab 保留(推荐)** | MO 研究树整体保留在自己 tab,门禁接入统一机制 | 不重排坐标/前置,MO 整合本体时改动最小;门禁统一已解决重复问题 | MO 研究树与 HSK 树并存,玩家要在两个 tab 间切换;部分原版研究被 MO 改前置(Stonecutting→RusticFurniture 等)仍会牵动 HSK 引用 |
| B. 并入 HSK tab | MO 研究按时代散入 Craft_SK/Weapon_SK 等现有 tab,重排坐标前置 | 单树体验统一 | 与 HSK 513 节点坐标/前置大量冲突(Change_ResearchProjectDef 83 ops 要重写),工作量大、风险高 |
| C. 只取门禁,不带研究树 | MO 的 18 门禁研究不进 HSK,只把"图纸门禁"概念落地到 HSK 既有节点(科技蓝图已做) | 最简,零树冲突 | 丢失 MO 中世纪研究内容(与"整合 MO 本体"目标矛盾) |

**推荐 A**: 门禁统一独立于研究树整合;MO 研究树整合与否是另一个话题(归 MO 整体整合方案),门禁方案只需保证"MO 存在时其门禁走统一机制"。

### 5.2 原版研究改动的边界

MO 的 `Change_ResearchProjectDef.xml`(83 ops,把 Smithing/Stonecutting/PlateArmor 等 16 个原版研究移入 MO tab 并改前置)属 MO 中世纪化范畴,**不属"科研解锁功能"**,不随门禁抽取。整合 MO 本体时按 MO 整体方案处理;门禁方案只处理其中与门禁重合的(LongBlades/Greatbow/PlateArmor 的门禁语义)。

---

## 六、实施步骤(分四阶段)

### 阶段 1: 抽取 MO 科研解锁功能(独立 mod,验证可行)

1. 新建独立 mod(如 `MO科研门禁抽取` / local.mo.schematicgate):
   - DLL: 仅保留 RequiredSchematic、CanBeResearchedAt/CanStartNow 判定、HasBook/PlayerHasSchematic、BookOutcomeProperties_GainResearchDefinable、超链接补丁、PatchOperation_ToggleSettings(自 MO DLL 用 dnfile 提取这些类,重新编译,其余类全弃)。
   - Defs: ResearchProjects_Misc.xml 中 18 门禁研究(重挂前置到原版: Smithing/Stonecutting/ComplexClothing/Brewing 等)+ ResearchTabs + 标签 + 16 图纸 BookDef + DankPyon_Schematics 分类 + 学者商人 + 书架。
   - 验证: 游戏内门禁生效、图纸书架阅读解锁、无 MO 其余内容报错。
2. 关键: 抽取版的前置链要能脱离 MO 研究树跑通(把 DankPyon_Crossbow 前置 Smithing 等 MO 节点改为原版 Smithing)。

### 阶段 2: 合并两个 mod → 新 mod

1. 以科技蓝图 BlueprintUnlockHSK.cs 为底座,把阶段 1 抽取的 MO 判定类融入(或改用统一判定,见 §3.3)。
2. Defs 合并: 蓝图清单 = 43 + MO 18;分类合并;投放补丁合并。
3. 重编译(改名 ResearchGateHSK.dll,避免与旧 dll 混淆);SHA1 双目录 0 差异。
4. 卸载旧「科技蓝图与逆向工程HSK」与阶段 1 独立 mod,由新 mod 接管(存档兼容: WorldComponent 类型名变化 → 需保留 BlueprintUnlockTracker 类名或写存档迁移)。

### 阶段 3: 门控清单落地(纯 Defs)

1. ThingDefs_BlueprintGates.xml 新增 18 张 MO 蓝图(tier1,250/1)。
2. Patches 新增 `MO_RequiredSchematic_转换.xml`(xpath 存在性门控: MO 存在时把 18 个研究挂上统一蓝图;MO 不存在整段跳过)。
3. `BlueprintUnlock_ClearTechprints.xml` 同步清理 MO 研究可能继承的 techprintCount。
4. 事件/商人/任务投放补丁同步纳入 MO 蓝图(卡节点动态提权自动覆盖)。
5. 用 `_tmp/patch_simulator.py` 验证所有新补丁;Unified.xml 核对 18 个 MO 研究 defName 精确存在。

### 阶段 4: 验证与收尾

1. 游戏内实测: MO 在场时门禁走统一蓝图(使用蓝图→加进度→解锁);MO 缺席时 MO 蓝图不出现、其余门控正常。
2. 存档兼容: 旧科技蓝图存档的 useCounts/unlocked/storedPoints 不丢失。
3. 汉化: 新 mod Keyed(蓝图使用提示/进度条 tooltip 沿用科技蓝图中文)+ MO 图纸若有汉化残留(WIP)一并清理。
4. 更新科技配置/ 五份方案文档引用为新方案。

---

## 七、风险与决策记录

### 7.1 风险

| 风险 | 缓解 |
|---|---|
| MO 18 个门禁研究前置链依赖 MO 研究树(抽取后断裂) | 阶段 1 重挂前置到原版研究;若必须依赖 MO 链,则抽取版只在 MO 在场时激活(存在性门控) |
| Greatbow/LongBlades 双重门控 | 统一走蓝图;MO 整合层关闭 biotechSchematic 模式 |
| 存档迁移(WorldComponent 类型名) | 阶段 2 保留 BlueprintUnlockTracker 类名或 ExposeData 迁移 |
| MO 图纸"不消耗"体验 vs 蓝图"消耗" | MO 研究对齐 tier1 只需 1 张,消耗 1 张即解锁,体验接近;可在蓝图描述文案注明 |
| 新 DLL 与旧 dll 冲突 | 改程序集名 ResearchGateHSK;部署目录只留一份,清残留(AGENTS.md Assemblies 铁律) |
| 蓝图物品泛滥(61 张) | 商人/掉落按 tier 权重限售;MO 蓝图仅 MO 整合时入池 |

### 7.2 待用户确认的决策点

1. **路线 A/B/C**(§5.1): MO 研究树是否并入 HSK 树?(影响门禁方案边界)
2. **MO 18 研究全量转换**还是**只转与科技蓝图不重叠的部分**?(推荐全量,语义统一)
3. **LongBlades/Greatbow**: 科技蓝图是否新增蓝图门控原版 LongBlades?(推荐加,承接 MO 门禁语义)
4. **独立 mod 是长期保留还是仅作中间态**: 用户说"独立成单独 mod 再整合",理解为中间态,最终合并为新 mod;独立 mod 是否需要对外发布形态?
5. **新 mod 名称/packageId**: 拟「HSK 科研蓝图与门控 / local.ratkin.researchgates」(可改)

### 7.3 已确认沿用(不重设计)

科技蓝图 v3.3 分级数值(250/400/500/700/1000、1/1/2/3/4)、科研进度银行上限(500~8000 动态)、动态提权(600 ticks 节流)、顶栏进度条 + 节点角标 UI、5 路投放、全部 Defs 驱动配置。

---

*本方案基于 2026-08-26 对两个 mod 的源码/Defs/DLL 实测整理;MO 侧机制详见 [`docs/中世纪大修内容梳理.md`](中世纪大修内容梳理.md)。*

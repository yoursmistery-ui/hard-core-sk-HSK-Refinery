# P2 · 生产设施与 Processor Framework 依赖

> 用户点名: 烟熏房**要搬**;丝绸床 / 造纸机 / 纺纱机**看能不能加,并与 HSK 纺织融合**;珠宝台 / 铁砧 / 棚架**要**;抄写 4 件(与原版阅读扩展整合)**要**(家具侧见 P3,台子在本册)。
> 关键结论: **六座机器全部可以搬,零自研 DLL**,前提是引入 [SYR] Processor Framework (Continued) 作为可选前置;代价是产物/原料材料链要重建。

## 一、PF(工坊 3210544395)事实

- packageId `syrchalis.processor.framework`,支持 1.3–1.6,作者 ViralReaction([SYR] Processor Framework **Continued** 版)。
- 前置仅 Harmony,**不需要 VEF**(VFE/RimBees 是 IfModActive 可选集成);1.6 的 DLL 在 `1.6/Assemblies/ProcessorFramework.dll`。
- 提供 `ProcessorFramework.CompProperties_Processor`、`ProcessDef`、`CompProcessor`、`Building_ColorCoded`、`ITab_ProcessorContents`;自带唯一种建筑 `BarrelProcessor`(发酵桶)。
- 用法: 机器 ThingDef 挂 PF comp 并列 `processes` → 每条 `ProcessDef` 定义 `thingDef`/`ingredientFilter`/`processDays`/`temperature`/`fuelUseFactor` 等(≈20 行/条)。

## 二、六座机器逐座判定(MO 源: `1.6/Defs/ThingDefs_Buildings/Production/Buildings_Processors(_Industrial).xml`,配方在 `Defs/ProcessorDef/`)

| 机器 | MO defName | 需要的非原版类 | 产物/原料(MO) | 搬法 |
|---|---|---|---|---|
| 烟熏房 | `DankPyon_Smoker` | **MO DLL**: `CompProperties_StoreFuelThing` + `ITab_Fuel`(附加型,可整块删) | 生肉 → `DankPyon_SmokedMeat` | 删 MO 两处 → 纯原版 Refuelable/HeatPusher + PF,**可直接搬** |
| 丝绸床 | `DankPyon_SilkBed` | 仅 PF | 蚕 `DankPyon_Silkworm`(MO 种植物)→ `DankPyon_Silk` | 可搬;原料需决定: 新增"蚕"(原版 Plant 派生)或改原料为 HSK 现有纤维 |
| 造纸压机 | `DankPyon_Press_Paper` | 仅 PF | 纸浆 `DankPyon_Mixture_Paper`(工作台配方 `DankPyon_Make_PaperMixture`)→ `DankPyon_Paper` | 可搬,但要**新建 Paper 体系**(HSK 无纸),整链 4 个 def |
| 工业纺纱机 | `DankPyon_ClothSpinner` | 仅 PF + 原版 Power/Flickable | 棉/毛 → **原版 `Cloth`**;亚麻 → `DankPyon_Linen` | **最省事**: 产物本来就是原版 Cloth,与 HSK 纺织融合只需把它挂进 HSK 纺织工作台体系/研究档 |
| 窑 | `DankPyon_Kiln` | 同烟熏房(StoreFuelThing/ITab_Fuel 可删) | `DankPyon_Clay` → `DankPyon_BlocksClay` | 可搬;黏土→砖映射到 `SoftClay`→自烧砖(与 P3 陶瓷砖地板联动) |
| 发酵桶 | MO **无自建桶**(已注释) | 仅 PF | MO 用补丁 `Mods/Processor_Framework/Patches/Add_Style.xml`(gate `IfModActive="syrchalis.processor.framework"`)往 `BarrelProcessor.processes` Add 5 条酒类 Process | 装 PF 即用、零 DLL;但 §9 酒类用户已决定**不做** → 本条仅保留机制模板 |

> 统一剥离动作(每座机器): 删 `<comps>` 里 `MedievalOverhaul.CompProperties_StoreFuelThing` 与 `<comps>`/`<inspectorTabs>` 里的 `ITab_Fuel`;燃料改用原版 `CompProperties_Refuelable`。

## 三、工作台/手动设施(用户点名)

| 设施 | MO defName | 类型 | 搬法 |
|---|---|---|---|
| 铁砧 | `DankPyon_Anvil` | 原版 `Building_WorkTable` + `CompFacility` 被铁砧当设施 | **建议不做独立铁砧**: HSK 已有锻造台体系且 `鼠族HSK拓展` 147 件武器配方全挂 HSK 锻造台;若要中世纪审美,做成**锻造台的美化皮肤/链接件**(挂 `CompProperties_Facility` 提升 WorkTableWorkSpeedFactor),而不是第二个工作台 → 避免 AGENTS §10 的双路径重复解锁 |
| 珠宝台 | `DankPyon_JewelryBench` | 原版 WorkTable | 可搬;配方挂 P5 的 6 档宝石 + 金;研究档接 HSK 珠宝/锻造档 |
| 棚架 / 种植箱 | `DankPyon_Trellis`/`PlanterBox` | 原版 `Plant` 载体(肥力 0.8) | 可搬,但要选种植对象(HSK 作物);若只为番茄/豆类则价值低 → 建议做成"无电早期种植箱",与 HSK 农业档互补 |
| 抄写台 | `DankPyon_ScribeTable` | **`thingClass=Building_ScribeTable`(MO DLL)** | 用原版 `Building_WorkTable` + RecipeDef 复刻(纸→书/专著),抄写 4 件链接件(`CompProperties_Facility` WorkTableWorkSpeedFactor +0.04,距 6)才有意义 → **抄写系必须先有这张台子** |
| 修补台 | `DankPyon_MendingBench` | WorkGiver/JobDriver 在 DLL | 用户未点名 → **暂缓**(HSK 有 MendAndRecycle) |
| 磨盘 / 木炭堆 / 冶炼炉 / 高炉 / 鞣革桶 / 脱水机等 | — | 材料链设施 | 材料链已 ⛔ → **不做** |

## 四、与 HSK 纺织/材料主链的融合规则(需拍板)

1. **纺纱机**: 产物保留原版 `Cloth` → 挂到 HSK 工业纺织档工作台旁(不新增顶级链);研究门禁挂 HSK 现有纺织节点。
2. **丝绸**: 两条路 —— (a) 新增 `RK_Silk`(Fabric 类 stuff)+ 蚕(原版 Plant 派生)+ 丝绸床;使 P3 皇家家具的"丝 200"造价有实物;(b) 皇家家具造价改 `Cloth`/高级布,丝绸链整体不做。**建议 (a)**,因为它同时解决皇家家具、横幅、床三处造价。
3. **纸**: (a) 新增 `RK_Paper` + 纸浆 + 造纸压机 + 抄写台,给 P1 的"专著/藏宝图残页"和一个可自产的书卷体系;(b) 纸全用 `Cloth`/皮革代 → 造纸机就不搬。**建议 (a)**,MO 的抄写链和我们的蓝图书体系天然搭。
4. **砖**: `SoftClay → 窑 → RK_Brick`,供 P3 陶瓷砖地板与都铎墙/城堡墙的黏土成分。

## 五、交付顺序与验收

1. 先做「PF 前置接入模板」: 新增 `Patches/70_PF门控_机器挂载.xml`(顶层 Conditional 以 `BarrelProcessor` def 存在性门控)+ 机器 Defs 放本 mod `1.6/Defs/Buildings_Processors.xml`,**未装 PF 时整套不加载**(靠 LoadFolders `IfModActive` 或 defs 目录版本化,与 MO 同法)。
2. 材料链四件(RK_Silk/RK_Paper/RK_Brick/蚕)先落地,再挂机器配方,避免配方指向不存在的 def。
3. 验收: 装 PF → 机器可建可投料可取出,`ProcessDef` 不报错;不装 PF → 本 mod 加载零错误、机器不出现在菜单。
4. 铁律提醒: 配方挂工作台只用 `recipeUsers` 单路径(AGENTS §10),ResearchTreeSK 同一无门槛配方不得双挂(重复解锁告警)。

---

*分册索引: `00_决策与优先级` · `01_P1_战利品废墟任务蓝图链` · `02_P2_生产设施与Processor依赖` · `03_P3_家具装饰地板储物` · `04_P4_商人剧本生物特性文化` · `05_P5_矿井与宝石方案`(同目录;进度看板见 `../中世纪MO整合进度盘点.md`,总索引见 `docs/00_HSK参考文档合集导航.md` §3)*

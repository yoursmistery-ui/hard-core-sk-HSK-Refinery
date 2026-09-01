# HSK 体系工作台造价总表(HSK + Vile's 全体系)

> 数据源:游戏最终合并 def `MissileGirl/Cache/Unified.xml`(2026-08-27 导出),中文名取自 `1.6HSK核心汉化`(+ 全 ai 汉化增补)。
> 覆盖范围:全部「制造/加工类工作台 + 研究台」——凡是被配方 `recipeUsers` 引用、或自带 `recipes` 列表、或 `thingClass` 为工作台/研究台、且确为可建造建筑的 def 全部收录。
> 造价 = **通用材料**(可指定材质类别)+ **固定部件清单**;工时为 `WorkToBuild`(游戏 tick,60 tick = 1 秒,括号内为秒,是「标准工人工时」参考,实际受建造技能影响)。

共 **141** 个工作台(含研究台)。**2026-08-28 更新**:酒馆「装饰台 `Tav_TableDecor`」已删除(地毯/旗帜/奖杯/厨房装饰改建筑菜单直建,垫料迁 HSK 纺织机 `TableLoom`),故酒馆工具整合HSK 由 3 降为 2;下表与总数按删除后口径,重跑脚本需以新的 `Unified.xml` 导出为准。来源分布:Core_SK(HSK 本体) 62 · 原版 Core 20 · 原版 DLC · Biotech 3 · Combat Extended 2 · Vile's Materials Science 3 · Vile's Metallurgy 8 · Vile's Hell Bent for Leather Tanning 5 · Vile's Wood You Please 3 · Vile's Amuse Bouche 3 · RatkinRaceHSK(鼠族本体) 4 · Rimatomics_SK 3 · Rimefeller_SK 1 · Dubs-Bad-Hygiene 1 · SeedsPlease 1 · Roos Painting Expansion 1 · 书籍拓展HSK 4 · 美狐HSK拓展 3 · 酒馆工具整合HSK 2 · 金鼠族 HSK版本 3 · 鼠族家具拓展 9

## 速查:HSK 研究台六档(与原版 `requiredResearchBuilding` 对应)

| 时代档 | 研究台(中文) | defName | 造价 |
|---|---|---|---|
| 原始(Neolithic) | 原始科技研究台 | `PrimitiveResearchBench` | 80〔石材类/砖材〕 |
| 中世纪(Medieval) | 基础研究台 | `SimpleResearchBench` | 150〔强固金属/木质类/塑料〕 + 原始零部件×8、机械组件×2 |
| 前工业(电力→石化) | 研究终端 | `LabTerminal` | 60〔强固金属/塑料〕 + 玻璃块×50、电子元件×7、普通零部件×3 |
| 后工业(石化→太空) | 高级研究台 | `HiTechResearchBench` | 180〔强固金属/塑料〕 + 塑料×80、合成橡胶×50、普通零部件×15、集成电路×5 |
| 太空(Spacer) | 多元分析仪 | `MultiAnalyzer` | 60〔强固金属/塑料〕 + 塑料×15、玻璃块×10、集成电路×5、普通零部件×5 |
| 极致(Ultra) | 实验室工作站 | `LabStation` | 50〔强固金属/塑料〕 + 塑料×5、玻璃块×10、普通零部件×5、集成电路×3 |

## 按来源 mod 分组

> 每表按「研究台 → 生产台」排序;「类」列 R=研究台、P=生产。

### Core_SK(HSK 本体)  · 62 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| R | **实验室工作站**<br>`laboratory station` | `LabStation` | 50〔强固金属/塑料〕 | 塑料×5、玻璃块×10、普通零部件×5、集成电路×3 | 600 (10s) | 科学技术研究 VI |
| R | **研究终端**<br>`research terminal` | `LabTerminal` | 60〔强固金属/塑料〕 | 玻璃块×50、电子元件×7、普通零部件×3 | 600 (10s) | 科学技术研究 III |
| R | **原始科技研究台**<br>`primitive research table` | `PrimitiveResearchBench` | 80〔石材类/砖材〕 | — | 1,900 (31s) | — |
| P | **电动装配台**<br>`electric assembling bench` | `AdvToolBench` | 220〔强固金属/塑料〕 | 钨钴合金×40、PVC塑料×20、集成电路×10、普通零部件×15、机械组件×10 | 5,500 (91s) | 零部件 I |
| P | **先进武器加工台**<br>`advanced weapon crafting workbench` | `AdvWeaponCraftingWorkTable` | 160〔坚韧金属/塑料〕 | 合成橡胶×55、普通零部件×15、机械组件×8、集成电路×5 | 6,500 (108s) | 枪械 II |
| P | **糖果桌**<br>`candy table` | `CandyTable` | 135〔强固金属/塑料〕 | 普通零部件×10、电子元件×5 | 3,100 (51s) | 甜品 |
| P | **电压力锅**<br>`electric pressure cooker` | `Canningstove` | 115〔强固金属〕 | 普通零部件×12、电子元件×3 | 1,700 (28s) | 烹饪学 V |
| P | **炭坑**<br>`Charcoal Pit` | `CharcoalPit` | — | 土堆×20 | 7,500 (125s) | 金属加工 I |
| P | **干酪槽**<br>`cheesemaking basin` | `CheeseBasin` | 40〔木质类〕 | 原始零部件×2 | 800 (13s) | 食物学 II |
| P | **乳酪桶**<br>`cheesemaking vat` | `CheeseVat` | 100〔坚韧金属〕 | 钢筋混凝土×10、普通零部件×10、电子元件×4、机械组件×5 | 1,200 (20s) | 高级烹饪学 II |
| P | **堆肥桶**<br>`compost barrel` | `CompostBarrel` | 75〔强固金属/木质类〕 | 原始零部件×2 | 1,700 (28s) | 农业 I |
| P | **手动混凝土搅拌器**<br>`hand concrete mixer` | `ConcreteMixer` | 135〔强固金属〕 | 普通零部件×5、机械组件×5 | 1,600 (26s) | 混凝土 I |
| P | **干燥架**<br>`drying rack` | `DryingRack` | 60〔木质类/塑料〕 | 原始零部件×2 | 800 (13s) | — |
| P | **电弧炉**<br>`electric arc furnace` | `EAF` | 180〔坚韧金属〕 | 玻璃生料×30、粘土砖×40、普通零部件×15、机械组件×10、电子元件×6 | 4,500 (75s) | 金属加工 IV |
| P | **电动混凝土搅拌机**<br>`electric concrete mixer` | `EConcreteMixer` | 115〔强固金属〕 | 合成橡胶×45、普通零部件×10、电子元件×5、机械组件×5 | 5,000 (83s) | 混凝土 II |
| P | **电动酿造台**<br>`electric brewery` | `ElectricBrewery` | 130〔强固金属/塑料〕 | 电子元件×5、普通零部件×10、机械组件×8 | 1,750 (29s) | 饮品 |
| P | **工业干燥机**<br>`electric drying rack` | `ElectricDryingRack` | 60〔强固金属〕 | 普通零部件×7、电子元件×4、机械组件×5、玻璃块×25 | 800 (13s) | 烹饪学 III |
| P | **电烤箱**<br>`electric oven` | `ElectricOven` | 100〔强固金属〕 | 普通零部件×10、电子元件×3、机械组件×3 | 1,600 (26s) | 烘培 III |
| P | **专业化烹饪炉**<br>`professional cook stove` | `ElectricStove_Pro` | 155〔强固金属〕 | 普通零部件×20、合成橡胶×30、集成电路×5、先进机械组件×3 | 4,500 (75s) | 烹饪学 IV |
| P | **玻璃制作台**<br>`glassworks table` | `GlassworkTable` | 130〔坚韧金属〕 | 普通零部件×10、机械组件×8 | 1,600 (26s) | 玻璃制作 II |
| P | **石磨**<br>`milling stone` | `GrindStone` | 70〔石材类/砖材〕 | — | 300 (5s) | 烘培 I |
| P | **手工修理台**<br>`hand mending workbench` | `HandMendingWorkbench` | 150〔坚韧金属/石材类/砖材/木质类〕 | 普通零部件×4、机械组件×2 | 1,800 (30s) | 手工维修台 |
| P | **重型武器机床**<br>`heavy arms lathe` | `HeavyArmsBench` | 170〔坚韧金属〕 | 普通零部件×15、机械组件×15 | 5,500 (91s) | 炮塔 I |
| P | **洗涤盆**<br>`Holy Metal Basin` | `HolyBasin` | 70〔金属类〕 | 原始零部件×3 | 500 (8s) | 建造 I |
| P | **先进纺织工作台**<br>`hyper tailoring bench` | `HyperTailoringBench` | 180〔坚韧金属/塑料〕 | ABS塑料×40、磁性材料×15、微芯片集成电路×3、先进机械组件×6 | 11,250 (187s) | 服装生产 V |
| P | **干肉架**<br>`Jerky Drying Rack` | `JerkyRack` | 60〔木质类/塑料〕 | 原始零部件×2 | 1,400 (23s) | — |
| P | **陶瓷窑**<br>`Ceramic Kiln` | `Kiln` | 120〔石材类/砖材〕 | — | 2,000 (33s) | 陶瓷学 I |
| P | **皮革工作台**<br>`leatherworker table` | `LeatherworkerTable` | 80〔石材类/砖材〕 | — | 1,250 (20s) | — |
| P | **无机物转化器**<br>`Nucleosynthetic Converter` | `MatterConverter` | 280〔坚韧金属/塑料〕 | 人造纤维×50、高级零部件×12、先进机械组件×6、微芯片集成电路×4 | 9,000 (150s) | 物质转换 I |
| P | **量子加工台**<br>`quantum fabricator` | `Matterfab` | 240〔稳固金属〕 | 磁性材料×25、高级零部件×10、先进机械组件×10、微芯片集成电路×2 | 10,000 (166s) | 物质转换 III |
| P | **未来武器工作台**<br>`energy weapon workbench` | `MechWeaponCraftingWorkTable` | 190〔稳固金属/塑料〕 | 磁性材料×20、高级零部件×12、先进机械组件×5、生物电子元件×2 | 11,000 (183s) | 定向能武器 |
| P | **电磨**<br>`Electric Mill` | `MillElectric` | 50〔强固金属/塑料〕 | 合成橡胶×45、玻璃块×25、普通零部件×8、机械组件×5 | 500 (8s) | 烘培 III |
| P | **自动化装配器**<br>`robotic assembler` | `RobAssem` | 120〔坚韧金属/塑料〕 | 钨钴合金×45、高级零部件×8、先进机械组件×10、微芯片集成电路×5、ABS塑料×30 | 9,000 (150s) | 零部件 II |
| P | **工业碎石机**<br>`Industrial Rock Crusher` | `RockCrusher` | 350〔坚韧金属〕 | 钢筋混凝土×120、普通零部件×25、机械组件×15、集成电路×2 | 10,000 (166s) | 采矿学 V |
| P | **风车磨坊**<br>`windmill` | `SK_Windmill` | 85〔木质类〕 | 原始零部件×8、普通零部件×2、机械组件×1、软粘土×25、粘土砖×20 | 1,600 (26s) | 烘培 II |
| P | **硝石转化坑**<br>`Saltpeter Pit` | `Saltpeter_pit` | — | — | 8,200 (136s) | 弓II |
| P | **青贮料堆**<br>`Silage Pile` | `SilagePile` | — | — | 2,200 (36s) | — |
| P | **假肢工作台**<br>`prosthetics workbench` | `TableBasicProsthetic` | 120〔强固金属/木质类/塑料〕 | 普通零部件×12、机械组件×8 | 3,700 (61s) | 义肢制造 III |
| P | **仿生体工作台**<br>`bionics workbench` | `TableBionics` | 160〔坚韧金属/塑料〕 | ABS塑料×80、钛铁合金×25、集成电路×15、高级零部件×5 | 8,500 (141s) | 义肢制造 VIII |
| P | **石化产品实验室**<br>`Petrochemical Laboratory` | `TableChemlab` | 130〔坚韧金属〕 | 钢筋混凝土×40、玻璃块×50、普通零部件×20、电子元件×5、机械组件×15 | 5,500 (91s) | 石油化学 IV |
| P | **咖啡机**<br>`coffee machine` | `TableCoffee` | 70〔强固金属/塑料〕 | 塑料×40、电子元件×3、普通零部件×8、机械组件×3 | 1,400 (23s) | 饮品 |
| P | **电子器件工作台**<br>`electronics table` | `TableElectronics` | 140〔坚韧金属/塑料〕 | 电线×20、普通零部件×10、机械组件×10 | 2,750 (45s) | 电子 I |
| P | **熔炉**<br>`blast furnace` | `TableFurnace` | 240〔石材类/砖材〕 | 原始零部件×6、狗皮×20 | 3,400 (56s) | 冶金学 III - 中世纪 |
| P | **简易烤架**<br>`simple grill` | `TableGrill` | 35〔坚韧金属〕 | 原始零部件×4 | 800 (13s) | 烹饪学 I |
| P | **重型弹药工作台**<br>`heavy ammunition bench` | `TableHeavyAmmunition` | 210〔坚韧金属〕 | 普通零部件×10、电子元件×5、机械组件×10 | 6,500 (108s) | 手榴弹 I |
| P | **厨房案桌**<br>`Kitchen Table` | `TableKitchen` | 115〔强固金属/木质类/塑料〕 | 原始零部件×8、机械组件×2 | 800 (13s) | 食物学 II |
| P | **纺织机**<br>`tailor's loom` | `TableLoom` | 125〔强固金属/木质类/塑料〕 | 原始零部件×10、电子元件×3、机械组件×6 | 3,850 (64s) | 服装生产 II |
| P | **修理工作台**<br>`mending workbench` | `TableMending` | 100〔坚韧金属/塑料〕 | 人造纤维×20、普通零部件×8、电子元件×6、机械组件×5 | 4,500 (75s) | 修理台 |
| P | **器官实验室**<br>`organ vat` | `TableOrganvat` | 125〔坚韧金属/塑料〕 | 合成橡胶×35、有机玻璃×25、玻璃块×25、集成电路×5 | 6,000 (100s) | 义肢制造 IV |
| P | **烤箱**<br>`oven` | `TableOven` | 120〔石材类/砖材〕 | 原始零部件×5 | 2,000 (33s) | 烘培 I |
| P | **原始假肢工作台**<br>`primitive prosthetics bench` | `TablePrimitiveProsthetic` | 80〔石材类/砖材/木质类〕 | — | 3,700 (61s) | 义肢制造 I |
| P | **回收站**<br>`recycling station` | `TableRecycling` | 120〔坚韧金属〕 | 普通零部件×12、电子元件×3、机械组件×5 | 4,500 (75s) | 修理台 |
| P | **电动锯木台**<br>`electric sawmill` | `TableSawmillElectric` | 150〔强固金属〕 | 普通零部件×10、电子元件×5、机械组件×5 | 5,000 (83s) | 混凝土 I |
| P | **手工锯台**<br>`hand sawmill` | `TableSawmillHand` | 120〔原木/木质类/软木原木/硬木原木/超硬木原木/生竹材〕 | — | 700 (11s) | — |
| P | **石匠工作台**<br>`stonecutter's bench` | `TableStonecutterNeolithic` | 60〔石材类/砖材/原木〕 | — | 1,100 (18s) | — |
| P | **人造器官组装台**<br>`synthetic organ assembler` | `TableSynthetics` | 135〔坚韧金属/木质类/塑料〕 | 塑料×65、石蜡×20、集成电路×6、高级零部件×5 | 5,500 (91s) | 义肢制造 VI |
| P | **酒类发酵桶**<br>`Alcohol Fermenting Barrel` | `UniversalFermenter` | 50〔木质类〕 | 原始零部件×2 | 900 (15s) | 酿酒 I |
| P | **神经机械体工作台**<br>`cybernetics workbench` | `UpgradingStation` | 250〔坚韧金属/塑料〕 | 碳纤维锭×80、镍钛合金×45、微芯片集成电路×5、高级零部件×7、先进机械组件×10 | 9,000 (150s) | 义肢制造 IX |
| P | **挖盐点**<br>`salt mine` | `VG_SaltMine` | — | — | 2,500 (41s) | — |
| P | **武器加工台**<br>`weapon crafting workbench` | `WeaponCraftingWorkTable` | 140〔坚韧金属/木质类/塑料〕 | 普通零部件×15、电子元件×6、机械组件×6 | 4,000 (66s) | 枪械 |
| P | **医药工作台**<br>`medical table` | `meditable` | 125〔强固金属/木质类/塑料〕 | 聚碳酸酯×30、PP塑料×45、塑料×20、普通零部件×10、集成电路×4 | 4,000 (66s) | 药物 IV |
| P | **营养物质机**<br>`soylent machine` | `soylenttable` | 130〔强固金属/塑料〕 | 合成橡胶×20、PP塑料×70、普通零部件×10、集成电路×5 | 2,700 (45s) | 营养膏技术 |

### 原版 Core  · 20 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| R | **高级研究台**<br>`hi-tech research bench` | `HiTechResearchBench` | 180〔强固金属/塑料〕 | 塑料×80、合成橡胶×50、普通零部件×15、集成电路×5 | 8,000 (133s) | 科学技术研究 IV |
| R | **多元分析仪**<br>`multi-analyzer` | `MultiAnalyzer` | 60〔强固金属/塑料〕 | 塑料×15、玻璃块×10、集成电路×5、普通零部件×5 | 5,500 (91s) | 科学技术研究 V |
| R | **基础研究台**<br>`research bench` | `SimpleResearchBench` | 150〔强固金属/木质类/塑料〕 | 原始零部件×8、机械组件×2 | 1,900 (31s) | 科学技术研究 I |
| P | **生物精炼设备**<br>`biorefinery` | `BiofuelRefinery` | 170〔强固金属〕 | 钢筋混凝土×25、普通零部件×15、电子元件×5、机械组件×8 | 1,200 (20s) | 化学 I |
| P | **酿造台**<br>`brewery` | `Brewery` | 80〔石材类/砖材〕 | 木板×50 | 1,300 (21s) | 酿酒 I |
| P | **屠宰点**<br>`butcher spot` | `ButcherSpot` | — | — | 300 (5s) | 食品工程原理 II |
| P | **篝火**<br>`campfire` | `Campfire` | — | 原木×20 | 300 (5s) | — |
| P | **手工加工台**<br>`craftsman table` | `CraftingSpot` | 80〔石材类/砖材〕 | — | 1,250 (20s) | — |
| P | **药物实验台**<br>`drug lab` | `DrugLab` | 150〔强固金属/木质类/塑料〕 | 合成橡胶×45、玻璃块×25、普通零部件×8、机械组件×5 | 1,700 (28s) | 化学 II |
| P | **电力火化炉**<br>`crematorium` | `ElectricCrematorium` | 130〔石材类/砖材〕 | 普通零部件×10、电子元件×2 | 2,500 (41s) | 金属加工 II |
| P | **电熔炼器**<br>`metal extraction plant` | `ElectricSmelter` | 155〔坚韧金属〕 | 普通零部件×12、电子元件×6、机械组件×8 | 3,500 (58s) | 金属加工 II |
| P | **电力锻造台**<br>`electric smithy` | `ElectricSmithy` | 155〔坚韧金属〕 | 普通零部件×12、电子元件×5、机械组件×5 | 3,750 (62s) | 金属加工 II |
| P | **电力炉灶**<br>`electric stove` | `ElectricStove` | 135〔强固金属〕 | 普通零部件×10、电子元件×5、机械组件×5 | 2,200 (36s) | 烹饪学 III |
| P | **电动裁缝工作台**<br>`electric tailoring bench` | `ElectricTailoringBench` | 150〔强固金属/塑料〕 | 普通零部件×15、集成电路×4、机械组件×5 | 6,250 (104s) | 服装生产 III |
| P | **手工装配台**<br>`hand assembling workbench` | `FabricationBench` | 145〔坚韧金属〕 | 机械组件×2 | 2,800 (46s) | — |
| P | **裁缝工作台**<br>`tailor's bench` | `HandTailoringBench` | 120〔强固金属/木质类〕 | 原始零部件×10、机械组件×5 | 2,150 (35s) | 服装生产 II |
| P | **屠宰台**<br>`butcher table` | `TableButcher` | 40〔木质类/石材类/砖材/硬木板材/超硬木板材/塑料〕 | 原始零部件×8 | 800 (13s) | 食物学 I |
| P | **电动机械加工台**<br>`electric cutting table` | `TableMachining` | 160〔坚韧金属〕 | 普通零部件×10、电子元件×5、机械组件×10 | 4,000 (66s) | 石料加工 II |
| P | **雕塑台**<br>`sculptor's table` | `TableSculpting` | 135〔强固金属〕 | 原始零部件×10、机械组件×5 | 2,800 (46s) | 艺术 II |
| P | **手动石料加工台**<br>`hand cutting table` | `TableStonecutter` | 140〔强固金属/木质类〕 | 原始零部件×10、机械组件×5 | 1,500 (25s) | 石料加工 I |

### 原版 DLC · Biotech  · 3 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **大型机械体培养器**<br>`large mech gestator` | `LargeMechGestator` | 260〔金属类〕 | 玻璃块×40、普通零部件×6、电子元件×4、机械组件×2、集成电路×4 | 16,000 (266s) | StandardMechtech |
| P | **机械体培养器**<br>`mech gestator` | `MechGestator` | 140〔金属类〕 | 玻璃块×25、普通零部件×2、电子元件×2、机械组件×1、集成电路×2 | 8,000 (133s) | BasicMechtech |
| P | **次核心编码器**<br>`subcore encoder` | `SubcoreEncoder` | 100〔金属类〕 | 合成橡胶×20、塑料×20、硅×10、机械组件×2、普通零部件×4、电子元件×1、集成电路×2 | 8,000 (133s) | BasicMechtech |

### Combat Extended  · 2 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **弹药工作台**<br>`ammo crafting table` | `AmmoBench` | 165〔坚韧金属〕 | 普通零部件×10、机械组件×10 | 5,500 (91s) | 枪械 |
| P | **枪械锻造台**<br>`gunsmithing bench` | `GunsmithingBench` | 100〔金属类/木质类〕 | 低碳钢×50、普通零部件×5 | 3,000 (50s) | — |

### Vile's Materials Science  · 3 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **化学工作台**<br>`Chemistry Station` | `ChemistryLab` | 120〔耐腐蚀〕 | 玻璃块×80、普通零部件×8 | 2,800 (46s) | 基础化学 |
| P | **电解处理器**<br>`electrolytic refinery` | `ElectrolyticRefinery` | 60〔耐腐蚀〕 | 普通零部件×12、高级零部件×6、电子元件×6、集成电路×2、机械组件×4 | 3,500 (58s) | 金属加工 IV |
| P | **感应熔炉**<br>`induction furnace` | `InductionFurnace` | 155〔坚韧金属〕 | 普通零部件×12、电线×100、机械组件×8 | 3,500 (58s) | 金属加工 II |

### Vile's Metallurgy  · 8 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **冶铁炉**<br>`bloomery` | `Bloomery` | 100〔石材类/砖材〕 | — | 1,000 (16s) | 冶金学 II - 铁器时代 |
| P | **渗碳炉**<br>`Cementation Furnace` | `CementationFurnace` | 120〔石材类/砖材〕 | — | 2,200 (36s) | 高级钢II |
| P | **精炼炉**<br>`finery forge` | `Finery` | 200〔坚韧金属〕 | 原始零部件×12、机械组件×1、木板×100 | 1,700 (28s) | 冶金学 III - 中世纪 |
| P | **火坑**<br>`fire pit` | `FirePit` | 40〔石材类/砖材〕 | — | 900 (15s) | — |
| P | **熔炼炉**<br>`foundry` | `Foundry` | 220〔坚韧金属/石材类/砖材〕 | 原始零部件×12、机械组件×8 | 1,700 (28s) | 冶金学 IV - 铸造 |
| P | **锻造台**<br>`blacksmith` | `FueledSmithy` | 220〔石材类/砖材〕 | 原始零部件×12、机械组件×8 | 1,700 (28s) | 金属加工 I |
| P | **陶艺工作台**<br>`pottery bench` | `PotteryStation` | 120〔石材类/砖材/原木〕 | — | 900 (15s) | 陶瓷学 I |
| P | **捣矿机**<br>`stamp mill` | `StampMill` | 100〔木质类/强固金属〕 | 原始零部件×8、机械组件×8 | 800 (13s) | 捣矿机 |

### Vile's Hell Bent for Leather Tanning  · 5 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **盐水腌制站**<br>`brine-curing station` | `BriningStation` | 110〔坚韧金属/木质类〕 | 原始零部件×6 | 1,500 (25s) | 服装生产 II |
| P | **皮革染色站**<br>`leather-dyeing station` | `DyeingStation` | 120〔石材类〕 | — | 1,500 (25s) | 手工维修台 |
| P | **鞣革转鼓**<br>`tanning drum` | `TanningDrum` | 120〔坚韧金属〕 | 普通零部件×10、机械组件×5 | 1,500 (25s) | 服装生产 III |
| P | **晾晒架**<br>`Drying Rack` | `TanningRack` | 20〔木质类〕 | 原始零部件×2 | 300 (5s) | — |
| P | **鞣革大缸**<br>`Tanning Vat` | `TanningVat` | 40〔石材类/砖材〕 | — | 300 (5s) | 服装生产 II |

### Vile's Wood You Please  · 3 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **劈柴墩**<br>`chopping block` | `ChoppingBlock` | 4〔硬木原木/软木原木/超硬木原木/硬木板材/软木板材/超硬木板材〕 | 手斧×1 | 400 (6s) | — |
| P | **劈柴点**<br>`splitting spot` | `LogSplittingSpot` | — | — | 300 (5s) | — |
| P | **干燥架**<br>`SeasoningRack` | `SeasoningRack` | 40〔木质类/金属类〕 | 原始零部件×2 | 2,200 (36s) | — |

### Vile's Amuse Bouche  · 3 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **油炸锅**<br>`deep fryer` | `Fryer` | 135〔强固金属〕 | 普通零部件×5、电子元件×3、机械组件×5 | 1,800 (30s) | 高级烹饪学 II |
| P | **壁炉**<br>`hearth` | `Hearth` | 120〔石材类/砖材/强固金属〕 | 原始零部件×8 | 2,000 (33s) | 食物学 I |
| P | **开放式壁炉**<br>`open hearth` | `OpenHearth` | 90〔石材类/砖材〕 | 原始零部件×4 | 800 (13s) | 食物学 I |

### RatkinRaceHSK(鼠族本体)  · 4 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **鼠族锻造台（电力）**<br>`electric smithy` | `RK_ElectricSmithy` | 90〔金属类/石材类〕 | 普通零部件×5、电子元件×3 | 3,000 (50s) | 金属加工 II |
| P | **鼠族裁缝台（电力）**<br>`electric tailor bench` | `RK_ElectricTailoringBench` | 85〔金属类/木质类/石材类〕 | 普通零部件×5、电子元件×1、机械组件×3 | 2,500 (41s) | Ratkin_Apparel_C1 |
| P | **鼠族锻造台（燃料）**<br>`fueled smithy` | `RK_FueledSmithy` | 70〔金属类/石材类/砖材〕 | 原始零部件×4 | 3,000 (50s) | 冶金学 II - 铁器时代 |
| P | **鼠族裁缝台（手工）**<br>`hand tailor bench` | `RK_HandTailoringBench` | 65〔木质类/金属类/石材类〕 | 机械组件×4 | 2,000 (33s) | Ratkin_Apparel_B1 |

### Rimatomics_SK  · 3 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| R | **「边缘核能」研究台**<br>`Rimatomics research bench` | `NuclearResearchBench` | 240〔强固金属〕 | 塑料×90、合成橡胶×80、普通零部件×15、集成电路×5 | 8,000 (133s) | 「边缘核能」 |
| P | **气体离心机**<br>`Gas centrifuge` | `GasCentrifuge` | 270〔坚韧金属〕 | 钢筋混凝土×90、人造纤维×35、普通零部件×15、机械组件×15 | 11,500 (191s) | 「边缘核能」 |
| P | **「边缘核能」加工台**<br>`Rimatomics machining table` | `TableRimatomicsMachining` | 200〔坚韧金属〕 | 合成橡胶×80、普通零部件×15、集成电路×5、机械组件×12 | 10,000 (166s) | 「边缘核能」 |

### Rimefeller_SK  · 1 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **石化控制台**<br>`Resource Console` | `ResourceConsole` | 115〔强固金属〕 | 普通零部件×12、电子元件×5、机械组件×5 | 8,000 (133s) | 石油工程 I |

### Dubs-Bad-Hygiene  · 1 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **燃烧坑**<br>`burn pit` | `BurnPit` | 50〔石材类/陶瓷/砖材〕 | 沙×30 | 200 (3s) | — |

### SeedsPlease  · 1 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **植物加工台**<br>`Plant Processing Bench` | `PlantProcessingTable` | 60〔金属类/木质类〕 | — | 2,000 (33s) | — |

### Roos Painting Expansion  · 1 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **画架**<br>`easel` | `RBPEaselPainting` | 30〔金属类/木质类/石材类/塑料〕 | — | 500 (8s) | 艺术 II |

### 书籍拓展HSK  · 4 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **打字机台**<br>`typewriter table` | `VBE_TypewritersTable` | 100〔木质类/金属类/石材类〕 | 低碳钢×20、普通零部件×1 | 3,500 (58s) | 印刷 |
| P | **写作台**<br>`writers' table` | `VBE_WritersTable` | 100〔木质类/金属类/石材类〕 | 原木×20 | 2,000 (33s) | 写作 |
| P | **电动印刷机**<br>`electric printing press` | `VanillaBooksExpandedExpanded_PrintingPressElectric` | 275〔金属类〕 | 低碳钢×30、普通零部件×10 | 9,000 (150s) | 印刷机、电力工程 I |
| P | **印刷机**<br>`printing press` | `VanillaBooksExpandedExpanded_PrintingPressManual` | 150〔木质类/金属类〕 | 低碳钢×30、普通零部件×2 | 3,000 (50s) | 印刷机 |

### 美狐HSK拓展  · 3 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **美狐天界熔炉**<br>`미호 천상의 용광로` | `Miho_CelestialForge` | — | 美狐军规陶瓷×1500、高级零部件×12、超级零部件×4、微芯片集成电路×6 | 30,000 (500s) | 超凡科技量产织造 |
| P | **美狐机械工厂**<br>`미호 메카닉 공장` | `Miho_MechFactory` | — | 陶瓷×300、低碳钢×75、普通零部件×8、高级零部件×1、碳聚物×20、集成电路×15 | 10,000 (166s) | 美狐重工业 |
| P | **美狐综合工作台**<br>`미호 종합작업대` | `Miho_TableMachining` | — | 陶瓷×100、普通零部件×6、复合粘剂×8、人造纤维×10 | 3,000 (50s) | 枪械 |

### 酒馆工具整合HSK  · 3 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **屠宰台** | `Tav_KitchenButch` | 20〔石材类〕 | 木板×50 | 3,000 (50s) | 家具 I |
| P | **酒馆烤箱** | `Tav_Oven` | 40〔石材类〕 | 木板×80 | 3,000 (50s) | 家具 I |

### 金鼠族 HSK版本  · 3 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| P | **Wind, snow, bonfire** | `OAGene_SnowstormCampfire` | 30〔石材类〕 | 原木×30 | 1,800 (30s) | — |
| P | **Flower Cake Craft Machinery** | `OA_OberoniaCakeProducter` | — | 钛铁合金×100、高级零部件×6、先进零部件×2、先进机械组件×2 | 38,000 (633s) | OA_RK_Oberonia_Aurea_Research_B |
| P | **Oberonia Aurea Sewing Platform** | `OA_RK_Tailor` | — | 钛铁合金×100、高级零部件×4、先进零部件×2 | 10,000 (166s) | 服装生产 III |

### 鼠族家具拓展  · 9 个

| 类 | 工作台 | defName | 通用材料 | 固定部件 | 工时 | 需科研 |
|:--:|---|---|---|---|---|---|
| R | **简易研究台**<br>`simple research bench` | `RKFC_RGK_SimpleResearchBench` | 150〔强固金属/木质类/塑料〕 | 原始零部件×8、机械组件×2 | 1,900 (31s) | 科学技术研究 I |
| P | **篝火**<br>`bonfire` | `RKFC_GL_Bonfire` | — | 原木×20 | 300 (5s) | — |
| P | **电动锻造台**<br>`electric smithy` | `RKFC_GL_ElectricSmithy` | 155〔坚韧金属〕 | 普通零部件×12、电子元件×5、机械组件×5 | 3,750 (62s) | 金属加工 II |
| P | **电灶**<br>`electric stove` | `RKFC_GL_ElectricStove` | 135〔强固金属〕 | 普通零部件×10、电子元件×5、机械组件×5 | 2,200 (36s) | 烹饪学 III |
| P | **柴火锻造台**<br>`fueled smithy` | `RKFC_GL_FueledSmithy` | 220〔坚韧金属/石材类/砖材〕 | 原始零部件×12、机械组件×8 | 1,700 (28s) | 金属加工 I |
| P | **butcher table** | `RKFC_GL_TableButcher` | 40〔木质类/石材类/砖材/硬木板材/超硬木板材/塑料〕 | 原始零部件×8 | 800 (13s) | 食物学 I |
| P | **柴火灶**<br>`fueled stove` | `RKFC_RGK_FueledStove` | 125〔强固金属〕 | 原始零部件×10、机械组件×5 | 1,200 (20s) | 烹饪学 II |
| P | **工案(电动)**<br>`electric workbench` | `RK_ElectricMsWorkbench` | 65〔木质类/金属类/石材类〕 | 低碳钢×65、普通零部件×6 | 2,500 (41s) | Ratkin_Apparel_C1 |
| P | **工案**<br>`hand workbench` | `RK_HandMsWorkbench` | 45〔木质类/金属类/石材类〕 | 低碳钢×20 | 2,000 (33s) | Ratkin_Apparel_B1 |

## 备注与口径

- **通用材料〔类别〕**:数字为该工作台所需材料件数,〔〕内是可用的材质类别(用金属/木材/石材等对应类别的资源填充即可);`—` 表示无通用材料需求。
- **固定部件**:必须提供的具体物品;名称与数值均为游戏终态(HSK 体系下的最终造价,已含各 mod 覆盖)。
- **免费/仅工时**:少数采集加工点(如盐硝坑 `Saltpeter_pit`、青贮堆 `SilagePile`、采盐坑 `VG_SaltMine`、劈木点 `LogSplittingSpot`、屠宰点 `ButcherSpot`)造价列多为 `—`,只需工时。
- **重名 def**:`Kiln`(陶瓷窑)在 `Core_SK` 与 `Vile's Metallurgy` 均有定义,按最终加载态归入 HSK 本体。
- 少数 Biotech/鼠族系工作台官方简体中文随 DLC/游戏内建语言提供,表中若 `**中文**` 缺失则回退英文原标签。
- 生成脚本:`_tmp/extract_workbenches.py`(取数)+`_tmp/build_cn_map.py`(中文映射);数据随 mod 版本变化,重跑即刷新。

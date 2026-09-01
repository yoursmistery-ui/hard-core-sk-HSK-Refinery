# HSK × Vile 工业材料与产业线总览

> 自动生成于 Unified.xml(全量合并)+ AI汉化 DefInjected。物料以 `defName` 为检索键;
> 中文名取自汉化包;无中文者保留 defName。'产自'='产出该物料的配方 / 所在建筑 / 前置研究'。

- 配方总数: **1759**  生产建筑: **136**  物料(被引用)数: **646**
- 识别为 Vile's Materials Science 的物料: **93** 个

## 零、核心产业线速查(后续调用入口)

> 下列链路基于全量配方自动归纳。括号内为典型建筑 / 研究档。

1. **矿石 → 基础金属**: `Smelt*_Basic / _Industrial / _HighTech` 系列(电熔炼炉 / 感应熔炉 / 电解处理器 / metal extraction plant)把 铁矿·铝土·铜矿·钛磁铁矿·方铅矿·闪锌矿… 炼成 `Steel / Iron / AluminiumBar / CopperBar / Titanium / LeadBar / Zinc / NickelBar …`;高级档额外副产 `Cr / Zn / Ag / V / Co / W / Mo`。
2. **基础金属 → 合金钢(多为 Vile)**: `PigIron → MakeCarbonSteel / MakeSpringSteel / MakeToolSteel / MakeAR500 / MakeMildSteel …`(induction furnace / electric arc furnace,Metals_C1~C3);**超级合金** `MakePyromet / MakeStellite / MakeNimonic / MakeAerMet / MakeMaxametSteel`(Metals_D1)。
3. **原油/化工 → 塑料与合成纤维(多为 Vile)**: Petrochemical Laboratory 由 `Plastics / Polymers / Propylene / Butadiene / Xylene` 产 `MakeNylon / MakePolymerFibers / SyntheticFibers / HMFibers / MakeABS / MakePVCPlastic / MakePolycarbonate / MakeKevlar / MakeFiberglass`(Oil_Industry_C3~D1)。
4. **纤维/布料 → 衣物**: `tailor's loom` 由 `RawCotton / RawFlax / SmokeleafLeaves` 产 `Cloth / Flaxcloth / HempCloth →` 各类 Fabric 衣物(Apparel_B1 起)。
5. **基础料 → 零部件**: `ComponentMedieval`(Craft_B1)→ `ComponentIndustrial`(Oil_Industry_C3/后工业)→ `ComponentSpacer`(太空)→ `ComponentUltra`(超科技);配套 `Mechanism / AdvMechanism / Weapon_Parts / Heavy_Component / Charged_Component / Plasma_Component / Microchips / Hexcell / MagneticMaterial`。
6. **HSK × Vile 交叉点**: 鼠族HSK拓展的武器/板甲/服饰配方大量消耗 Vile 物料 `Nylon / HMFibers / CarbonSteel / SpringSteel / Compaste / ABS / PVCPlastic / Rubber / SewingKit / Flaxcloth`(详见第三节);这些物料在 HSK 环境下由 Vile 产业链供给,须保证 Vile 已装载或提供等价配方。

## 一、物料总表(按类型分组)

判定为"物料":被 ≥2 条配方作为原料消耗,或本身是 stuff(可塑材)。

### 金属/合金

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| BogIron | 沼泽铁矿 | - | Extract_BogIron(blacksmith/BogIronTech) | 5 |
| BlisterSteel | 泡钢棒 | - | blisterSteel_process(Cementation Furnace/?) | 2 |
| WroughtIron | 熟铁 | - | MakeWroughtIron_Forge(?/?) | 5 |
| MetalCannedMeat_prime | 生肉罐头 | - | MakeMetalCannedMeat_prime(electric pressure cooker/Food_C7) | 6 |
| PigIron | 生铁 (Fe) | - | MakePigIron_Furnace(blast furnace/?) | 13 |
| CarbonAlloy | 碳纤维合金锭 | - | DisassembleAIPersonaCore(robotic assembler/Components_D2) | 3 |
| PureSilver | 纯银 (Ag) | - | MakeLeadBars_Hand(fire pit/Craft_B1) | 2 |
| MetalCan | 罐头 | - | MakeMetalCan(induction furnace/Metals_C1) | 4 |
| Gold | 金块 | - | ConvertToGold(Nucleosynthetic Converter/Super_matter_E1) | 3 |
| GoldBar | 金锭 | - | MakeGoldBars_Hand(fire pit/Craft_B1) | 12 |
| Vanadium | 钒（V） | - | SmeltTitanomagnetite_HighTech(electrolytic refinery/Metals_C3) | 3 |
| Titanomagnetite | 钛磁铁矿 | - | —(基础/矿石) | 3 |
| Titanium | 钛锭 | - | ProcessTitanium(?/?) | 5 |
| Steel | 钢锭 | - | ExtractMetalFromSlag(metal extraction plant/Metals_C1) | 72 |
| Tungsten | 钨锭 | - | ProcessTungsten(?/?) | 6 |
| Cobalt | 钴锭 | - | ProcessCobalt(?/?) | 8 |
| Molybdenum | 钼 (Mo) | - | SmeltScheelite_HighTech(electrolytic refinery/Metals_C3) | 3 |
| Uranium | 铀矿石 | - | ConvertToUranium(Nucleosynthetic Converter/Super_matter_E1) | 4 |
| IronBloom | 铁坯 | - | MakeIronBloom_Bloomery(bloomery/?) | 2 |
| Iron | 铁矿石 | - | ConvertToSteel(Nucleosynthetic Converter/Super_matter_E1) | 10 |
| Plasteel | 铁钛合金锭 | - | MakeFerrotitaniumAlloy(?/?) | 9 |
| LeadBar | 铅 | - | MakeLeadBars_Hand(fire pit/Craft_B1) | 13 |
| CopperBar | 铜 | - | MakeCopperBars_Hand(fire pit/Craft_B1) | 14 |
| Copper | 铜矿石 | - | ConvertToCopper(Nucleosynthetic Converter/Super_matter_E1) | 6 |
| Aluminium | 铝土矿 | - | ConvertToAluminium(Nucleosynthetic Converter/Super_matter_E1) | 4 |
| AluminiumBar | 铝锭 | - | MakeAluminiumBars_Electric(metal extraction plant/?) | 11 |
| Silver | 银块 | - | ConvertToSilver(Nucleosynthetic Converter/Super_matter_E1) | 3 |
| SilverBar | 银锭 | - | MakeSilverBars_Hand(fire pit/Craft_B1) | 5 |
| Zinc | 锌 (Zn) | - | SmeltMagnetite_HighTech(electrolytic refinery/Metals_C3) | 5 |
| Manganese | 锰（Mn） | - | SmeltBogIron_Industrial(metal extraction plant/Metals_C2) | 5 |
| Nickel | 镍矿石 | - | ConvertToNickel(Nucleosynthetic Converter/Super_matter_E1) | 5 |
| NickelBar | 镍锭 | - | ProcessNickelOre(?/?) | 10 |

### 塑料/纤维/布料/皮革

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| RawFlax | 亚麻 | - | —(基础/矿石) | 3 |
| Chitin | 几丁质 | - | —(基础/矿石) | 2 |
| SyntheticSkin | 合成人造皮肤 | - | —(基础/矿石) | 4 |
| SyntheticAmmonia | 合成氨 | - | MakeAmmonia(Chemistry Station/Oil_Industry_C3) | 5 |
| SyntheticFibers | 合成纤维 | - | MakePolymerFibers(Chemistry Station/Oil_Industry_C6) | 35 |
| Plastic | 塑料 | - | MakePlastic(?/?) | 82 |
| Cloth | 布料 | - | MakeCloth(tailor's loom/Apparel_B1) | 24 |
| RawDevilstrand | 恶魔菌丝蘑菇 | - | —(基础/矿石) | 2 |
| Leather_CorrectedGrain | 未抛光皮革 (C-) | - | TanDrum_SplitHeavy(tanning drum/?) | 8 |
| BiosyntheticMaterial | 生物科技合成面料 | - | BuildBiosyntheticMaterial(robotic assembler/?) | 12 |
| Polystyrene | 聚苯乙烯 | - | MakePolystyrene(Petrochemical Laboratory/Oil_Industry_C3) | 2 |
| Hypericum | 贯叶金丝桃 | - | —(基础/矿石) | 6 |
| Polymers | 高分子化合物 | - | ConvertToPolymers(Nucleosynthetic Converter/Super_matter_E1) | 12 |
| HMFibers | 高模量纤维 | - | —(基础/矿石) | 3 |

### 化工/原料

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| Butadiene | 丁二烯 | - | —(基础/矿石) | 3 |
| Propylene | 丙烯 | - | —(基础/矿石) | 5 |
| Xylene | 二甲苯 | - | —(基础/矿石) | 5 |
| FluxPowder | 助熔剂 | - | fireFlux_process(Ceramic Kiln/?) | 8 |
| Chemfuel | 化学燃料 | - | Make_ChemfuelFromWood(biorefinery/?) | 12 |
| IsopropylAlcohol | 异丙醇 | - | MakeIsopropylAlcohol(Petrochemical Laboratory/HospitalBed) | 2 |
| PlantWax | 植物蜡 | - | MakePlantWax(Kitchen Table/Chemistry_C0) | 3 |
| Rubber | 橡胶 | - | MakeRubber(Petrochemical Laboratory/Oil_Industry_C3) | 21 |
| Chlorine | 氯精 | - | MakeChlorine(Chemistry Station/Chemistry_C0) | 5 |
| Powder | 火药 | - | MakePowder(blacksmith/?) | 100 |
| BlackPowder | 火药 | - | MakePowder_medieval(blacksmith/GunPowderTech) | 19 |
| Lye | 烧碱 | - | MakeChlorine(Chemistry Station/Chemistry_C0) | 4 |
| Salt | 盐 | - | VG_Minesalt(salt mine/?) | 24 |
| Paraffins | 石蜡 | - | ConvertToParaffins(Nucleosynthetic Converter/Super_matter_E1) | 6 |
| Sulfur | 硫磺 | - | MakeSulfur(blacksmith/?) | 7 |
| Sulphates | 硫酸盐 | - | ConvertToSulphates(Nucleosynthetic Converter/Super_matter_E1) | 9 |
| Neutroamine | 神经胺 | - | MakeNaturalNeutroamine(drug lab/Drugs_C2) | 4 |
| Compaste | 粘合剂 | - | MakeEpoxy(Petrochemical Laboratory/Oil_Industry_C3) | 46 |
| Cellulose | 纤维素 | - | MakeCelluloseCotton(Chemistry Station/Chemistry_C0) | 2 |
| NeutroPetals | 纽特罗花 | - | —(基础/矿石) | 2 |
| FishOil | 鱼油 | - | MakeFishOil(Campfire/Food_00) | 2 |

### 矿石/基础料

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| BroadshieldCore |  | - | —(基础/矿石) | 2 |
| ChunkLimestone |  | - | —(基础/矿石) | 2 |
| SubcoreHigh |  | - | —(基础/矿石) | 6 |
| SubcoreRegular |  | - | —(基础/矿石) | 8 |
| Glass | 干草 | - | MakeGlass(blast furnace/Glass_B1) | 6 |
| GlassBatch | 未加工玻璃 | - | MakeGlassBatch(glassworks table/Glass_B1) | 12 |
| TreeBark | 树皮 | - | ExtractTreeBark_Hand(chopping block/?) | 19 |
| SandResource | 沙子 | - | MakeSand_Hand(hand cutting table/Stonecutting) | 14 |
| CrushedStone | 石块 | - | MakeCrushedStone_Hand(hand cutting table/?) | 10 |
| AIPersonaCore | 芯片：AI核心 | - | —(基础/矿石) | 2 |
| TinBar | 锡 | - | MakeTinBars_Hand(fire pit/Craft_B1) | 7 |

### 零部件/电子

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| ComponentIndustrial |  | - | Make_ComponentIndustrial(hand assembling workbench/Craft_0) | 239 |
| ComponentSpacer |  | - | Make_ComponentSpacer(electric assembling bench/Components_D1) | 48 |
| MAAI_Chip | MAAI芯片 | - | —(基础/矿石) | 4 |
| ComponentUltra | 先进组件 | - | Make_ComponentUltra(robotic assembler/Components_E1) | 14 |
| SMG_Component | 冲锋枪零件 | - | MakeSMG_Component(weapon crafting workbench/BlowbackOperation) | 10 |
| ChipBoard | 刨花板 | - | SawPlanks_Chipboard(hand sawmill/?) | 2 |
| ComponentMedieval | 原始组件 | - | Make_ComponentMedieval(craftsman table/Craft_0) | 108 |
| Launcher_Component | 发射器零件 | - | MakeLauncher_Component(advanced weapon crafting workbench/Rockets_C1) | 10 |
| AdvMechanism | 复杂机械装置 | - | MakeAdvMechanism_Hand(hand assembling workbench/Components_D1) | 18 |
| Microchips | 大规模集成电路 | - | MakeMicrochipIntegratedCircuits(electronics table/Electronics_D1) | 11 |
| Pistol_Component | 手枪零件 | - | MakePistol_Component(weapon crafting workbench/Gunsmithing) | 9 |
| Mechanism | 机械装置 | - | MakeSimpleMechanism_Hand(hand assembling workbench/?) | 34 |
| Rifle_Component | 步枪零件 | - | MakeRifle_Component(weapon crafting workbench/Rifles_C1) | 16 |
| Laser_Component | 激光枪部件 | - | MakeLaser_Component(energy weapon workbench/ChargedShot) | 7 |
| Cannon_Component | 火炮零件 | - | MakeCannon_Component(advanced weapon crafting workbench/Turrets_31) | 11 |
| Sniper_Component | 狙击步枪零件 | - | MakeSniper_Component(weapon crafting workbench/Sniper_rifles_C1) | 10 |
| BioMicrochips | 生物技术集成电路 | - | MakeBioelectronicDevices(robotic assembler/Components_D3) | 6 |
| ElectronicComponents | 电子零件 | - | MakeElectronicComponents(electronics table/?) | 51 |
| NeurocureFramework | 神经复合体 | - | —(基础/矿石) | 15 |
| NeuromuscularFramework | 神经肌肉框架 | - | —(基础/矿石) | 2 |
| Plasma_Component | 等离子枪部件 | - | MakePlasma_Component(energy weapon workbench/ChargedShot) | 8 |
| NanotechFramework | 纳米技术结构 | - | MakeNanotechFramework(Petrochemical Laboratory/Components_D2) | 3 |
| Charged_Component | 能量武器部件 | - | MakeCharged_Component(energy weapon workbench/ChargedShot) | 4 |
| Heavy_Component | 重型武器零件 | - | MakeHeavy_Component(advanced weapon crafting workbench/HeavyTurrets) | 22 |
| Shotgun_Component | 霰弹枪零件 | - | MakeShotgun_Component(weapon crafting workbench/GasOperation) | 10 |
| AdvRifle_Component | 高级步枪零件 | - | MakeAdvRifle_Component(advanced weapon crafting workbench/Rifles_C2) | 12 |
| AdvSniper_Component | 高级狙击步枪零件 | - | MakeAdvSniper_Component(advanced weapon crafting workbench/Sniper_rifles_D1) | 6 |
| ComponentAdvanced | 高级组件 | - | Make_ComponentAdvanced(hand assembling workbench/Components_D1) | 11 |

### Stuff/Metallic

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| RK_Halberd |  | Metallic | RKHSKFix_Make_RK_Halberd(fueled smithy/LongBlades) | 1 |
| RK_Mace |  | Metallic | RKHSKFix_Make_RK_Mace(fueled smithy/Smithing) | 1 |
| RK_Spear |  | Metallic | RKHSKFix_Make_RK_Spear(fueled smithy/Ratkin_Armor_B1) | 1 |
| SubcoreBasic |  | Metallic | —(基础/矿石) | 7 |

### Stuff/StrongMetallic

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| Apparel_ShieldBelt |  | StrongMetallic | —(基础/矿石) | 1 |
| RK_HeavyLance |  | StrongMetallic | RKHSKFix_Make_RK_HeavyLance(fueled smithy/LongBlades) | 1 |
| RK_LongSword |  | StrongMetallic | RKHSKFix_Make_RK_LongSword(fueled smithy/Smithing) | 1 |
| RK_OneHanded |  | StrongMetallic | RKHSKFix_Make_RK_OneHanded(fueled smithy/Smithing) | 1 |
| RK_TwoHanded |  | StrongMetallic | RKHSKFix_Make_RK_TwoHanded(fueled smithy/LongBlades) | 1 |
| RK_Weapon_Arbalest |  | StrongMetallic | —(基础/矿石) | 1 |
| MeleeWeapon_Dagger | 匕首 | StrongMetallic | —(基础/矿石) | 1 |
| Bow_Compound | 复合弓 | StrongMetallic | —(基础/矿石) | 1 |
| MedievalTimes_Gauntlets | 板甲手套 | StrongMetallic | —(基础/矿石) | 1 |
| Apparello_MetalBoots | 板甲靴 | StrongMetallic | —(基础/矿石) | 1 |
| Crossbow_Compound | 滑轮弩 | StrongMetallic | —(基础/矿石) | 1 |
| ViTech_MailMitts | 锁甲手套 | StrongMetallic | —(基础/矿石) | 1 |
| Arbalest | 阿瓦雷斯特 | StrongMetallic | —(基础/矿石) | 1 |

### Stuff/Woody/SoftwoodLumber/HardwoodLumber/UltrahardwoodLumber/EngineeredLumber

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| RK_LightLance |  | Woody/SoftwoodLumber/HardwoodLumber/UltrahardwoodLumber/EngineeredLumber | RKHSKFix_Make_RK_LightLance(fueled smithy/Smithing) | 1 |

### Stuff/Woody

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| Bow_Great_Unique |  | Woody | —(基础/矿石) | 1 |
| Bow_Great | 大弓 | Woody | —(基础/矿石) | 1 |
| Crossbow | 弩 | Woody | —(基础/矿石) | 1 |
| MeleeWeapon_Club | 棍棒 | Woody | —(基础/矿石) | 1 |

### Stuff/WoodLogs/Woody

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| Bow_Recurve | 反曲弓 | WoodLogs/Woody | —(基础/矿石) | 1 |
| Bow_Short | 短弓 | WoodLogs/Woody | —(基础/矿石) | 1 |

### Stuff/HF/Fabric

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| Gloves_Chef | 厨房手套 | HF/Fabric | —(基础/矿石) | 1 |

### Stuff/Leathery

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| Gloves_Worker | 工作手套 | Leathery | —(基础/矿石) | 1 |
| Gloves_Tactical | 皮革握柄 | Leathery | —(基础/矿石) | 1 |

### Stuff/LeatheryHard/StrongMetallic

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| MedievalTimes_Boots_Steel_Plated | 强化护腿 | LeatheryHard/StrongMetallic | —(基础/矿石) | 1 |
| MedievalTimes_Gloves_Plated | 强化臂铠 | LeatheryHard/StrongMetallic | —(基础/矿石) | 1 |

### Stuff/RareMetallic

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| Bow_Archotech_Prototype | 古代科技复刻版 A-1「弓」 | RareMetallic | —(基础/矿石) | 1 |
| MeleeWeapon_Archotech1H_Prototype | 始祖科技B-2「刃」的复制品 | RareMetallic | —(基础/矿石) | 1 |
| Gun_ArchotechBlaster_Prototype | 阿尔科科技A-2“爆能枪”复制品 | RareMetallic | —(基础/矿石) | 1 |
| Gun_ArchotechSMG_Prototype | 阿尔科科技A-3“冲锋枪”复制品 | RareMetallic | —(基础/矿石) | 1 |
| MeleeWeapon_Archotech2H_Prototype | 阿尔科科技B-1“阔剑”复制品 | RareMetallic | —(基础/矿石) | 1 |

### Stuff/StrongMetallic/StrongMetallic

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| FissionWarhead | 核弹头 | StrongMetallic/StrongMetallic | —(基础/矿石) | 1 |

### 其它

| defName | 中文 | 类别 | 产自配方(建筑/研究) | 用于配方数 |
|---|---|---|---|---|
| Chocolate |  | - | MakeChocolate(candy table/Food_C4) | 3 |
| Corpse_Duck |  | - | —(基础/矿石) | 2 |
| Dye |  | - | —(基础/矿石) | 2 |
| Hay |  | - | —(基础/矿石) | 9 |
| Hexcell | Hex电池 | - | MakeNanowireBattery(electronics table/Droids_D1) | 13 |
| Meat_ThingDefFishSduiggles |  | - | ProcessSmallFish(butcher table/Fishing) | 10 |
| MedicineHerbal |  | - | —(基础/矿石) | 12 |
| MedicineIndustrial |  | - | makemedicine(medical table/MedicineProduction) | 26 |
| MedicineUltratech |  | - | makeadvmedicine(medical table/Medicine_D1) | 50 |
| Milk |  | - | —(基础/矿石) | 12 |
| OAGene_IceCrystal |  | - | —(基础/矿石) | 2 |
| Oberonia_Aurea |  | - | —(基础/矿石) | 12 |
| Oberonia_Aurea_Chanwu_D |  | - | Make_Oberonia_Aurea_Chanwu_D(electric stove/OA_RK_Oberonia_Aurea_Research_B) | 4 |
| PsychoidLeaves |  | - | —(基础/矿石) | 2 |
| RK_Crossbow |  | - | RKHSKFix_Make_RK_Crossbow(fueled smithy/Greatbow) | 2 |
| RawBerries |  | - | —(基础/矿石) | 4 |
| RawCorn |  | - | —(基础/矿石) | 10 |
| RawFungus |  | - | —(基础/矿石) | 2 |
| RawHops |  | - | —(基础/矿石) | 3 |
| RawPotatoes |  | - | —(基础/矿石) | 11 |
| RawRice |  | - | —(基础/矿石) | 16 |
| Seed_Acacia |  | - | —(基础/矿石) | 2 |
| SmokeleafLeaves |  | - | —(基础/矿石) | 8 |
| TavCornDough |  | - | Make_TavCornDough(tavern oven/?) | 4 |
| TavPadding |  | - | TavMake_Padding(tailor's bench/?) | 3 |
| Firewood_Spruce | 云杉柴薪 | - | —(基础/矿石) | 3 |
| ArtificialBone | 人工骨骼 | - | Make_ArtificialBone(synthetic organ assembler/Prosthesis_making_D3) | 3 |
| Meat_Human | 人肉 | - | MinceOrgans(butcher table/Food_0) | 3 |
| InstinctOptimizingNanobots | 优化本能纳米机器人 | - | —(基础/矿石) | 3 |
| RawGlowbulb | 光草 | - | —(基础/矿石) | 8 |
| Napalm | 凝固汽油 | - | MakeNapalm(ammo crafting table/?) | 5 |
| Seitan | 制作素肉（x5） | - | Seitan(Kitchen Table/Food_B2) | 3 |
| Hide_ImmunogenicHideCured | 加工过的免疫源性皮革 | - | BrineCure_ImmunogenicHide(brine-curing station/?) | 4 |
| Hide_RodentSkinCured | 加工过的啮齿动物皮 | - | BrineCure_RodentSkins(brine-curing station/?) | 2 |
| Hide_PorousHideCured | 加工过的多孔皮革 | - | BrineCure_PorousHide(brine-curing station/?) | 4 |
| Hide_SoftHideCured | 加工过的柔软皮革 | - | BrineCure_SoftHide(brine-curing station/?) | 4 |
| WoodLog | 原木 | - | MakeRivenBoards(electric sawmill/?) | 29 |
| Antimatter | 反物质 | - | —(基础/矿石) | 3 |
| Rawcocoa | 可可果 | - | —(基础/矿石) | 2 |
| CecropiaLog | 号角树原木 | - | —(基础/矿石) | 2 |
| FSX | 含胺乳化燃料 | - | —(基础/矿石) | 56 |
| RawCoffee | 咖啡豆 | - | —(基础/矿石) | 6 |
| Dirt | 土 | - | make_dirt(butcher spot/Craft_0) | 6 |
| Compost | 堆肥 | - | MakeCompost(butcher table/SewageSludgeComposting) | 6 |
| Rawbean | 大豆 | - | —(基础/矿石) | 6 |
| GleamcapStem | 奶油蘑菇 | - | —(基础/矿石) | 3 |
| Cheese | 奶酪 | - | Makecheese(Kitchen Table/Food_B1) | 9 |
| GuardianHairs | 守护者毛发 | - | —(基础/矿石) | 2 |
| AntidoteSafe | 安全解毒剂 | - | MakeAntidoteSafe(medical table/DrugProduction) | 4 |
| Rawwheat | 小麦 | - | —(基础/矿石) | 7 |
| Meat_Muffalo | 屠宰后的肉 | - | —(基础/矿石) | 15 |
| Meat_Elephant | 屠宰的兽肉 | - | —(基础/矿石) | 4 |
| RawGiantLeaf | 巨大的叶子 | - | —(基础/矿石) | 6 |
| driedfruit | 干果 | - | Make_DriedFruit(drying rack/?) | 2 |
| Bandagekit | 急救绷带 | - | MakeBandageKit(medical table/Medicine_A2) | 3 |
| Pasta | 意大利面 | - | MakePasta(Kitchen Table/Food_C2) | 5 |
| Matter | 无机物质 | - | ConvertResourcesRaw(Nucleosynthetic Converter/Super_matter_D1) | 12 |
| WoodPlank | 木板 | - | MakeWoodPlanks_Hand(hand sawmill/?) | 7 |
| Charcoal | 木炭 | - | MakeCharcoal_Hand(blast furnace/Craft_B1) | 3 |
| PoplarLog | 杨木原木 | - | —(基础/矿石) | 2 |
| PoplarPlank | 杨木板 | - | SawPlanks_Poplar(hand sawmill/?) | 7 |
| PineLog | 松木原木 | - | —(基础/矿石) | 2 |
| PinePlank | 松木板 | - | SawPlanks_Pine(hand sawmill/?) | 7 |
| Firewood_Pine | 松木柴火 | - | —(基础/矿石) | 2 |
| MapleLog | 枫木原木 | - | —(基础/矿石) | 2 |
| MaplePlank | 枫木板 | - | SawPlanks_Maple(hand sawmill/?) | 7 |
| CypressLog | 柏木原木 | - | —(基础/矿石) | 2 |
| CypressPlank | 柏木板 | - | SawPlanks_Cypress(hand sawmill/?) | 7 |
| TeakLog | 柚木原木 | - | —(基础/矿石) | 2 |
| TeakPlank | 柚木板 | - | SawPlanks_Teak(hand sawmill/?) | 7 |
| WillowLog | 柳木原木 | - | —(基础/矿石) | 2 |
| WillowPlank | 柳木板 | - | SawPlanks_Willow(hand sawmill/?) | 7 |
| Kindling | 柴火 | - | MakeKindling_Hand(?/Craft_0) | 5 |
| Pitch | 树脂 | - | ExtractPitch(chopping block/?) | 2 |
| Rawpeach | 桃子 | - | —(基础/矿石) | 2 |
| BirchLog | 桦木原木 | - | —(基础/矿石) | 2 |
| BirchPlank | 桦木板 | - | SawPlanks_Birch(hand sawmill/?) | 7 |
| RawCotton | 棉花 | - | —(基础/矿石) | 4 |
| coconutmilk | 椰奶 | - | HalfCoconut(butcher table/Pemmican) | 3 |
| RawCoconut | 椰子果实 | - | —(基础/矿石) | 4 |
| Raworange | 橙子 | - | —(基础/矿石) | 2 |
| OakLog | 橡木原木 | - | —(基础/矿石) | 2 |
| OakPlank | 橡木板 | - | SawPlanks_Oak(hand sawmill/?) | 7 |
| RawCaoutchouc | 橡胶树汁液 | - | —(基础/矿石) | 2 |
| Weapon_Parts | 武器配件 | - | MakeWeapon_Parts(weapon crafting workbench/Machining) | 125 |
| PoisonGland | 毒腺 | - | —(基础/矿石) | 2 |
| Tallow | 油脂 | - | MakeTallow(Campfire/Food_00) | 3 |
| Rawonion | 洋葱 | - | —(基础/矿石) | 7 |
| Bioferrite | 活铁 | - | —(基础/矿石) | 3 |
| RawAlgae | 海藻 | - | —(基础/矿石) | 4 |
| ConcreteResource | 混凝土 | - | MixConcrete(hand concrete mixer/Concrete_C1) | 2 |
| Ash | 灰烬 | - | CremateCorpse(crematorium/Craft_0) | 22 |
| CannonParts | 炮管部件 | - | Make_CannonParts_Forged(blacksmith/GunPowderTech) | 5 |
| RoastedCoffeeBeans | 烘焙咖啡豆 | - | RoastingCoffeeBeans(oven/Pemmican) | 2 |
| RawTobacco | 烟叶 | - | —(基础/矿石) | 6 |
| Solder | 焊料 | - | MakeSolder(induction furnace/Metals_C1) | 3 |
| Coke | 焦炭 | - | coke_process(Ceramic Kiln/Metallurgy_B3) | 6 |
| Coal | 煤炭 | - | —(基础/矿石) | 4 |
| Hide_HumanSkinCured | 熟制人皮 | - | BrineCure_HumanSkins(brine-curing station/?) | 2 |
| Hide_RichFurPeltCured | 熟制优质毛皮 | - | RackSalt_RichFurPelt(Drying Rack/?) | 5 |
| Hide_WarmFurPeltCured | 熟制保暖毛皮 | - | BrineCure_WarmFurPelt(brine-curing station/?) | 7 |
| Hide_RuggedFurPeltCured | 熟制坚韧毛皮 | - | BrineCure_RuggedFurPelt(brine-curing station/?) | 7 |
| TalonAether | 爪之以太 | - | —(基础/矿石) | 2 |
| Cornmeal | 玉米粉 | - | CraftCornmeal(milling stone/?) | 3 |
| Rawsugarcane | 甘蔗 | - | —(基础/矿石) | 4 |
| Hide_HumanSkin | 生人皮 | - | —(基础/矿石) | 5 |
| Hide_RichFurPelt | 生优质毛皮 | - | —(基础/矿石) | 7 |
| Hide_WarmFurPelt | 生保暖毛皮 | - | —(基础/矿石) | 9 |
| Hide_ImmunogenicHide | 生免疫源性皮革 | - | —(基础/矿石) | 6 |
| Hide_HeavyFurPelt | 生厚重毛皮 | - | —(基础/矿石) | 9 |
| Hide_HeavyHide | 生厚重皮革 | - | —(基础/矿石) | 6 |
| lifesupport | 生命维持装置 | - | —(基础/矿石) | 5 |
| Hide_RodentSkin | 生啮齿动物皮 | - | —(基础/矿石) | 5 |
| Hide_RuggedFurPelt | 生坚韧毛皮 | - | —(基础/矿石) | 9 |
| Hide_PorousHide | 生多孔皮革 | - | —(基础/矿石) | 6 |
| Hide_ExoticSkin | 生异兽皮 | - | —(基础/矿石) | 7 |
| Hide_StockHide | 生普通皮革 | - | —(基础/矿石) | 6 |
| Hide_SoftHide | 生柔软皮革 | - | —(基础/矿石) | 6 |
| Biomatter | 生物工程物质 | - | ExtractBiomatterCorpse(organ vat/?) | 21 |
| Hide_DurableHide | 生耐用皮革 | - | —(基础/矿石) | 6 |
| Hide_LightAnimalSkin | 生轻型动物皮 | - | —(基础/矿石) | 7 |
| Hide_WaterproofSkin | 生防水皮 | - | —(基础/矿石) | 5 |
| Wire | 电线 | - | MakeWires_Hand(hand assembling workbench/Craft_0) | 10 |
| RawTomatoes | 番茄 | - | —(基础/矿石) | 10 |
| Firewood_Poplar | 白杨木柴薪 | - | —(基础/矿石) | 2 |
| Rawmushroom | 白蘑菇 | - | —(基础/矿石) | 11 |
| Silicon | 硅 | - | MakeSilicon_Electric(glassworks table/MicroelectronicsBasics) | 3 |
| Nitre | 硝酸钾 | - | Make_Nitre_Body(Saltpeter Pit/?) | 4 |
| Carbon | 碳纤维 | - | MakeCarbonNanotubes(Petrochemical Laboratory/Oil_Industry_C6) | 48 |
| Bamboo | 竹子 | - | —(基础/矿石) | 6 |
| BambooPlank | 竹板 | - | MakeBambooPlanks_Hand(hand sawmill/?) | 7 |
| AntidoteSimple | 简易解毒剂 | - | MakeAntidoteSimple(butcher table/Medicine_A2) | 2 |
| FecalSludge | 粪便污泥 | - | —(基础/矿石) | 3 |
| Hide_ExoticSkinCured | 精制异兽皮 | - | BrineCure_ExoticSkins(brine-curing station/?) | 4 |
| Hide_LightAnimalSkinCured | 精制轻型动物皮 | - | BrineCure_LightAnimalSkins(brine-curing station/?) | 4 |
| Hide_WaterproofSkinCured | 精制防水皮 | - | BrineCure_WaterproofSkin(brine-curing station/?) | 3 |
| AgedCheese | 精英奶酪 | - | Make_AgedCheese(cheesemaking basin/?) | 5 |
| Sugar | 糖 | - | Grind_Sugar(windmill/Bakery_B2) | 16 |
| RedWoodLog | 红木 | - | —(基础/矿石) | 2 |
| RedWoodPlank | 红木木板 | - | MakeRedWoodPlanks_Hand(hand sawmill/?) | 7 |
| MangrovePlank | 红树木板 | - | SawPlanks_Mangrove(hand sawmill/?) | 7 |
| MangroveLog | 红树林原木 | - | —(基础/矿石) | 2 |
| PaintingSupplies | 绘画工具 | - | CookPaint(electric stove/Art_A1) | 8 |
| MealJerky | 肉干 | - | Make_Jerky(Jerky Drying Rack/?) | 2 |
| MuscleStimulator | 肌肉刺激器 | - | —(基础/矿石) | 2 |
| Fertilizer | 肥料 | - | MakeFertilizer(?/?) | 3 |
| RawCarrots | 胡萝卜 | - | —(基础/矿石) | 5 |
| RottedMush | 腐烂的食物 | - | —(基础/矿石) | 2 |
| aloe | 芦荟 | - | —(基础/矿石) | 4 |
| Prometheum | 苯基液体燃料 | - | SeperatePrometheum(Gas centrifuge/RimatomicsActivate) | 16 |
| Rawapple | 苹果 | - | —(基础/矿石) | 3 |
| RawTea | 茶叶 | - | —(基础/矿石) | 4 |
| Rawgrape | 葡萄 | - | —(基础/矿石) | 3 |
| Alcohol_Wine | 葡萄酒 | - | EmptyCrate_Wine(brewery/Brewing) | 2 |
| MintLeaves | 薄荷叶 | - | —(基础/矿石) | 5 |
| CecropiaPlank | 蚁巢树木板 | - | SawPlanks_Cecropia(hand sawmill/?) | 7 |
| Surfactant | 表面活性剂 | - | MakeSurfactant(drug lab/Firefoam) | 4 |
| SoyMilk | 豆浆 | - | Makesoymilk(butcher table/Pemmican) | 6 |
| Tofu | 豆腐 | - | MakeTofu(Kitchen Table/Food_B1) | 12 |
| MagneticMaterial | 超强力磁铁 | - | BuildMagneticMaterial(robotic assembler/Components_D3) | 14 |
| SoftClay | 软粘土 | - | ExtractSoftClay(stonecutter's bench/?) | 17 |
| RawPeppers | 辣椒 | - | —(基础/矿石) | 4 |
| Yeast | 酵母 | - | Makeyeast(brewery/Brewing) | 10 |
| WildRose | 野生玫瑰 | - | —(基础/矿石) | 7 |
| AcaciaLog | 金合欢原木 | - | —(基础/矿石) | 2 |
| AcaciaPlank | 金合欢木板 | - | SawPlanks_Acacia(hand sawmill/?) | 7 |
| Ilmenite | 钛矿石 | - | ConvertToTitanium(Nucleosynthetic Converter/Super_matter_E1) | 3 |
| Wolframite | 钨矿石 | - | —(基础/矿石) | 3 |
| Anglesite | 铅矾 | - | —(基础/矿石) | 7 |
| Chromium | 铬锭 | - | ProcessForChromium(?/?) | 29 |
| Tin | 锡矿石 | - | ConvertToTin(Nucleosynthetic Converter/Super_matter_E1) | 6 |
| RawShimmershroom | 闪光蘑菇 | - | —(基础/矿石) | 8 |
| Sphalerite | 闪锌矿 | - | —(基础/矿石) | 4 |
| ClayPotWetUnglazed | 陶罐（未烧制） | - | MakeClayPotWet(pottery bench/?) | 3 |
| Electronics | 集成电路 | - | MakeIntegratedCircuits(electronics table/Electronics_C2) | 33 |
| bread | 面包 | - | MakeBread(oven/Bakery_B1) | 5 |
| Flour | 面粉 | - | Grind_Flour(windmill/Bakery_B2) | 20 |
| Hide_HeavyFurPeltCured | 鞣制厚重毛皮 | - | BrineCure_HeavyFurPelt(brine-curing station/?) | 7 |
| Hide_HeavyHideCured | 鞣制厚重皮革 | - | BrineCure_HeavyHide(brine-curing station/?) | 4 |
| Hide_StockHideCured | 鞣制普通皮革 | - | BrineCure_StockHide(brine-curing station/?) | 4 |
| Hide_DurableHideCured | 鞣制耐用皮革 | - | BrineCure_DurableHide(brine-curing station/?) | 4 |
| Rawbanana | 香蕉 | - | —(基础/矿石) | 2 |
| DragonwoodPlank | 龙木木板 | - | SawPlanks_Drago(hand sawmill/?) | 7 |
| DragonwoodLog | 龙血木原木 | - | —(基础/矿石) | 2 |
| DragonEsters | 龙酯 | - | —(基础/矿石) | 2 |

## 二、Vile's Materials Science 专属物料产业线

以下为 Vile 模组独有的物料(defName 以 Vile 汉化包识别),列出其**直接生产配方**与**原料**,即 Vile 的产业链。

| 物料 | 中文 | 生产配方 | 原料(数量) | 建筑 | 研究 |
|---|---|---|---|---|---|
| ABS | ABS塑料 | MakeABS | Propylene×10.0, SyntheticAmmonia×5.0, Butadiene×10.0, Polystyrene×10.0 | Petrochemical Laboratory | Oil_Industry_C4 |
| AerMet | 艾尔梅特-100不锈钢 | MakeAerMetSteel | PigIron×15.0, Cobalt×4.0, NickelBar×3.0, Chromium×2.0, Molybdenum×2.0 | electric arc furnace | Metals_D1 |
| Aerographene | 气石墨烯复合材料 | MakeAerographene | Fiberglass×10.0, Sulphates×15.0, Carbon×10.0 | Petrochemical Laboratory | Oil_Industry_D1 |
| AlnicoAlloy | 铝镍钴合金锭 | MakeAlnicoAlloy | Steel×8.0, AluminiumBar×3.0, Cobalt×3.0, NickelBar×6.0 | ? | ? |
| AlnicoAlloy | 铝镍钴合金锭 | MakeToolSteel | PigIron×10.0, Manganese×5.0, Chromium×2.0, Tungsten×2.0 | induction furnace | Metals_C3 |
| AlphaPoly | 阿尔法聚合物 | DisassembleAIPersonaCore | AIPersonaCore×1.0 | robotic assembler | Components_D2 |
| AlphaPoly | 阿尔法聚合物 | ProduceAPoly | cat:SuperBar×10.0, RoughGem/CutGem×1.0, Antimatter×3.0 | quantum fabricator | ? |
| AluminiumBar | 铝锭 | MakeAluminiumBars_Electric | Aluminium×20.0 | metal extraction plant | ? |
| AluminiumBar | 铝锭 | SmeltBauxite_Basic | Aluminium×15.0, Salt×5.0, Coke×5.0 | metal extraction plant | Metals_C1 |
| AluminiumBar | 铝锭 | SmeltBauxite_Industrial | Aluminium×20.0, Lye×5.0 | metal extraction plant | Metals_C2 |
| AluminiumBar | 铝锭 | SmeltBauxite_HighTech | Aluminium×20.0, Lye×10.0 | electrolytic refinery | Metals_C3 |
| AnodizedAluminiumBlue | 6061铝合金 | MakeAnodizedAluminiumBlue | AluminiumBar×20.0, GlassBatch×6.0, CopperBar×2.0 | induction furnace | Metals_C2 |
| AnodizedAluminiumRed | 3003铝合金 | MakeAnodizedAluminiumRed | AluminiumBar×20.0, GlassBatch×6.0 | induction furnace | Metals_C2 |
| BetaPoly | 贝塔聚合物 | ProduceBPoly | BiosyntheticMaterial×10.0, Antimatter×3.0, Vanadium×5.0 | quantum fabricator | ? |
| BlackPowder | 火药 | MakePowder_medieval | Nitre×75.0, cat:Coal×20.0, Sulfur×20.0 | blacksmith | GunPowderTech |
| BogIron | 沼泽铁矿 | Extract_BogIron | Peat×25.0 | blacksmith | BogIronTech |
| Butadiene | 丁二烯 | (无配方/为基础料) | — | — | — |
| CarbonAlloy | 碳纤维合金锭 | DisassembleAIPersonaCore | AIPersonaCore×1.0 | robotic assembler | Components_D2 |
| CarbonAlloy | 碳纤维合金锭 | MakeNanocomposite | Carbon×10.0, Compaste×10.0, HMFibers/GuardianHairs×5.0 | Petrochemical Laboratory | Oil_Industry_D1 |
| CarbonSteel | 中碳钢 | MakeCarbonSteel | PigIron×20.0, Manganese×5.0 | induction furnace | Metals_C1 |
| Carborundum | 金刚砂 | MakeCarborundum | GlassBatch×40.0, Coke×20.0 | electric arc furnace | Metals_D1 |
| Cellulose | 纤维素 | MakeCelluloseCotton | RawCotton/SmokeleafLeaves×10.0 | Chemistry Station | Chemistry_C0 |
| Cellulose | 纤维素 | MakeCelluloseWood | Kindling×50.0 | Chemistry Station | Chemistry_C0 |
| ChitinPlating | 几丁质覆层 | MakeZylon | Modacrylic×5.0, Xylene×5.0, Chitin×20.0 | Chemistry Station | Apparel_D1 |
| Chlorine | 氯精 | MakeChlorine | Salt×10.0 | Chemistry Station | Chemistry_C0 |
| Cloth | 布料 | MakeCloth | RawCotton×20.0 | tailor's loom | Apparel_B1 |
| Compaste | 粘合剂 | MakeEpoxy | Sulphates/TalonAether×20.0, Lye×5.0 | Petrochemical Laboratory | Oil_Industry_C3 |
| Compaste | 粘合剂 | MakeCompasteFromDragonScales | DragonScales×50.0 | Chemistry Station | AdvancedFabrication |
| CupronickelAlloy | 美柯尔合金锭 | MakeCupronickelAlloy | CopperBar×24.0, NickelBar×6.0 | ? | ? |
| CupronickelAlloy | 美柯尔合金锭 | MakeBrass | CopperBar×20.0, Zinc×15.0 | induction furnace | Metals_C1 |
| DepletedUranium | 贫铀 | MakeDepletedUraniumAlloy | Uranium×10.0 | ? | ? |
| DepletedUranium | 贫铀 | SmeltCarnotite_Industrial | Uranium×15.0 | metal extraction plant | Metals_C2 |
| DepletedUranium | 贫铀 | SmeltCarnotite_HighTech | Uranium×15.0 | electrolytic refinery | Metals_C3 |
| DevilstrandCloth | 魔鬼布 | MakeMycotanLeather | RawDevilstrand×20.0, Xylene/PlantWax/Tallow×5.0 | Chemistry Station | AdvancedFabrication |
| FerrosiliconAlloy | 桑德斯合金锭 | MakeFerrosiliconAlloy | Steel×15.0, Silicon×10.0, AluminiumBar×5.0 | metal extraction plant | ? |
| FerrosiliconAlloy | 桑德斯合金锭 | MakeStainlessSteel | PigIron×15.0, Chromium×7.0, NickelBar×3.0 | induction furnace | Metals_C2 |
| Fiberglass | 玻璃纤维 | MakeFiberglass | GlassBatch×40.0, Compaste×10.0 | Petrochemical Laboratory | Oil_Industry_C4 |
| Flaxcloth | 亚麻布 | MakeLinen | RawFlax×20.0 | tailor's loom | ? |
| Fleece | 摇粒绒 | (无配方/为基础料) | — | — | — |
| HMFibers | 高模量纤维 | (无配方/为基础料) | — | — | — |
| HempCloth | 麻布 | MakeHempCloth | SmokeleafLeaves×30.0 | tailor's loom | Apparel_B1 |
| Hyperweave | 超织物 | MakeDyneema | HMFibers×10.0, Synthread×10.0 | Chemistry Station | Apparel_D1 |
| IsopropylAlcohol | 异丙醇 | MakeIsopropylAlcohol | Propylene×20.0 | Petrochemical Laboratory | HospitalBed |
| Kevlar | 凯夫拉布 | MakeKevlar | Sulphates×10.0, Xylene×10.0, SyntheticAmmonia×10.0 | Chemistry Station | AdvancedFabrication |
| Lye | 烧碱 | MakeChlorine | Salt×10.0 | Chemistry Station | Chemistry_C0 |
| Manganese | 锰（Mn） | SmeltBogIron_Industrial | BogIron×15.0, cat:Coal/Metallurgical×5.0 | metal extraction plant | Metals_C2 |
| Manganese | 锰（Mn） | SmeltCassiterite_Industrial | Tin×20.0 | metal extraction plant | Metals_C2 |
| Manganese | 锰（Mn） | SmeltCassiterite_HighTech | Tin×20.0 | electrolytic refinery | Metals_C3 |
| Manganese | 锰（Mn） | SmeltSphalerite_Industrial | Sphalerite×20.0, cat:Coal/Metallurgical×3.0 | metal extraction plant | Metals_C2 |
| Manganese | 锰（Mn） | SmeltSphalerite_HighTech | Sphalerite×20.0, cat:Coal/Metallurgical×5.0 | electrolytic refinery | Metals_C3 |
| MaxametSteel | 马克萨梅特钢 | MakeMaxametSteel | PigIron×12.0, Cobalt×4.0, Tungsten×6.0, Vanadium×3.0 | electric arc furnace | Metals_D1 |
| Micropel | 米克洛佩尔 | MakeMicropel | Cloth×10.0, Carbon×4.0, SilverBar×2.0 | Chemistry Station | Apparel_D1 |
| Modacrylic | 改性腈纶 | MakeModacrylic | SyntheticFibers×15.0, Propylene×20.0 | Chemistry Station | AdvancedFabrication |
| Molybdenum | 钼 (Mo) | SmeltScheelite_HighTech | Wolframite×15.0 | electrolytic refinery | Metals_C3 |
| Molybdenum | 钼 (Mo) | SmeltRutile_HighTech | Ilmenite×15.0 | electrolytic refinery | Metals_C3 |
| NanotechFramework | 纳米技术结构 | MakeNanotechFramework | Carbon×30.0, Biomatter×20.0 | Petrochemical Laboratory | Components_D2 |
| Neutroamine | 神经胺 | MakeNaturalNeutroamine | NeutroPetals×8.0, Hypericum×2.0 | drug lab | Drugs_C2 |
| Nimonic | 尼莫尼克超级合金 | MakeNimonic | NickelBar×15.0, Chromium×5.0, Titanium×3.0, AluminiumBar×2.0 | electric arc furnace | Metals_D1 |
| NitinolAlloy | 镍钛诺合金锭 | MakeNitinolAlloy | Titanium×4.0, NickelBar×11.0 | ? | ? |
| NitinolAlloy | 镍钛诺合金锭 | MakeNitinol | NickelBar×15.0, Titanium×10.0 | electric arc furnace | Metals_D1 |
| Nomex | 诺梅克斯 | MakeNomex | Sulphates×10.0, Xylene×10.0, Manganese/Cobalt×1.0 | Chemistry Station | AdvancedFabrication |
| Nylon | 尼龙 | MakeNylon | Butadiene×40.0, Chlorine×15.0, SyntheticAmmonia×5.0 | Chemistry Station | Oil_Industry_C6 |
| PPPlastic | PP塑料 | MakePolypropylene | Propylene×50.0 | Petrochemical Laboratory | ? |
| PVCLeather | 乙烯基 | MakePVCLeather | Polymers×20.0, Chlorine×5.0 | Chemistry Station | AdvancedFabrication |
| PVCPlastic | PVC塑料 | MakePVCPlastic | Polymers×40.0, Chlorine×5.0 | Petrochemical Laboratory | ? |
| Paraffins | 石蜡 | ConvertToParaffins | Matter×50.0 | Nucleosynthetic Converter | Super_matter_E1 |
| Paraffins | 石蜡 | MakePTFE | Chlorine×3.0, CrushedStone×10.0, Sulfur×2.0 | Chemistry Station | Chemistry_C2 |
| PlantWax | 植物蜡 | MakePlantWax | RawCoconut/Rawbean×1.5 | Kitchen Table | Chemistry_C0 |
| PlantWax | 植物蜡 | ExtractVegOil | cat:SeedsCategory×5.0 | milling stone | ? |
| PlantWax | 植物蜡 | ExtractOliveOilWindmill | cat:SeedsCategory×15.0 | windmill | ? |
| Plasteel | 铁钛合金锭 | MakeFerrotitaniumAlloy | Titanium×5.0, Steel×12.0, CarbonAlloy×3.0 | ? | ? |
| Plasteel | 铁钛合金锭 | MakeDualPhaseTitanium | Titanium×15.0, AluminiumBar×5.0, Vanadium×5.0 | electric arc furnace | Metals_D1 |
| Plasteel | 铁钛合金锭 | RKHSKSmeltWeaponCrate | cat:WeaponCrate×1.0 | blacksmith | ? |
| Plastic | 塑料 | MakePlastic | Polymers×30.0 | ? | ? |
| Plastic | 塑料 | MakePlasticSulphate | Polymers×20.0, Sulphates×5.0 | ? | ? |
| Plastic | 塑料 | MakePlasticSulphateBulk | Polymers×60.0, Sulphates×15.0 | ? | ? |
| Plastic | 塑料 | MakeHDPE | Polymers×50.0 | Petrochemical Laboratory | ? |
| Plexiglass | 有机玻璃 | MakePlexiglass | Propylene×50.0 | Petrochemical Laboratory | ? |
| PobediteAlloy | 波维达特合金锭 | MakePobediteAlloy | Tungsten×3.0, CarbonAlloy×3.0, Cobalt×3.0 | ? | ? |
| PobediteAlloy | 波维达特合金锭 | MakeTungstenCarbide | Tungsten×15.0, Cobalt×5.0, Coke×5.0 | electric arc furnace | Metals_D1 |
| Polycarbonate | 聚碳酸酯 | MakePolycarbonate | Sulphates×30.0, Chlorine×10.0, ActivatedCarbon×5.0 | Petrochemical Laboratory | Oil_Industry_C4 |
| Polymers | 高分子化合物 | ConvertToPolymers | Matter×50.0 | Nucleosynthetic Converter | Super_matter_E1 |
| Polystyrene | 聚苯乙烯 | MakePolystyrene | Sulphates×5.0, Polymers×5.0 | Petrochemical Laboratory | Oil_Industry_C3 |
| Propylene | 丙烯 | (无配方/为基础料) | — | — | — |
| PureSilver | 纯银 (Ag) | MakeLeadBars_Hand | Anglesite×10.0 | fire pit | Craft_B1 |
| PureSilver | 纯银 (Ag) | MakeLeadBars_Foundry | Anglesite×20.0 | foundry | Craft_B1 |
| PureSilver | 纯银 (Ag) | SmeltGalena_Basic | Anglesite×10.0 | metal extraction plant | Metals_C1 |
| PureSilver | 纯银 (Ag) | SmeltGalena_Industrial | Anglesite×20.0 | metal extraction plant | Craft_0 |
| PureSilver | 纯银 (Ag) | SmeltGalena_HighTech | Anglesite×20.0 | electrolytic refinery | Metals_C3 |
| Pyromet | 派罗梅特超级合金 | MakePyromet | PigIron×12.0, NickelBar×8.0, Chromium×6.0, Molybdenum×4.0 | electric arc furnace | Metals_D1 |
| Quartz | 石英 | (无配方/为基础料) | — | — | — |
| Rayon | 粘胶纤维 | MakeRayonViscose | Cellulose×20.0, Lye×5.0 | Chemistry Station | AdvancedFabrication |
| ReinforcedGlass | 强化玻璃 | MakeReinforcedGlass | Glass×20.0, Polycarbonate×10.0 | glassworks table | Glass_C1 |
| SewingKit | 缝纫工具包 | MakeSewingKits | ComponentMedieval×5.0, cat:Textiles×15.0, cat:SLDBar×5.0 | hand assembling workbench | Apparel_A2 |
| Solder | 焊料 | MakeSolder | TinBar×6.0, LeadBar×4.0 | induction furnace | Metals_C1 |
| Solder | 焊料 | MakeSolderGalvanite | Zinc×5.0, TinBar×4.0, CopperBar×1.0 | induction furnace | Metals_C1 |
| Sphalerite | 闪锌矿 | (无配方/为基础料) | — | — | — |
| SpringSteel | 弹簧钢 | MakeSpringSteel | PigIron×15.0, Manganese×2.0, Chromium×4.0, GlassBatch×5.0 | induction furnace | Metals_C3 |
| Steel | 钢锭 | ExtractMetalFromSlag | ChunkSlagSteel/ChunkMechanoidSlag×1.0 | metal extraction plant | Metals_C1 |
| Steel | 钢锭 | MakeSteelBars_Hand | Iron×15.0, cat:Coal×13.0 | ? | Craft_B1 |
| Steel | 钢锭 | MakeSteelBars_Electric | Iron×15.0, cat:Coal×7.0 | metal extraction plant | ? |
| Steel | 钢锭 | MakeMildSteel | PigIron×25.0, Zinc×5.0 | induction furnace | Metals_C1 |
| Steel | 钢锭 | RKHSKSmeltWeaponCrate | cat:WeaponCrate×1.0 | blacksmith | ? |
| Steel | 钢锭 | RKHSKSmeltWeaponParts | cat:WeaponParts×1.0 | metal extraction plant | ? |
| SteelBar | 钢锭 | MakeFerronickelAlloy | Steel×15.0, NickelBar×5.0 | ? | ? |
| SteelBar | 钢锭 | DismantleWarhead | FissionWarhead×1.0 | TableRimatomicsMachining | ? |
| SteelBar | 钢锭 | MakeAR500 | PigIron×15.0, GlassBatch×5.0, Manganese×2.0, NickelBar×3.0 | induction furnace | Metals_C3 |
| StelliteAlloy | 司太立合金锭 | MakeStelliteAlloy | Cobalt×5.0, Chromium×13.0, Tungsten×2.0 | ? | ? |
| StelliteAlloy | 司太立合金锭 | MakeStellite | Cobalt×12.0, Chromium×8.0, Tungsten×6.0, Molybdenum×4.0 | electric arc furnace | Metals_D1 |
| Sulphates | 硫酸盐 | ConvertToSulphates | Matter×50.0 | Nucleosynthetic Converter | Super_matter_E1 |
| SyntheticAmmonia | 合成氨 | MakeAmmonia | Coke×20.0 | Chemistry Station | Oil_Industry_C3 |
| SyntheticFibers | 合成纤维 | MakePolymerFibers | Polymers×20.0, Xylene×15.0 | Chemistry Station | Oil_Industry_C6 |
| SyntheticFibers | 合成纤维 | MakePolymersFromChitin | Chitin×20.0 | Chemistry Station | Repair_table_B1 |
| Synthread | 合成纤维布 | MakePolyester | SyntheticFibers×10.0 | Chemistry Station | Fabrication |
| Synthread | 合成纤维布 | OA_RK_Make_Hecheng | Cloth×75.0, Neutroamine×10.0, Chemfuel×12.0 | biorefinery | Ratkin_Apparel_B2A |
| Tennalum | 特纳铝合金 | MakeTennalum | AluminiumBar×20.0, Zinc×6.0, CopperBar×4.0 | electric arc furnace | Metals_D1 |
| Titanium | 钛锭 | ProcessTitanium | Ilmenite×15.0 | ? | ? |
| Titanium | 钛锭 | SmeltBlades | turbineBlade×2.0 | metal extraction plant | ? |
| Titanium | 钛锭 | SmeltTitanomagnetite_Industrial | Titanomagnetite×10.0, cat:Coal/Metallurgical×3.0 | metal extraction plant | Metals_C2 |
| Titanium | 钛锭 | SmeltTitanomagnetite_HighTech | Titanomagnetite×15.0, cat:Coal/Metallurgical×5.0 | electrolytic refinery | Metals_C3 |
| Titanium | 钛锭 | SmeltRutile_Industrial | Ilmenite×15.0 | metal extraction plant | Metals_C2 |
| Titanium | 钛锭 | SmeltRutile_HighTech | Ilmenite×15.0 | electrolytic refinery | Metals_C3 |
| Titanomagnetite | 钛磁铁矿 | (无配方/为基础料) | — | — | — |
| TurbineBlade_Vile | 涡轮叶片 | (无配方/为基础料) | — | — | — |
| Vanadium | 钒（V） | SmeltTitanomagnetite_HighTech | Titanomagnetite×15.0, cat:Coal/Metallurgical×5.0 | electrolytic refinery | Metals_C3 |
| Vanadium | 钒（V） | SmeltCarnotite_HighTech | Uranium×15.0 | electrolytic refinery | Metals_C3 |
| Velour | 天鹅绒 | MakeVelour | Compaste×5.0, cat:BTextiles×40.0 | tailor's loom | AdvancedFabrication |
| WoolAlpaca | 羊驼毛 | (无配方/为基础料) | — | — | — |
| WoolBison | 野牛毛 | (无配方/为基础料) | — | — | — |
| WoolDromedary | 骆驼毛 | (无配方/为基础料) | — | — | — |
| WoolMegasloth | 巨懒兽羊毛 | (无配方/为基础料) | — | — | — |
| WoolMuffalo | 猛犸牛羊毛 | (无配方/为基础料) | — | — | — |
| WoolSheep | 羊毛 | (无配方/为基础料) | — | — | — |
| Xylene | 二甲苯 | (无配方/为基础料) | — | — | — |
| Yautjavium | 扬塔维合金 | ProduceYautjavium | CopperBar×5.0, GlassBatch×20.0, Antimatter×3.0, CarbonAlloy×10.0 | quantum fabricator | ? |
| Zinc | 锌 (Zn) | SmeltMagnetite_HighTech | Iron×15.0, cat:Coal/Metallurgical×5.0 | electrolytic refinery | Metals_C3 |
| Zinc | 锌 (Zn) | SmeltSphalerite_Basic | Sphalerite×20.0 | metal extraction plant | Metals_C1 |
| Zinc | 锌 (Zn) | SmeltSphalerite_Industrial | Sphalerite×20.0, cat:Coal/Metallurgical×3.0 | metal extraction plant | Metals_C2 |
| Zinc | 锌 (Zn) | SmeltSphalerite_HighTech | Sphalerite×20.0, cat:Coal/Metallurgical×5.0 | electrolytic refinery | Metals_C3 |
| turbineBlade | 涡轮叶片 | Make_turbineBlade | cat:SuperBar×50.0 | electric smithy | Components_D1 |

## 三、交叉引用:HSK 配方消耗的 Vile 物料

列出所有把 Vile 物料当作原料的配方(即 HSK 产业线对 Vile 的依赖点)。

| Vile 物料 | 中文 | 被以下配方消耗 |
|---|---|---|
| AerMet | 艾尔梅特-100不锈钢 | Make_ComponentAdvanced |
| AlnicoAlloy | 铝镍钴合金锭 | MakePipe |
| AluminiumBar | 铝锭 | MakeElectronicComponents, MakeBronze_Hand, MakeBronze_Electric, MakeFerrosiliconAlloy, MakeAlnicoAlloy, MakeBronze_Foundry, MakeNimonic, MakeTennalum, MakeDualPhaseTitanium, MakeAnodizedAluminiumBlue, MakeAnodizedAluminiumRed |
| BlackPowder | 火药 | MakeAmmo_69Musket_Lead, MakeAmmo_69Musket_Buck, MakeAmmo_MinieBall, MakeAmmo_5070Govt, MakeAmmo_44Rimfire, MakeAmmo_200Pounder_Shell, MakeAmmo_200Pounder_GreekFire, MakeAmmo_3inCannon_Shell, MakeAmmo_3inCannon_Canister, MakeAmmo_Grapeshot, MakeAmmo_38mmCannonball, CastAmmo_3inCannon_Shell, CastAmmo_3inCannon_Canister, CastAmmo_200Pounder_Shell, CastAmmo_200Pounder_GreekFire, CastAmmo_38mmCannonball, CastAmmo_MinieBall, CastAmmo_69Musket_Lead, CastAmmo_69Musket_Buck |
| BogIron | 沼泽铁矿 | MakeIronBloom_Bloomery, MakePigIron_Furnace, MakePigIron_Furnace, SmeltMagnetite_Basic, SmeltBogIron_Industrial |
| Butadiene | 丁二烯 | MakeRubber, MakeABS, MakeNylon |
| CarbonAlloy | 碳纤维合金锭 | MakePobediteAlloy, MakeFerrotitaniumAlloy, ProduceYautjavium |
| CarbonSteel | 中碳钢 | MakePipe |
| Carborundum | 金刚砂 | MakeMicrochipIntegratedCircuits |
| Cellulose | 纤维素 | MakeNitrocellulose, MakeRayonViscose |
| Chlorine | 氯精 | MakePTFE, MakePVCPlastic, MakePolycarbonate, MakeNylon, MakePVCLeather |
| Cloth | 布料 | MakeAmmo_12Gauge_Beanbag, MakeMicropel, MakeCandles, MakeYinPiao10, RKHSKExtra_Make_RA_DetectiveDress, RKHSKExtra_Make_RA_DetectiveHat, Make_Tav_Dec2x1Flags, Make_Tav_Dec3x1Flags, Make_Tav_Dec4x1Flags, OA_RK_Make_Hecheng, RKHSKFix_Make_RK_EarCostume, RKHSKFix_Make_RK_WhiteCoat, RKHSKFix_Make_RK_Backpack, RKHSKFix_Make_RK_GaurdenUniform, RKHSKVictoria_Make_Crusaders, RKHSKVictoria_Make_Crusadershead, RKFC_RL_Make_Paint_Earring, RKFC_RL_Make_Paint_Umbrella, RKFC_RL_Make_Paint_Shout, RKFC_RL_Make_Paint_Traveler, RKFC_RL_Make_Paint_VanGogh, RKFC_RL_Make_Paint_MonaLisa, RKFC_RL_Make_Paint_Joan, RKFC_RL_Make_Paint_Genesis |
| Compaste | 粘合剂 | MakeIntegratedCircuits, MakeVelour, MakeSurfactant_Polymers, MakeFiberglass, MakeNanocomposite, RKHSKFix_Make_RK_PrototypePulseRifle, RKHSKFix_Make_RK_Weapon_SawedOff, RKHSKFix_Make_RK_AutoCrossBow, RKHSKFix_Make_RK_Weapon_ProtoChainSword, RKHSKFix_Make_RK_Apparel_Vacsuit, RKHSKFix_Make_RK_Apparel_VacsuitHelmet, RKHSKFix_Make_RK_Apparel_SpaceArmor, RKHSKFix_Make_RK_Apparel_SpaceArmorHelmet, RKHSKFix_Make_RK_BattleSuit, RKHSKFix_Make_RK_BattleSuitB, RKHSKFix_Make_RK_Mask, RKHSKFix_Make_RK_MaskB, RKHSKFix_Make_RK_Rifle, RKHSKFix_Make_RK_SniperRifle, RKHSKFix_Make_RK_BFR, RKHSKVictoria_Make_FBZX, RKHSKVictoria_Make_JD_shield, RKHSKVictoria_Make_MKSW, RKHSKVictoria_Make_MKSWWJ, RKHSKVictoria_Make_MKSWheat, RKHSKVictoria_Make_beileimao, RKHSKVictoria_Make_fangdanjunfu, RKHSKVictoria_Make_gfzd, RKHSKVictoria_Make_gjhpf, RKHSKVictoria_Make_hjzj, RKHSKVictoria_Make_hjzjmz, RKHSKVictoria_Make_hjzjmzB, RKHSKVictoria_Make_hjzjwj, RKHSKVictoria_Make_hsdy, RKHSKVictoria_Make_huimie, RKHSKVictoria_Make_kamz, RKHSKVictoria_Make_kydc, RKHSKVictoria_Make_kydcmz, RKHSKVictoria_Make_maorongdayi, RKHSKVictoria_Make_nwtg, RKHSKVictoria_Make_sxpf, RKHSKVictoria_Make_tybb, RKHSKVictoria_Make_xinpian, RKHSKVictoria_Make_xqzmj, RKHSKVictoria_Make_xqzzj, RKHSKFix_Make_RK_Apparel_VacsuitChildren |
| FerrosiliconAlloy | 桑德斯合金锭 | BuildSplitter |
| Fiberglass | 玻璃纤维 | MakeAerographene |
| Flaxcloth | 亚麻布 | MakeCandles |
| HMFibers | 高模量纤维 | MakeBioelectronicDevices, MakeNanocomposite, MakeDyneema |
| HempCloth | 麻布 | MakeCandles |
| IsopropylAlcohol | 异丙醇 | makemedicine, makeadvmedicine |
| Lye | 烧碱 | MakeEpoxy, SmeltBauxite_Industrial, SmeltBauxite_HighTech, MakeRayonViscose |
| Manganese | 锰（Mn） | MakeCarbonSteel, MakeAR500, MakeToolSteel, MakeSpringSteel, MakeNomex |
| Micropel | 米克洛佩尔 | makeadvmedicine |
| Modacrylic | 改性腈纶 | MakeZylon |
| Molybdenum | 钼 (Mo) | MakeAerMetSteel, MakePyromet, MakeStellite |
| NanotechFramework | 纳米技术结构 | MakeBioelectronicDevices, BuildBiosyntheticMaterial, Make_ComponentUltra |
| Neutroamine | 神经胺 | VG_BulkIbuprofen, makeadvmedicine, MakeAntidoteSafe, OA_RK_Make_Hecheng |
| NitinolAlloy | 镍钛诺合金锭 | BuildBiosyntheticMaterial |
| Paraffins | 石蜡 | Make_ArtificialBone, Make_ArtificialBoneBulk, MakeCPS, MakeCPH, MakeAdvMechanism_Hand, MakeAdvMechanism_Electric |
| PlantWax | 植物蜡 | MakeMycotanLeather, MakeRushlight, MakeCandles |
| Plasteel | 铁钛合金锭 | Warqueen, Cyclops, Make_BeamGraser, MakeAmmo_ChargedRocket, MakeAmmo_ChargedRocket_ICP, MakeAmmo_ChargedRocket_BB, MakeAmmoSet_LightbulletA, MakeAmmoSet_LightbulletB, OAGene_Make_SnowyCrystalTree_Seed |
| Plastic | 塑料 | MakeAmmo_30x29mmGrenade_Smoke, MakeAmmo_45ACP_HP, MakeAmmo_9x19mmPara_HP, MakeAmmo_303British_HP, MakeAmmo_556x45mmNATO_HP, MakeAmmo_762x39mmSoviet_HP, MakeAmmo_762x51mmNATO_HP, Make_ArtificialBone, Make_ArtificialBoneBulk, MakeAdvRifle_Component, MakeAdvSniper_Component, BuildCXfour, BuildUSC, BuildM4A1Gun, BuildChargeRifle, BuildSplitter, BuildMantis, BuildLaserRifle, BuildHeavyLaserRifle, BuildBurstLaserRifle, BuildBlackWidow, BuildPlasmaPistol, BuildPlasmaPrecision, BuildPlasmaCarbine, BuildPlasmaShotgun, BuildPlasmaRifle, MakeSmokeGrenade, BuildM56_USCM, BuildMinigun, BuildGalilBurstRifle, BuildHeavyCombatRifle, BuildM249, Build_M60GPMG, BuildIncendiaryLauncher, BuildMilkorGrenadeLauncher, BuildRPG, BuildM5_USCM, BuildLAW, BuildFlamethrowerRM, BuildSphinxPistol, BuildParaOrdnancePFourteenFortyfive, BuildTecnine, Build93RBurstPistol, BuildAssaultRifle, BuildSG553Gun, BuildAUG, BuildSCAR_H, BuildFAMAS, BuildXM8MilitaryRifle, BuildAK47Gun, BuildTacticalCombatrifle, BuildMarineSturmRifle, BuildStrikerShotgun, BuildTacticalShotgun, BuildAA12AutoShotgun, BuildTacticalAutoshotgun, BuildHeavySMG, BuildVector, BuildMP5Gun, BuildUMP45Gun, BuildSniperRifle, BuildM42A_USCM, BuildWA2000SpecialistRifle, BuildM82Gun, BuildTAMSR, BuildARFifty, BuildHecateII, BuildATR, Build_RPK74M_HMC, Build_RPD_HMC, Build_HMC_PKM, BuildAK74M_HMC, BuildGroza, BuildVintorezRifle, BuildOSV96_MHC, MakeAmmo_762x25mmTT_HP, MakeAmmo_545x39mmSoviet_HP, MakeAmmo_762x54mmR_HP, RKHSKFix_Make_RK_PrototypePulseRifle, RKHSKFix_Make_RK_AutoCrossBow, RKHSKFix_Make_RK_Rifle, RKHSKFix_Make_RK_SniperRifle |
| Polycarbonate | 聚碳酸酯 | MakeReinforcedGlass |
| Polymers | 高分子化合物 | MakeNapalm, MakePlastic, MakePlasticSulphate, MakePlasticSulphateBulk, MakeSurfactant_Polymers, MakeHDPE, MakePVCPlastic, MakeCarbonNanotubes, MakePolystyrene, MakePolymerFibers, MakePVCLeather, Make_GenuineLeather |
| Polystyrene | 聚苯乙烯 | MakeRubber, MakeABS |
| Propylene | 丙烯 | MakePolypropylene, MakeABS, MakePlexiglass, MakeIsopropylAlcohol, MakeModacrylic |
| PureSilver | 纯银 (Ag) | MakeSilverBars_Hand, MakeSterlingSilver_Industrial |
| Quartz | 石英 | RefineQuartz_Basic |
| Solder | 焊料 | MakeElectronicComponents, MakeIntegratedCircuits, MakeMicrochipIntegratedCircuits |
| Sphalerite | 闪锌矿 | ExtractSulfur, SmeltSphalerite_Basic, SmeltSphalerite_Industrial, SmeltSphalerite_HighTech |
| SpringSteel | 弹簧钢 | MakePipe |
| Steel | 钢锭 | Cyclops, Make_BeamGraser, MakeAmmo_ChargedRocket, MakeAmmo_ChargedRocket_ICP, MakeAmmo_ChargedRocket_BB, MakeAmmo_30x29mmGrenade_HEDP, MakeAmmo_Arrow_Steel, MakeAmmo_Arrow_Venom, MakeAmmo_Arrow_Flame, MakeAmmo_Nail, MakeAmmo_FlareBioferrite, MakeAmmo_80mmRocket_HE, MakeAmmo_80mmRocket_HEAT, MakeAmmo_80mmRocket_Thermobaric, MakeAmmo_BGM71_HEAT, MakeShell_120mmMortar_HE, MakeShell_120mmMortar_HE_HFuzed, MakeShell_120mmMortar_Incendiary, MakeShell_120mmMortar_EMP, MakeShell_120mmMortar_Firefoam, MakeShell_120mmMortar_Smoke, MakeFerrosiliconAlloy, MakeFerronickelAlloy, MakeAlnicoAlloy, MakeFerrotitaniumAlloy, MakeAmmoSet_LightbulletA, Make_Tav_KitchenAdd, RKHSKFix_Make_RK_EnhanceCrossBow, RKHSKFix_Make_RK_EnhanceCrossBow, RKHSKFix_Make_RK_Crossbow, RKHSKFix_Make_RK_Crossbow, RKHSKFix_Make_RK_HeavyLance, RKHSKFix_Make_RK_AutoCrossBow, RKHSKFix_Make_RK_AutoCrossBow, RKHSKFix_Make_RK_HeavyShield, RKHSKFix_Make_RK_TowerShield, RKHSKFix_Make_RK_Ammo_WyvernFire, RKHSKFix_Make_RK_Rifle, RKHSKFix_Make_RK_Rifle, RKHSKFix_Make_RK_SniperRifle, RKHSKFix_Make_RK_SniperRifle, RKHSKVictoria_Make_Crusaders, RKHSKVictoria_Make_Crusadershead, RKHSKVictoria_Make_FBZX, RKHSKVictoria_Make_huimie, RKHSKVictoria_Make_tybb, RKHSKVictoria_Make_yiliaob, RKHSKRAE_Make_RK_SafetyTether, RKHSKGW_Make_RK_WR_BrodieHelmet, RKHSKGW_Make_RK_IR_AdrianHelmet, RKHSKGW_Make_RK_IR_CuirassierArmor, RKHSKGW_Make_RK_KB_Stahlhelm, RKHSKGW_Make_RK_KB_Sappenpanzer, RKHSKGW_Make_RK_EU_BerndorfHelmet, RKHSKGW_Make_RK_VK_AdrianHelmet, RKHSKGW_Make_RK_MR_BrodieHelmet, RKHSKGW_Make_RK_LR_BrodieHelmet, RKHSKGW_Make_RK_KP_AdrianHelmet, RKHSKGW_Make_RK_KO_KnightHelmet, RKHSKGW_Make_RK_KO_KnightArmor, RKHSKGW_Make_RK_KB_Fichte, RKHSKGW_Make_RK_EU_Edelweib, RKHSKGW_Make_RK_YE_YukiSakura, RKHSKGW_Make_RK_WR_Camellia, RKHSKGW_Make_RK_KB_SommerEiche, RKHSKGW_Make_RK_WR_Senecio, RKHSKGW_Make_RK_MR_Eschscholzia, RKHSKGW_Make_RK_WR_Rush, RKHSKGW_Make_RK_EU_Zundkraut, RKHSKGW_Make_RK_KO_GoldenGarden, RKHSKGW_Make_RK_KB_Rotkiefer, RKHSKFix_Make_RK_Apparel_VacsuitChildren |
| SteelBar | 钢锭 | MakePipe |
| Sulphates | 硫酸盐 | MakeKevlar, MakePlasticSulphate, MakePlasticSulphateBulk, MakeOxidizedChemfuel, MakePolycarbonate, MakeAerographene, MakePolystyrene, MakeEpoxy, MakeNomex |
| SyntheticAmmonia | 合成氨 | MakeAmmo_6x24mmCharged_Ion, MakeKevlar, SeperatePrometheum, MakeABS, MakeNylon |
| SyntheticFibers | 合成纤维 | Paramedic, BuildBiosyntheticMaterial, MakeAdvRifle_Component, MakeAdvSniper_Component, MakePolyester, MakeModacrylic, RKHSKFix_Make_RK_PrototypePulseRifle, RKHSKFix_Make_RK_Weapon_SawedOff, RKHSKFix_Make_RK_AutoCrossBow, RKHSKFix_Make_RK_Weapon_ProtoChainSword, RKHSKFix_Make_RK_Apparel_Vacsuit, RKHSKFix_Make_RK_Apparel_VacsuitHelmet, RKHSKFix_Make_RK_Apparel_SpaceArmor, RKHSKFix_Make_RK_Apparel_SpaceArmorHelmet, RKHSKFix_Make_RK_Rifle, RKHSKFix_Make_RK_SniperRifle, RKHSKFix_Make_RK_BFR, RKHSKVictoria_Make_FBZX, RKHSKVictoria_Make_MKSW, RKHSKVictoria_Make_MKSWWJ, RKHSKVictoria_Make_MKSWheat, RKHSKVictoria_Make_beileimao, RKHSKVictoria_Make_hjzj, RKHSKVictoria_Make_hjzjmz, RKHSKVictoria_Make_hjzjmzB, RKHSKVictoria_Make_hjzjwj, RKHSKVictoria_Make_huimie, RKHSKVictoria_Make_kydc, RKHSKVictoria_Make_kydcmz, RKHSKVictoria_Make_nwtg, RKHSKVictoria_Make_tybb, RKHSKVictoria_Make_xinpian, RKHSKVictoria_Make_xqzmj, RKHSKVictoria_Make_xqzzj, RKHSKFix_Make_RK_Apparel_VacsuitChildren |
| Synthread | 合成纤维布 | MakeDyneema |
| Titanium | 钛锭 | MakeFerrotitaniumAlloy, MakeNitinolAlloy, MakeNimonic, MakeDualPhaseTitanium, MakeNitinol |
| Titanomagnetite | 钛磁铁矿 | MakePigIron_TitanoFurnace, SmeltTitanomagnetite_Industrial, SmeltTitanomagnetite_HighTech |
| Vanadium | 钒（V） | ProduceBPoly, MakeMaxametSteel, MakeDualPhaseTitanium |
| Xylene | 二甲苯 | MakeKevlar, MakePolymerFibers, MakeMycotanLeather, MakeNomex, MakeZylon |
| Zinc | 锌 (Zn) | BuildMagneticMaterial, MakeTennalum, MakeMildSteel, MakeBrass, MakeSolderGalvanite |
| turbineBlade | 涡轮叶片 | SmeltBlades |

## 四、生产建筑索引(产出物料者)

列出全部生产建筑及其产出物料数;标记为 Vile 的为 Vile 专属建筑。

| 建筑 defName | 中文 | 产出配方数 | 示例产物 |
|---|---|---|---|
| UniversalFermenter | Alcohol Fermenting Barrel | 9 | Alcohol_Rum, Alcohol_BerryWine, Alcohol_Sake | 
| AquacultureBasin | Aquaculture basin | 0 |  | 
| CementationFurnace | Cementation Furnace | 4 | BlocksSandstone, ClayBrick, BlisterSteel | 
| Kiln | Ceramic Kiln | 17 | WootzSteel, Ash, ClayPotSalt | 
| CharcoalPit | Charcoal Pit | 6 | Ash, Dirt, Charcoal, Dirt, ClayBrick, Dirt | 
| ChemistryLab | Chemistry Station | 22 | Paraffins, Turpentine, SyntheticFibers | Vile
| TanningRack | Drying Rack | 20 | Hide_DurableHideCured, Hide_RodentSkinCured, Rawhide | 
| MillElectric | Electric Mill | 5 | Cornmeal, GroundCoffee, Sugar | 
| GasCentrifuge | Gas centrifuge | 2 | UraniumPellets, Prometheum | 
| RockCrusher | Industrial Rock Crusher | 4 | SandResource, CrushedStone, SandResource | 
| JerkyRack | Jerky Drying Rack | 2 | MealJerky, MealJerky | 
| TableKitchen | Kitchen Table | 38 | Compost, Pemmican, Pasta | 
| MatterConverter | Nucleosynthetic Converter | 13 | Matter, Nickel, Aluminium | 
| OA_RK_Shuipei_A | Oberonia Aurea Plant Culture Cabinet (Oberonia Aurea) | 0 |  | 
| OA_RK_Shuipei_B | Oberonia Aurea Plant Culture Cabinet (Rice) | 0 |  | 
| TableChemlab | Petrochemical Laboratory | 16 | NanotechFramework, Plexiglass, Fiberglass | 
| PlantProcessingTable | Plant Processing Bench | 51 | SeedFlax, SeedCoffee, Seed_Hops | 
| Saltpeter_pit | Saltpeter Pit | 3 | Nitre, Nitre, Nitre | 
| SeasoningRack | SeasoningRack | 15 | FirewoodSeasoned_Poplar, FirewoodSeasoned_Cypress, FirewoodSeasoned_Birch | 
| SilagePile | Silage Pile | 1 | Silage | 
| TanningVat | Tanning Vat | 19 | Leather_Lizard, Leather_Scraps, Leather_Light, Leather_Rhinoceros, Leather_Scraps | 
| NewHydroponicsBasin | advanced hydroponic basin | 0 |  | 
| RareExtractor | advanced mine extractor | 0 |  | 
| AdvWeaponCraftingWorkTable | advanced weapon crafting workbench | 82 | SMG_Component, Gun_M37A2_USCM, Gun_FireExtinguisher | 
| AmmoBench | ammo crafting table | 6 | Napalm, huimie, Sulfur | 
| ChJAndroidPrinter | android printer | 0 |  | 
| TableBionics | bionics workbench | 1 |  | 
| BiofuelRefinery | biorefinery | 6 | Synthread, Chemfuel, Fertilizer | 
| FueledSmithy | blacksmith | 60 | Ammo_Arrow_Stone, Ammo_StoneBall, Sulfur | 
| TableFurnace | blast furnace | 10 | LeadBar, PureSilver, Glass, PigIron | 
| Bloomery | bloomery | 1 | IronBloom | 
| Brewery | brewery | 28 | Alcohol_Rum, Alcohol_Moonshine, Surfactant | 
| BriningStation | brine-curing station | 16 | Hide_HeavyHideCured, Hide_StockHideCured, Hide_DurableHideCured | 
| Tav_KitchenButch | butcher counter | 0 |  | 
| ButcherSpot | butcher spot | 14 | FishingBait, Compost, MushroomTincture | 
| TableButcher | butcher table | 22 | Compost, MushroomTincture, Pemmican | 
| CandyTable | candy table | 5 | Sundae, Taffy, IceCream | 
| CheeseBasin | cheesemaking basin | 4 | AgedCheese, AgedCheese, Cheese | 
| CheeseVat | cheesemaking vat | 2 | Cheese, AgedCheese | 
| ChoppingBlock | chopping block | 6 | WoodLog, TreeBark, Kindling, WoodLog, Pitch | 
| TableCoffee | coffee machine | 4 | Chocomilk, Cuptea, PsychiteTea | 
| CompostBarrel | compost barrel | 2 | Fertilizer, Fertilizer | 
| CraftingSpot | craftsman table | 33 | Ammo_Arrow_Stone, Ammo_StoneBall, Vile200Pounder_Crate | 
| ElectricCrematorium | crematorium | 6 | Ash, Ash | 
| UpgradingStation | cybernetics workbench | 1 |  | 
| DeepDrill | deep drill | 0 |  | 
| Fryer | deep fryer | 6 | FishandChips, Samosa, Donut | 
| ChJDroidPrinter | droid fabricator | 0 |  | 
| DrugLab | drug lab | 15 | Surfactant, Oberonia_Aurea_Chanwu_F, Surfactant | 
| DryingRack | drying rack | 8 | DriedLeavesSmokeleaf, CoffeeBeans, DriedLeavesSmokeleaf | 
| EAF | electric arc furnace | 16 | Tennalum, AerMet, StelliteAlloy | 
| AdvToolBench | electric assembling bench | 11 | ComponentMedieval, SewingKit, Pipe | 
| ElectricBrewery | electric brewery | 28 | Alcohol_Rum, Alcohol_Moonshine, Surfactant | 
| EConcreteMixer | electric concrete mixer | 3 | ConcreteResource, Dirt, ReinforcedConcrete | 
| TableMachining | electric cutting table | 29 | BlocksVacstone, ZF_BlocksBasalt | 
| ElectricDryingRack | electric drying rack | 15 | driedfruit, CoffeeBeans, DriedLeavesSmokeleaf | 
| ElectricOven | electric oven | 18 | Cornbreadmuffin, PotPie, MatchaCupcake | 
| Canningstove | electric pressure cooker | 6 | MealSurvivalPack, MealSurvivalPack, BasicCannedFood | 
| TableSawmillElectric | electric sawmill | 18 | OakPlank, DragonwoodPlank, PoplarPlank | 
| ElectricSmithy | electric smithy | 22 | Ammo_Arrow_Stone, RK_Lance, Steel, ComponentIndustrial, Plasteel, ElectronicComponents | 
| RK_ElectricSmithy | electric smithy | 33 | RK_LongSword, RK_CaesarCrownB, RK_Spear | 
| ElectricStove | electric stove | 49 | FriedVegetables, Soup, CoconutCurry | 
| RK_ElectricTailoringBench | electric tailor bench | 140 | RK_WhiteCloak, RK_ChefHat, RK_ExplorerWear | 
| ElectricTailoringBench | electric tailoring bench | 5 | Bandagekit, TavPadding, Bandagekit | 
| RK_ElectricMsWorkbench | electric workbench | 0 |  | 
| ElectrolyticRefinery | electrolytic refinery | 11 | Zinc, Manganese, Cobalt, LeadBar, PureSilver, CopperBar, Sulfur, AluminiumBar, Chromium | Vile
| TableElectronics | electronics table | 6 | RobotParts, MineralSonarModule, Microchips | 
| MechWeaponCraftingWorkTable | energy weapon workbench | 29 | Techprint_Bow_Archotech, Plasma_Component, Weapon_GrenadeChargedFrag | 
| Finery | finery forge | 5 | PipeSection, WroughtIron, ForgedSteel | 
| FirePit | fire pit | 15 | LeadBar, PureSilver, Ammo_69Musket_Lead, Ammo_69Musket_Buck | 
| HSK_FishTrap | fish trap | 0 |  | 
| HSK_FishingSpotSpawner | fishing spot | 0 |  | 
| Foundry | foundry | 32 | TFJ_Tool_Hoe, Ammo_69Musket_Lead, TFJ_Tool_Mining_Pickaxe | 
| RK_FueledSmithy | fueled smithy | 31 | RK_LongSword, RK_CaesarCrownB, RK_Spear | 
| FueledStove | fueled stove | 41 | FriedVegetables, Soup, MeatSoup | 
| FungiponicsBasin | fungiponics basin | 0 |  | 
| GlassworkTable | glassworks table | 4 | GlassBatch, ReinforcedGlass, Glass | 
| HadronColliderFeeder | hadron collider hopper | 0 |  | 
| FabricationBench | hand assembling workbench | 11 | Wire, ComponentMedieval, Mechanism | 
| ConcreteMixer | hand concrete mixer | 3 | Dirt, ReinforcedConcrete, ConcreteResource | 
| TableStonecutter | hand cutting table | 16 | SandResource, BlocksVacstone | 
| HandMendingWorkbench | hand mending workbench | 2 |  | 
| TableSawmillHand | hand sawmill | 20 | MaplePlank, PinePlank, BirchPlank | 
| RK_HandTailoringBench | hand tailor bench | 138 | RK_WhiteCloak, RK_ChefHat, RK_ExplorerWear | 
| RK_HandMsWorkbench | hand workbench | 0 |  | 
| TableHeavyAmmunition | heavy ammunition bench | 11 | CE_Weapon_GrenadeStickBomb, CE_Weapon_GrenadeFlashbang, Weapon_GrenadeFrag | 
| HeavyArmsBench | heavy arms lathe | 29 | Launcher_Component, HeavyTurret_Crate, Oerlikonautocannon_Crate | 
| Hopper | hopper | 0 |  | 
| HydroponicsBasin | hydroponic basin | 0 |  | 
| Agrarian | hydroponic box | 0 |  | 
| ClutterAlloyHydroponicsBasinVS | hydroponic pot | 0 |  | 
| HyperTailoringBench | hyper tailoring bench | 18 | hjzjmz, MKSWheat, hjzj | 
| InductionFurnace | induction furnace | 17 | FerrosiliconAlloy, Steel, CarbonSteel | Vile
| Tav_KitchenCloset | kitchen closet | 0 |  | 
| Tav_KitchenShelfs | kitchen shelfs | 0 |  | 
| DyeingStation | leather-dyeing station | 8 | Leather_UpholsteryBordeaux, Leather_BarghestFur, Leather_Fox | 
| LeatherworkerTable | leatherworker table | 9 | Flaxcloth, Leather_Patch, Bandagekitcrude | 
| QRY_MediQuarry | med quarry | 0 |  | 
| meditable | medical table | 12 | Bandagekit, AntidoteSafe, Bandagekit | 
| TableMending | mending workbench | 6 |  | 
| ElectricSmelter | metal extraction plant | 45 | Zinc, Manganese, GlassBatch, PigIron | Vile
| HadronCollider | micro hadron collider | 0 |  | 
| GrindStone | milling stone | 5 | Cornmeal, GroundCoffee, Sugar | 
| Extractor | mine extractor | 0 |  | 
| QRY_MiniQuarry | mini quarry | 0 |  | 
| NutrientPasteDispenser | nutrient paste dispenser | 0 |  | 
| OpenHearth | open hearth | 16 | Soup, FriedVegetables, Turpentine | 
| TableOrganvat | organ vat | 9 | Lung, Eye, Liver | 
| TableOven | oven | 11 | Cornbreadmuffin, PotPie, MatchaCupcake | 
| PicklingStation | pickling station | 0 |  | 
| PotteryStation | pottery bench | 6 | ClayPotWetPainted, ClayPotWetTin, ClayPotWetSalt | 
| TablePrimitiveProsthetic | primitive prosthetics bench | 3 | Herbmedicine, Herbmedicine | 
| ElectricStove_Pro | professional cook stove | 42 | MealFine, HealthyBroth, Taco | 
| TableBasicProsthetic | prosthetics workbench | 11 | CochlearImplant, SimpleProstheticHand | 
| Matterfab | quantum fabricator | 3 | Yautjavium, BetaPoly, AlphaPoly | 
| QRY_Quarry | quarry | 0 |  | 
| TableRecycling | recycling station | 5 | ComponentAdvanced, Electronics | 
| RobAssem | robotic assembler | 5 | ComponentUltra, BioMicrochips, AlphaPoly, CarbonAlloy, MagneticMaterial | 
| VG_SaltMine | salt mine | 1 | Salt | 
| PrimitiveHydroponic | saturated soil | 0 |  | 
| TableSculpting | sculptor's table | 0 |  | 
| TableGrill | simple grill | 8 | Kippers, FriedVegetables, MealFine | 
| soylenttable | soylent machine | 1 | soylentgreen | 
| LogSplittingSpot | splitting spot | 6 | WoodLog, TreeBark, Kindling, WoodLog, Pitch | 
| StampMill | stamp mill | 4 | SandResource, CrushedStone, SandResource | 
| TableStonecutterNeolithic | stonecutter's bench | 10 | BlocksVacstone, BlocksMarble | 
| SulfurExtractor | sulfur extractor | 2 | Nitre, Sulfur, Sulfur | 
| SunLamp | sun lamp | 0 |  | 
| TableSynthetics | synthetic organ assembler | 3 | ArtificialBone, ArtificialBone | 
| HandTailoringBench | tailor's bench | 5 | Bandagekit, TavPadding, Bandagekit | 
| TableLoom | tailor's loom | 14 | Leather_Thrumbo, Leather_KirinHide, Flaxcloth | 
| TanningDrum | tanning drum | 21 | Leather_Light, Leather_CorrectedGrain, Leather_Chinchilla, Leather_Boomanimal, Leather_Scraps | 
| Tav_Oven | tavern oven | 11 | TavMeatRice, TavCornDough, TavCornMush | 
| WeaponCraftingWorkTable | weapon crafting workbench | 42 | HMC_Gun_SVD, SMG_Component, Gun_M37A2_USCM | 
| SK_Windmill | windmill | 3 | Sugar, Flour, PlantWax | 
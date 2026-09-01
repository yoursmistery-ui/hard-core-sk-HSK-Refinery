# RimWorld Mod 整合任务调查记录

> 整理时间：2026-08-21
> 用途：新开对话时可直接基于本文件继续工作

---

## 一、任务概述

用户正在将多个工坊 mod 整合为本地 mod，适配 HSK（Hardcore SK）体系，涉及 3 个 mod：

| # | 工坊 ID | 名称 | 处理方式 | 状态 |
|---|---------|------|----------|------|
| 1 | 3767304373 | Simple Doors（简单门） | 本地化 + HSK 材料适配 | ✅ 已完成 |
| 2 | 3775694305 | Tavern（酒馆家具） | 本地化 + 材料适配 + **属性等价**（对齐 HSK 同水平同材料家具/建筑），简单做 | ⏳ 调查完成，待实施 |
| 3 | 3738350131 | Show Me Your Tools（JobEffects 动画工具） | 本地化 + 整合 HSK/鼠族工具（先做方案） | ⏳ 调查完成，待做方案 |
| 4 | 3780770870 | Borders of the Rim（边境拓展HSK） | **整 mod 直搬本地化,保留原 packageId/作者**,叠加 HSK 三步标记(HSK 空文件 + NativeAddon ModAssistant 补丁 + Core SK 依赖/loadAfter) | ✅ 已完成并启用 |

**关键决策点（待用户确认）：**
- 用户最初说"和上面 mod 整理成一个 mod"（与 Simple Doors 合并），但后来语境变化，**是否合并成一个 mod 需确认**
- JobEffects 的动画基于 jobDefs 触发（不依赖工具物品），整合方式需确认

---

## 二、已完成：Simple Doors（3767304373）

**本地 mod 位置：** `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\简单门SimpleDoorsHSK`

**已完成内容：**
- 完整复制原版文件（Defs、Textures、LoadFolders、DLC 补丁）
- **材料适配 HSK 体系：**
  - 木门/木柱 → HSK 木板（Woody 类别）
  - 石门/石柱 → 加入 Bricks（砖）类别，与 HSK 石墙一致
  - 石门内木质门扇由原木（WoodLog）改为木板（WoodPlank）
  - 工业门/工业柱 → 维持 HSK 金属（Metallic）+ 工业组件惯例
  - 研究沿用 HSK：木门=家具I、石门=石材加工I、工业门=机械加工
- **About.xml：** 保留原 packageId `ODs.SimpleDoors`，loadAfter HSK 系列，含 HSK 适配说明
- **ModsConfig.xml：** 已激活 `<li>ODs.SimpleDoors</li>`（第 176 行）
- **中文翻译：** 简/繁中 DefInjected 翻译，UTF-8 BOM 编码
- **蓝色标记：** 已在 `ModListerSettingsDefs.xml` 的 NativeAddon 节点添加 `ODs.SimpleDoors`
- **工坊原版已删除：** 避免 packageId 重复冲突

**关键文件：**
- `简单门SimpleDoorsHSK\1.6\Defs\ThingDefs_Buildings\Buildings_Structure.xml`（门）
- `简单门SimpleDoorsHSK\1.6\Defs\ThingDefs_Buildings\Buildings_Misc.xml`（柱）
- `简单门SimpleDoorsHSK\About\About.xml`
- `HSK Autosort and Mod Assistant\Defs\ModListerSettingsDef\ModListerSettingsDefs.xml`（NativeAddon 标记）

---

## 三、Tavern 家具 mod（3775694305）调查结果

**工坊路径：** `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\3775694305`
**依赖：** Vanilla Expanded Framework（`OskarPotocki.VanillaFactionsExpanded.Core`）

### 3.1 家具/建筑清单与属性

| defName | 类型 | HP | WorkToBuild | Comfort | Beauty | 材料 | costStuffCount | 额外材料 |
|---------|------|-----|-------------|---------|--------|------|----------------|----------|
| Tav_1x1Table | 1x1桌 | 50 | 400 | - | 1 | Woody | 15 | - |
| Tav_1x2Table | 1x2桌 | 100 | 750 | - | 2 | Woody | 30 | - |
| Tav_2x2Table | 2x2桌 | 200 | 1500 | - | 2 | Woody | 50 | - |
| Tav_2x2RoundTable | 2x2圆桌 | 200 | 1500 | - | 2 | Woody | 50 | - |
| Tav_1x2Counter | 柜台1x2 | 150 | 750 | - | 2 | Woody | 50 | - |
| Tav_1x3Counter | 柜台1x3 | 200 | 1000 | - | 2 | Woody | 75 | - |
| Tav_RackA | 架子 | 160 | 1200 | - | 6 | Woody | 75 | - |
| Tav_ChairA | 椅子 | 150 | 6000 | 0.75 | 1 | Woody | 65 | - |
| Tav_SoftChair | 软椅 | 160 | 8000 | 0.9 | 4 | Woody | 60 | TavPadding 10 |
| Tav_StoolA | 凳子 | 75 | 450 | 0.5 | 0 | Woody | 25 | - |
| Tav_TavernBench | 酒馆长凳 | 120 | 1500 | 0.6 | 2 | Woody | 50 | - |
| Tav_SoftStool | 软凳 | 90 | 1500 | 0.85 | 4 | Woody | 25 | TavPadding 10 |
| Tav_SoftArmchair | 软扶手椅 | 160 | 10000 | 1.0 | 8 | TavPadding | 50 | WoodLog 75 |
| Tav_SoftBed | 软床 | 140 | 2500 | - | 6 | Woody | 45 | TavPadding 20 |
| Tav_SoftDoubleBed | 软双人床 | 200 | 4000 | - | 8 | Woody | 85 | TavPadding 40 |
| Tav_EndTable | 床头柜 | 75 | 1000 | - | 3 | Woody | 30 | - |
| Tav_Dresser | 梳妆台 | 120 | 2000 | - | 5 | Woody | 50 | - |
| Tav_Wardrobe | 衣柜 | 160 | 2500 | - | 6 | Woody | 75 | - |
| Tav_1x1Wardrobe | 1x1衣柜 | 120 | 1600 | - | 4 | Woody | 50 | - |
| Tav_Candleholder | 烛台 | 120 | 300 | - | 2 | Metallic | 20 | TavTallow 10 |
| Tav_TableLight | 台灯 | - | 250 | - | 1 | - | - | TavTallow 10 |
| Tav_DecTrophy | 装饰奖杯 | 50 | 600 | - | 2 | Metallic/Stony/Woody | 40 | TavPadding 20 |
| Tav_Dec2x1Flags | 旗帜2x1 | 20 | 1000 | - | 2 | Cloth | 20 | - |
| Tav_Dec3x1Flags | 旗帜3x1 | 20 | 1000 | - | 3 | Cloth | 30 | - |
| Tav_Dec4x1Flags | 旗帜4x1 | 20 | 1000 | - | 4 | Cloth | 40 | - |

### 3.2 生产台

| defName | 类型 | HP | WorkToBuild | 材料 | 继承配方 | 研究 |
|---------|------|-----|-------------|------|----------|------|
| ~~Tav_TableDecor~~ | ~~装饰台~~ | 180 | 3000 | Woody, cost 120 | 装饰配方 | ComplexFurniture |

> **2026-08-28 变更**:装饰台已删除——垫料 `TavMake_Padding` 迁 HSK 共享纺织机 `TableLoom`(`TailoringSpeed` + `Apparel_B1`),地毯/旗帜/奖杯/厨房装饰 11 条工作台配方全部移除,这些物件本就带 `costList`,改在建筑菜单直接建造。
| Tav_Oven | 酒馆烤箱 | 180 | 3000 | Stony 40 + WoodLog 80 | FueledStove 配方 | ComplexFurniture |
| Tav_KitchenButch | 屠宰台 | 180 | 3000 | Stony 20 + WoodLog 50 | TableButcher 配方 | ComplexFurniture |

### 3.3 资源与食物

**资源：**
- `TavPadding`（垫料）：Fabric 类别，MarketValue 1.8，用于软家具
- `TavTallow`（油脂）：ItemsMisc，MarketValue 0.1，用于蜡烛燃料

**食物（MealSimple 级别）：**
- TavCornDough（玉米面团）：Nutrition 0.5，原料
- TavMeatRice（肉饭）：Nutrition 0.9
- TavCornMush（玉米糊）：Nutrition 0.9
- TavCornBread（玉米面包）：Nutrition 0.9
- TavRiceBuns（米包子）：Nutrition 0.9

### 3.4 工作定义
- ~~DoBills_Tav_TableDecor（装饰台，Crafting）~~ 2026-08-28 随装饰台删除
- DoBills_Tav_Oven（酒馆烤箱，Cooking）
- DoBills_KitchenButch（屠宰台，Cooking）

### 3.5 其他文件
- `Defs\DesignationCategoryDefs\`（设计类别）
- `Defs\ThoughtDef\Thoug_HomeFood.xml`（食物心情）
- `Defs\RecipeDefs\Recipes_Decor.xml`（装饰配方）
- `Defs\RecipeDefs\Recipes_HomeFood.xml`（家常食物配方）
- `Patches\Stoves.xml`（炉子补丁）

---

## 四、HSK 家具属性参考（用于属性等价）

**HSK 家具属性水平（Core_SK）：**

### 4.1 椅子/凳子
| defName | HP | WorkToBuild | Comfort | 材料 | costStuffCount |
|---------|-----|-------------|---------|------|----------------|
| PrimitiveStoolWood（木凳） | 50 | 200 | 0.2 | Woody | 20 |
| PrimitiveStoolStone（石凳） | 50 | 300 | 0.15 | Stony | 25 |
| Stool（无靠背凳） | 50 | 350 | 0.3 | Metallic/Woody | 30 |
| StoolBackrest（靠背凳） | 50 | 500 | 0.4 | Woody | 50 |
| SlabChair（石扶手椅） | 70 | 850 | 0.35 | Stony | 45 |
| FoldingStool（折叠凳） | 70 | 650 | 0.6 | Metallic/Woody | 40 |
| Bench（长凳） | 80 | 550 | 0.5 | Metallic/Woody | 55 |
| DiningChair（餐椅） | 90 | 900 | 0.5 | Fabric/Leathery | 65 |
| Armchair（扶手椅） | 80 | 1100 | 0.6 | Fabric/Leathery | 60 |
| RusticCouch（乡村沙发） | 110 | 1200 | 0.7 | Fabric/Leathery | 55 |
| MilleniumChair（千禧椅） | 90 | 1500 | 0.85 | Metallic | 50 |
| EggArmchair（蛋椅） | 90 | 2100 | 0.9 | Metallic | 70 |
| MilleniumCouch（千禧沙发） | 150 | 3100 | 0.9 | Fabric | 70 |
| PodChair（豆荚椅） | 220 | 3500 | 0.85 | Metallic | 50 |

### 4.2 桌子
| defName | HP | WorkToBuild | Beauty | 材料 | costStuffCount |
|---------|-----|-------------|--------|------|----------------|
| Table1x1c | 65 | 500 | 1 | Metallic/Woody | 15 |
| Table1x2c | 75 | 700 | 2 | Metallic/Woody | 30 |
| Table1x3c | 85 | 800 | 3 | Metallic/Woody | 40 |
| Table2x2c | 100 | 900 | 4 | Metallic/Woody | 50 |
| Table2x4c | 150 | 1400 | 6 | Metallic/Woody | 80 |
| Table3x3c | 150 | 1600 | 7 | Metallic/Woody | 85 |
| TableStone（石桌） | 125 | 1100 | 4 | Stony | 90 |
| PrimitiveTableWood（木桌） | 95 | 1500 | 3 | Woody | 60 |

### 4.3 储物/家具
| defName | HP | WorkToBuild | Beauty | 材料 | costStuffCount |
|---------|-----|-------------|--------|------|----------------|
| ClutterLocker（储物柜） | 100 | 750 | 10 | Metallic/Woody | 55 |
| Dresser（梳妆台） | 120 | 5000 | 10 | Metallic/Woody | 70 |
| Bookcase（书柜） | 70 | 3000 | 15 | Metallic/Woody | 85 |
| EndTableSimple（床头柜） | 100 | 1100 | 8 | Metallic/Woody | 25 |

### 4.4 床
| defName | HP | WorkToBuild | Comfort | Beauty | BedRest | 材料 | costStuffCount |
|---------|-----|-------------|---------|--------|---------|------|----------------|
| Bed（简易床） | ~80 | 800 | 0.75 | - | 0.95 | Fabric/Leathery | 65 |
| DoubleBed（双人床） | - | 1540 | 0.85 | 12 | 0.95 | Fabric/Leathery | 110 |
| Bed_Industrial_Single | - | 1400 | 0.9 | 18 | 1.0 | Fabric | 65 |
| Bed_Industrial_Double | - | 1700 | 1.0 | 32 | 1.0 | Fabric | 130 |

### 4.5 灯
| defName | HP | WorkToBuild | Beauty | glowRadius | 材料 | costStuffCount |
|---------|-----|-------------|--------|------------|------|----------------|
| Candle（蜡烛） | 20 | 130 | 3 | 6 | Woody/Metallic | 15 |
| GlowstoneLamp（辉光石灯） | - | 350 | 9 | 8 | Woody/Metallic | 30 |
| GlowstoneTableLamp（辉光石台灯） | - | 300 | 7 | 4 | Metallic | 10 |
| ClutterFloorLampA（落地灯） | 100 | 320 | 15 | 12 | - | - |
| CeilingLamp（吊灯） | 35 | 320 | 10 | 9 | - | - |
| Lighting_WallLight（壁灯） | 75 | 360 | 12 | 8 | - | - |

### 4.6 对比结论（Tavern vs HSK）
- **Tavern 椅子普遍属性偏高**（如 Tav_ChairA HP150/Work6000 vs HSK DiningChair HP90/Work900），需下调对齐
- **Tavern 桌子属性偏高**（如 Tav_2x2Table HP200/Work1500 vs HSK Table2x2c HP100/Work900），需下调对齐
- **Tavern 床头柜/梳妆台属性略低**（如 Tav_EndTable HP75 vs HSK EndTableSimple HP100），需上调对齐
- **材料需替换**：WoodLog → WoodPlank（HSK 木板）、TavPadding → HSK 对应材料、TavTallow → HSK 对应燃料

---

## 五、JobEffects 动画工具 mod（3738350131）调查结果

**工坊路径：** `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\3738350131`
**功能：** 殖民者工作时在手上绘制动画工具（挥动动画、命中音效、材料感知碎片效果：木屑/石屑/焊接火花/土块/锯末）
**核心机制：** 动画基于 **jobDefs + workbenchDefs 触发**（不依赖工具物品 ThingDef）。Harmony patch `PawnRenderUtility.DrawEquipmentAndApparelExtras` 隐藏真实装备的工具，绘制动画工具。
**有完整 C# 源码**（Source/ 目录，ToolAnimator.cs 251KB 是核心引擎）

### 5.1 完整工具动画列表（约 52 个 JobToolDef）

| defName | 触发工作/工作台 | 工具 | 动画风格 |
|---------|-----------------|------|----------|
| JE_Tool_GraveShovel | FinishFrame, Frame_Grave | 铲 | Dig |
| JE_Tool_Pickaxe | Mine | 镐 | - |
| JE_Tool_Axe | CutPlant, CutPlantDesignated, HarvestDesignated, Harvest, ExtractTree | 斧 | - |
| JE_Tool_Hammer | FinishFrame, BuildRoof, RemoveRoof | 锤 | - |
| JE_Tool_BuildChisel | FinishFrame（Stony） | 凿 | Stab |
| JE_Tool_Welder | FinishFrame, Repair, FixBrokenDownBuilding, DisassembleMech（Metallic/None） | 焊枪 | Stir |
| JE_Tool_BenchWelder | DoBill（FueledSmithy, ElectricSmithy, BioferriteShaper） | 焊枪 | Stir |
| JE_Tool_Sickle | CutPlant, CutPlantDesignated, HarvestDesignated, Harvest | 镰刀 | Sweep |
| JE_Tool_Crowbar | Deconstruct, DeconstructForBlueprint, Uninstall, RemoveFloor, RemoveFoundation | 撬棍 | Pry |
| JE_Tool_Chisel | SmoothWall, SmoothFloor | 凿 | Stab |
| JE_Tool_Broom | Clean, ClearSnow | 扫帚 | Sweep |
| JE_Tool_Trowel | Sow, PlantSeed, Replant | 泥铲 | Dig |
| JE_Tool_SmithHammer | DoBill（DankPyon_Anvil, DankPyon_Workbench） | 锤 | - |
| JE_Tool_BenchChisel | DoBill（TableSculpting, TableStonecutter） | 凿 | Stab |
| JE_Tool_Spoon | DoBill（ElectricStove, FueledStove, Campfire） | 勺 | Stir |
| JE_Tool_Needle | DoBill（HandTailoringBench, ElectricTailoringBench） | 针 | Stab |
| JE_Tool_SmeltRod | DoBill（ElectricSmelter） | 熔炼棒 | Stir |
| JE_Tool_Cleaver | DoBill（TableButcher, ButcherSpot） | 菜刀 | Chop |
| JE_Tool_MetalChisel | DoBill（TableMachining, FabricationBench） | 凿 | Stab |
| JE_Tool_DrugSpoon | DoBill（DrugLab） | 试管 | Hold |
| JE_Tool_BrewSpoon | DoBill（Brewery） | 勺 | Stir |
| JE_Tool_Knife | Slaughter | 刀 | Stab |
| JE_Tool_Shears | Shear | 剪刀 | Chop |
| JE_Tool_FeedingBowl | Tame | 喂食碗 | Hold |
| JE_Tool_FishingRod | Fish, VCEF_FishJob | 鱼竿 | Hold |
| JE_Tool_Paintbrush | PaintBuilding, PaintFloor, RemovePaintBuilding, RemovePaintFloor | 画笔 | Sweep |
| JE_Tool_FireExtinguisher | BeatFire, ExtinguishFiresNearby | 灭火器 | Spray |
| JE_Tool_MedicineKit | TendPatient（MedicineHerbal, MedicineUltratech） | 医疗箱 | Hold |
| JE_Tool_HerbalMedicine | TendPatient | 草药 | Hold |
| JE_Tool_GlitterMedicineKit | TendPatient | 高级医疗箱 | Hold |
| JE_Tool_Notepad | Research | 记事本 | Scratch |
| JE_Tool_Auger | OperateDeepDrill | 钻头 | Stab |
| JE_Tool_Torch | Ignite | 火炬 | Stir |
| JE_Tool_Shovel | FillIn | 铲 | Dig |
| JE_Tool_HackDevice | Hack, StudyInteract, StudyItem, AnalyzeItem, InvestigateMonolith | 黑客设备 | Stir |
| JE_Tool_Wrench | Maintain | 扳手 | Crank |
| JE_Tool_Screwdriver | RepairMech | 螺丝刀 | Crank |
| JE_Tool_GeneSyringe | CreateXenogerm | 基因注射器 | Stab |
| JE_Tool_Bonesaw | ExtractSkull | 骨锯 | Saw |
| JE_Tool_SurgicalSaw | DoBill（RemoveBodyPart, Amputate, Dismember） | 手术锯 | Saw |
| JE_Tool_Scalpel | DoBill | 手术刀 | Stab |
| JE_Tool_Shell | RearmTurret | 炮弹 | Load |
| JE_Tool_Instrument | Play_MusicalInstrument | 乐器 | Hold |
| JE_Tool_Crayon | Floordrawing | 蜡笔 | Stir |
| JE_Tool_TeachingBook | Lessongiving | 教学书 | Read |
| JE_Tool_Spyglass | UseTelescope, Skygaze | 望远镜 | Hold |
| JE_Tool_PrayerBeads | Pray, Meditate | 念珠 | Hold |
| JE_Tool_BioferriteChisel | DoBill（BioferriteShaper） | 凿 | Stab |
| JE_Tool_SerumSpoon | DoBill（SerumCentrifuge） | 勺 | Stir |
| JE_Tool_BabyBottle | BottleFeedBaby | 奶瓶 | Hold |
| JE_Tool_ToyRattle | BabyPlay | 摇铃 | Hold |
| JE_Tool_ToyPlush | BabyPlay | 玩偶 | Hold |

### 5.2 Patches 内容（修改原版 EffecterDef）
- Cleaning.xml, Construction.xml, Crafting.xml, Mining.xml, PlantWork.xml
- Compat_AppliancesExpanded.xml, Compat_CombatExtended.xml, Compat_MedievalOverhaul.xml, Compat_StonebornCuisine.xml, Compat_VFEProduction.xml
- 修改的 EffecterDef：Mine, Drill, CutStone, Smith, Smelt, Cook, Tailor, Sculpt, Research, Clean, ClearSnow, ConstructMetal, ConstructWood, ConstructDirt, RoofWork, Repair, Harvest_Tree, Harvest_Plant, Sow

### 5.3 设置项（Settings）
- AnimatedTools, OverrideToolMods, handsOnTools, drawArms, completionEffects, intensity, debrisScale, gateModernTools, seasonalParticles, materialChips, groundLitter, holsterTools, toolFoley, toolsMatchPawnDepth, zoomLod, maxAnimatedPawns
- 每个 JobToolDef 有独立开关（toolEnabled / toolEffectsEnabled）
- 兼容：SmyhCompat（Show Me Your Hands）、SmyhArmHook、MeleeAnimationCompat、YayoCompat

### 5.4 工具贴图
- `Textures\Things\JobEffects\Tools\`（大量工具 sprite）
- `Textures\Things\JobEffects\Effects\Foam.png`
- `Textures\Things\Mote\JobEffects\`（粒子贴图）

---

## 六、HSK 工具物品调查结果

**来源：** `Core_SK\Defs\ThingDefs_Items\Items_Tools.xml`（8 个工具，继承 BaseTool）

| defName | label | 材料体系 | texPath | 用途 |
|---------|-------|----------|---------|------|
| TFJ_Tool_Woodcutting_Handaxe | Handaxe | Woody/Stony/RuggedMetallic | Things/Item/Equipment/WeaponMelee/Handaxe | 伐木 +30%、剪枝 +30% |
| TFJ_Tool_Mining_Pickaxe | Pickaxe | RuggedMetallic/StrongMetallic | .../Pickaxe | 挖矿 +30% |
| TFJ_Tool_Building_Hammer | Hammer | Woody/Stony/StrongMetallic | .../Hammer | 建造 +35%、锻造 +30% |
| TFJ_Tool_Sickle | Sickle | Woody/StrongMetallic | .../Sickle | 种植 +30% |
| TFJ_Tool_Broom | Broom | Woody/RuggedMetallic | .../Broom | 打扫 +100% |
| TFJ_Tool_Hoe | Hoe | RuggedMetallic | .../Hoe | 种植 +30% |
| TFJ_Tool_Multitool | Multitool | StrongMetallic | .../Multitool | 12 项工作加成（缝纫/屠宰/熔炼/烹饪等） |
| TFJ_Tool_Paxe | Paxe | Woody/Stony/RuggedMetallic | .../Paxe | 三合一（砍树+挖矿+锤） |

**STL（SurvivalToolsLite）转换：** 7 个工具被转换为 `SurvivalTool` 类型，添加 `SurvivalToolProperties.baseWorkStatFactors` 和分配标签（PlantWorker/Miner/Constructor/Crafter/Cleaning）

**HSK修复整合 的工具改动：**
- `07_多功能工具科技后移.xml`：Multitool 科技从 Electronics_D1 后移到 Components_D3
- `09_多功能工具数值强化.xml`：Multitool 追加 12 项工作加成（Tailoring 1.3, ButcheryFlesh 1.2, Smelting 1.2, Cook 1.15 等）
- `18_熔炼自动换工具.xml`：熔炼工作自动换工具（requiredStats=SmeltingSpeed）
- `Source\SurvivalToolStatCache.cs`：工具性能缓存（TTL 60 tick）
- `Source\SurvivalToolTransformCache.cs`：乘数缓存（TTL 120 tick）
- `Source\MeleeWeaponSwapFix.cs`：征召时工具切换为近战武器

**HSK 无 JobToolDef / 无工作动画系统**（不会与 JobEffects 冲突）

**工具分类层级：** SurvivalTools → SurvivalToolsNeolithic（7 工具）/ SurvivalToolsIndustrial / SurvivalToolsSpacer（Multitool）

---

## 七、鼠族工具物品调查结果

**来源：** RatkinRaceHSK + 鼠族HSK拓展

### 7.1 核心鼠族工具（基础武器改造）
| defName | 中文 | 用途 | texPath |
|---------|------|------|---------|
| RK_Cleaver | 剁肉刀 | 屠宰、烹饪 | Weapon/RK_Cleaver |
| RK_Pickaxe | 矿镐 | 采矿、钻井 | Weapon/RK_Texture_Pickaxe |
| RK_Axe | 斧头 | 伐木、收获 | Weapon/RK_Axe |
| RK_Hockey | 锄头 | 种植 | Weapon/RK_Hockey |
| RK_Weapon_Maul | 锤子 | 建造、锻造、冶炼、切石 | Weapon/RK_Texture_Maul |
| RK_Fork | 草叉 | 农收 | Weapon/RK_Fork |
| RK_Dagger | 匕首 | 屠宰、烹饪、医疗 | - |
| RK_Mace | 钉头锤 | 建造、锻造、冶炼 | - |

### 7.2 鼠族HSK拓展新增工具
| defName | 中文 | 用途 | texPath | techLevel |
|---------|------|------|---------|-----------|
| RK_Scissors | 鼠族剪刀 | 缝纫、屠宰、收获、动物采集、手术 | Weapon/RK_Scissors | Medieval |
| RK_ImpactDrill | 鼠族冲击钻 | 采矿、钻井 | Weapon/RK_ImpactDrill | Industrial |
| RK_HydraulicPliers | 鼠族液压钳 | 锻造、建造、电子/零部件制造、冶炼 | Weapon/RK_HydraulicPliers | Industrial |
| RK_WeldingTorch | 鼠族电焊枪 | 电子/零部件制造、锻造、冶炼、切石 | Weapon/RK_WeldingTorch | Industrial |

### 7.3 关键补丁（鼠族HSK拓展 Patches/）
- `27_生存工具适配.xml`：鼠族工具加 STL 属性（thingClass=SurvivalTool）
- `28_大锤工具化.xml`：Sledgehammer 移除 HeavyMelee/BluntMelee 分类
- `29_鼠族工具分类.xml`：7 件工具加入 SurvivalToolsMedieval
- `35_剪刀与钉头锤工具.xml`：剪刀配方 + 钉头锤工具化
- `36_冲击钻.xml`：冲击钻配方（RK_ElectricSmithy, Metals_C3）
- `37_液压钳.xml`：液压钳配方
- `39_新工具CE适配.xml`：4 件新工具 CE 适配
- `45_电焊枪.xml`：电焊枪配方
- `47_工具属性补强.xml`：STL 原生工具补强
- `48_工具属性StatPart挂载.xml`：SmeltingSpeed 等 StatPart 挂载
- `56_生存工具全面补强.xml`：全面属性补强
- `58_液压钳冶炼适配.xml`：液压钳 SmeltingSpeed 1.2
- `59_工具CE单手武器.xml`：CE_OneHandedWeapon
- `61_工具单手武器.xml`：握持改 OneHand

**鼠族无 JobToolDef / 无工作动画系统**

---

## 八、重叠对比分析（JobEffects vs HSK vs 鼠族）

### 8.1 重叠（JobEffects 有动画 + HSK/鼠族有对应工具物品）
| JobEffects 动画 | HSK 对应 | 鼠族对应 | 处理 |
|-----------------|----------|----------|------|
| JE_Tool_Pickaxe（镐） | TFJ_Tool_Mining_Pickaxe | RK_Pickaxe | 改贴图+加动画 |
| JE_Tool_Axe（斧） | TFJ_Tool_Woodcutting_Handaxe | RK_Axe | 改贴图+加动画 |
| JE_Tool_Hammer（锤） | TFJ_Tool_Building_Hammer | RK_Weapon_Maul | 改贴图+加动画 |
| JE_Tool_Sickle（镰刀） | TFJ_Tool_Sickle | - | 改贴图+加动画 |
| JE_Tool_Broom（扫帚） | TFJ_Tool_Broom | - | 改贴图+加动画 |
| JE_Tool_Trowel（泥铲/锄） | TFJ_Tool_Hoe | RK_Hockey | 改贴图+加动画 |
| JE_Tool_Cleaver（菜刀） | - | RK_Cleaver | 改贴图+加动画 |
| JE_Tool_Welder（焊枪） | - | RK_WeldingTorch | 改贴图+加动画 |
| JE_Tool_Auger（钻头） | - | RK_ImpactDrill | 改贴图+加动画 |
| JE_Tool_Knife（刀） | - | RK_Dagger | 改贴图+加动画 |
| JE_Tool_Shears（剪刀） | - | RK_Scissors | 改贴图+加动画（注意：Shears 是剪毛，RK_Scissors 是缝纫） |

### 8.2 JobEffects 有但 HSK/鼠族没有（加工具 + 加动画）
- 撬棍（Crowbar）、凿子（Chisel）、勺子（Spoon）、针（Needle）、熔炼棒（SmeltRod）、试管（DrugSpoon）、鱼竿（FishingRod）、画笔（Paintbrush）、灭火器（FireExtinguisher）、医疗箱（MedicineKit）、记事本（Notepad）、火炬（Torch）、黑客设备（HackDevice）、扳手（Wrench）、螺丝刀（Screwdriver）、基因注射器（GeneSyringe）、骨锯（Bonesaw）、手术锯（SurgicalSaw）、手术刀（Scalpel）、乐器（Instrument）、望远镜（Spyglass）、念珠（PrayerBeads）、奶瓶（BabyBottle）、玩具（ToyRattle/ToyPlush）等

### 8.3 HSK/鼠族有但 JobEffects 不覆盖（看能否写动画）
- TFJ_Tool_Multitool（多功能工具）
- TFJ_Tool_Paxe（三合一工具）
- RK_HydraulicPliers（液压钳）
- RK_Mace（钉头锤）
- RK_Fork（草叉）
- MeleeWeapon_Sledgehammer（大锤）
- MeleeWeapon_Monkeywrench（扳手）

### 8.4 技术要点
- **JobEffects 动画基于 jobDefs 触发，不依赖工具物品** → "给工具物品加动画"实际已自动生效（只要工作类型匹配）
- **整合方向**：让 HSK/鼠族工具物品的贴图与 JobEffects 动画贴图视觉统一；或新增 JobToolDef 覆盖鼠族特有工具的工作
- **HSK/鼠族均无工作动画系统**，不会与 JobEffects 冲突

---

## 九、待办事项与方案方向

### 9.1 Tavern 家具 mod（简单做）
1. 创建本地 mod 目录（如 `酒馆家具TavernHSK`），复制原版文件
2. **材料适配**：WoodLog → WoodPlank（HSK 木板）、TavPadding → HSK 对应材料（Fabric）、TavTallow → HSK 燃料、Metallic → HSK 金属
3. **属性等价**：对齐 HSK 同水平同材料家具/建筑（见第四节对比）：
   - 椅子/凳子 → 对齐 HSK DiningChair/Armchair/Stool 水平
   - 桌子 → 对齐 HSK Table1x1c/1x2c/2x2c 水平
   - 床 → 对齐 HSK Bed/DoubleBed 水平
   - 床头柜/梳妆台/衣柜 → 对齐 HSK EndTableSimple/Dresser/ClutterLocker 水平
   - 灯 → 对齐 HSK Candle/GlowstoneLamp 水平
4. About.xml（保留原 packageId，loadAfter HSK）
5. ModsConfig.xml 激活
6. 中文翻译（简/繁中）
7. NativeAddon 蓝色标记
8. 删除工坊原版

### 9.2 JobEffects 动画工具 mod（先做方案）
1. 创建本地 mod 目录，复制原版文件（含 Source 源码可编译）
2. **重叠工具**：改贴图 + 加动画（统一 HSK/鼠族工具物品贴图与动画）
3. **JobEffects 有但 HSK 没有**：新增工具物品 + 动画
4. **HSK 有但 JobEffects 不覆盖**：尝试写动画（新增 JobToolDef 或补丁），写不出来就不管
5. About.xml、ModsConfig.xml、翻译、NativeAddon 标记、删除工坊原版

### 9.3 待确认决策点
- [ ] 3 个 mod（Simple Doors + Tavern + JobEffects）是否合并成一个 mod？
- [ ] Tavern 的 TavPadding/TavTallow 是否保留还是替换为 HSK 材料？
- [ ] JobEffects 整合的具体方式（贴图统一 vs 新增动画 vs 新增工具物品）
- [ ] JobEffects 是否保留其 C# 源码编译（还是用现成 DLL）

---

## 十、关键路径速查

| 路径 | 说明 |
|------|------|
| `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\` | 本地 mod 目录 |
| `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\3775694305` | Tavern 工坊原版 |
| `C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\3738350131` | JobEffects 工坊原版 |
| `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\简单门SimpleDoorsHSK` | Simple Doors 本地 mod（已完成） |
| `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Core_SK\Defs\ThingDefs_Items\Items_Tools.xml` | HSK 工具定义 |
| `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\Core_SK\Defs\ThingDefs_Buildings\Buildings_Furniture.xml` | HSK 家具定义 |
| `C:\Users\admin\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml` | ModsConfig 配置 |
| `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\HSK Autosort and Mod Assistant\Defs\ModListerSettingsDef\ModListerSettingsDefs.xml` | NativeAddon 蓝色标记 |


---

## 附:边境拓展HSK(3780770870)本地化记录(2026-08-27)

- **源**: Steam 3780770870「Borders of the Rim」 by **NehsModsForDev**,1.6,DLL mod(~582KB,`BordersOfTheRim.dll`)。派系世界地图化玩法: 地理疆域/首都-大城-城镇-哨站层级/可见边界/领地战争联盟附庸内战/世界路线可见兵团与商队/玩家六类战争任务 + 自主殖民地体系。
- **处理**: **整 mod 直搬**,不改玩法/def/DLL。用户明确要求 **保留原 packageId `NehsModsForDev.bordersoftherim` + 作者**,便于日后与 `content/294100/3780770870` 逐文件 diff 修 bug, 也让启用后取代工坊条目、存档与加载顺序不变。
- **HSK 三步**(见 `docs/维护详录_鼠族HSK拓展.md` §1.3 与 §2.2):
  - 根目录 `touch HSK`(约定,非生效依据)。
  - `Patches/00_ModAssistant标记.xml`: 复用 Reel/HSK修复整合 模板, MayRequire=`DimonSever000.ModIndicator.Specific`,xpath `.../modIndicators/NativeAddon`,注入 `<li>` = `<id>NehsModsForDev.bordersoftherim</id>` + **`<HSKMod>true</HSKMod>`** + **`<loadAfter>`**(skyarkhangel.hsk / Solaris.RatkinRaceMod / skyarkhangel.rimthemeslite / sr.modrimworld.factionalwarcontinued),格式对齐 def 内 EndMod 条目。
  - `About.xml`: 保留原 packageId/author/name/description, 只 modDependencies 追加 Core SK, loadAfter 追加 `skyarkhangel.HSK` + `DimonSever000.ModIndicator.Specific`。
- **双目录**: 工作区 `边境拓展HSK` ↔ 部署 `Mods\边境拓展HSK`,20 文件 SHA256 一致,无嵌套目录残留。
- **已启用**(2026-08-27): ModsConfig `<activeMods>` 在 `mlie.wikirim`(EndMod 组)之前插入 `<li>NehsModsForDev.bordersoftherim</li>`(pos 221/228,满足 loadAfter ModIndicator/Core SK),ET.parse 通过;改前备份 `_tmp/ModsConfig_backup_borders_20260827.xml`。

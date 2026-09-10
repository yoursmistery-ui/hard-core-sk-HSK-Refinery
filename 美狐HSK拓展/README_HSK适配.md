# 美狐HSK拓展 — 适配说明

> 整合对象: `Miho, the celestial fox`(美狐, workshop 2816826107)+ 官方 CE patch 合并版(附件 MihoCF_Main_HSK_Compatible-master)
> 适配环境: RimWorld 1.6 + Core_SK(HSK)+ HumanoidAlienRaces + CombatExtended.HSK + SurvivalToolsLite
> 最后更新: 2026-08-26

## 1. 这个 mod 是什么

将美狐种族 mod 整体打包为单个本地 mod(工作区 `C:\Personal\Project\ratkin-patch\美狐HSK拓展`,部署到 `...\RimWorld\Mods\美狐HSK拓展`):

- 保留 packageId `miho.fortifiedoutremer`,**兼容脸部动画适配(2924786335)与贴图重置(3713606176)的依赖**
- 内置完整 HSK 适配层(HSK/ + HSK_1.6/): CE 武器/弹药/护甲、HSK 材料、研究台门槛、二级菜单
- 内置中文汉化(HSK/Languages/ChineseSimplified (简体中文)/)
- 1.6 启动会按 LoadFolders.xml 加载: 1.6 → Cont → Seedsplease(可选)→ Odyssey(可选)→ HSK → HSK_1.6

## 2. 本适配新增内容(相对附件仓库)

附件仓库本身已做大量 HSK 适配(16 把远程枪 CE 弹药、9 把近战 ToolCE、机械体 CE、材料 CE 化、_Miho 弹种注入)。本次补全:

### 2.1 衣物适配(Miho_Apparel_Gap.xml + Miho_Apparel_Materials.xml)
- 附件已覆盖 34 件衣物;本次补 **15 件缺口衣物的 CE 属性**(护甲 mm 化 + Bulk/WornBulk,增量补丁风格与附件一致):
  - Hat_Sorceress(4/6mm)、OnSkin_Celestial(6/10mm)、OnSkin_Shirt、Special_Maid
  - Under_* 全系列(EltexOne/Talisman/Industrial×3/SeasonalSanta/Tribal×3 等,0.5/0.5mm 打底)
- **衣物材料 HSK 化**(Miho_Apparel_Materials.xml,2026-08-26): Gold→GoldBar 锭化(Hat_EltexCaptain)、Cloth→HempCloth(3 件 Militia 甲)、DevilstrandCloth→Kevlar(SeasonalSanta)
- OrnatedNomadic/RoundGlasses/SunGlasses 由附件 Apparel_New16.xml 覆盖,不重复
- Steel/Plasteel/WoodLog/Hay/Thrumbo 为 HSK 原样保留材料,不改

### 2.2 武器适配(Weapons_Melee_HSK.xml + Miho_Weapons_Materials.xml)
- 5 把近战武器补 CE 单手标记 `CE_OneHandedWeapon`(可配盾): PowerClaw / Sappertool / Tachi / EltexA / EltexB
- PowerHammer 做**生存工具适配**(MayRequire SurvivalToolsLite): ToolEffectivenessFactor + MiningSpeed 1.5 / SmoothingSpeed 1.3,分配 Miner/Constructor
- **武器材料 HSK 化**(Miho_Weapons_Materials.xml,2026-08-26): PlasmaTachi/EltexAxe 的 Gold→GoldBar
- **远程武器体系(附件已做)**: 11 把枪的 HSK 配方在 HSK/Defs/RecipeDefs_Weapons/Recipes_Weapons.xml(SMG_Component/Weapon_Parts/ComponentIndustrial + SLDBar/USLDBar 金属条,挂 WeaponCraftingWorkTable 武器制作台),Recipes_HSKWeapons.xml 移除 vanilla recipeMaker 避免双配方
- 弹药核验: 19 个 ammoSet 中 14 个为 HSK 既有弹种,5 个为附件自带 Miho 专属弹种(57x280mmR / MechPsyRifle / MechPsyShot / MihoMissile / MihoPlasmaThermo,定义在 HSK/Defs/Ammo/),全部有效

### 2.3 建筑菜单适配(Miho_Buildings_Menu.xml)
- 3 座炮塔基类(Miho_DeployableHMG/Missile/Autocannon): designationCategory MihoBuilding → **Security**,挂 HSK 二级菜单(重机枪塔→SubCategory_Machineguns、导弹塔→SubCategory_RocketLaunchers、步兵炮→SubCategory_Cannons)
- 4 个工作台(TableMachining/ShaftFurnace/CelestialForge/MechFactory)+ CraftingSpot: → **Production**,挂 SubCategory_Machining/Furnaces/Hightech
- 榻榻米 Miho_Tatami: → **Accessories**,挂 SubCategory_ModernRugs
- 全部用 PatchOperationConditional 门控(子分类存在才挂,Core_SK 必在,安全)

### 2.3b 建筑材料适配(Miho_Buildings_Materials.xml,2026-08-26)
- 综合工作台/工作AI: 工业/太空档补 HSK 材料(Compaste 粘合剂 + SyntheticFibers 合成纤维 / Electronics 电子元件)
- 高炉: **材质化建造**(StrongMetallic 金属材质 ×120,替代固定 Steel 400),与炮塔风格一致
- 天界熔炉(极致档): 补 ComponentUltra 终极零件 + Microchips 微芯片
- 机械工厂(太空档): 补 Carbon 碳纤维 + Electronics 电子元件
- 炮塔 3 座附件已做(StrongMetallic + Weapon_Parts/Launcher_Component/Plastic),未动
- 全部 HSK 材料已核验存在(Core_SK 定义),保留美狐自产陶瓷/丝绸(生产链自洽)

### 2.3c 头部渲染修复(2026-09-02)
- 移除 `1.6/Defs/Race_Miho.xml` 的 `headOffsetDirectional`(north -0.09 / south -0.05 / east,west -0.08):HAR 的 alignWithHead 头部挂件(头发/耳/脑后发)定位用硬编码 Head 预设、**不跟随**该偏移(反编译 AlienRace.dll 证实: 偏移只经 BaseHeadOffsetAt postfix 加在头部上),导致头部下坠前移而头发留在原位,侧向出现"秃头+耳朵错位"。移除后头部回标准位,与头发/耳朵/帽子全部对齐(静态叠图核验东向 0 间隙)。

### 2.4 研究台门槛(Miho_ResearchBench.xml)
- Miho_Celestial(极致档)研究台门槛 HiTechResearchBench → **LabStation**(实验室工作站),补 SK.AdvancedResearchExtension 扩展(仅 LabStation)
- 其余 12 个 HiTech 节点门槛附件已写好,无需改动
- **2026-08-27 techLevel 越级修复**: `Miho_GuardDrone` 前置 `GunTurrets` — 原版是 Industrial,但 Core_SK 把它抬到 **Spacer**(HiTechResearchBench + MultiAnalyzer,Weapon_SK 18,36),而 GuardDrone 仍是 Industrial → 启动报 `Miho_GuardDrone has a lower techlevel than (one of) it's prerequisites`;`Miho_LightWarDrone` 前置 GuardDrone,被**传递闭包**连带第二条告警。修法: 该前置改挂 GunTurrets 自己的工业档前置 **Turrets_31**(炮塔 I,Industrial),Miho 机械炮塔线保持工业档;LightWarDrone 另挂 Mortars(其前置即 Turrets_31),炮塔科技链未断。改后全树直连/传递越级均为 0。

### 2.5 汉化(2026-08-27 全量重做)
- `HSK_1.6/Languages/ChineseSimplified` → 改名 `ChineseSimplified (简体中文)`(铁律: 必须带后缀否则不加载)
- **中文层全量重写**: `HSK/Languages/ChineseSimplified (简体中文)/` 共 **188 个文件 / 2636 条**,覆盖 1.6 加载链
  (`1.6 → Cont → Seedsplease → SeedspleaseTranslation → Odyssey → HSK → HSK_1.6`)全部可见文本。
  旧版词表替换产物(1117 条,含 `nail→n人工智能l` 之类坏译名、480 条仍是英文)已整棵替换,备份在 `_tmp/旧中文备份_*/`。
- **工作管线**(全部留在 `_tmp/`,可复跑):
  `build_en_table.py`(按加载优先级合成"生效英文表",含 Defs/Patches 内联英文与 RulePack 词库)
  → `gen_batches.py`(按领域切 17 批,唯一英文串 2045 条)→ 并行代理翻译 `_tmp/batches/out_bNN.json`
  → `postfix.py`(术语/冒号/弹名代号统一)→ `apply_zh.py`(镜像英文层文件布局写出)
  → `validate_zh.py` + `gap_check.py` + `check_terms.py`(终检)→ `sync_deploy.py`(双目录同步)。
- **终检结果**: 缺中文 0 / 占位符与 `\n` 不一致 0 / 残留整句英文 0 / 同键冲突 0 /
  Defs+Patches 漏译字段 0 / 中文层孤儿键 0。21 条"无中文"均为命名词库里纯变量拼接条目(按规则原样保留)。
- **术语口径**: 与 `1.6HSK核心汉化`、CE 官方中文对齐 —— Eltex=灵素, mechanoid=机械体, mechanite=机械素,
  persona core=人格核心, Bulk/WornBulk=体积/穿着体积, GoJuice=冲刺剂, Arisaka=有坂, Nambu=南部,
  研究台六档用 HSK 官方名(实验室研究台/研究终端/多分析仪等);弹药代号(TuF/AP-IM/9x19mm Para)保留原文。
- **目录名规范化**: 英文层的复数类型目录 `FactionDefs/GeneDefs/MemeDefs` 在中文层统一并为
  `FactionDef/GeneDef/MemeDef`(防按目录名解析类型时静默丢翻译)。
- 原 mod 的英文汉化(`Cont/Languages/English`)与派系命名词库(`HSK/Languages/English/RulePackDef`)已并入,
  命名词库已中文化(派系/意识形态/神名/人名音译,如 Ahri→阿狸);`HSK/Languages/Russian` 保留原作者内容未动。

## 3. 家具/建筑清单

### 3.1 可建造建筑(7 项)

| defName | 中文名 | 一级分类(适配后) | HSK 二级菜单 | 材料(HSK 适配后) |
|---|---|---|---|---|
| Miho_Tatami | 美狐榻榻米 | Accessories | 现代地毯 | Hay 10(原版) |
| Miho_TableMachining | 美狐综合工作台 | Production | 机械加工 | Miho_Ceramics 100 + CompInd 6 + **Compaste 8 + SyntheticFibers 10**(工业档) |
| Miho_CraftingSpot | 美狐简易制作点 | Production | (挂 Machining) | 无成本 |
| Miho_WorkAI | 美狐工作辅助AI | Misc(设施) | (娱乐设施) | Miho_Ceramics 60 + CompInd 10 + CompSpacer 4 + **Electronics 10**(太空档) |
| Miho_ShaftFurnace | 美狐高炉 | Production | 熔炉 | **StrongMetallic 材质 120 + CompInd 4 + Compaste 6**(材质化建造) |
| Miho_CelestialForge | 美狐天界熔炉 | Production | 高科技 | Miho 军规陶瓷 1500 + CompSpacer 12 + **ComponentUltra 4 + Microchips 6**(极致档) |
| Miho_MechFactory | 美狐机械工厂 | Production | 高科技 | Miho_Ceramics 300 + Steel 75 + CompInd 8 + CompSpacer 1 + **Carbon 20 + Electronics 15**(太空档) |

(注: TableMachining_Weapon/Apparel/Techweave 三个专用工作台在原 mod 中被 XML 注释,不加载。)

### 3.2 炮塔/武器建筑(3 座塔 + 弹药)

| defName | 中文名 | 一级分类(适配后) | HSK 二级菜单 | 弹药 |
|---|---|---|---|---|
| Miho_Turret_Machinegun | 美狐重机枪塔 | Security | 机枪 | AmmoSet_303British(HSK) |
| Miho_Turret_Missile | 美狐中型反机甲导弹塔 | Security | 火箭发射器 | AmmoSet_MihoMissile(自带) |
| Miho_Turret_InfantryGun | 美狐步兵炮 | Security | 加农炮 | AmmoSet_57x280mmR(自带) |
| Miho_Missile / Miho_MissilePlasma | 制导导弹(弹药) | - | - | HSK 材料成本 |
| Miho_Weapon_RifleRecoillessSharp | 美狐无后坐力炮 | - | - | Miho_Missile 3 |

### 3.3 地板(3 项 TerrainDef,含研究门槛)

| defName | 中文名 | 研究门槛 |
|---|---|---|
| Miho_Ceramics_Floor | 美狐陶瓷地板 | 无 |
| Miho_CeramicsBlue_Floor | 美狐蓝色陶瓷地板 | 无 |
| Miho_CeramicsGood_Floor | 高级美狐陶瓷地板 | Miho_HeavyFactory |

### 3.4 植物/作物(4 项)

| defName | 中文名 | 产物 |
|---|---|---|
| Plant_PosFlower | 太阳花 | RawPosFlower(花瓣)→ Solar Juice/Tea/Pipe |
| Plant_PosMoss_Hydro | 黎明苔藓 | RawPosMoss |
| Plant_Ebony_Silk | 乌木桑树 | Miho_Ebony_Mulberry(桑葚)/Miho_Ebony_SilkCloth(乌木丝绸) |
| Miho_Ebony_Bonsai | 乌木盆景(家具) | 装饰 |

### 3.5 材料(9 项,附件已做 CE 化)

Miho_Ceramics(陶瓷)/ Miho_MilitaryGradeBalisticCeramics(军规陶瓷)/ Miho_CelestialScale(天界鳞)/ Miho_ExoticMatter(异质物质)/ Miho_Ebony_SilkCloth(乌木丝绸)/ Miho_HeavyMechCore / Miho_HeavyRocketCore / Miho_InariZushi(稻荷寿司食物)/ Miho_Schematic(科技文档,阅读+研究)

## 4. 研究线(23 节点,独立 tab)

- 前置全部核验存在于 HSK(Electricity/Machining/Turrets_31/BlowbackOperation/ChargedShot/MultiAnalyzer/BasicMechtech 等 14 个)
- 13 个节点有研究台门槛(HiTechResearchBench / MultiAnalyzer),Miho_Celestial 已升级 LabStation
- techLevel 档位已对齐 Core_SK(唯一越级边 GuardDrone→GunTurrets 已改挂 Turrets_31,详见 §2.4),ResearchTreeSK 无 "lower techlevel" 告警
- 衣物/武器配方挂 Miho 自己的研究线(非 HSK 鼠族线),保持美狐独立科技树

## 5. 重要提示(packageId 冲突)

本适配版与工坊原 mod(2816826107)**同 packageId `miho.fortifiedoutremer`**。若同时启用:
- 游戏会报 packageId 冲突/重复 mod 警告
- 建议: **取消订阅工坊原 mod**(保留脸部动画 2924786335 + 贴图重置 3713606176,它们依赖的是 packageId 不是文件夹),只用本本地适配版

## 6. 验证状态

- 3 个新补丁(Miho_Apparel_Gap / Weapons_Melee_HSK / Miho_Buildings_Menu / Miho_ResearchBench)已用 lxml 模拟验证,全部 xpath 命中
- 与 Core_SK 子分类的挂载门控在完整文档(含 Core_SK)下全部命中
- 中文汉化 XML 全部合法
- 双目录同步: 工作区 ↔ `...\RimWorld\Mods\美狐HSK拓展`(改动必须双份同步)
